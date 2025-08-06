using tarkin.hideoutcat.States;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace tarkin.hideoutcat
{
    [RequireComponent(typeof(CatLocomotion))]
    [RequireComponent(typeof(CatSenses))]

    [RequireComponent(typeof(CatIdleHandler))]
    [RequireComponent(typeof(CatGraphTraverser))]
    [RequireComponent(typeof(CatAttackHandler))]
    public class HideoutCat : MonoBehaviour
    {
        private CatLocomotion locomotion;

        private CatSenses senses;

        private CatIdleHandler idleHandler;
        private CatGraphTraverser graphTraverser;
        private CatAttackHandler attackHandler;

        private Transform potentialTarget;

        private CatStateBase CurrentState;

        void Awake()
        {
            locomotion = GetComponent<CatLocomotion>();

            senses = GetComponent<CatSenses>();

            idleHandler = GetComponent<CatIdleHandler>();
            graphTraverser = GetComponent<CatGraphTraverser>();
            attackHandler = GetComponent<CatAttackHandler>();
        }

        void Update()
        {
            UpdateSenses();

            if (CurrentState == null)
            {
                TransitionToState(idleHandler);
            }

            StateTickResult result = CurrentState.Tick();

            locomotion.SetInput(result.Input);

            if (result.IsStateDone)
            {
                DecideNextState();
            }
        }

        void UpdateSenses()
        {
        }

        void DecideNextState()
        {

        }

        void TransitionToState(CatStateBase catState)
        {
            if (catState == null || catState == CurrentState)
                return;

            CurrentState?.OnExitState();

            CurrentState = catState;

            CurrentState.OnEnterState();
        }
    }
}