using System;
using System.Linq;

namespace tarkin.hideoutcat.Persistent
{
    public class CatPersistentDataController
    {
        private Coat[] _allAvailableCoats;
        public string CatName { get; private set; } = "Kuzya";
        public float CurrentHunger { get; private set; } = 50f;
        public float CurrentFoodBowl { get; private set; } = 0f;
        public Coat CurrentCoat { get; private set; }

        private float hungerSpeedIdle = 0.01f;

        public CatPersistentDataController(Coat[] allAvailableCoats)
        {
            _allAvailableCoats = allAvailableCoats ?? throw new ArgumentNullException(nameof(allAvailableCoats));

            CurrentCoat = _allAvailableCoats.FirstOrDefault();
        }

        public void Tick(float deltaTime)
        {
            CurrentHunger -= deltaTime * hungerSpeedIdle;
            if (CurrentHunger < 0) CurrentHunger = 0;
        }

        public void ApplySaveData(CatSaveData loadedData)
        {
            if (loadedData == null)
            {
                UnityEngine.Debug.Log("No cat save data found, using default data.");
                return;
            }

            this.CurrentHunger = loadedData.HungerLevel;
            this.CurrentFoodBowl = loadedData.FoodBowlLevel;

            if (!string.IsNullOrEmpty(loadedData.CatName))
            {
                this.CatName = loadedData.CatName;
            }

            Coat loadedCoat = _allAvailableCoats.FirstOrDefault(c => c.Id == loadedData.CoatGuid);

            if (loadedCoat != null)
            {
                this.CurrentCoat = loadedCoat;
            }
            else
            {
                UnityEngine.Debug.LogWarning($"Could not find saved coat with GUID {loadedData.CoatGuid}. Using default.");
            }
        }

        public bool SetCoat(Coat newCoat)
        {
            if (newCoat != null && _allAvailableCoats.Contains(newCoat))
            {
                CurrentCoat = newCoat;
                return true;
            }
            return false;
        }

        public void SetCatName(string name)
        {
            if (!string.IsNullOrWhiteSpace(name))
            {
                CatName = name;
            }
        }

        public void AddFoodToBowl(float amount)
        {
            CurrentFoodBowl += amount;
        }

        public CatSaveData GetSaveData()
        {
            return new CatSaveData
            {
                LastSaveTime = DateTime.UtcNow,
                HungerLevel = this.CurrentHunger,
                FoodBowlLevel = this.CurrentFoodBowl,
                CoatGuid = CurrentCoat?.Id,
                CatName = this.CatName
            };
        }
    }
}
