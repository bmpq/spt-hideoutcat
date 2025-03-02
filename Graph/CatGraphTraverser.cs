using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using tarkin;

namespace hideoutcat.Pathfinding
{
    public class CatGraphTraverser : MonoBehaviour
    {
        public string[] testTargetNodes;
        public int testTargetNodeIndex;

        private Graph pathfindingGraph;
        private Node currentNode;
        private List<Node> currentPath;
        private int currentPathIndex;

        Animator animator;

        CatLookAt catLookAt;

        void Start()
        {
            catLookAt = GetComponent<CatLookAt>();
            animator = GetComponent<Animator>();

            pathfindingGraph = Plugin.CatGraph;
        }

        public void LayNewPath(Node targetNode)
        {
            currentNode = pathfindingGraph.GetNodeClosestNoPathfinding(transform.position);

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
            else if (currentPathIndex >= currentPath.Count) // on the end
            {
                animator.SetFloat("Thrust", Mathf.Lerp(animator.GetFloat("Thrust"), 0f, Time.deltaTime * 4f));
                animator.SetFloat("Turn", Mathf.Lerp(animator.GetFloat("Turn"), 0f, Time.deltaTime * 4f));
            }

            if (currentPathIndex < currentPath.Count)
            {
                catLookAt.SetLookTarget(currentPath[Mathf.Min(currentPath.Count - 1, currentPathIndex + 1)].position + new Vector3(0, 0.3f, 0));

                if (Vector3.Distance(transform.position, currentPath[currentPathIndex].position) < 0.2f && !animator.GetBool("JumpingUp") && !animator.GetBool("JumpingDown"))
                {
                    currentNode = currentPath[currentPathIndex];
                    currentPathIndex++;

                    Plugin.Log.LogError($"Set next node to: {currentNode.name}");

                    // the end of the path
                    if (currentPathIndex >= currentPath.Count)
                    {
                        catLookAt.SetLookTarget(null);
                        Plugin.Log.LogInfo("Reached final destination!");
                    }
                }
            }
        }

        void LateUpdate()
        {
            if (currentPath == null)
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

        float prevDistToDest = 0f;

        void Locomotion()
        {
            Node targetNode = currentPath[currentPathIndex];
            Vector3 targetPosition = targetNode.position;

            if (animator.GetBool("JumpingUp"))
            {
                HandleJumpingUp();
            }
            else if (animator.GetBool("JumpingDown"))
            {
                HandleJumpingDown();
            }
            else if (animator.GetBool("JumpingForward"))
            {
                HandleJumpingForward();
            }
            else if (animator.GetCurrentAnimatorStateInfo(0).IsName("JumpUpEnd") && animator.GetCurrentAnimatorStateInfo(0).normalizedTime < 0.45f)
            {
                // this condition scope is purposely empty to not let the ground stick logic affect the root motion of this clip, in the first half
            }
            else
            {
                Vector3 directionToTarget = (targetPosition - transform.position).normalized;
                directionToTarget.y = 0f;

                float angleToTarget = Vector3.SignedAngle(transform.forward, directionToTarget, Vector3.up);
                float turn = Mathf.Lerp(animator.GetFloat("Turn"), Mathf.Clamp(angleToTarget / 45f, -1f, 1f), Time.deltaTime * 5f);

                // turn in place logic
                float distToDest = Vector3.Distance(transform.position, targetPosition);
                float targetThrust = prevDistToDest < distToDest ? 0f : 1f;

                if ((targetNode.forwardJump && distToDest > 0.4f) || // jump to
                    (currentPathIndex > 0 && currentPath[currentPathIndex - 1].forwardJump)) // jump from
                {
                    if (Mathf.Abs(angleToTarget) > 7f) // wait to face the direction
                    {
                        targetThrust = 0f;
                    }
                    else // then start the jump up
                    {
                        prevDistToDest = float.MaxValue;
                        animator.SetBool("JumpingForward", true);
                    }
                }
                else if (targetPosition.y > transform.position.y + 0.4f) // means we need to initiate jump up
                {
                    if (Mathf.Abs(angleToTarget) > 10f) // wait to face the direction
                    {
                        targetThrust = 0f;
                    }
                    else // then start the jump up
                    {
                        animator.SetBool("JumpingUp", true);
                    }
                }
                else if (targetPosition.y < transform.position.y - 0.4f) // means we need to initiate jump down
                {
                    if (Mathf.Abs(angleToTarget) > 10f) // wait to face the direction
                    {
                        targetThrust = 0f;
                    }
                    else // then start the jump down
                    {
                        animator.SetBool("JumpingDown", true);
                    }
                }
                else
                {
                    // evil hack to keep the cat on the ground
                    transform.SetPositionIndividualAxis(y: Mathf.Lerp(transform.position.y, targetPosition.y, Time.deltaTime * 3f));
                }

                float thrust = Mathf.Lerp(animator.GetFloat("Thrust"), targetThrust, Time.deltaTime * 3f);
                animator.SetFloat("Thrust", thrust);
                animator.SetFloat("Turn", turn);
                prevDistToDest = distToDest;
            }
        }

        private void HandleJumpingForward()
        {
            Vector3 targetPosition = currentPath[currentPathIndex].position;

            AnimatorStateInfo curState = animator.GetCurrentAnimatorStateInfo(0);
            if (curState.IsName("JumpForwardStart") || curState.IsName("JumpForwardAir"))
            {
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

            if (animator.GetCurrentAnimatorStateInfo(0).IsName("JumpUpAir"))
            {
                float timeInAir = animator.GetCurrentAnimatorStateInfo(0).normalizedTime;

                float upSpeed = Time.deltaTime * 3f;
                float horizontalSpeed = Time.deltaTime;

                // slowing down as time goes, but with hard min limit, the cat still has to go up no matter what
                upSpeed -= Mathf.Sin((Mathf.Clamp01(timeInAir) * Mathf.PI) / 2f) * Time.deltaTime * 2f;
                upSpeed = Mathf.Max(upSpeed, Time.deltaTime / 4f);

                transform.position += new Vector3(0, upSpeed, 0);
                transform.position += transform.forward * horizontalSpeed;
            }

            if (transform.position.y > targetPosition.y - 0.45f)
            {
                transform.SetPositionIndividualAxis(y: targetPosition.y - 0.45f);
                animator.SetBool("JumpingUp", false);
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