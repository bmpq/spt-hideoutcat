using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace tarkin.hideoutcat.Persistent
{
    public class CatPersistentDataController
    {
        public Func<CatSaveData> OnLoadRequested;
        public Action<CatSaveData> OnSaveRequested;

        public float CurrentHunger { get; private set; } = 50f;

        private float hungerSpeedIdle = 0.01f;

        public void Tick(float deltaTime)
        {
            CurrentHunger -= deltaTime * hungerSpeedIdle;
            if (CurrentHunger < 0) CurrentHunger = 0;
        }

        public void SaveData()
        {
            if (OnSaveRequested == null) return;

            var saveData = new CatSaveData
            {
                LastSaveTime = DateTime.UtcNow,
                HungerLevel = this.CurrentHunger
            };
            OnSaveRequested.Invoke(saveData);
        }

        public void LoadData()
        {
            if (OnLoadRequested == null) return;

            CatSaveData loadedData = OnLoadRequested.Invoke();
            if (loadedData != null)
            {
                this.CurrentHunger = loadedData.HungerLevel;
            }
        }
    }
}
