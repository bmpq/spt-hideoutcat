using System.Collections.Generic;
using UnityEngine;

namespace hideoutcat.Pathfinding
{
    [System.Serializable]
    public class Node
    {
        public string name;
        public Vector3 position;
        public List<Node> connectedTo;
    }
}
