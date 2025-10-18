using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace tarkin.hideoutcat.Pathfinding
{
    public class Graph : MonoBehaviour
    {
        public List<Node> Nodes
        {
            get
            {
#if UNITY_EDITOR
                _nodes = null;
#endif
                if (_nodes == null)
                    _nodes = GetComponentsInChildren<Node>(true).ToList();
                return _nodes;
            }
        }

        private List<Node> _nodes;

        public Node WorldPosToClosestNode(Vector3 worldPos)
        {
            if (Nodes == null || Nodes.Count == 0)
            {
                return null;
            }

            Node closestNode = null;
            float minSqrDistance = float.MaxValue;

            foreach (var node in Nodes)
            {
                float sqrDistance = (node.position - worldPos).sqrMagnitude;
                if (sqrDistance < minSqrDistance)
                {
                    minSqrDistance = sqrDistance;
                    closestNode = node;
                }
            }
            return closestNode;
        }

        public List<Node> FindPathBFS(Node startNode, Node endNode)
        {
            if (startNode == null || endNode == null)
            {
                Debug.LogError("Start or End node is null!");
                return null;
            }

            Queue<Node> queue = new Queue<Node>();
            Dictionary<Node, Node> cameFrom = new Dictionary<Node, Node>();

            queue.Enqueue(startNode);
            cameFrom[startNode] = null;

            while (queue.Count > 0)
            {
                Node current = queue.Dequeue();

                if (current == endNode)
                {
                    return ReconstructPath(cameFrom, endNode);
                }

                foreach (Node neighbor in current.connectedTo)
                {
                    if (!cameFrom.ContainsKey(neighbor))
                    {
                        queue.Enqueue(neighbor);
                        cameFrom[neighbor] = current;
                    }
                }
            }
            return null;
        }

        private List<Node> ReconstructPath(Dictionary<Node, Node> cameFrom, Node endNode)
        {
            List<Node> path = new List<Node>();
            Node current = endNode;

            while (current != null)
            {
                path.Add(current);
                current = cameFrom[current];
            }

            path.Reverse();
            return path;
        }
    }
}