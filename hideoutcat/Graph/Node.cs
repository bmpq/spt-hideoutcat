using Newtonsoft.Json;
using System.Collections.Generic;
using UnityEngine;

namespace tarkin.hideoutcat.Pathfinding
{
    public class Node : MonoBehaviour
    {
        public List<Node> connectedTo { get; private set; } = new List<Node>();

        public bool forwardJump;

        public EAreaType areaType;

        public int areaLevel;

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

        public float poseRotation
        {
            get => transform.eulerAngles.y;
            set => transform.eulerAngles = new Vector3(0, value, 0);
        }
        public Vector3 position
        {
            get => transform.position;
            set => transform.position = value;
        }
    }
}
