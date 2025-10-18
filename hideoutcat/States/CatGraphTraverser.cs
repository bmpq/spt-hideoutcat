using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using tarkin.hideoutcat.Pathfinding;

namespace tarkin.hideoutcat.States
{
    public class CatGraphTraverser : CatStateMoveTowardsTarget
    {
        private enum TraverserState 
        { 
            Moving,
            Done 
        }
        private TraverserState _currentState = TraverserState.Done;

        [SerializeField] private Graph pathfindingGraph;

        private List<Node> _currentPath;
        private int _currentPathIndex;
        private Node CurrentTargetNode => (_currentPath != null && _currentPath.Count > 0 && _currentPathIndex < _currentPath.Count) ? _currentPath[_currentPathIndex] : null;

        public event Action<Node> OnDestinationReached;
        public event Action<Node> OnNodeReached;

        public override void OnEnterState()
        {
        }

        public override void OnExitState()
        {
            StopAndForgetPath();
        }

        public override StateTickResult Tick(float _)
        {
            if (_currentState == TraverserState.Done || CurrentTargetNode == null)
            {
                return StateTickResult.StateDone;
            }

            switch (_currentState)
            {
                case TraverserState.Moving:
                    StateTickResult input = GetMovementInputToTarget(CurrentTargetNode.position, out float distToTarget);

                    if (distToTarget < ARRIVAL_THRESHOLD)
                        ProcessNodeArrival();

                    return input;

                default:
                    // failsafe case
                    StopAndForgetPath();
                    return StateTickResult.StateDone;
            }
        }

        public void LayNewPath(Vector3 worldPos)
        {
            LayNewPath(pathfindingGraph.WorldPosToClosestNode(worldPos));
        }

        private void LayNewPath(Node targetNode)
        {
            if (targetNode == null)
            {
                Debug.LogWarning("Cannot lay path to a null target node.");
                return;
            }

            Node startNode = pathfindingGraph.WorldPosToClosestNode(transform.position);
            _currentPath = pathfindingGraph.FindPathBFS(startNode, targetNode);

            if (_currentPath == null || _currentPath.Count == 0)
            {
                Debug.LogError($"No valid path found from '{startNode?.name}' to '{targetNode.name}'.", this);
                StopAndForgetPath();
                return;
            }

            _currentPathIndex = 0;
            _currentState = TraverserState.Moving;
            Debug.Log($"New path laid. Nodes: {_currentPath.Count}. Destination: {targetNode.name}");
        }

        private void StopAndForgetPath()
        {
            _currentPath = null;
            _currentPathIndex = 0;
            _currentState = TraverserState.Done;
        }

        private Node GetRandomNode()
        {
            if (pathfindingGraph == null || pathfindingGraph.Nodes == null || pathfindingGraph.Nodes.Count == 0)
            {
                Debug.LogWarning("Pathfinding graph has no nodes to choose from.", this);
                return null;
            }
            return pathfindingGraph.Nodes[UnityEngine.Random.Range(0, pathfindingGraph.Nodes.Count)];
        }

        private void ProcessNodeArrival()
        {
            Node reachedNode = CurrentTargetNode;
            OnNodeReached?.Invoke(reachedNode);

            bool isFinalNode = (_currentPathIndex == _currentPath.Count - 1);

            if (isFinalNode)
            {
                CompletePath();
            }
            else
            {
                // it's an intermediate node, advance to the next one.
                _currentPathIndex++;
                Debug.Log($"Node '{reachedNode.name}' reached. Moving to next: {CurrentTargetNode.name}");
            }
        }

        private void CompletePath()
        {
            Debug.Log("Destination reached and path completed!");
            if (_currentPath != null && _currentPath.Any())
            {
                OnDestinationReached?.Invoke(_currentPath.Last());
            }

            StopAndForgetPath();
        }
    }
}
