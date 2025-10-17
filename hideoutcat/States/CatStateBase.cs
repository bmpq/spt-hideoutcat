using tarkin.hideoutcat.Persistent;
using UnityEngine;

namespace tarkin.hideoutcat.States
{
    [RequireComponent(typeof(HideoutCat))]
    public abstract class CatStateBase : MonoBehaviour
    {
        protected HideoutCat cat;
        protected Animator animator;

        protected virtual void Awake()
        {
            cat = GetComponent<HideoutCat>();
            animator = GetComponent<Animator>();
        }

        public abstract StateTickResult Tick();

        public abstract void OnEnterState();
        public abstract void OnExitState();

        public virtual bool IsPettable()
        {
            return false;
        }

        public virtual void Pet()
        {
            animator.SetTrigger("Caress");
        }
    }
}
