using System;

namespace tarkin.hideoutcat.Persistent
{
    public class CatSaveData
    {
        public DateTime LastSaveTime { get; set; }
        public float HungerLevel { get; set; }
        public string CoatGuid { get; set; }
    }
}
