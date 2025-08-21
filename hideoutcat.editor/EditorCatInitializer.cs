using UnityEngine;
using UnityEngine.Bindings;

namespace tarkin.hideoutcat.editor
{
    [DefaultExecutionOrder(-100)]
    internal class EditorCatInitializer : MonoBehaviour
    {
        [SerializeField] private Coat[] allCoats;

        void OnEnable()
        {
            HideoutCat.OnCatSpawned += HideoutCat_OnCatSpawned;
        }

        void OnDisable()
        {
            HideoutCat.OnCatSpawned -= HideoutCat_OnCatSpawned;
        }

        private void HideoutCat_OnCatSpawned(HideoutCat cat)
        {
            cat.Initialize(new Persistent.CatPersistentDataController(allCoats));
        }

        void Update()
        {

        }
    }
}
