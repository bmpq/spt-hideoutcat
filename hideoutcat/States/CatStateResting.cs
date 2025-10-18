using tarkin.hideoutcat.Persistent;
using UnityEngine;

namespace tarkin.hideoutcat.States
{
    public class CatStateResting : CatStateBase, IPersistentDataDependent
    {
        [SerializeField] private float sleepEnergyRestoreRate = 5f;
        [SerializeField] private float minTimeAsleep = 10f;
        [SerializeField] private float timeBeforeSleep = 5f;

        private readonly int P_LYING_SIDE = Animator.StringToHash("LyingSide");
        private readonly int P_LYING_BELLY = Animator.StringToHash("LyingBelly");
        private readonly int P_SLEEPING = Animator.StringToHash("Sleeping");

        private readonly int P_STRETCH = Animator.StringToHash("Stretch");

        public bool isSleeping { get; private set; }

        private float timeLyingAwake;
        private float timeAsleep;

        bool interruptedSleep;

        CatPersistentDataController _data;

        public void SetPersistentData(CatPersistentDataController data)
        {
            _data = data;
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

        public void ForceWakeUp()
        {
            interruptedSleep = true;
            isSleeping = false;
            animator.SetBool(P_SLEEPING, false);
        }

        public override StateTickResult Tick(float _)
        {
            if (isSleeping)
            {
                timeAsleep += Time.deltaTime;
                _data.RestoreEnergy(sleepEnergyRestoreRate * Time.deltaTime);
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

            if (interruptedSleep)
            {
                return StateTickResult.StateDone;
            }

            if (_data.Energy >= 95f && timeAsleep > minTimeAsleep)
            {
                return StateTickResult.StateDone;
            }

            return new StateTickResult(CatInput.ToStop);
        }
    }
}
