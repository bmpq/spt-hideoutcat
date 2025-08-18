using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace tarkin.hideoutcat.ui.Generic
{
    /// <summary>
    /// Replicates EFT's UI.Hideout.HideoutCustomizationCell fields, but with completely new logic
    /// the original wasn't usable
    /// </summary>
    internal class MyHideoutCustomizationCell : MyTab
    {
        [SerializeField] private CanvasGroup _canvasGroup;

        [Space(10f)]
        [SerializeField] private Image _imageMainIdle;
        [SerializeField] private Image _imageMainSelected;

        [Space(10f)]
        [SerializeField] private GameObject _layerIdle;
        [SerializeField] private GameObject _overlayHover;
        [SerializeField] private GameObject _layerSelected;

        [Space(10f)]
        [SerializeField] private Image _lockedImage;
        [SerializeField] private Image _lockedBorderImage;

        [Space(10f)]
        [SerializeField] private GameObject _cellNumberObject;
        [SerializeField] private Image _cellNumberImage;
        [SerializeField] private Sprite _cellNumberIdleSprite;
        [SerializeField] private Sprite _cellNumberSelectedSprite;
        [SerializeField] private TMP_Text _cellNumberText;

        public void Show(Sprite sprite, int? cellNumber)
        {
            if (sprite != null)
            {
                _imageMainIdle.sprite = sprite;
                _imageMainSelected.sprite = sprite;
            }

            if (_cellNumberObject != null)
            {
                _cellNumberText.text = cellNumber.HasValue ? cellNumber.Value.ToString() : "";
                _cellNumberObject.SetActive(cellNumber.HasValue);
            }

            Deselect();
        }

        public override void SetInteractable(bool on)
        {
            base.SetInteractable(on);

            _lockedImage.gameObject.SetActive(!on);
            _lockedBorderImage.gameObject.SetActive(!on);
        }

        private void Shade(bool shaded)
        {
            _canvasGroup.alpha = shaded ? 0.7f : 1.0f;
        }

        public override void Select()
        {
            base.Select();

            _layerIdle.gameObject.SetActive(false);
            _layerSelected.gameObject.SetActive(true);

            _cellNumberImage.sprite = _cellNumberSelectedSprite;

            Shade(false);
        }

        public override void Deselect()
        {
            base.Deselect();

            _layerIdle.gameObject.SetActive(true);
            _layerSelected.gameObject.SetActive(false);

            _cellNumberImage.sprite = _cellNumberIdleSprite;

            Shade(true);
        }

        public override void OnPointerEnter(PointerEventData eventData)
        {
            base.OnPointerEnter(eventData);
            if (!IsInteractable) return;

            _overlayHover.SetActive(true);
        }

        public override void OnPointerExit(PointerEventData eventData)
        {
            base.OnPointerExit(eventData);

            _overlayHover.SetActive(false);

            Shade(!IsInteractable && !IsSelected);
        }
    }
}
