using Newtonsoft.Json;
using System.Collections.Generic;
using UnityEngine;

namespace hideoutcat.Pathfinding
{
    public class Node : MonoBehaviour
    {
        public Vector3 position;

        public List<Node> connectedTo = new List<Node>();

        public bool forwardJump;

        public EAreaType areaType;

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

        public void ConnectTo(Node other)
        {
            if (connectedTo == null)
                connectedTo = new List<Node>();
            if (!connectedTo.Contains(other))
            {
                connectedTo.Add(other);
            }
        }
    }
}
