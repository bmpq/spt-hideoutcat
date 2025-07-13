using EFT.Ballistics;
using UnityEngine;

namespace tarkin.hideoutcat.bepinex
{
    internal class Floor
    {
        private const int GroundLayerMask = 1 << 12; // HighPolyCollider
        private const float GroundRaycastDistance = 0.2f;

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

        public string GetCurrentGroundMaterialPrefix(Transform catTransform)
        {
            MaterialType? material = GetGroundMaterial(catTransform);

            return GetMaterialClipNamePrefix(material ?? MaterialType.Fabric);
        }

        private MaterialType? GetGroundMaterial(Transform transform)
        {
            return GetMaterialFromRaycast(transform.position, - transform.up) ?? // straight down
                   GetMaterialFromRaycast(transform.position, transform.forward + new Vector3(0, -0.1f, 0)); // when jumping up have to check forward down
        }

        private MaterialType? GetMaterialFromRaycast(Vector3 source, Vector3 direction)
        {
            if (Physics.Raycast(source, direction, out RaycastHit hitInfo, GroundRaycastDistance, GroundLayerMask, QueryTriggerInteraction.Ignore) &&
                hitInfo.collider.gameObject.TryGetComponent(out BallisticCollider ballistic))
            {
                return ballistic.TypeOfMaterial;
            }
            return null;
        }
    }
}
