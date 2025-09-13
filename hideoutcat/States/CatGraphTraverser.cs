using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using tarkin.hideoutcat.Pathfinding;

namespace tarkin.hideoutcat.States
{
    public class CatGraphTraverser : CatStateBase
    {
        private enum TraverserState 
        { 
            Moving,
            Done 
        }
        private TraverserState _currentState = TraverserState.Done;

        [SerializeField] private Graph pathfindingGraph;
        [SerializeField] private float arrivalThreshold = 0.2f;
        [SerializeField] private float finalTurnThreshold = 5.0f;

        [Header("movement")]
        [SerializeField] private float baseSpeed = 1.0f;
        [SerializeField] private float straightPathSpeedBoost = 3.6f;

        private List<Node> _currentPath;
        private int _currentPathIndex;
        private Node CurrentTargetNode => (_currentPath != null && _currentPath.Count > 0 && _currentPathIndex < _currentPath.Count) ? _currentPath[_currentPathIndex] : null;

        public event Action<Node> OnDestinationReached;
        public event Action<Node> OnNodeReached;

        public override void OnEnterState()
        {
            if (CurrentTargetNode != null)
            {
                _currentState = TraverserState.Moving;
            }
            else
            {
                _currentState = TraverserState.Done;
            }
        }

        public override void OnExitState()
        {
            StopAndForgetPath();
        }

        public override StateTickResult Tick()
        {
            if (_currentState == TraverserState.Done || CurrentTargetNode == null)
            {
                return StateTickResult.StateDone;
            }

            switch (_currentState)
            {
                case TraverserState.Moving:
                    return HandleMovement();

                default:
                    // failsafe case
                    StopAndForgetPath();
                    return StateTickResult.StateDone;
            }
        }

        public void LayNewPath(Node targetNode)
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

        public void StopAndForgetPath()
        {
            _currentPath = null;
            _currentPathIndex = 0;
            _currentState = TraverserState.Done;
        }

        public Node GetRandomNode()
        {
            if (pathfindingGraph == null || pathfindingGraph.Nodes == null || pathfindingGraph.Nodes.Count == 0)
            {
                Debug.LogWarning("Pathfinding graph has no nodes to choose from.", this);
                return null;
            }
            return pathfindingGraph.Nodes[UnityEngine.Random.Range(0, pathfindingGraph.Nodes.Count)];
        }

        private StateTickResult HandleMovement()
        {
            float distanceToTarget = Vector3.Distance(transform.position, CurrentTargetNode.position);

            if (distanceToTarget < arrivalThreshold)
            {
                ProcessNodeArrival();
                return new StateTickResult(CatInput.ToStop);
            }

            Vector3 directionToTarget = (CurrentTargetNode.position - transform.position).normalized;
            directionToTarget.y = 0;
            float angleToTarget = Vector3.SignedAngle(transform.forward, directionToTarget, Vector3.up);

            float turnInput = Mathf.Clamp(angleToTarget / 45f, -1f, 1f);
            float thrustInput = CalculateThrust(angleToTarget, distanceToTarget);

            bool toJump = CurrentTargetNode.position.y > transform.position.y + 0.5f;

            return new StateTickResult(new CatInput(turnInput, thrustInput, requestJumpUp: toJump));
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

        private float CalculateThrust(float angleToTarget, float distanceToTarget)
        {
            // don't move forward if facing the wrong way at close range.
            if (Mathf.Abs(angleToTarget) > 60f && distanceToTarget < 0.5f)
            {
                return 0f;
            }

            // check if can apply a speed boost.
            if (Mathf.Abs(angleToTarget) < 15f)
            {
                int nextNodeIndex = _currentPathIndex + 1;
                if (nextNodeIndex < _currentPath.Count)
                {
                    Node nextNode = _currentPath[nextNodeIndex];
                    Vector3 directionToNextNode = (nextNode.position - transform.position).normalized;
                    directionToNextNode.y = 0;

                    if (Vector3.Angle(transform.forward, directionToNextNode) < 15f)
                    {
                        return straightPathSpeedBoost;
                    }
                }
            }

            return baseSpeed;
        }
    }
}
