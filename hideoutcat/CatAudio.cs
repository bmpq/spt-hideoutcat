using tarkin.hideoutcat.Pathfinding;
using System;
using UnityEngine;
using Random = UnityEngine.Random;

namespace tarkin.hideoutcat
{
    public class CatAudio : MonoBehaviour
    {
        public event Action<AudioClip, float> OnClipPlayRequest;

        public Func<string> GetGroundMaterialPrefixFunc;

        private CatGraphTraverser graphTraverser;

        private AudioClip[] allClips;

        private Func<string> getGroundMaterialPrefixFunc;

        float stepTimer;

        public enum MeowType
        {
            Address,
            Far,
            Exertion,
            Grumpy,
            Short
        }

        public void Init(AudioClip[] clips, Func<string> groundMaterialFunc)
        {
            allClips = clips;

            getGroundMaterialPrefixFunc = groundMaterialFunc ?? throw new ArgumentNullException(nameof(groundMaterialFunc),
                "The ground material provider function cannot be null.");
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
            if (getGroundMaterialPrefixFunc == null)
            {
                Debug.LogError("CatAudio was not properly initialized. getGroundMaterialPrefixFunc is null.");
                return;
            }

            string materialPrefix = getGroundMaterialPrefixFunc.Invoke();
            string clipPrefix = prefix + materialPrefix;

            PlayRandomClipByPrefix(allClips, clipPrefix);
        }

        private void PlayRandomClipByPrefix(AudioClip[] clips, string prefix)
        {
            if (clips == null || clips.Length == 0)
            {
                Debug.LogWarning("CatAudio: No clips loaded!");
                return;
            }

            AudioClip[] filteredClips = System.Array.FindAll(clips, clip => clip.name.StartsWith(prefix));

            if (filteredClips.Length > 0)
            {
                AudioClip clipToPlay = filteredClips[Random.Range(0, filteredClips.Length)];
                OnClipPlayRequest.Invoke(clipToPlay, 1f);
            }
            else
            {
                Debug.LogWarning("CatAudio: No clips found with prefix: " + prefix);
            }
        }
    }
}