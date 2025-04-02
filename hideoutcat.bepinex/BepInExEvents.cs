using EFT.Hideout;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace hideoutcat.bepinex
{
    internal class BepInExPlayerEvents : IPlayerEvents
    {
        public event Action<AreaData> AreaSelected;
        public event Action<AreaData> AreaLevelUpdated;
        public event Action PlayerPrepareWorkout;
        public event Action PlayerStopWorkout;

        public void TriggerAreaSelected(AreaData area) => AreaSelected?.Invoke(area);
        public void TriggerAreaLevelUpdated(AreaData area) => AreaLevelUpdated?.Invoke(area);
        public void TriggerPlayerWorkoutPrepare() => PlayerPrepareWorkout?.Invoke();
        public void TriggerPlayerWorkoutStop() => PlayerStopWorkout?.Invoke();
    }
}
