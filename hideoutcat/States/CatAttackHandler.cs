using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace tarkin.hideoutcat.States
{
    [RequireComponent(typeof(CatSenses))]
    [RequireComponent(typeof(CatLookAt))]
    public class CatAttackHandler : CatStateBase
    {
        private CatSenses senses;
        private CatLookAt lookAt;

        public Transform CurrentTarget { get; private set; }
        private Vector3 targetLastSeenPos;
        private bool hasLineOfSight;

        private float timeTargetStationary;

        private float angleToTargetFromRoot;
        private float distanceToTargetFromRootXZ;
        private float distanceToTargetFromRootY;

        public enum AttackSubstate
        {
            None,
            Search,
            Track,
            Approach,
            Prime,
            PrimeWall,
            Pounce,
            Sniff
        }
        public AttackSubstate CurrentSubstate { get; private set; }
        public AttackSubstate PreviousSubstate { get; private set; }
        private float substateTimeElapsed;

        [Header("General")]
        [Range(0, 0.1f)]
        [SerializeField] private float targetMovingThreshold = 0.01f;
        [SerializeField] private float targetConsideredDeadTime = 1f;

        [Header("Search")]
        [SerializeField] private float searchCrouchDuration = 4f;
        [SerializeField] private float searchGiveUpTime = 8f;

        [Header("Track")]
        [SerializeField] private float trackToApproachTime = 3f;
        [SerializeField] private float trackTurnAngleThreshold = 70f;
        [SerializeField] private float trackToPrimeWallDistance = 0.37f;

        [Header("Approach")]
        [SerializeField] private float approachTurnAngleThreshold = 15f;
        [SerializeField] private float approachTooCloseDistance = 0.22f;
        [SerializeField] private float approachIdealDistanceMin = 0.22f;
        [SerializeField] private float approachIdealDistanceMax = 0.5f;
        [SerializeField] private float approachWalkThreshold = 1.5f;
        [SerializeField] private float approachGiveUpTime = 10f;

        [Header("Prime")]
        [SerializeField] private float primeTurnAngleThreshold = 10f;
        [SerializeField] private float primeMinPounceTime = 2f;
        [SerializeField] private float primeMaxDuration = 5f;
        [SerializeField] private float primeClosePounceDistance = 0.35f;
        [SerializeField] private float primeOptimalLongPounceMin = 2.4f;
        [SerializeField] private float primeOptimalLongPounceMax = 2.5f;
        [SerializeField] private float primeHeightDifferenceThreshold = 0.2f;

        [Header("Pounce")]
        [SerializeField] private float pounceToSniffDistance = 0.4f;

        private readonly int P_POUNCE_PRIMING = Animator.StringToHash("PouncePriming");
        private readonly int P_POUNCE = Animator.StringToHash("Pounce");
        private readonly int P_DISTANCE = Animator.StringToHash("Distance");
        private readonly int P_SNIFF = Animator.StringToHash("Sniff");
        private readonly int P_SITTING = Animator.StringToHash("Sitting");

        protected override void Awake()
        {
            base.Awake();
            senses = GetComponent<CatSenses>();
            lookAt = GetComponent<CatLookAt>();
        }

        public void SetTarget(Transform _target)
        {
            CurrentTarget = _target;
            if (CurrentTarget != null)
            {
                targetLastSeenPos = CurrentTarget.position;
                SetSubstate(AttackSubstate.Track);
            }
        }

        public override void OnEnterState()
        {
            if (CurrentTarget != null)
                SetSubstate(AttackSubstate.Track);
            else
                SetSubstate(AttackSubstate.None);
        }

        public override void OnExitState()
        {
            animator.ResetTrigger(P_POUNCE);
            animator.ResetTrigger(P_SNIFF);
            animator.SetBool(P_POUNCE_PRIMING, false);
            animator.SetBool(P_SITTING, false);
            lookAt.Release();
            SetSubstate(AttackSubstate.None);
        }

        private void SetSubstate(AttackSubstate newSubstate)
        {
            if (CurrentSubstate == newSubstate) return;

            PreviousSubstate = CurrentSubstate;
            substateTimeElapsed = 0;
            CurrentSubstate = newSubstate;

            if (newSubstate == AttackSubstate.None)
            {
                lookAt.Release();
            }
            else if (newSubstate == AttackSubstate.Pounce)
            {
                animator.SetFloat(P_DISTANCE, distanceToTargetFromRootXZ);
                animator.SetTrigger(P_POUNCE);
            }
            else if (newSubstate == AttackSubstate.Sniff)
            {
                animator.SetTrigger(P_SNIFF);
            }
        }

        public override StateTickResult Tick()
        {
            substateTimeElapsed += Time.deltaTime;

            if (CurrentTarget == null)
            {
                SetSubstate(AttackSubstate.None);
                return StateTickResult.StateDone;
            }

            UpdateTargetData();

            CatInput input = new CatInput();
            switch (CurrentSubstate)
            {
                case AttackSubstate.None:
                    if (hasLineOfSight && !IsTargetDead())
                        SetSubstate(AttackSubstate.Track);
                    else
                        return StateTickResult.StateDone;
                    break;

                case AttackSubstate.Search:    input = Tick_Search(); break;
                case AttackSubstate.Track:     input = Tick_Track(); break;
                case AttackSubstate.Approach:  input = Tick_Approach(); break;
                case AttackSubstate.Prime:     input = Tick_Prime(); break;
                case AttackSubstate.PrimeWall: input = Tick_PrimeWall(); break;
                case AttackSubstate.Pounce:    input = Tick_Pounce(); break;
                case AttackSubstate.Sniff:     input = Tick_Sniff(); break;
            }

            UpdateSharedState();

            return new StateTickResult { Input = input };
        }

        private CatInput Tick_Search()
        {
            if (hasLineOfSight)
            {
                SetSubstate(AttackSubstate.Track);
                return new CatInput { Crouch = 1 };
            }

            if (substateTimeElapsed > searchGiveUpTime)
            {
                SetSubstate(AttackSubstate.None);
                return new CatInput();
            }

            if (substateTimeElapsed < 1f)
            {
                lookAt.LookAt(targetLastSeenPos);
            }
            else
            {
                float randomLookInterval = UnityExtensions.InverseLerpUnclamped(-1f, 8f, substateTimeElapsed);
                float randomLookRange = UnityExtensions.InverseLerpUnclamped(-1f, 3f, substateTimeElapsed);

                if ((int)(substateTimeElapsed / randomLookInterval) > (int)((substateTimeElapsed - Time.deltaTime) / randomLookInterval))
                {
                    lookAt.LookAt(transform.position + transform.forward +
                        transform.right * Random.Range(-randomLookRange, randomLookRange) +
                        new Vector3(0, Random.Range(-randomLookRange, randomLookRange), 0));
                }
            }

            float crouch = substateTimeElapsed < searchCrouchDuration ? 1f : 0f;
            return new CatInput { Crouch = crouch };
        }

        private CatInput Tick_Track()
        {
            if (!hasLineOfSight)
            {
                SetSubstate(AttackSubstate.Search);
                return new CatInput { Crouch = 1 };
            }

            lookAt.SetLookTarget(CurrentTarget);

            bool isCloseAndLevel = distanceToTargetFromRootXZ < approachIdealDistanceMax && distanceToTargetFromRootY < 0.2f;
            if (isCloseAndLevel)
            {
                SetSubstate(AttackSubstate.Prime);
                return new CatInput { Crouch = 1 };
            }

            if (substateTimeElapsed > trackToApproachTime)
            {
                if (IsAtOptimalLongPounceRange())
                    SetSubstate(AttackSubstate.Prime);
                else
                    SetSubstate(AttackSubstate.Approach);
                return new CatInput { Crouch = 1 };
            }

            float turn = 0;
            if (Mathf.Abs(angleToTargetFromRoot) > trackTurnAngleThreshold)
            {
                turn = Mathf.Sign(angleToTargetFromRoot);
            }

            return new CatInput { Turn = turn, Crouch = 1 };
        }

        private CatInput Tick_Approach()
        {
            if (!hasLineOfSight)
            {
                SetSubstate(AttackSubstate.Search);
                return new CatInput { Crouch = 1 };
            }

            if (substateTimeElapsed > approachGiveUpTime)
            {
                SetSubstate(AttackSubstate.None);
                return new CatInput();
            }

            lookAt.SetLookTarget(CurrentTarget);

            float turn = 0, thrust = 0, crouch = 1;

            // Target is on a higher platform (e.g., a wall)
            if (distanceToTargetFromRootY > 0.1f)
            {
                if (Mathf.Abs(angleToTargetFromRoot) > approachTurnAngleThreshold)
                {
                    turn = Mathf.Sign(angleToTargetFromRoot);
                }
                else
                {
                    if (distanceToTargetFromRootXZ < trackToPrimeWallDistance)
                    {
                        SetSubstate(AttackSubstate.PrimeWall);
                    }
                    else
                    {
                        thrust = 1f;
                    }
                }
            }
            // Target is on the same level
            else
            {
                if (distanceToTargetFromRootXZ > approachIdealDistanceMin && distanceToTargetFromRootXZ < approachIdealDistanceMax)
                {
                    SetSubstate(AttackSubstate.Track);
                    return new CatInput { Crouch = 1 };
                }

                if (Mathf.Abs(angleToTargetFromRoot) > approachTurnAngleThreshold)
                {
                    turn = Mathf.Sign(angleToTargetFromRoot);
                }
                else
                {
                    if (distanceToTargetFromRootXZ < approachTooCloseDistance)
                        thrust = -1f; // Too close, back up
                    else if (distanceToTargetFromRootXZ > approachWalkThreshold)
                    {
                        thrust = distanceToTargetFromRootXZ; // Run
                        crouch = 0;
                    }
                    else
                        thrust = 1f; // Creep forward
                }
            }

            return new CatInput { Turn = turn, Thrust = thrust, Crouch = crouch };
        }

        private CatInput Tick_Prime()
        {
            if (!hasLineOfSight)
            {
                SetSubstate(AttackSubstate.Search);
                return new CatInput { Crouch = 1 };
            }

            lookAt.SetLookTarget(CurrentTarget);
            float turn = 0;

            if (Mathf.Abs(angleToTargetFromRoot) <= primeTurnAngleThreshold)
            {
                animator.SetBool(P_POUNCE_PRIMING, true);

                if (ShouldPounce())
                {
                    SetSubstate(AttackSubstate.Pounce);
                    return new CatInput { Crouch = 1 };
                }
            }
            else
            {
                turn = Mathf.Sign(angleToTargetFromRoot);
                animator.SetBool(P_POUNCE_PRIMING, false);
            }

            if (substateTimeElapsed > primeMaxDuration || distanceToTargetFromRootY > primeHeightDifferenceThreshold)
            {
                SetSubstate(AttackSubstate.Track);
                return new CatInput { Crouch = 1 };
            }

            if (distanceToTargetFromRootXZ < approachTooCloseDistance)
            {
                SetSubstate(AttackSubstate.Approach);
                return new CatInput { Crouch = 1 };
            }

            return new CatInput { Turn = turn, Crouch = 1 };
        }

        private CatInput Tick_PrimeWall()
        {
            if (!hasLineOfSight) { SetSubstate(AttackSubstate.Search); return new CatInput(); }

            if (distanceToTargetFromRootY < 0.2f)
            {
                SetSubstate(AttackSubstate.Track);
                return new CatInput();
            }

            animator.SetBool(P_SITTING, true);

            if (substateTimeElapsed > 1f)
            {
                // It's a fixed vertical pounce animation
                SetSubstate(AttackSubstate.Pounce);
                animator.SetFloat(P_DISTANCE, 1f);
            }
            else if (substateTimeElapsed > 5f) // Timeout
            {
                SetSubstate(AttackSubstate.Track);
            }

            return new CatInput();
        }

        private CatInput Tick_Pounce()
        {
            if (substateTimeElapsed > 0.5f)
            {
                if (IsTargetDead())
                {
                    lookAt.Release();
                    if (distanceToTargetFromRootXZ < pounceToSniffDistance && Mathf.Abs(angleToTargetFromRoot) < 30f)
                    {
                        SetSubstate(AttackSubstate.Sniff);
                    }
                    else
                    {
                        SetSubstate(AttackSubstate.None);
                    }
                }
                else
                {
                    SetSubstate(AttackSubstate.Track);
                }
            }
            return new CatInput { Crouch = 1 };
        }

        private bool IsTargetDead()
        {
            return timeTargetStationary > targetConsideredDeadTime;
        }

        private CatInput Tick_Sniff()
        {
            if (substateTimeElapsed > 0.5f)
            {
                SetSubstate(AttackSubstate.None);
            }
            return new CatInput();
        }

        private void UpdateTargetData()
        {
            hasLineOfSight = senses.HasLineOfSight(CurrentTarget, out _);

            Vector3 directionToTargetFromRoot = CurrentTarget.position - transform.position;

            distanceToTargetFromRootY = directionToTargetFromRoot.y;
            directionToTargetFromRoot.y = 0;
            distanceToTargetFromRootXZ = directionToTargetFromRoot.magnitude;

            angleToTargetFromRoot = Vector3.SignedAngle(transform.forward, directionToTargetFromRoot, Vector3.up);

            bool targetMoving = Vector3.SqrMagnitude(CurrentTarget.position - targetLastSeenPos) > targetMovingThreshold * targetMovingThreshold;
            if (targetMoving)
                timeTargetStationary = 0;
            else
                timeTargetStationary += Time.deltaTime;
        }

        private void UpdateSharedState()
        {
            if (hasLineOfSight)
            {
                targetLastSeenPos = CurrentTarget.position;
            }

            if (CurrentSubstate != AttackSubstate.Prime && CurrentSubstate != AttackSubstate.Pounce)
                animator.SetBool(P_POUNCE_PRIMING, false);
            if (CurrentSubstate != AttackSubstate.PrimeWall)
                animator.SetBool(P_SITTING, false);
        }

        private bool IsAtOptimalLongPounceRange()
        {
            return distanceToTargetFromRootXZ > primeOptimalLongPounceMin && distanceToTargetFromRootXZ < primeOptimalLongPounceMax;
        }

        private bool ShouldPounce()
        {
            if (distanceToTargetFromRootXZ < primeClosePounceDistance)
                return true;

            if (IsAtOptimalLongPounceRange())
                return true;

            if (substateTimeElapsed > primeMinPounceTime)
                return true;

            return false;
        }

#if UNITY_EDITOR
        private void OnGUI()
        {
            if (Application.isPlaying)
            {
                GUILayout.BeginVertical(EditorStyles.textArea);
                GUILayout.Label($"AttackSubState: {CurrentSubstate} ({substateTimeElapsed:F2}s)");
                GUILayout.Label($"PrevAttackSubState: {PreviousSubstate}", EditorStyles.miniLabel);
                GUILayout.Label($"Angle to target: {angleToTargetFromRoot:F1}");
                GUILayout.Label($"DistanceToTarget Y: {distanceToTargetFromRootY:F2}");
                GUILayout.Label($"DistanceToTarget XZ: {distanceToTargetFromRootXZ:F2}");
                GUILayout.Label($"Has LoS: {hasLineOfSight}");
                GUILayout.EndVertical();
            }
        }
#endif
    }
}