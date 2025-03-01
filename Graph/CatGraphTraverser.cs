using System.Collections.Generic;
using UnityEngine;
using tarkin;

namespace hideoutcat.Pathfinding
{
    public class CatGraphTraverser : MonoBehaviour
    {
        public string[] testTargetNodes = { "restspace3_bed_center", "724d104c", "watercol3_top_yeah" };
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

            LayNewPath();
        }

        void LayNewPath()
        {
            currentNode = pathfindingGraph.GetNodeClosestNoPathfinding(transform.position);

            Node target = null;
            if (testTargetNodes.Length > 0)
            {
                if (testTargetNodeIndex >= testTargetNodes.Length)
                    testTargetNodeIndex = 0;
                target = pathfindingGraph.FindNodeById(testTargetNodes[testTargetNodeIndex]);
                testTargetNodeIndex++;
            }
            if (target == null)
                target = pathfindingGraph.nodes[Random.Range(0, pathfindingGraph.nodes.Count)];

            currentPath = pathfindingGraph.FindPathBFS(currentNode, target);
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
                LayNewPath();
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

                if (Vector3.Distance(transform.position, currentPath[currentPathIndex].position) < 0.2f)
                {
                    currentNode = currentPath[currentPathIndex];
                    currentPathIndex++;

                    Plugin.Log.LogError($"Set target node to: {currentNode.name}");

                    // the end of the path
                    if (currentPathIndex >= currentPath.Count)
                    {
                        catLookAt.SetLookTarget(null);
                        Plugin.Log.LogInfo("Reached final destination!");

                        Invoke("LayNewPath", 2f);
                    }
                }
            }
        }

        void LateUpdate()
        {
            if (currentPathIndex < currentPath.Count)
            {
                Locomotion();
            }
        }

        float prevDistToDest = 0f;

        void Locomotion()
        {
            Vector3 targetPosition = currentPath[currentPathIndex].position;

            Vector3 directionToTarget = (targetPosition - transform.position).normalized;
            directionToTarget.y = 0f;

            float angleToTarget = Vector3.SignedAngle(transform.forward, directionToTarget, Vector3.up);
            float turn = Mathf.Lerp(animator.GetFloat("Turn"), Mathf.Clamp(angleToTarget / 45f, -1f, 1f), Time.deltaTime * 5f);

            // turn in place logic
            float distToDest = Vector3.Distance(transform.position, targetPosition);
            float targetThrust = prevDistToDest < distToDest ? 0f : 1f;


            if (animator.GetBool("JumpingUp"))
            {
                HandleJumpingUp();
            }
            else if (animator.GetBool("JumpingDown"))
            {
                HandleJumpingDown();
            }
            else if (animator.GetCurrentAnimatorStateInfo(0).IsName("JumpUpAir")) // if (JumpingUp == false) but still on this state, means we are in transition to end
            {
                // transitioning from script controlled root motion to clip controlled
                transform.position += new Vector3(0, Time.deltaTime * (1f - animator.GetAnimatorTransitionInfo(0).normalizedTime) * 3f, 0);
            }
            else if (animator.GetCurrentAnimatorStateInfo(0).IsName("JumpUpEnd"))
            {
                // jump_up_end clip expects 0.5f offset, but we cannot provide exact 0.5 offset (because of transition blend) so we do this hack
                // we can just skip the blend, but I don't like how it looks, so I choose to suffer
                if (animator.GetCurrentAnimatorStateInfo(0).normalizedTime > 0.45f) // the point where the clip stops affecting root position
                    transform.SetPositionIndividualAxis(y: Mathf.Lerp(transform.position.y, targetPosition.y, Time.deltaTime * 10f));
            }
            else
            {
                if (targetPosition.y > transform.position.y + 0.4f) // means we need to initiate jump up
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

                float thrust = Mathf.Lerp(animator.GetFloat("Thrust"), targetThrust, Time.deltaTime * 3f);
                animator.SetFloat("Thrust", thrust);
                animator.SetFloat("Turn", turn);
                prevDistToDest = distToDest;
            }
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

            if (transform.position.y >= targetPosition.y - 0.57f) // the jump_up_end clip expects 0.5f offset (it has root motion) but we end a little sooner because transition blend
            {
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

                transform.position += new Vector3(0, -fallingSpeed, 0);

                transform.position += transform.forward * horizontalSpeed;
            }

            if (transform.position.y < targetPosition.y + 0.02f)
            {
                transform.SetPositionIndividualAxis(y: targetPosition.y);
                animator.SetBool("JumpingDown", false);
            }
        }
    }
}