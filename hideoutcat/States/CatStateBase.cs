using UnityEngine;

namespace tarkin.hideoutcat.States
{
    [RequireComponent(typeof(HideoutCat))]
    public abstract class CatStateBase : MonoBehaviour
    {
        protected Animator animator;

        protected virtual void Awake()
        {
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
