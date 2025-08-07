using System;
using System.Collections.Generic;
using tarkin.hideoutcat.States;
using UnityEngine;
using System.Linq;

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

        private readonly Dictionary<Transform, Vector3> _boredTargets = new Dictionary<Transform, Vector3>();
        [Tooltip("how far a 'boring' target must move for the cat to regain interest")]
        [SerializeField] private float _interestRegainMovementThreshold = 0.1f;

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

            foreach (var key in _boredTargets.Keys.Where(k => k == null).ToList())
            {
                _boredTargets.Remove(key);
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
            // If we just finished an attack, the cat is now "bored" of that target.
            // Record its position at the moment of boredom.
            if (CurrentState == attackHandler && attackHandler.CurrentTarget != null)
            {
                _boredTargets[attackHandler.CurrentTarget] = attackHandler.CurrentTarget.position;
            }

            potentialTargets.Clear();
            OnRequestPotentialTargets?.Invoke(potentialTargets); // passing the list to the subscribers to be filled

            if (potentialTargets.Count > 0)
            {
                foreach (var potentialTarget in potentialTargets)
                {
                    if (potentialTarget == null) continue;

                    if (_boredTargets.TryGetValue(potentialTarget, out Vector3 boringPosition))
                    {
                        float distanceMovedSqr = (potentialTarget.position - boringPosition).sqrMagnitude;

                        // If the target hasn't moved enough, it's still boring. Skip it.
                        if (distanceMovedSqr < _interestRegainMovementThreshold * _interestRegainMovementThreshold)
                        {
                            continue;
                        }
                        else
                        {
                            // It moved!! It's interesting again. Remove it from the bored list.
                            _boredTargets.Remove(potentialTarget);
                        }
                    }

                    if (senses.HasLineOfSight(potentialTarget, out float _))
                    {
                        // Found a valid, interesting target. Attack it.
                        attackHandler.SetTarget(potentialTarget);
                        TransitionToState(attackHandler);
                        return;
                    }
                }
            }

            TransitionToState(idleHandler);
        }

        void TransitionToState(CatStateBase catState)
        {
            if (catState == null || catState == CurrentState)
                return;

            CurrentState?.OnExitState();

            CurrentState = catState;

            CurrentState.OnEnterState();
        }

#if UNITY_EDITOR
        void OnDrawGizmos()
        {
            Handles.Label(transform.position, CurrentState?.GetType().Name);
        }
#endif
    }
}