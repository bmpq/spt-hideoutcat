using System;

namespace tarkin.hideoutcat.Persistent
{
    public class CatSaveData
    {
        public string CatName { get; set; }
        public DateTime LastSaveTime { get; set; }
        public float HungerLevel { get; set; }
        public float FoodBowlLevel { get; set; }
        public string CoatGuid { get; set; }
        public float Energy { get; set; }
        public float Bladder { get; set; }
    }
}
