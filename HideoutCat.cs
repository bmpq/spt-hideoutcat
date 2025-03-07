using BepInEx;
using Comfort.Common;
using EFT;
using EFT.Hideout;
using EFT.Interactive;
using hideoutcat.Pathfinding;
using System.Collections.Generic;
using System.Linq;
using tarkin;
using UnityEngine;

namespace hideoutcat
{
    public class HideoutCat : InteractableObject
    {
        Animator animator;
        AreaData currentArea;

        CatLookAt lookAt;

        CatGraphTraverser catGraphTraverser;

        float timeLying;
        float timeSleeping;

        public bool pettable { get; private set; }

        void OnEnable()
        {
            Singleton<HideoutClass>.Instance.OnAreaUpdated += OnAreaUpdated;
            PatchAreaSelected.OnAreaSelected += SetCurrentSelectedArea;
        }

        void OnDisable()
        {
            Singleton<HideoutClass>.Instance.OnAreaUpdated -= OnAreaUpdated;
            PatchAreaSelected.OnAreaSelected -= SetCurrentSelectedArea;
        }

        void Start()
        {
            animator = GetComponent<Animator>();
            lookAt = gameObject.GetOrAddComponent<CatLookAt>();
            catGraphTraverser = gameObject.GetOrAddComponent<CatGraphTraverser>();
            catGraphTraverser.OnDestinationReached += CatGraphTraverser_OnDestinationReached;
            catGraphTraverser.OnNodeReached += CatGraphTraverser_OnNodeReached;

            SphereCollider interactiveCollider = new GameObject("InteractiveCollider").AddComponent<SphereCollider>();
            interactiveCollider.radius = 0.3f;
            interactiveCollider.center = new Vector3(0, 0.15f, 0);
            interactiveCollider.gameObject.layer = 22; // Interactive
            interactiveCollider.transform.SetParent(transform, false);
        }

        Camera playerCam;

        Transform GetPlayerCam()
        {
            if (playerCam == null)
                playerCam = Camera.main;

            return playerCam.transform;
        }

        public void SetTargetNode(Node node)
        {
            if (catGraphTraverser == null)
                catGraphTraverser = gameObject.GetOrAddComponent<CatGraphTraverser>();

            Plugin.Log.LogDebug($"Set destination node to: {node.name}");

            catGraphTraverser.LayNewPath(node);
        }

        private void CatGraphTraverser_OnNodeReached(List<Node> nodesLeft)
        {
            if (nodesLeft.Count > 0)
            {
                lookAt.LookAt(nodesLeft[Mathf.Min(1, nodesLeft.Count - 1)].position + new Vector3(0, 0.3f, 0));
            }
        }

        private void CatGraphTraverser_OnDestinationReached(Node node)
        {
            foreach (var parameter in node.poseParameters)
            {
                parameter.Apply(animator);
            }

            lookAt.SetLookTarget(null);
        }

        private void OnAreaUpdated()
        {
            ResetPositionToClosestWaypoint();

            AreaData areaData = currentArea;
            currentArea = null; // force update
            SetCurrentSelectedArea(areaData);
        }

        void ResetPositionToClosestWaypoint()
        {
            transform.position = Plugin.CatGraph.GetNodeClosestWaypoint(transform.position).position;
        }

        public void Pet()
        {
            animator.SetTrigger("Caress");
        }

        void FixedUpdate()
        {
            pettable = false;

            animator.SetFloat("Random", Random.value); // used for different variants of fidgeting

            bool hasDestination = catGraphTraverser.currentPath != null;

            bool playerInTheWay = IsPlayerInTheWay();
            catGraphTraverser.pathBlocked = playerInTheWay;

            // velocity is calculated in LateUpdate() there, so we divide by deltaTime
            bool stationary = catGraphTraverser.Velocity.magnitude / Time.deltaTime < 0.1f;

            if (hasDestination)
            {
                if (playerInTheWay)
                {
                    lookAt.SetLookAtPlayer();
                    if (UnityExtensions.RandomShouldOccur(4f, Time.fixedDeltaTime))
                        animator.SetBool("Sitting", true);

                    if (stationary && animator.GetBool("Sitting"))
                        pettable = true;
                }
                else
                {
                    animator.SetBool("Sitting", false);

                    if (stationary && animator.GetCurrentAnimatorStateInfo(0).IsName("Idle"))
                    {
                        pettable = true;
                    }
                }
            }
            else if (stationary)
            {
                AnimatorStateInfo curState = animator.GetCurrentAnimatorStateInfo(0);

                if (UnityExtensions.RandomShouldOccur(20f, Time.fixedDeltaTime))
                {
                    animator.SetTrigger("Fidget");
                    lookAt.SetLookTarget(null);
                }

                bool lyingSide = animator.GetBool("LyingSide");
                bool lyingBelly = animator.GetBool("LyingBelly");
                if (lyingSide || lyingBelly)
                {
                    timeLying += Time.fixedDeltaTime;

                    if (animator.GetBool("Sleeping"))
                    {
                        timeSleeping += Time.fixedDeltaTime;
                        if (UnityExtensions.RandomShouldOccur(60f, Time.fixedDeltaTime))
                        {
                            animator.SetBool("Sleeping", false);
                            animator.SetTrigger("Fidget");
                        }
                    }
                    else if (UnityExtensions.RandomShouldOccur(30f, Time.fixedDeltaTime))
                    {
                        animator.SetBool("Sleeping", true);
                        timeSleeping = 0;
                        lookAt.SetLookTarget(null);
                    }
                    else if (lyingBelly && !curState.IsTag("Caress"))
                    {
                        pettable = true;
                    }
                }
                else
                {
                    timeLying = 0f;
                }

                if (!curState.IsTag("Fidget"))
                {
                    if (UnityExtensions.RandomShouldOccur(5f, Time.fixedDeltaTime))
                    {
                        lookAt.SetLookAtPlayer();
                    }

                    pettable = true;
                }
                else
                {
                    pettable = false;
                }
            }
        }

        bool IsPlayerInTheWay()
        {
            float distToPlayer = Vector3.Distance(GetPlayerCam().position, transform.position);
            Vector3 directionToTarget = (GetPlayerCam().position - transform.position).normalized;
            directionToTarget.y = 0f;
            float angleToPlayer = Vector3.SignedAngle(transform.forward, directionToTarget, Vector3.up);

            bool playerInTheWay = distToPlayer < 2f && Mathf.Abs(angleToPlayer) < 40f;

            return playerInTheWay;
        }

        void ResetAnimatorParameters()
        {
            animator.SetBool("Defecating", false);
            animator.SetBool("Sleeping", false);
            animator.SetBool("LyingSide", false);
            animator.SetBool("LyingBelly", false);
            animator.SetBool("Sitting", false);
            animator.SetBool("Crouching", false);
            animator.SetBool("Eating", false);
            animator.ResetTrigger("Fidget");

            animator.ResetTrigger("Jump");
        }

        public void SetCurrentSelectedArea(AreaData area)
        {
            if (area == currentArea)
            {
                return;
            }
            currentArea = area;

            List<Node> targetNodes = Plugin.CatGraph.FindDeadEndNodesByAreaTypeAndLevel(area.Template.Type, area.CurrentLevel);
            if (targetNodes.Count == 0)
            {
                Plugin.Log.LogInfo($"No available nodes for {area.Template.Type} level {area.CurrentLevel}");
                return;
            }

            ResetAnimatorParameters();
            Node targetNode = targetNodes[Random.Range(0, targetNodes.Count)];
            catGraphTraverser.LayNewPath(targetNode);
        }
    }
}
