using System;
using System.Collections.Generic;
using tarkin.hideoutcat.States;
using tarkin.hideoutcat.Pathfinding;
using UnityEngine;
using System.Linq;
using tarkin.hideoutcat.Persistent;
using tarkin.hideoutcat.Environment;

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

        public CatStateBase CurrentState { get; private set; }
        private float timeElapsedInCurrentState;

        public static event Action<List<Transform>> OnRequestPotentialTargets;
        private readonly List<Transform> potentialTargets = new List<Transform>();

        private readonly Dictionary<Transform, Vector3> _boredTargets = new Dictionary<Transform, Vector3>();
        [Tooltip("how far a 'boring' target must move for the cat to regain interest")]
        [SerializeField] private float _interestRegainMovementThreshold = 0.1f;

        [SerializeField] private FoodBowl foodBowl;

        public void Initialize(CatPersistentDataController dataController)
        {
            this.PersistentData = dataController ?? throw new ArgumentNullException(nameof(dataController));

            foreach (var component in GetComponents<IPersistentDataDependent>())
            {
                component.SetPersistentData(dataController);
            }

            dataController.OnFoodBowlFoodAdded += foodBowl.SetLevel;
        }

        void Awake()
        {
            locomotion = GetComponent<CatLocomotion>();
            manualController = GetComponent<CatManualController>();

            senses = GetComponent<CatSenses>();

            appearance = GetComponent<CatAppearance>();

            OnCatSpawned?.Invoke(this);
        }

        void Start()
        {
            if (CurrentState == null)
            {
                TransitionToState<CatIdleHandler>();
            }

            if (PersistentData == null)
            {
                Debug.LogWarning("No persistent data!");
            }
        }

        void Update()
        {
            PersistentData?.Tick(Time.deltaTime);

            foreach (var key in _boredTargets.Keys.Where(k => k == null).ToList())
            {
                _boredTargets.Remove(key);
            }

            CheckForInterrupts();

            StateTickResult result = CurrentState.Tick(timeElapsedInCurrentState);
            locomotion.SetInput(result.Input);

            if (result.IsStateDone)
            {
                DecideNextState();
            }
            else
            {
                timeElapsedInCurrentState += Time.deltaTime;
            }
        }

        private bool CheckForInterrupts()
        {
            // don't interrupt manual control
            if (manualController != null && manualController.isActiveAndEnabled)
            {
                if (CurrentState != manualController)
                {
                    TransitionToState<CatManualController>();
                    return true;
                }
                return false;
            }

            // don't interrupt an attack, let it finish
            if (CurrentState is CatAttackHandler)
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
                    TransitionToState<CatAttackHandler>().SetTarget(bestTarget);
                    return true;
                }
            }

            return false;
        }

        void DecideNextState()
        {
            if (CurrentState is CatAttackHandler attackHandler && attackHandler.CurrentTarget != null)
            {
                _boredTargets[attackHandler.CurrentTarget] = attackHandler.CurrentTarget.position;
                attackHandler.SetTarget(null);
            }

            if (IsHungry())
            {
                if (senses.HasLineOfSight(foodBowl.transform.position, out float _))
                {
                    if (PersistentData.CurrentFoodBowl > 5f)
                    {
                        TransitionToState<CatStateMoveTowardsTarget>().SetTarget(foodBowl.transform.position);
                    }
                }
                else
                {
                    TransitionToState<CatGraphTraverser>().LayNewPath(foodBowl.transform.position);
                }
            }

            TransitionToState<CatIdleHandler>();
        }

        bool IsHungry()
        {
            return PersistentData.FedLevel < 30f;
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

        T TransitionToState<T>() where T : CatStateBase
        {
            T newState = GetComponent<T>();
            if (newState == null)
                newState = gameObject.AddComponent<T>();

            if (CurrentState == newState)
                return newState;

            // special exit logic for AttackHandler
            if (CurrentState is CatAttackHandler attackHandler)
            {
                _boredTargets[attackHandler.CurrentTarget] = attackHandler.CurrentTarget.position;
                attackHandler.SetTarget(null);
            }

            CurrentState?.OnExitState();

            CurrentState = newState;
            timeElapsedInCurrentState = 0f;

            if (newState is IPersistentDataDependent dependent)
                dependent.SetPersistentData(PersistentData);

            CurrentState.OnEnterState();

            return newState;
        }

        void OnDestroy()
        {
            OnCatDestroyed?.Invoke(this);
        }

#if UNITY_EDITOR
        void OnDrawGizmos()
        {
            Handles.Label(transform.position, CurrentState?.GetType().Name);
        }
#endif
    }
}