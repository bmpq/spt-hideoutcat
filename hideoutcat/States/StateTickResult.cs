namespace tarkin.hideoutcat.States
{
    public struct StateTickResult
    {
        public CatInput Input;

        public bool IsStateDone;

        public StateTickResult(CatInput input)
        {
            Input = input;
        }

        public static StateTickResult StateDone
        {
            get
            {
                StateTickResult result = new StateTickResult(CatInput.ToStop);
                result.IsStateDone = true;
                return result;
            }
        }
    }

    public struct CatInput
    {
        public float Thrust;
        public float Turn;
        public float Crouch;
        public bool RequestJumpUp;

        public CatInput(float turn = 0f, float thrust = 0f, float crouch = 0f, bool requestJumpUp = false)
        {
            Thrust = thrust;
            Turn = turn;
            Crouch = crouch;
            RequestJumpUp = requestJumpUp;
        }

        public static CatInput ToJump => new CatInput(requestJumpUp: true);
        public static CatInput ToStop => new CatInput(0, 0, 0, false);
    }
}
