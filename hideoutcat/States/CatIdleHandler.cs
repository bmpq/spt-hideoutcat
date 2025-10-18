namespace tarkin.hideoutcat.States
{
    internal class CatIdleHandler : CatStateBase
    {
        private CatSenses senses;
        private CatLookAt lookAt;

        void Start()
        {
            senses = GetComponent<CatSenses>();
            lookAt = GetComponent<CatLookAt>();
        }

        public override void OnEnterState()
        {
            animator.SetBool("Sitting", true);
        }

        public override void OnExitState()
        {
            animator.SetBool("Sitting", false);
            lookAt.Release();
        }

        public override StateTickResult Tick(float timeInStateElapsed)
        {
            if (timeInStateElapsed > 10f) 
                return StateTickResult.StateDone;

            return new StateTickResult(CatInput.ToStop);
        }
    }
}
