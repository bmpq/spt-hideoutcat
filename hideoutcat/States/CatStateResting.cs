using UnityEngine;

namespace tarkin.hideoutcat.States
{
    public class CatStateResting : CatStateBase
    {
        [SerializeField] private float sleepEnergyRestoreRate = 5f;
        [SerializeField] private float minTimeAsleep = 10f;
        [SerializeField] private float timeBeforeSleep = 5f;

        private readonly int P_LYING_SIDE = Animator.StringToHash("LyingSide");
        private readonly int P_LYING_BELLY = Animator.StringToHash("LyingBelly");
        private readonly int P_SLEEPING = Animator.StringToHash("Sleeping");

        public bool isSleeping { get; private set; }

        private float timeLyingAwake;
        private float timeAsleep;

        bool interruptedSleep;

        void Start()
        {

        }

        public override void OnEnterState()
        {
            timeLyingAwake = 0;
            timeAsleep = 0;
            interruptedSleep = false;

            bool lyingOnSide = Random.value > 0.5f;
            animator.SetBool(lyingOnSide ? P_LYING_SIDE : P_LYING_BELLY, true);
        }

        public override void OnExitState()
        {
            animator.SetBool(P_SLEEPING, false);
            animator.SetBool(P_LYING_SIDE, false);
            animator.SetBool(P_LYING_BELLY, false);
        }

        public void WakeUp()
        {
            interruptedSleep = true;
            isSleeping = false;
            animator.SetBool(P_SLEEPING, false);
        }

        public override StateTickResult Tick()
        {
            if (isSleeping)
            {
                timeAsleep += Time.deltaTime;
                PersistentData.RestoreEnergy(sleepEnergyRestoreRate * Time.deltaTime);
            }
            else
            {
                timeLyingAwake += Time.deltaTime;
                if (timeLyingAwake > timeBeforeSleep)
                {
                    isSleeping = true;
                    animator.SetBool(P_SLEEPING, true);
                }
            }

            if (PersistentData.Energy >= 95f && timeAsleep > minTimeAsleep)
            {
                return StateTickResult.StateDone;
            }
            
            if (interruptedSleep)
            {
                return StateTickResult.StateDone;
            }

            return new StateTickResult(CatInput.ToStop);
        }
    }
}
