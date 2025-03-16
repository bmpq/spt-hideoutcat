using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using tarkin;
using System;
using EFT.Interactive;
using EFT;
using Comfort.Common;

namespace hideoutcat.Pathfinding
{
    public class CatGraphTraverser : MonoBehaviour
    {
        public Vector3 Velocity { get; private set; }
        public float VelocityMagnitude => Velocity.magnitude / Time.deltaTime;
        private Vector3 prevPos;

        public float DeltaY { get; private set; }

        private Graph pathfindingGraph => Plugin.CatGraph;
        private Node currentNode;
        public List<Node> currentPath;
        private int currentPathIndex;

        public bool HasDestination => currentPath != null;

        Animator animator;

        public event Action<Node> OnDestinationReached;
        public event Action<List<Node>> OnNodeReached; // the parameter is the list of nodes that are left to traverse

        public event Action OnJumpAirEnd;

        public Door[] doors;

        public Door doorInTheWay { get; private set; }

        void Start()
        {
            animator = GetComponent<Animator>();

            doors = FindObjectsByType<Door>(FindObjectsSortMode.None);
        }

        public void ForgetDestination()
        {
            currentNode = null;
            currentPath = null;
        }

        public void LayNewPath(Node targetNode)
        {
            if (currentNode == null)
                currentNode = pathfindingGraph.GetNodeClosestWaypoint(transform.position);

            currentPath = pathfindingGraph.FindPathBFS(currentNode, targetNode);
            currentPathIndex = 0;

            if (currentPath == null)
            {
                Plugin.Log.LogError($"No Path Found from {currentNode} to {targetNode}");
            }
        }

        void Update()
        {
            if (currentPath == null)
            {
                TickMovement(0, 0);
                return;
            }

            if (currentPathIndex < currentPath.Count)
            {
                bool lastNode = currentPathIndex == currentPath.Count - 1;

                float distToTargetNode = Vector3.Distance(transform.position, currentPath[currentPathIndex].position);

                if (distToTargetNode < 0.1f && !animator.GetBool("JumpingUp") && !animator.GetBool("JumpingDown"))
                {
                    currentNode = currentPath[currentPathIndex];

                    if (lastNode)
                    {
                        float angleDifference = Mathf.DeltaAngle(currentNode.poseRotation, transform.eulerAngles.y);
                        if (currentNode.poseParameters.Count > 0 && Mathf.Abs(angleDifference) > 10f) // if no pose needed, skip turning, consider reached
                        {
                            float turn = -Mathf.Sign(angleDifference);
                            TickMovement(0, turn);
                        }
                        else
                        {
                            currentPathIndex++;

                            Node destinationNode = currentPath[currentPath.Count - 1];
                            currentPath = null;

                            Plugin.Log.LogInfo("Reached final destination!");
                            OnDestinationReached?.Invoke(destinationNode);
                        }
                    }
                    else
                    {
                        currentPathIndex++;
                        Plugin.Log.LogInfo($"Set next node to: {currentPath[currentPathIndex].name}");
                        
                        // broadcast nodes left to traverse
                        OnNodeReached?.Invoke(currentPath.Skip(currentPathIndex).ToList());
                    }
                }
            }
            else
            {
                TickMovement(0, 0);
            }
        }

        void LateUpdate()
        {
            Velocity = transform.position - prevPos;
            prevPos = transform.position;

            if (currentPath == null || currentPath.Count == 0)
                return;

            if (currentPathIndex < currentPath.Count)
            {
                Locomotion();
            }
            else
            {
                transform.SetPositionIndividualAxis(y: Mathf.Lerp(transform.position.y, currentPath[currentPath.Count - 1].position.y, Time.deltaTime * 3f));
            }

            DeltaY = transform.position.y - currentPath[Mathf.Min(currentPathIndex, currentPath.Count - 1)].position.y;
        }

        float currentTurnVelocity;
        float currentThrustVelocity;

        float smoothTimeTurn = 0.2f;
        float smoothTimeThrust = 0.3f;

        void TickMovement(float thrust, float turn)
        {
            float smoothedThrust = Mathf.SmoothDamp(animator.GetFloat("Thrust"), thrust, ref currentThrustVelocity, smoothTimeThrust);
            float smoothedTurn = Mathf.SmoothDamp(animator.GetFloat("Turn"), turn, ref currentTurnVelocity, smoothTimeTurn);

            animator.SetFloat("Thrust", smoothedThrust);
            animator.SetFloat("Turn", smoothedTurn);
        }

        float prevDistToDest = 0f;
        float jumpUpEndOffset = -0.5f;

        void Locomotion()
        {
            Node targetNode = currentPath[currentPathIndex];
            Vector3 targetPosition = targetNode.position;

            if (animator.GetBool("JumpingUp"))
            {
                HandleJumpingUp();
            }
            else if (animator.IsInTransition(0) && 
                (animator.GetAnimatorTransitionInfo(0).IsName("JumpUpAir -> JumpUpEnd")
                || animator.GetAnimatorTransitionInfo(0).IsName("JumpUpStart -> JumpUpEnd"))) // precarious
            {
                float t = animator.GetAnimatorTransitionInfo(0).normalizedTime;
                transform.SetPositionIndividualAxis(y: Mathf.Lerp(targetPosition.y - jumpUpEndOffset, targetPosition.y, t));

                transform.position += transform.forward * Time.deltaTime * (1f - t);
            }
            else if (animator.GetBool("JumpingDown"))
            {
                HandleJumpingDown();
            }
            else if (animator.GetBool("JumpingForward"))
            {
                HandleJumpingForward();
            }
            else
            {
                Vector3 directionToTarget = (targetPosition - transform.position).normalized;
                directionToTarget.y = 0f;
                float angleToTarget = Vector3.SignedAngle(transform.forward, directionToTarget, Vector3.up);

                float targetThrust = 0;
                float targetTurn = angleToTarget.RemapClamped(-40f, 40f, -1f, 1f);

                float distToTarget = Vector3.Distance(transform.position, targetPosition);

                if (targetNode.forwardJump && distToTarget > 0.4f) // jump forward
                {
                    if (Mathf.Abs(angleToTarget) < 7f) // wait to face the direction
                    {
                        animator.SetBool("JumpingForward", true);
                        animator.Update(0);
                    }
                }
                else if (targetPosition.y > transform.position.y + 0.3f) // means we need to initiate jump up
                {
                    if (Mathf.Abs(angleToTarget) < 10f) // wait to face the direction
                    {
                        animator.SetBool("JumpingUp", true);
                        animator.Update(0);
                    }
                }
                else if (targetPosition.y < transform.position.y - 0.3f) // means we need to initiate jump down
                {
                    if (Mathf.Abs(angleToTarget) < 10f) // wait to face the direction
                    {
                        animator.SetBool("JumpingDown", true);
                        animator.Update(0);
                    }
                }
                else // keep moving
                {
                    // turn in place logic
                    if (Mathf.Abs(angleToTarget) > 20f && distToTarget < 0.5f)
                    {
                        targetThrust *= 0;
                    }
                    else
                    {
                        targetThrust = 1f;

                        // move faster if on a straight path and the target is far
                        if (Mathf.Abs(angleToTarget) < 30f)
                        {
                            for (int i = currentPathIndex; i < currentPath.Count; i++)
                            {
                                Vector3 positionNextNextTarget = currentPath[i].position;
                                Vector3 directionToNextNextTarget = (positionNextNextTarget - transform.position).normalized;
                                float angleToNextNextTarget = Vector3.SignedAngle(transform.forward, directionToNextNextTarget, Vector3.up);
                                float distToNextNextTarget = Vector3.Distance(transform.position, positionNextNextTarget);
                                if (distToNextNextTarget > 8f && Mathf.Abs(angleToTarget) < 5f)
                                    targetThrust = Mathf.Max(targetThrust, 3.6f);
                                else if (distToNextNextTarget > 6f)
                                    targetThrust = Mathf.Max(targetThrust, 2.55f);
                                else if (distToNextNextTarget > 3f)
                                    targetThrust = Mathf.Max(targetThrust, 1.66f);
                            }
                        }
                    }

                    // evil hack to keep the cat on the ground
                    transform.SetPositionIndividualAxis(y: Mathf.Lerp(transform.position.y, targetPosition.y, Time.deltaTime * 3f));

                    // last node, yield movement control
                    if (currentPathIndex == currentPath.Count - 1)
                    {
                        if (distToTarget < 0.1f)
                        {
                            return;
                        }
                    }

                    doorInTheWay = BlockedPathByDoor();
                    if (doorInTheWay != null)
                    {
                        targetThrust = 0f;
                    }
                }

                TickMovement(targetThrust, targetTurn);
                prevDistToDest = distToTarget;
            }
        }

        public bool IsMovement()
        {
            return animator.GetCurrentAnimatorStateInfo(0).IsName("Movement");
        }

        Door BlockedPathByDoor()
        {
            for (int i = 0; i < doors.Length; i++)
            {
                if (doors[i] == null || !doors[i].gameObject.activeInHierarchy)
                    continue;

                if (doors[i].DoorState == EDoorState.Open)
                    continue;

                float distToDoor = Vector3.Distance(doors[i].transform.parent.position, transform.position);
                Vector3 directionToTarget = (doors[i].transform.parent.position - transform.position).normalized;
                directionToTarget.y = 0f;
                float angleToDoor = Vector3.SignedAngle(transform.forward, directionToTarget, Vector3.up);

                bool doorInTheWay = distToDoor < 2f && Mathf.Abs(angleToDoor) < 90f;

                if (doorInTheWay)
                {
                    return doors[i];
                }
            }

            return null;
        }

        private void HandleJumpingForward()
        {
            Vector3 targetPosition = currentPath[currentPathIndex].position;

            AnimatorStateInfo curState = animator.GetCurrentAnimatorStateInfo(0);
            if (curState.IsName("JumpForwardStart") || curState.IsName("JumpForwardAir"))
            {
                // unsatisfactory linear vertical movement. not sure how to do this better
                float verticalSpeed = Time.deltaTime * 10f;
                float horizontalSpeed = Time.deltaTime * 2f;
                if (curState.IsName("JumpForwardStart"))
                {
                    // 0.75 is the start of the transition blend to JumpDownAir, and since it has no root motion, we smoothly start moving in script
                    float t = curState.normalizedTime.RemapClamped(0.75f, 1f, 0f, 1f);
                    verticalSpeed = Mathf.Lerp(0, verticalSpeed, t);
                    horizontalSpeed = Mathf.Lerp(0, horizontalSpeed, t);
                }

                float yDelta = targetPosition.y - transform.position.y;
                verticalSpeed *= yDelta;

                transform.position += new Vector3(0, verticalSpeed, 0);
                transform.position += transform.forward * horizontalSpeed;
            }

            float distToTarget = Vector3.Distance(transform.position, targetPosition);

            // beside obvious check for distane to target, we also check for previous distance, it's a failsafe in case the cat misses and overshoots
            if (distToTarget < 0.2f || distToTarget > prevDistToDest)
            {
                transform.SetPositionIndividualAxis(y: targetPosition.y);
                animator.SetBool("JumpingForward", false);
                animator.Update(0);

                OnJumpAirEnd?.Invoke();
            }

            prevDistToDest = distToTarget;
        }

        private void HandleJumpingUp()
        {
            Vector3 targetPosition = currentPath[currentPathIndex].position;

            bool drive =
                // 0.75 is the point in the (start) clip where the hind legs liftoff the ground
                (animator.GetCurrentAnimatorStateInfo(0).IsName("JumpUpStart") && animator.GetCurrentAnimatorStateInfo(0).normalizedTime > 0.75f)
                // or just the in air clip
                || animator.GetCurrentAnimatorStateInfo(0).IsName("JumpUpAir");

            if (drive)
            {
                float upSpeed = Time.deltaTime * 3f;
                float horizontalSpeed = Time.deltaTime;

                transform.position += new Vector3(0, upSpeed, 0);
                transform.position += transform.forward * horizontalSpeed;

                if (transform.position.y > targetPosition.y - 0.7f) // the (end) clip expects exactly 0.5 offset on Y
                {
                    animator.SetBool("JumpingUp", false);
                    animator.Update(0);
                    jumpUpEndOffset = targetPosition.y - transform.position.y;

                    OnJumpAirEnd?.Invoke();
                }
            }
        }

        private void HandleJumpingDown()
        {
            Vector3 targetPosition = currentPath[currentPathIndex].position;

            AnimatorStateInfo curState = animator.GetCurrentAnimatorStateInfo(0);
            if (curState.IsName("JumpDownStart") || curState.IsName("JumpDownAir"))
            {
                float fallingSpeed = Time.deltaTime * 3f;
                float horizontalSpeed = Time.deltaTime;
                if (curState.IsName("JumpDownStart"))
                {
                    // 0.75 is the start of the transition blend to JumpDownAir, and since it has no root motion, we smoothly start moving in script
                    float t = curState.normalizedTime.RemapClamped(0.75f, 1f, 0f, 1f);
                    fallingSpeed = Mathf.Lerp(0, fallingSpeed, t);
                    horizontalSpeed = Mathf.Lerp(0, horizontalSpeed, t);
                }
                else
                {
                    // "gravity" acceleration
                    fallingSpeed += curState.normalizedTime * Time.deltaTime * 5f;
                }

                transform.position += new Vector3(0, -fallingSpeed, 0);

                transform.position += transform.forward * horizontalSpeed;
            }

            if (transform.position.y < targetPosition.y + 0.02f)
            {
                transform.SetPositionIndividualAxis(y: targetPosition.y);
                animator.SetBool("JumpingDown", false);
                animator.Update(0);

                // another quick evil fix to avoid seeking the landing node after already landed, just consider the current target node reached on land
                if (currentPathIndex < currentPath.Count - 2)
                    currentPathIndex++;

                OnJumpAirEnd?.Invoke();
            }
        }
    }
}