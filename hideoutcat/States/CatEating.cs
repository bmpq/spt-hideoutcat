using tarkin.hideoutcat.Environment;
using tarkin.hideoutcat.Persistent;
using UnityEngine;

namespace tarkin.hideoutcat.States
{
    internal class CatEating : CatStateBase, IPersistentDataDependent
    {
        private readonly int P_EATING = Animator.StringToHash("Eating");

        CatPersistentDataController _data;

        private FoodBowl targetBowl;

        public void SetPersistentData(CatPersistentDataController data)
        {
            this._data = data;
        }

        public override void OnEnterState()
        {
        }

        public override void OnExitState()
        {
            animator.SetBool(P_EATING, false);
        }

        public void SetBowl(FoodBowl bowl)
        {
            targetBowl = bowl;
        }

        public override StateTickResult Tick(float _)
        {
            animator.SetBool(P_EATING, true);

            float amountToEat = 0.1f;

            if (amountToEat > _data.CurrentFoodBowl)
                return StateTickResult.StateDone;

            _data.Eat(amountToEat);
            targetBowl?.SetLevel(_data.CurrentFoodBowl);

            return StateTickResult.StateDone;
        }
    }
}
