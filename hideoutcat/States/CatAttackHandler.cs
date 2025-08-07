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

        [Range(0, 0.05f)]
        [SerializeField] private float targetMovingThreshold = 0.01f;

        private Transform target;
        public Transform CurrentTarget => target;
        private Vector3 targetLastSeenPos;

        public enum AttackSubstate
        {
            None,
            Search,
            Track,
            Approach,
            Prime,
            Pounce
        }
        public AttackSubstate CurrentSubstate { get; private set; }

        void SetSubstate(AttackSubstate newSubstate)
        {
            substateTimeElapsed = 0;
            CurrentSubstate = newSubstate;

            if (newSubstate == AttackSubstate.None)
                lookAt.Release();
        }

        private float substateTimeElapsed;

        private readonly int P_POUNCE_PRIMING = Animator.StringToHash("PouncePriming");
        private readonly int P_POUNCE = Animator.StringToHash("Pounce");
        private readonly int P_DISTANCE = Animator.StringToHash("Distance");
        private readonly int P_SNIFF = Animator.StringToHash("Sniff");

        protected override void Awake()
        {
            base.Awake();
            senses = GetComponent<CatSenses>();
            lookAt = GetComponent<CatLookAt>();
        }

        public void SetTarget(Transform _target)
        {
            target = _target;
        }

        public override void OnEnterState()
        {

        }

        public override void OnExitState()
        {
            lookAt.Release();
        }

        public override StateTickResult Tick()
        {
            substateTimeElapsed += Time.deltaTime;
            if (target == null)
            {
                SetSubstate(AttackSubstate.Search);
                return StateTickResult.StateDone;
            }

            bool lineOfSight = senses.HasLineOfSight(target, out float distanceToTargetFromEyes);

            Vector3 directionToTargetFromRoot = target.position - transform.position;
    
            directionToTargetFromRoot.y = 0;
            float angleToTargetFromRoot = Vector3.SignedAngle(transform.forward, directionToTargetFromRoot, Vector3.up);
            float distanceToTargetFromRoot = Vector3.Distance(transform.position, target.position);

            float thrustInput = 0f;
            float turnInput = 0f;
            float crouchInput = 0f;

            switch (CurrentSubstate)
            {
                case AttackSubstate.None:
                    if (lineOfSight && Vector3.SqrMagnitude(target.position - targetLastSeenPos) > targetMovingThreshold * targetMovingThreshold)
                        SetSubstate(AttackSubstate.Track);
                    break;
                case AttackSubstate.Search:
                    if (lineOfSight)
                        SetSubstate(AttackSubstate.Track);
                    else
                    {
                        crouchInput = substateTimeElapsed < 4f ? 1f : 0f;

                        if (substateTimeElapsed < 1f)
                        {
                            lookAt.LookAt(targetLastSeenPos);
                            break;
                        }
                        else if (substateTimeElapsed > 8f)
                        {
                            SetSubstate(AttackSubstate.None);
                        }

                        float randomLookInterval = UnityExtensions.InverseLerpUnclamped(-1f, 8f, substateTimeElapsed);
                        float randomLookRange = UnityExtensions.InverseLerpUnclamped(-1f, 3f, substateTimeElapsed);

                        if ((int)(substateTimeElapsed / randomLookInterval) > (int)((substateTimeElapsed - Time.deltaTime) / randomLookInterval))
                        {
                            lookAt.LookAt(transform.position + transform.forward +
                                transform.right * Random.Range(-randomLookRange, randomLookRange) +
                                new Vector3(0, Random.Range(-randomLookRange, randomLookRange), 0));
                        }
                    }
                    break;
                case AttackSubstate.Track:
                    if (lineOfSight)
                    {
                        crouchInput = 1f;
                        lookAt.SetLookTarget(target);
                        if (Mathf.Abs(angleToTargetFromRoot) > 30f)
                        {
                            turnInput = Mathf.Clamp(angleToTargetFromRoot, -1f, 1f);
                        }

                        if (distanceToTargetFromRoot < 0.5f)
                        {
                            SetSubstate(AttackSubstate.Prime);
                        }
                        else
                        {
                            if (distanceToTargetFromRoot > 2.4f && distanceToTargetFromRoot < 2.5f)
                                SetSubstate(AttackSubstate.Prime);
                            else
                                SetSubstate(AttackSubstate.Approach);
                        }
                    }
                    else
                    {
                        SetSubstate(AttackSubstate.Search);
                    }
                    break;
                case AttackSubstate.Approach:
                    crouchInput = 1f;
                    if (Mathf.Abs(angleToTargetFromRoot) > 15f)
                        turnInput = Mathf.Clamp(angleToTargetFromRoot, -1f, 1f);

                    if (distanceToTargetFromRoot < 0.22f)
                        thrustInput = -1f;
                    else if (Mathf.Abs(angleToTargetFromRoot) < 30f)
                    {
                        if (distanceToTargetFromRoot > 0.5f && distanceToTargetFromRoot < 1.5f)
                            thrustInput = 1f;
                        else if (distanceToTargetFromRoot >= 1.5f)
                        {
                            thrustInput = distanceToTargetFromRoot;
                            crouchInput = 0f;
                        }
                    }

                    if (distanceToTargetFromRoot > 0.22f && distanceToTargetFromRoot < 0.5f)
                    {
                        SetSubstate(AttackSubstate.Track);
                    }
                    break;
                case AttackSubstate.Prime:
                    if (lineOfSight)
                    {
                        crouchInput = 1f;
                        lookAt.SetLookTarget(target);

                        if (distanceToTargetFromRoot < 0.22f)
                        {
                            SetSubstate(AttackSubstate.Approach);
                            break;
                        }

                        if (Mathf.Abs(angleToTargetFromRoot) > 10f)
                        {
                            animator.SetBool(P_POUNCE_PRIMING, false);
                            turnInput = Mathf.Clamp(angleToTargetFromRoot, -1f, 1f);
                        }
                        else
                        {
                            animator.SetBool(P_POUNCE_PRIMING, true);

                            bool ShouldPounce()
                            {
                                if (distanceToTargetFromRoot < 0.35f) // if close enough, dont wait for priming
                                    return true;

                                if (substateTimeElapsed < 2f)
                                    return false;

                                if (distanceToTargetFromRoot < 0.5f)
                                    return true;

                                if (distanceToTargetFromRoot > 2.4f && distanceToTargetFromRoot < 2.5f) // special long distance jump clip
                                {
                                    return true;
                                }

                                return false;
                            }

                            if (ShouldPounce())
                            {
                                SetSubstate(AttackSubstate.Pounce);
                                animator.SetFloat(P_DISTANCE, distanceToTargetFromRoot);
                                animator.SetTrigger(P_POUNCE);
                            }
                            else
                            {
                                if (substateTimeElapsed > 5f)
                                    SetSubstate(AttackSubstate.Track);
                            }
                        }
                    }
                    else
                    {
                        SetSubstate(AttackSubstate.Search);
                    }
                    break;
                case AttackSubstate.Pounce:
                    crouchInput = 1f;

                    if (substateTimeElapsed > 0.2f)
                    {
                        if (distanceToTargetFromRoot < 0.4f &&
                            Mathf.Abs(angleToTargetFromRoot) < 30f &&
                            Vector3.SqrMagnitude(target.position - targetLastSeenPos) < targetMovingThreshold * targetMovingThreshold)
                        {
                            lookAt.Release();
                            animator.SetTrigger(P_SNIFF);
                            SetSubstate(AttackSubstate.None);
                        }
                        else
                            SetSubstate(AttackSubstate.Track);
                    }
                    break;
            }

            if (CurrentSubstate != AttackSubstate.Prime && CurrentSubstate != AttackSubstate.Pounce)
                animator.SetBool(P_POUNCE_PRIMING, false);

            if (lineOfSight)
            {
                targetLastSeenPos = target.position;
            }

            if (CurrentSubstate == AttackSubstate.None)
            {
                return StateTickResult.StateDone;
            }

            StateTickResult result = new StateTickResult();
            result.Input.Turn = turnInput;
            result.Input.Thrust = thrustInput;

            return result;
        }
    }
}
