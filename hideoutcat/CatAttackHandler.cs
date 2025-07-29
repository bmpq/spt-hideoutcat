using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace tarkin.hideoutcat
{
    public class CatAttackHandler : MonoBehaviour
    {
        private Animator animator;
        private HideoutCat cat;
        private CatLookAt lookAt;

        [SerializeField] private Transform visionOrigin;
        [SerializeField] private float visionFOV = 200f;
        [SerializeField] private float visionMaxDistance = 20f;
        [SerializeField] private LayerMask obstacleLayerMask = 1 << 12;

#if UNITY_EDITOR
        [SerializeField] private bool visualizeFOV;
#endif

        private Transform target;
        private Vector3 targetLastSeenPos;

        public enum AttackState
        {
            None,
            Search,
            Track,
            Approach,
            Prime,
            Pounce
        }
        public AttackState CurrentState { get; private set; }
        private AttackState queuedState;

        // enforce setting duration
        void SetState(AttackState newState, float duration, AttackState nextState)
        {
            CurrentState = newState;
            stateTimer = duration;
            queuedState = nextState;
        }

        private float stateTimer;

        private readonly int P_POUNCE_PRIMING = Animator.StringToHash("PouncePriming");
        private readonly int P_POUNCE = Animator.StringToHash("Pounce");
        private readonly int P_DISTANCE = Animator.StringToHash("Distance");


        void Awake()
        {
            animator = GetComponent<Animator>();
            cat = GetComponent<HideoutCat>();
            lookAt = GetComponent<CatLookAt>();
        }

        void Update()
        {
            stateTimer -= Time.deltaTime;
            if (stateTimer < 0f)
            {
                SetState(queuedState, 1f, queuedState);
            }

            if (target == null)
            {
                SetState(AttackState.None, 1f, AttackState.None);
                return;
            }

            bool lineOfSight = LineOfSight(out float distanceToTargetFromEyes);

            Vector3 directionToTargetFromRoot = (target.position - transform.position);
            directionToTargetFromRoot.y = 0;
            float angleToTargetFromRoot = Vector3.SignedAngle(transform.forward, directionToTargetFromRoot, Vector3.up);
            float distanceToTargetFromRoot = Vector3.Distance(transform.position, target.position);

            float thrustInput = 0f;
            float turnInput = 0f;
            float crouchInput = 0f;

            switch (CurrentState)
            {
                case AttackState.None:
                    if (lineOfSight)
                        SetState(AttackState.Track, 20f, AttackState.None);
                    break;
                case AttackState.Search:
                    if (lineOfSight)
                        SetState(AttackState.Track, 10f, AttackState.Search);
                    else
                    {
                        crouchInput = stateTimer < 4f ? 0f : 1f;

                        if (stateTimer > 7f)
                        {
                            lookAt.LookAt(targetLastSeenPos);
                            break;
                        }

                        // good luck tweaking this later lol
                        float randomLookInterval = Mathf.InverseLerp(10f, 0f, stateTimer) * 1.1f;
                        float randomLookRange = Mathf.InverseLerp(7f, 0f, stateTimer) * 3f;

                        if ((int)(stateTimer / randomLookInterval) < (int)((stateTimer + Time.deltaTime) / randomLookInterval))
                        {
                            lookAt.LookAt(transform.position + transform.forward + 
                                transform.right * Random.Range(-randomLookRange, randomLookRange) + 
                                new Vector3(0, Random.Range(-randomLookRange, randomLookRange), 0));
                        }
                    }
                    break;
                case AttackState.Track:
                    if (lineOfSight)
                    {
                        crouchInput = 1f;
                        lookAt.SetLookTarget(target);
                        if (Mathf.Abs(angleToTargetFromRoot) > 30f)
                        {
                            turnInput = Mathf.Clamp(angleToTargetFromRoot / 60f, -1f, 1f);
                        }

                        if (distanceToTargetFromRoot < 0.5f)
                        {
                            SetState(AttackState.Prime, 5f, AttackState.Track);
                        }
                        else if (distanceToTargetFromRoot > 3f)
                        {
                            SetState(AttackState.Approach, 5f, AttackState.Track);
                        }
                    }
                    else
                    {
                        SetState(AttackState.Search, 8f, AttackState.None);
                    }
                    break;
                case AttackState.Prime:
                    if (lineOfSight)
                    {
                        crouchInput = 1f;
                        lookAt.SetLookTarget(target);

                        if (Mathf.Abs(angleToTargetFromRoot) > 10f)
                        {
                            turnInput = Mathf.Clamp(angleToTargetFromRoot / 20f, -1f, 1f);
                            SetState(AttackState.Track, 10f, AttackState.Search);
                        }
                        else
                        {
                            animator.SetBool(P_POUNCE_PRIMING, true);

                            bool ShouldPounce()
                            {
                                if (distanceToTargetFromRoot < 0.35f) // if close enough, dont wait for priming
                                    return true;

                                if (stateTimer > 3f)
                                    return false;

                                if (distanceToTargetFromRoot < 0.5f)
                                    return true;

                                if (Mathf.Abs(angleToTargetFromRoot) > 10f)
                                    return false;

                                if (distanceToTargetFromRoot > 2.4f && distanceToTargetFromRoot < 2.5f) // special long distance jump clip
                                {
                                    return true;
                                }

                                return false;
                            }

                            if (ShouldPounce())
                            {
                                SetState(AttackState.Pounce, 0.5f, AttackState.Prime);
                                animator.SetFloat(P_DISTANCE, distanceToTargetFromRoot);
                                animator.SetTrigger(P_POUNCE);
                            }
                        }
                    }
                    else
                    {
                        SetState(AttackState.Search, 10f, AttackState.None);
                    }
                    break;
                case AttackState.Pounce:
                    crouchInput = 1f;

                    break;
            }

            if (CurrentState != AttackState.Prime && CurrentState != AttackState.Pounce)
                animator.SetBool(P_POUNCE_PRIMING, false);

            if (lineOfSight)
            {
                Debug.DrawLine(visionOrigin.position, target.position, Color.green);
                targetLastSeenPos = target.position;
            }

            cat.MovementInput = new Vector2(turnInput, thrustInput);
            cat.CrouchInput = crouchInput;
        }

        bool LineOfSight(out float distanceToTargetFromEyes)
        {
            Vector3 directionToTarget = (target.position - visionOrigin.position);
            distanceToTargetFromEyes = directionToTarget.magnitude;

            if (distanceToTargetFromEyes > visionMaxDistance)
                return false;

            if (Vector3.Angle(visionOrigin.forward, directionToTarget.normalized) > visionFOV / 2)
                return false;

            if (Physics.Raycast(visionOrigin.position, directionToTarget.normalized, out RaycastHit hit, distanceToTargetFromEyes - 0.01f, obstacleLayerMask))
            {
                Debug.DrawLine(visionOrigin.position, hit.point, Color.red);

                return false;
            }
            return true;
        }

        public void SetTarget(Transform _target)
        {
            this.target = _target;
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            Handles.Label(transform.position, CurrentState.ToString());

            if (visualizeFOV)
                UnityEditorExtensions.DrawVolumetricCone(visionOrigin.position, visionOrigin.forward, visionFOV / 2f, 0.3f, new Color(1f, 1f, 0.1f, 0.2f));
        }
#endif
    }
}
