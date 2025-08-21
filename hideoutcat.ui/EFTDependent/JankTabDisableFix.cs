using UnityEngine;

namespace tarkin.hideoutcat.ui.EFTDependent
{
    public class JankTabDisableFix : MonoBehaviour
    {
        private Tab tab;
        void Awake()
        {
            tab = GetComponent<Tab>();
        }

        void OnDisable()
        {
            tab.Deselect();
        }
    }
}
