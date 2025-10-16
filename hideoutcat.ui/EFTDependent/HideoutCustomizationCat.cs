using UnityEngine;
using tarkin.hideoutcat.ui.Generic;
using System.Collections.Generic;
using TMPro;

#if EFT_RUNTIME
using EFT.UI;
#endif

namespace tarkin.hideoutcat.ui.EFTDependent
{
    public class HideoutCustomizationCat : MonoBehaviour
    {
#if !EFT_RUNTIME
        [SerializeField] private TMP_InputField inputName;
#else
        [SerializeField] private ValidationInputField inputName;
#endif

        [Space(10)]
        [SerializeField] private RectTransform containerCoatCells;
        [SerializeField] private MyHideoutCustomizationCell prefabCoatCell;

#if EFT_RUNTIME
        private MyTabGroup coatCellsGroup;
        private readonly Dictionary<MyHideoutCustomizationCell, Coat> coatCells = new Dictionary<MyHideoutCustomizationCell, Coat>();
        private bool _isUiInitialized = false;

        void Start()
        {
            inputName.OnValidatedTextChanged.Subscribe(OnValidatedTextChanged);
            CatUIDataProvider.WhenReady(InitializeUI);
        }

        private void InitializeUI()
        {
            if (_isUiInitialized) return;
            _isUiInitialized = true;

            inputName.SetTextWithoutNotify(CatUIDataProvider.GetCurrentCatName());

            coatCellsGroup = new MyTabGroup();

            Coat[] coats = CatUIDataProvider.GetCoats();
            Coat currentCoat = CatUIDataProvider.GetCurrentCoat();

            foreach (var coat in coats)
            {
                GameObject cellGo = Instantiate(prefabCoatCell.gameObject, containerCoatCells);
                MyHideoutCustomizationCell cell = cellGo.GetComponent<MyHideoutCustomizationCell>();

                cell.Show(coat.Icon, coatCells.Count + 1);
                cell.SetInteractable(true);
                coatCellsGroup.AddTab(cell);
                coatCells.Add(cell, coat);

                if (coat == currentCoat)
                {
                    coatCellsGroup.SelectTab(cell, false);
                }
            }

            coatCellsGroup.OnTabSelected += OnCoatSelect;
        }

        void OnValidatedTextChanged(string name)
        {
            CatUIDataProvider.SetCatName(name);
        }

        private void OnCoatSelect(MyTab tab)
        {
            if (tab is MyHideoutCustomizationCell cell && coatCells.TryGetValue(cell, out Coat coat))
            {
                CatUIDataProvider.ApplyCoat(coat);
            }
        }

        private void OnDisable()
        {
            // when the parent window closes, close this panel
            gameObject.SetActive(false);
        }

        void OnDestroy()
        {
            if (coatCellsGroup != null)
            {
                coatCellsGroup.OnTabSelected -= OnCoatSelect;
            }
        }
#endif
    }
}