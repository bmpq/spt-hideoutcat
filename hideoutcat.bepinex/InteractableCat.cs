using EFT.Interactive;
using UnityEngine;

namespace tarkin.hideoutcat.bepinex
{
    internal class InteractableCat : InteractableObject
    {
        HideoutCat cat;

        SphereCollider collider;

        void Start()
        {
            cat = GetComponentInParent<HideoutCat>();
            if (cat == null)
            {
                Destroy(this);
                return;
            }

            gameObject.layer = LayerMask.NameToLayer("Interactive");
            collider = gameObject.GetOrAddComponent<SphereCollider>();
            collider.isTrigger = true;
            collider.radius = 0.4f;
        }

        public bool IsPettable()
        {
            return cat.CurrentState.IsPettable();
        }

        public bool IsSleeping()
        {
            return false;
        }

        public void Pet()
        {
            cat.CurrentState.Pet();
        }

        public void WakeUp()
        {

        }
    }
}
