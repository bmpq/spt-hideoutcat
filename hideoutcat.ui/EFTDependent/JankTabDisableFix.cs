using UnityEngine;

namespace tarkin.hideoutcat.ui.EFTDependent
{
    public class JankTabDisableFix : MonoBehaviour
    {
#if EFT_RUNTIME
        private Tab tab;
        void Awake()
        {
            tab = GetComponent<Tab>();
        }

        void OnDisable()
        {
            tab.Deselect();
        }
#endif
    }
}
