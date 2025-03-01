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

        void Start()
        {
            animator = GetComponent<Animator>();
            lookAt = gameObject.GetOrAddComponent<CatLookAt>();
            catGraphTraverser = gameObject.GetOrAddComponent<CatGraphTraverser>();

            transform.position = new Vector3(4.837f, 0f, -2.884f);
            HideUnwantedSceneObjects();
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
            SetCurrentSelectedArea(currentArea, true);
        }

        void FixedUpdate()
        {
            animator.SetFloat("Random", Random.value); // used for different variants of fidgeting
            if (Random.value < 0.00166f) // on avg every 10 seconds @ 60 calls per sec (1/600)
            {
                //animator.SetTrigger("Fidget");
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
            animator.SetFloat("Thrust", 0);
            animator.SetFloat("Turn", 0);
        }

        public void SetCurrentSelectedArea(AreaData area, bool force = false)
        {
            return;

            if (!OnAreaUpgradeInstalledUnsubscribeActions.ContainsKey(area))
                // could not find a better place to hook into the area upgrade install event, seems it's individual area based, it'd be simpler if there was a hideout-wide event 
                OnAreaUpgradeInstalledUnsubscribeActions[area] = area.LevelUpdated.Subscribe(new System.Action(OnAreaUpdated)); // I'm 90% sure this is a valid way to use BindableEvent

            if (!force && area == currentArea)
            {
                return;
            }
            currentArea = area;

            CatAreaAction[] actionVariants = Plugin.CatConfig
                .Where(action =>
                action.Area == area.Template.Type &&
                action.AreaLevel == area.CurrentLevel)
                .ToArray();
            if (actionVariants.Length == 0)
                return;

            ResetAnimatorParameters();
            animator.SetBool("Sleeping", Random.value < 0.3f);

            CatAreaAction selectedAction = ChooseActionFromChance(actionVariants);

            Plugin.Log.LogInfo($"Playing in {selectedAction.Area} (level {selectedAction.AreaLevel})");
            foreach (var parameter in selectedAction.Parameters)
            {
                switch (parameter.Type)
                {
                    case AnimatorControllerParameterType.Bool:
                        animator.SetBool(parameter.Name, parameter.BoolValue);
                        break;
                    case AnimatorControllerParameterType.Float:
                        animator.SetFloat(parameter.Name, parameter.FloatValue);
                        break;
                    case AnimatorControllerParameterType.Int:
                        animator.SetInteger(parameter.Name, parameter.IntValue);
                        break;
                    case AnimatorControllerParameterType.Trigger:
                        animator.SetTrigger(parameter.Name);
                        break;
                    default:
                        break;
                }
                Plugin.Log.LogInfo($"Setting animator parameter {parameter.Name}");
            }

            transform.position = selectedAction.TransformPosition;
            transform.eulerAngles = selectedAction.TransformRotation;

            string instantStateOverride = "Idle";
            if (animator.GetBool("Sitting"))
                instantStateOverride = "Sit";
            else if (animator.GetBool("LyingSide"))
                instantStateOverride = animator.GetBool("Sleeping") ? "LieSideSleep" : "LieSide";
            else if (animator.GetBool("LyingBelly"))
                instantStateOverride = animator.GetBool("Sleeping") ? "LieBellySleep" : "LieBelly";

            animator.Play(instantStateOverride, 0, 0);
            animator.Update(0f);
        }

        private CatAreaAction ChooseActionFromChance(CatAreaAction[] actions)
        {
            if (actions.Length == 1)
                return actions[0];

            float totalChance = actions.Sum(action => action.Chance);
            float randomValue = Random.Range(0f, totalChance);
            float cumulativeChance = 0;

            foreach (var action in actions)
            {
                cumulativeChance += action.Chance;
                if (randomValue <= cumulativeChance)
                {
                    return action;
                }
            }

            // should not reach here
            return actions[actions.Length - 1];
        }
    }
}
