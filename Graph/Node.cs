using EFT;
using Newtonsoft.Json;
using System.Collections.Generic;
using UnityEngine;

namespace hideoutcat.Pathfinding
{
    [System.Serializable]
    public class Node
    {
        public string name;
        public Vector3 position;

        [JsonProperty("connectedTo")]
        public List<string> connectedToNamesForSerialization;
        [JsonIgnore]
        public List<Node> connectedTo = new List<Node>();

        public bool forwardJump;

        public EAreaType areaType; // if NotSet, it's a waypoint
        public int areaLevel;

        public float poseRotation;
        public Pose pose;

        public enum Pose
        {
            None,
            Sitting,
            LyingBelly,
            LyingSide,
            Eating,
            Defecating,
            Grooming,
            SharpeningVertical,
            SharpeningHorizontal
        }
    }
}
