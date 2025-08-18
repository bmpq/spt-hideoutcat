using System;
using System.Collections.Generic;
using tarkin.hideoutcat.States;
using tarkin.hideoutcat.Pathfinding;
using UnityEngine;
using System.Linq;
using tarkin.hideoutcat.Persistent;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace tarkin.hideoutcat
{
    [RequireComponent(typeof(CatLocomotion))]
    [RequireComponent(typeof(CatSenses))]
    [RequireComponent(typeof(CatAppearance))]

    [RequireComponent(typeof(CatIdleHandler))]
    [RequireComponent(typeof(CatGraphTraverser))]
    [RequireComponent(typeof(CatAttackHandler))]
    public class HideoutCat : MonoBehaviour
    {
        public static event Action<HideoutCat> OnCatSpawned;
        public static event Action<HideoutCat> OnCatDestroyed;
        public CatPersistentDataController PersistentData { get; private set; }
        public CatAppearance Appearance => appearance;
        private CatAppearance appearance;

        private CatLocomotion locomotion;

        private CatSenses senses;

        private CatManualController manualController;
        private CatIdleHandler idleHandler;
        private CatGraphTraverser graphTraverser;
        private CatAttackHandler attackHandler;

        private CatStateBase CurrentState;

        public static event Action<List<Transform>> OnRequestPotentialTargets;
        private readonly List<Transform> potentialTargets = new List<Transform>();

        private readonly Dictionary<Transform, Vector3> _boredTargets = new Dictionary<Transform, Vector3>();
        [Tooltip("how far a 'boring' target must move for the cat to regain interest")]
        [SerializeField] private float _interestRegainMovementThreshold = 0.1f;

        public void Initialize(CatPersistentDataController dataController)
        {
            this.PersistentData = dataController ?? throw new ArgumentNullException(nameof(dataController));

            if (PersistentData.CurrentCoat != null)
            {
                Appearance.ApplyCoatTexture(PersistentData.CurrentCoat.MainTexture);
            }
        }

        void Awake()
        {
            locomotion = GetComponent<CatLocomotion>();

            senses = GetComponent<CatSenses>();

            idleHandler = GetComponent<CatIdleHandler>();
            graphTraverser = GetComponent<CatGraphTraverser>();
            attackHandler = GetComponent<CatAttackHandler>();

            manualController = GetComponent<CatManualController>();

            appearance = GetComponent<CatAppearance>();

            OnCatSpawned?.Invoke(this);
        }

        void Start()
        {
            if (CurrentState == null)
            {
                TransitionToState(idleHandler);
            }
        }

        void Update()
        {
            PersistentData.Tick(Time.deltaTime);

            foreach (var key in _boredTargets.Keys.Where(k => k == null).ToList())
            {
                _boredTargets.Remove(key);
            }

            CheckForInterrupts();

            StateTickResult result = CurrentState.Tick();
            locomotion.SetInput(result.Input);

            if (result.IsStateDone)
            {
                DecideNextState();
            }
        }

        private bool CheckForInterrupts()
        {
            // don't interrupt manual control
            if (manualController != null && manualController.isActiveAndEnabled)
            {
                if (CurrentState != manualController)
                {
                    TransitionToState(manualController);
                    return true;
                }
                return false;
            }

            // don't interrupt an attack, let it finish
            if (CurrentState == attackHandler)
            {
                return false;
            }

            potentialTargets.Clear();
            OnRequestPotentialTargets?.Invoke(potentialTargets);

            if (potentialTargets.Count > 0)
            {
                Transform bestTarget = FindBestTarget();
                if (bestTarget != null)
                {
                    attackHandler.SetTarget(bestTarget);
                    TransitionToState(attackHandler);
                    return true;
                }
            }

            return false;
        }

        void DecideNextState()
        {
            if (CurrentState == attackHandler && attackHandler.CurrentTarget != null)
            {
                _boredTargets[attackHandler.CurrentTarget] = attackHandler.CurrentTarget.position;
                attackHandler.SetTarget(null);
            }

            var randomPatrolNode = graphTraverser.GetRandomNode();
            if (randomPatrolNode != null)
            {
                graphTraverser.LayNewPath(randomPatrolNode);
                TransitionToState(graphTraverser);
            }
            else
            {
                TransitionToState(idleHandler);
            }
        }

        private Transform FindBestTarget()
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
                    return potentialTarget;
                }
            }
            return null;
        }

        void TransitionToState(CatStateBase newState)
        {
            if (newState == null || newState == CurrentState)
                return;

            CurrentState?.OnExitState();

            CurrentState = newState;

            CurrentState.OnEnterState();
        }

        void OnDestroy()
        {
            OnCatDestroyed.Invoke(this);
        }

#if UNITY_EDITOR
        void OnDrawGizmos()
        {
            Handles.Label(transform.position, CurrentState?.GetType().Name);
        }
#endif
    }
}