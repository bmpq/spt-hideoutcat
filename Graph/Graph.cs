using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace hideoutcat.Pathfinding
{
    public class Graph
    {
        public List<Node> nodes;

        public Graph(List<Node> nodes)
        {
            this.nodes = nodes;
        }

        public void AddNode(Node node)
        {
            nodes.Add(node);
        }

        public Node FindNodeById(string id)
        {
            return nodes.Find(n => n.name == id);
        }

        public Node GetNodeClosestNoPathfinding(Vector3 worldPos)
        {
            return nodes
                .OrderBy(t => (t.position - worldPos).sqrMagnitude)
                .FirstOrDefault();
        }

        public List<Node> FindPathBFS(Node startNode, Node endNode)
        {
            if (startNode == null || endNode == null)
            {
                Plugin.Log.LogError("Start or End node is null!");
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