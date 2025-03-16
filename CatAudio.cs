using Comfort.Common;
using EFT.Ballistics;
using hideoutcat.Pathfinding;
using tarkin;
using UnityEngine;

namespace hideoutcat
{
    internal class CatAudio : MonoBehaviour
    {
        private const int GroundLayerMask = 1 << 12; // HighPolyCollider
        private const float GroundRaycastDistance = 0.2f;

        private CatGraphTraverser graphTraverser;

        private BetterSource audioSource;
        private AudioClip[] allClips;

        private MaterialType lastPlayedMaterialType = MaterialType.Concrete;

        float stepTimer;

        public enum MeowType
        {
            Address,
            Far,
            Exertion,
            Grumpy,
            Short
        }

        private void OnEnable()
        {
            audioSource = Singleton<BetterAudio>.Instance.GetSource(BetterAudio.AudioSourceGroupType.Character, true);
            if (audioSource == null)
                Debug.LogError("CatAudio: Could not get BetterAudio source.");
        }

        private void OnDisable()
        {
            if (audioSource != null)
            {
                audioSource.Release();
            }
        }

        public void Meow(MeowType meowType)
        {
            switch (meowType)
            {
                case MeowType.Address:
                    PlayRandomClipByPrefix(allClips, "cat_meow_look");
                    break;
                case MeowType.Far:
                    PlayRandomClipByPrefix(allClips, "cat_generic_meow");
                    break;
                case MeowType.Exertion:
                    PlayRandomClipByPrefix(allClips, "cat_meow_after_jump");
                    break;
                case MeowType.Grumpy:
                    PlayRandomClipByPrefix(allClips, "cat_meow_grumpy");
                    break;
                case MeowType.Short:
                    PlayRandomClipByPrefix(allClips, "cat_meow_ok");
                    break;
            }
        }

        public void Purr()
        {
            PlayRandomClipByPrefix(allClips, "cat_purr");
        }

        void Update()
        {
            audioSource.Position = transform.position;

            if (graphTraverser.VelocityMagnitude > 0.1f)
            {
                stepTimer += Time.deltaTime;

                float maxStepInterval = 0.5f;
                float minStepInterval = 0.1f;
                float maxVelocity = 3.6f;
                float normalizedVelocity = Mathf.Clamp01(graphTraverser.VelocityMagnitude / maxVelocity);
                float currentStepInterval = Mathf.Lerp(maxStepInterval, minStepInterval, normalizedVelocity);

                if (stepTimer >= currentStepInterval && graphTraverser.IsMovement())
                {
                    PlayStep();
                    stepTimer = 0f;
                }
            }
            else
            {
                stepTimer = 0f;
            }
        }

        private void Start()
        {
            graphTraverser = GetComponent<CatGraphTraverser>();

            graphTraverser.OnJumpAirEnd += GraphTraverser_OnJumpAirEnd;

            allClips = AssetBundleLoader.LoadAssetBundle("hideoutcat_audio").LoadAllAssets<AudioClip>();
            if (allClips == null || allClips.Length == 0)
            {
                Plugin.Log.LogError("CatAudio: No audio clips loaded from bundle!");
                enabled = false;
                return;
            }
        }

        private void GraphTraverser_OnJumpAirEnd()
        {
            PlayMaterialSound("cat_land_");
            Meow(MeowType.Exertion);
        }

        public void PlayStep()
        {
            PlayMaterialSound("cat_walk_");
        }

        private void PlayMaterialSound(string prefix)
        {
            MaterialType materialType = GetGroundMaterial();
            string clipPrefix = prefix + GetMaterialClipNamePrefix(materialType);
            PlayRandomClipByPrefix(allClips, clipPrefix);
            lastPlayedMaterialType = materialType;
        }

        private MaterialType GetGroundMaterial()
        {
            return GetMaterialFromRaycast(-transform.up) ?? // straight down
                   GetMaterialFromRaycast(transform.forward + new Vector3(0, -0.1f, 0)) ?? // when jumping up have to check forward down
                   lastPlayedMaterialType;
        }

        private MaterialType? GetMaterialFromRaycast(Vector3 direction)
        {
            if (Physics.Raycast(transform.position, direction, out RaycastHit hitInfo, GroundRaycastDistance, GroundLayerMask, QueryTriggerInteraction.Ignore) &&
                hitInfo.collider.gameObject.TryGetComponent(out BallisticCollider ballistic))
            {
                return ballistic.TypeOfMaterial;
            }
            return null;
        }

        private string GetMaterialClipNamePrefix(MaterialType materialType)
        {
            switch (materialType)
            {
                case MaterialType.Asphalt:
                case MaterialType.Concrete:
                    return "concrete";
                case MaterialType.MetalThick:
                case MaterialType.MetalThin:
                case MaterialType.MetalNoDecal:
                    return "metal";
                case MaterialType.Tile:
                    return "tile";
                case MaterialType.WoodThick:
                case MaterialType.WoodThin:
                    return "wood";
                case MaterialType.Plastic:
                    return "plastic";
                case MaterialType.GarbageMetal:
                    return "garbage";
                case MaterialType.GarbagePaper:
                    return "paper";
                case MaterialType.Cardboard:
                    return "cardboard";
                case MaterialType.Fabric:
                default:
                    return "carpet";
            }
        }

        private void PlayRandomClipByPrefix(AudioClip[] clips, string prefix)
        {
            AudioClip[] filteredClips = System.Array.FindAll(clips, clip => clip.name.StartsWith(prefix));

            if (filteredClips.Length > 0)
            {
                AudioClip clipToPlay = filteredClips[Random.Range(0, filteredClips.Length)];
                audioSource.Play(clipToPlay, null, 1f);
            }
            else
            {
                Plugin.Log.LogWarning("CatAudio: No clips found with prefix: " + prefix);
            }
        }
    }
}