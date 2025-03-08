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
        AreaData currentTargetArea;

        CatLookAt lookAt;

        CatGraphTraverser catGraphTraverser;

        void OnEnable()
        {
            Singleton<HideoutClass>.Instance.OnAreaUpdated += OnAreaUpdated;
            PatchAreaSelected.OnAreaSelected += SetTargetArea;
        }

        void OnDisable()
        {
            Singleton<HideoutClass>.Instance.OnAreaUpdated -= OnAreaUpdated;
            PatchAreaSelected.OnAreaSelected -= SetTargetArea;
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
            TeleportToClosestWaypoint();
            StartTraversingToArea(currentTargetArea);
        }

        void TeleportToClosestWaypoint()
        {
            ResetAnimatorParameters();
            transform.position = Plugin.CatGraph.GetNodeClosestWaypoint(transform.position).position;
        }

        void GoToClosestWaypoint()
        {
            ResetAnimatorParameters();
            catGraphTraverser.LayNewPath(Plugin.CatGraph.GetNodeClosestWaypoint(transform.position));
        }

        public void Pet()
        {
            animator.SetTrigger("Caress");
        }

        public bool IsPettable()
        {
            AnimatorStateInfo curState = animator.GetCurrentAnimatorStateInfo(0);

            // already petting
            if (animator.GetNextAnimatorStateInfo(0).IsTag("Caress") || curState.IsTag("Caress"))
                return false;

            // the only states we have fitting animations for

            if (curState.IsName("Sit"))
                return true;

            if (curState.IsName("LieBelly"))
                return true;

            if (curState.IsName("Idle"))
                return true;

            return false;
        }

        void FixedUpdate()
        {
            animator.SetFloat("Random", Random.value); // used for different variants of fidgeting

            bool hasDestination = catGraphTraverser.currentPath != null;

            bool playerInTheWay = IsPlayerInTheWay();
            catGraphTraverser.pathBlocked = playerInTheWay;

            if (playerInTheWay && hasDestination)
            {
                catGraphTraverser.ForgetDestination();
                lookAt.SetLookAtPlayer();
            }

            // velocity is calculated in LateUpdate() there, so we divide by deltaTime
            bool stationary = catGraphTraverser.Velocity.magnitude / Time.deltaTime < 0.1f;

            if (hasDestination)
            {
                if (playerInTheWay)
                {
                    lookAt.SetLookAtPlayer();
                    if (UnityExtensions.RandomShouldOccur(4f, Time.fixedDeltaTime))
                        animator.SetBool("Sitting", true);
                }
                else
                {
                    animator.SetBool("Sitting", false);
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
                    if (animator.GetBool("Sleeping"))
                    {
                        if (UnityExtensions.RandomShouldOccur(60f, Time.fixedDeltaTime))
                        {
                            animator.SetBool("Sleeping", false);
                            animator.SetTrigger("Fidget");
                        }
                    }
                    else if (UnityExtensions.RandomShouldOccur(30f, Time.fixedDeltaTime))
                    {
                        animator.SetBool("Sleeping", true);
                        lookAt.SetLookTarget(null);
                    }
                }
                else if (curState.IsName("Idle"))
                {
                    if (UnityExtensions.RandomShouldOccur(3f, Time.fixedDeltaTime))
                    {
                        animator.SetBool("Sitting", true);
                    }
                }
                else if (curState.IsName("Eating"))
                {
                    if (UnityExtensions.RandomShouldOccur(15f, Time.fixedDeltaTime))
                    {
                        GoToClosestWaypoint();
                    }
                }
                else if (animator.GetBool("Sitting"))
                {
                    if (UnityExtensions.RandomShouldOccur(30f, Time.fixedDeltaTime))
                    {
                        GoToRandomArea();
                    }

                    if (UnityExtensions.RandomShouldOccur(5f, Time.fixedDeltaTime))
                    {
                        lookAt.SetLookAtPlayer();
                    }
                }
                else if (animator.GetBool("Defecating"))
                {
                    if (UnityExtensions.RandomShouldOccur(20f, Time.fixedDeltaTime))
                    {
                        animator.SetBool("Defecating", false);
                        GoToClosestWaypoint();
                    }
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
        }

        bool IsBusy()
        {
            if (animator.GetBool("Sleeping"))
                return true;

            if (animator.GetBool("Eating"))
                return true;

            if (animator.GetBool("Defecating"))
                return true;

            return false;
        }

        void GoToRandomArea()
        {
            var areas = Singleton<HideoutClass>.Instance.AreaDatas.OrderBy(x => Random.value);

            foreach (var area in areas)
            {
                var nodes = Plugin.CatGraph.FindDeadEndNodesByAreaTypeAndLevel(area.Template.Type, area.CurrentLevel);
                if (nodes.Count == 0)
                    continue;

                SetTargetArea(area);

                break;
            }
        }

        public void SetTargetArea(AreaData area)
        {
            if (area == currentTargetArea)
                return;

            if (IsBusy())
                return;

            StartTraversingToArea(area);
        }

        void StartTraversingToArea(AreaData area)
        {
            currentTargetArea = area;

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
