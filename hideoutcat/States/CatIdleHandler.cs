namespace tarkin.hideoutcat.States
{
    internal class CatIdleHandler : CatStateBase
    {
        public override void OnEnterState()
        {
            animator.SetBool("Sitting", true);
        }

        public override void OnExitState()
        {
            animator.SetBool("Sitting", false);
        }

        public override StateTickResult Tick()
        {
            return StateTickResult.StateDone;
        }
    }
}
