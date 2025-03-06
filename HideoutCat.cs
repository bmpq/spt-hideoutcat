using BepInEx;
using Comfort.Common;
using EFT;
using EFT.Hideout;
using hideoutcat.Pathfinding;
using System.Collections.Generic;
using System.Linq;
using tarkin;
using UnityEngine;

namespace hideoutcat
{
    public class HideoutCat : MonoBehaviourSingleton<HideoutCat>
    {
        Animator animator;
        AreaData currentArea;

        Dictionary<AreaData, System.Action> OnAreaUpgradeInstalledUnsubscribeActions = new Dictionary<AreaData, System.Action>();

        CatLookAt lookAt;

        CatGraphTraverser catGraphTraverser;

        float timeLying;
        float timeSleeping;

        void Start()
        {
            animator = GetComponent<Animator>();
            lookAt = gameObject.GetOrAddComponent<CatLookAt>();
            catGraphTraverser = gameObject.GetOrAddComponent<CatGraphTraverser>();
            catGraphTraverser.OnDestinationReached += CatGraphTraverser_OnDestinationReached;
            catGraphTraverser.OnNodeReached += CatGraphTraverser_OnNodeReached;

            HideUnwantedSceneObjects();
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

        public override void OnDestroy()
        {
            base.OnDestroy();

            // half the time BSG doesn't bother with unsubscribing from BindableEvent? or maybe I'm looking wrong and they unsubscribe somewhere sneaky
            foreach (var kvp in OnAreaUpgradeInstalledUnsubscribeActions)
            {
                kvp.Value.Invoke();
            }
        }

        void HideUnwantedSceneObjects()
        {
            // heating 1
            UnityExtensions.FindGameObjectWithComponentAtPosition<LODGroup>(new Vector3(14.38238f, 0.5160349f, -5.618773f))?.SetActive(false); // books_01 (1)

            // heating 2
            UnityExtensions.FindGameObjectWithComponentAtPosition<LODGroup>(new Vector3(14.20716f, 0.5158756f, -5.420396f))?.SetActive(false); // books_01 (2)

            // heating 3
            UnityExtensions.FindGameObjectWithComponentAtPosition<LODGroup>(new Vector3(15.85126f, 0.5397013f, -4.845883f))?.SetActive(false); // paper3 (1)
            UnityExtensions.FindGameObjectWithComponentAtPosition<LODGroup>(new Vector3(15.84810f, 0.5374010f, -5.039324f))?.SetActive(false); // paper3 (2)
            UnityExtensions.FindGameObjectWithComponentAtPosition<LODGroup>(new Vector3(15.97384f, 0.5497416f, -4.821522f))?.SetActive(false); // Firewood_4 (7)
            UnityExtensions.FindGameObjectWithComponentAtPosition<LODGroup>(new Vector3(16.07953f, 0.5244959f, -4.975954f))?.SetActive(false); // Firewood_4 (6)
        }

        private void OnAreaUpdated()
        {
            HideUnwantedSceneObjects();
            ResetPositionToClosestWaypoint();
            SetCurrentSelectedArea(currentArea, true);
        }

        void ResetPositionToClosestWaypoint()
        {
            transform.position = Plugin.CatGraph.GetNodeClosestWaypoint(transform.position).position;
        }

        void FixedUpdate()
        {
            animator.SetFloat("Random", Random.value); // used for different variants of fidgeting

            bool stationary = catGraphTraverser.Velocity.magnitude < 0.1f;

            if (stationary)
            {
                if (UnityExtensions.RandomShouldOccur(10f, Time.fixedDeltaTime))
                {
                    animator.SetTrigger("Fidget");
                }

                if (animator.GetBool("LyingSide") || animator.GetBool("LyingBelly"))
                {
                    timeLying += Time.fixedDeltaTime;

                    if (animator.GetBool("Sleeping"))
                    {
                        timeSleeping += Time.fixedDeltaTime;
                        if (UnityExtensions.RandomShouldOccur(50f, Time.fixedDeltaTime))
                        {
                            animator.SetBool("Sleeping", false);
                            animator.SetTrigger("Fidget");
                        }
                    }
                    else if (UnityExtensions.RandomShouldOccur(20f, Time.fixedDeltaTime))
                    {
                        animator.SetBool("Sleeping", true);
                        timeSleeping = 0;
                    }

                    AnimatorStateInfo curState = animator.GetCurrentAnimatorStateInfo(0);
                    if (curState.IsName("LieBelly") || curState.IsName("LieSide")) // not fidgeting
                    {
                        if (UnityExtensions.RandomShouldOccur(5f, Time.fixedDeltaTime))
                        {
                            lookAt.SetLookAtPlayer();
                        }
                    }
                }
                else
                {
                    timeLying = 0f;
                }
            }
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

        public void SetCurrentSelectedArea(AreaData area, bool force = false)
        {
            if (!OnAreaUpgradeInstalledUnsubscribeActions.ContainsKey(area))
                // could not find a better place to hook into the area upgrade install event, seems it's individual area based, it'd be simpler if there was a hideout-wide event 
                OnAreaUpgradeInstalledUnsubscribeActions[area] = area.AreaUpgraded.Subscribe(new System.Action(OnAreaUpdated)); // I'm 90% sure this is a valid way to use BindableEvent

            if (!force && area == currentArea)
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
