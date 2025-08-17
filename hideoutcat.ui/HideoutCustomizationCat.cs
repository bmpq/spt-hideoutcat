using EFT.UI;
using UnityEngine;

#if RUNTIME
#endif

namespace tarkin.hideoutcat.ui
{
    public class HideoutCustomizationCat : MonoBehaviour
    {
        [SerializeField] private ValidationInputField inputName;

#if RUNTIME
        public static HideoutCustomizationCat Instance;

        void Start()
        {
            inputName.OnValidatedTextChanged.Subscribe(OnValidatedTextChanged);
        }

        void OnValidatedTextChanged(string name)
        {
            // todo
        }
#endif
    }
}
