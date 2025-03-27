using EFT;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace hideoutcat.Pathfinding
{
    public class Graph
    {
        public List<Node> nodes;
        public readonly List<Node> waypointNodes;

        public Graph(List<Node> nodes)
        {
            this.nodes = nodes;

            waypointNodes = new List<Node>();

            foreach (var node in nodes)
            {
                if (node.areaType == EAreaType.NotSet)
                    waypointNodes.Add(node);
            }
        }

        public void AddNode(Node node)
        {
            nodes.Add(node);
        }

        public Node FindNodeByName(string name)
        {
            return nodes.Find(n => n.name == name);
        }

        public Node GetNodeClosestAny(Vector3 worldPos)
        {
            return nodes
                .OrderBy(t => (t.position - worldPos).sqrMagnitude)
                .FirstOrDefault();
        }

        public Node GetNodeClosestWaypoint(Vector3 worldPos)
        {
            return waypointNodes
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

        public List<Node> FindDeadEndNodesByAreaTypeAndLevel(EAreaType areaType, int areaLevel)
        {
            Plugin.Log.LogDebug($"requesting deadend node for {areaType} (level {areaLevel})");

            List<Node> deadEndNodes = new List<Node>();

            foreach (Node node in nodes)
            {
                if (node.pose != Node.Pose.None)
                {
                    if (node.areaType == areaType && node.areaLevel == areaLevel)
                    {
                        deadEndNodes.Add(node);
                    }
                }
            }
            return deadEndNodes;
        }
    }
}