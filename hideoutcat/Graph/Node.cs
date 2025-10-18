using System.Collections.Generic;
using UnityEngine;

namespace tarkin.hideoutcat.Pathfinding
{
    public class Node : MonoBehaviour
    {
        public List<Node> connectedTo;

        public Vector3 position
        {
            get => transform.position;
            set => transform.position = value;
        }
    }
}
