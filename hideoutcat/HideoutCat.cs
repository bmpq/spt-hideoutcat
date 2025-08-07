using System;
using System.Collections.Generic;
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

        private CatStateBase CurrentState;

        public static event Action<List<Transform>> OnRequestPotentialTargets;
        private readonly List<Transform> potentialTargets = new List<Transform>();

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

        void DecideNextState()
        {
            potentialTargets.Clear();
            OnRequestPotentialTargets?.Invoke(potentialTargets); // passing the list to the subscribers to be filled

            if (potentialTargets.Count > 0 && CurrentState != attackHandler)
            {
                foreach (var potentialTarget in potentialTargets)
                {
                    if (potentialTarget != null && senses.HasLineOfSight(potentialTarget, out float _))
                    {
                        attackHandler.SetTarget(potentialTarget);
                        TransitionToState(attackHandler);
                        return;
                    }
                }
            }
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