using UnityEngine;

namespace tarkin.hideoutcat.States
{
    public class CatStateMoveTowardsTarget : CatStateBase
    {
        protected const float ARRIVAL_THRESHOLD = 0.2f;
        private const float MIN_DIST_TO_TARGET_TO_DASH = 3f;

        private const float ANIMATOR_LOCOMOTION_SPEED_BASE = 1f;
        private const float ANIMATOR_LOCOMOTION_SPEED_MAX = 3.6f;

        private const float MAX_TIME_STATE = 10f;

        protected CatSenses senses;

        Vector3 targetPos;

        protected override void Awake()
        {
            base.Awake();

            senses = GetComponent<CatSenses>();
        }

        public override void OnEnterState()
        {
        }

        public override void OnExitState()
        {
            targetPos = Vector3.zero;
        }

        public void SetTarget(Vector3 position)
        {
            targetPos = position;
        }

        public override StateTickResult Tick(float timeElapsedInCurrentState)
        {
            if (targetPos == Vector3.zero)
                return StateTickResult.StateDone;

            StateTickResult tickResult = GetMovementInputToTarget(targetPos, out float dist);
            if (dist < ARRIVAL_THRESHOLD)
                return StateTickResult.StateDone;

            if (timeElapsedInCurrentState > MAX_TIME_STATE)
                return StateTickResult.StateDone;

            return tickResult;
        }

        protected StateTickResult GetMovementInputToTarget(Vector3 targetPos, out float distanceToTarget)
        {
            distanceToTarget = Vector3.Distance(transform.position, targetPos);

            if (distanceToTarget < ARRIVAL_THRESHOLD)
            {
                return new StateTickResult(CatInput.ToStop);
            }

            Vector3 directionToTarget = (targetPos - transform.position).normalized;
            directionToTarget.y = 0;
            float angleToTarget = Vector3.SignedAngle(transform.forward, directionToTarget, Vector3.up);

            float turnInput = Mathf.Clamp(angleToTarget / 45f, -1f, 1f);
            float thrustInput = CalculateThrust(angleToTarget, distanceToTarget, targetPos);

            bool toJump = targetPos.y > transform.position.y + 0.5f;

            return new StateTickResult(new CatInput(turnInput, thrustInput, requestJumpUp: toJump));
        }

        private float CalculateThrust(float angleToTarget, float distanceToTarget, Vector3 targetPos)
        {
            // don't move forward if facing the wrong way at close range.
            if (Mathf.Abs(angleToTarget) > 60f && distanceToTarget < 0.5f)
            {
                return 0f;
            }

            // check if can apply a speed boost.
            if (Mathf.Abs(angleToTarget) < 15f && senses != null)
            {
                if (distanceToTarget > MIN_DIST_TO_TARGET_TO_DASH && senses.HasLineOfSight(targetPos, out float _))
                {
                    return ANIMATOR_LOCOMOTION_SPEED_MAX;
                }
            }

            return ANIMATOR_LOCOMOTION_SPEED_BASE;
        }
    }
}
