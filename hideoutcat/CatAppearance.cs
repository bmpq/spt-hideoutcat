using tarkin.hideoutcat.Persistent;
using UnityEngine;

namespace tarkin.hideoutcat
{
    public class CatAppearance : MonoBehaviour, IPersistentDataDependent
    {
        [SerializeField] private SkinnedMeshRenderer mainRenderer;

        public void ApplyCoatTexture(Texture2D coatTex)
        {
            mainRenderer.materials[0].mainTexture = coatTex;
        }

        public void OnPersistentDataLoad(CatPersistentDataController data)
        {
            ApplyCoatTexture(data.CurrentCoat.MainTexture);
        }
    }
}
