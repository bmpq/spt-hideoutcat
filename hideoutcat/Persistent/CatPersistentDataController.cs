using System;
using System.Linq;
using UnityEngine;

namespace tarkin.hideoutcat.Persistent
{
    public class CatPersistentDataController
    {
        private Coat[] _allAvailableCoats;
        public string CatName { get; private set; } = "Kuzya";
        public float FedLevel { get; private set; } = 50f;
        public float CurrentFoodBowl { get; private set; } = 0f;
        public Coat CurrentCoat { get; private set; }
        public float Energy { get; private set; }

        private float hungerSpeedIdle = 0.01f;
        private float energyDrainIdle = 0.005f;

        public CatPersistentDataController(Coat[] allAvailableCoats)
        {
            _allAvailableCoats = allAvailableCoats ?? throw new ArgumentNullException(nameof(allAvailableCoats));

            CurrentCoat = _allAvailableCoats.FirstOrDefault();
        }

        public void Tick(float deltaTime)
        {
            FedLevel -= deltaTime * hungerSpeedIdle;
            if (FedLevel < 0) FedLevel = 0;

            Energy -= deltaTime * energyDrainIdle;
            if (Energy < 0) Energy = 0;
        }

        public void RestoreEnergy(float amount)
        {
            Energy = Mathf.Clamp(Energy + amount, 0f, 100f);
        }

        public void ApplySaveData(CatSaveData loadedData)
        {
            if (loadedData == null)
            {
                UnityEngine.Debug.Log("No cat save data found, using default data.");
                return;
            }

            this.FedLevel = loadedData.HungerLevel;
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
                HungerLevel = this.FedLevel,
                FoodBowlLevel = this.CurrentFoodBowl,
                CoatGuid = CurrentCoat?.Id,
                CatName = this.CatName,
                Energy = this.Energy
            };
        }
    }
}
