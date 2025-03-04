using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using tarkin;
using System;

namespace hideoutcat.Pathfinding
{
    public class CatGraphTraverser : MonoBehaviour
    {
        public Vector3 Velocity { get; private set; }
        private Vector3 prevPos;

        private Graph pathfindingGraph => Plugin.CatGraph;
        private Node currentNode;
        private List<Node> currentPath;
        private int currentPathIndex;

        Animator animator;

        CatLookAt catLookAt;

        public event Action<Node> OnDestinationReached;

        void Start()
        {
            catLookAt = GetComponent<CatLookAt>();
            animator = GetComponent<Animator>();
        }

        public void LayNewPath(Node targetNode)
        {
            currentNode = pathfindingGraph.GetNodeClosestAny(transform.position);

            currentPath = pathfindingGraph.FindPathBFS(currentNode, targetNode);
            currentPathIndex = 0;

            if (currentPath == null)
            {
                Plugin.Log.LogError("No Path Found");
            }
        }

        void Update()
        {
            if (currentPath == null)
            {
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
                            SetMovement(0, turn);
                        }
                        else
                        {
                            SetMovement(0, 0);

                            currentPathIndex++;
                            Plugin.Log.LogInfo("Reached final destination!");
                            OnDestinationReached?.Invoke(currentPath[currentPath.Count - 1]);

                            catLookAt.SetLookTarget(null);
                        }
                    }
                    else
                    {
                        currentPathIndex++;
                        Plugin.Log.LogInfo($"Set next node to: {currentPath[currentPathIndex].name}");

                        catLookAt.SetLookTarget(currentPath[Mathf.Min(currentPath.Count - 1, currentPathIndex + 1)].position + new Vector3(0, 0.3f, 0));
                    }
                }
            }
            else
            {
                SetMovement(0, 0);
            }
        }

        void LateUpdate()
        {
            Velocity = transform.position - prevPos;

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
        }

        float currentTurnVelocity;
        float currentThrustVelocity;

        float smoothTimeTurn = 0.2f;
        float smoothTimeThrust = 0.3f;

        public void SetMovement(float thrust, float turn)
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
            else if (animator.IsInTransition(0) && animator.GetAnimatorTransitionInfo(0).IsName("JumpUpAir -> JumpUpEnd"))
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
                    }
                }
                else if (targetPosition.y > transform.position.y + 0.3f) // means we need to initiate jump up
                {
                    if (Mathf.Abs(angleToTarget) < 10f) // wait to face the direction
                    {
                        animator.SetBool("JumpingUp", true);
                    }
                }
                else if (targetPosition.y < transform.position.y - 0.3f) // means we need to initiate jump down
                {
                    if (Mathf.Abs(angleToTarget) < 10f) // wait to face the direction
                    {
                        animator.SetBool("JumpingDown", true);
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
                                else if (distToNextNextTarget > 5f)
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

                    Debug.Log(angleToTarget);
                }

                SetMovement(targetThrust, targetTurn);
                prevDistToDest = distToTarget;
            }
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
            }


            if (transform.position.y > targetPosition.y - 0.7f) // the (end) clip expects exactly 0.5 offset on Y
            {
                animator.SetBool("JumpingUp", false);
                jumpUpEndOffset = targetPosition.y - transform.position.y;
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

                // another quick evil fix to avoid seeking the landing node after already landed, just consider the current target node reached on land
                if (currentPathIndex < currentPath.Count - 2)
                    currentPathIndex++;
            }
        }
    }
}