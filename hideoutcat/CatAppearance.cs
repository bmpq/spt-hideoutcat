using UnityEngine;

namespace tarkin.hideoutcat
{
    public class CatAppearance : MonoBehaviour
    {
        [SerializeField] private SkinnedMeshRenderer mainRenderer;

        public void ApplyCoatTexture(Texture2D coatTex)
        {
            mainRenderer.materials[0].mainTexture = coatTex;
        }
    }
}
