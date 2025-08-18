using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace tarkin.hideoutcat.ui.Generic
{
    public abstract class MyTab : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
    {
        public bool IsSelected { get; private set; }
        public bool IsInteractable { get; private set; }

        protected MyTabGroup TabGroup;

        public virtual void Initialize(MyTabGroup owner)
        {
            TabGroup = owner;
        }

        public virtual void Select()
        {
            IsSelected = true;
        }

        public virtual void Deselect()
        {
            IsSelected = false;
        }

        public virtual void SetInteractable(bool on)
        {
            IsInteractable = on;
        }

        public virtual void OnPointerClick(PointerEventData eventData)
        {
            if (!IsInteractable || TabGroup == null) return;

            TabGroup.OnTabClicked(this);
        }

        public virtual void OnPointerEnter(PointerEventData eventData)
        {
        }

        public virtual void OnPointerExit(PointerEventData eventData)
        {
        }
    }
}
