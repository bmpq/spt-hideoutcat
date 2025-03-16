﻿using BepInEx;
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
        CatEyelids eyelids;
        CatPupils pupils;
        CatAudio audio;

        CatGraphTraverser catGraphTraverser;

        private CatState _currentState = CatState.Idle;

        private enum CatState
        {
            Idle,
            Moving,
            Sitting,
            LyingSide,
            LyingBelly,
            Sleeping,
            Eating,
            Defecating,
            Scratching,
            WaitingByDoor,
            Grooming
        }

        private CatState _prevState;

        GamePlayerOwner owner;

        Door doorGym;

        float meowCooldown;

        void OnEnable()
        {
            Singleton<HideoutClass>.Instance.OnAreaUpdated += OnAreaUpdated;
            PatchAreaSelected.OnAreaSelected += SetTargetArea;
            PatchPlayerPrepareWorkout.OnPlayerPrepareWorkout += OnPlayerPrepareWorkout;
            PatchPlayerStopWorkout.OnPlayerStopWorkout += GoToRandomArea;
        }

        void OnDisable()
        {
            Singleton<HideoutClass>.Instance.OnAreaUpdated -= OnAreaUpdated;
            PatchAreaSelected.OnAreaSelected -= SetTargetArea;
            PatchPlayerPrepareWorkout.OnPlayerPrepareWorkout -= OnPlayerPrepareWorkout;
            PatchPlayerStopWorkout.OnPlayerStopWorkout -= GoToRandomArea;
        }

        void Start()
        {
            animator = GetComponent<Animator>();
            lookAt = gameObject.GetOrAddComponent<CatLookAt>();
            eyelids = gameObject.GetOrAddComponent<CatEyelids>();
            pupils = gameObject.GetOrAddComponent<CatPupils>();
            catGraphTraverser = gameObject.GetOrAddComponent<CatGraphTraverser>();
            catGraphTraverser.OnDestinationReached += OnDestinationReached;
            catGraphTraverser.OnNodeReached += OnNodeReached;

            audio = gameObject.GetOrAddComponent<CatAudio>();

            SphereCollider interactiveCollider = new GameObject("InteractiveCollider").AddComponent<SphereCollider>();
            interactiveCollider.radius = 0.4f;
            interactiveCollider.center = new Vector3(0, 0.15f, 0);
            interactiveCollider.gameObject.layer = 22; // Interactive
            interactiveCollider.transform.SetParent(transform, false);

            owner = Singleton<GameWorld>.Instance.MainPlayer.GetComponent<GamePlayerOwner>();
            doorGym = FindObjectsByType<Door>(FindObjectsSortMode.None).Where(door => door.Id == "door_bunker_2_00002").FirstOrDefault();
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

            Plugin.Log.LogInfo($"Set destination node to: {node.name}");

            catGraphTraverser.LayNewPath(node);
        }

        private void OnNodeReached(List<Node> nodesLeft)
        {
            if (nodesLeft.Count > 0)
            {
                lookAt.LookAt(nodesLeft[Mathf.Min(1, nodesLeft.Count - 1)].position + new Vector3(0, 0.3f, 0));
            }
        }

        private void OnDestinationReached(Node node)
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

        private void OnPlayerPrepareWorkout()
        {
            if (doorGym.DoorState != EDoorState.Open)
                return;

            Node[] nodes = Plugin.CatGraph.nodes.Where(n => 
                n.areaType == EAreaType.Gym && 
                n.areaLevel == 1 && 
                n.poseParameters.Count > 0 &&
                n.poseParameters[0].Name == "Grooming").ToArray();

            if (nodes == null || nodes.Length == 0)
                return;
            Node node = nodes[Random.Range(0, nodes.Length)];

            lookAt.SetLookTarget(null);
            catGraphTraverser.ForgetDestination();
            transform.position = node.position;
            transform.eulerAngles = new Vector3(0, node.poseRotation, 0);
            ResetAnimatorParameters();
            animator.Play("Idle", 0);
            animator.SetBool("Grooming", true);
            animator.Update(0);
        }

        public bool IsSleeping()
        {
            return _currentState == CatState.Sleeping;
        }

        public void WakeUp()
        {
            animator.SetBool("Sleeping", false); 
            Fidget();

            owner.InteractionsChangedHandler();

            audio.Meow(CatAudio.MeowType.Short);
        }

        public void Pet()
        {
            animator.SetTrigger("Caress");
            animator.Update(0);

            owner.InteractionsChangedHandler();

            audio.Purr();
        }

        public bool IsPettable()
        {
            AnimatorStateInfo curState = animator.GetCurrentAnimatorStateInfo(0);

            if (animator.IsInTransition(0))
                return false;

            // already petting
            if (curState.IsTag("Caress"))
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

        public bool IsFidgeting()
        {
            animator.Update(0);
            return animator.GetCurrentAnimatorStateInfo(0).IsTag("Fidget");
        }

        public void Meow()
        {
            if (IsBusy()) return;

            if (meowCooldown > 0f)
                return;
            meowCooldown = 1f;

            animator.SetTrigger("Meow");

            if (DistanceToPlayer() < 5f)
                audio.Meow(CatAudio.MeowType.Address);
            else
                audio.Meow(CatAudio.MeowType.Far);
        }

        public void Fidget()
        {
            if (IsFidgeting())
                return;

            lookAt.SetLookTarget(null);
            animator.SetTrigger("Fidget");
            animator.Update(0);

            owner.InteractionsChangedHandler();
        }

        void FixedUpdate()
        {
            meowCooldown -= Time.fixedDeltaTime;

            animator.SetFloat("Random", Random.value); // used for different variants of fidgeting

            UpdateState(); // Update the _currentState based on the Animator (mirror)
            HandleBehavior(); // Handle behavior based on _currentState

            HandlePlayerInteraction();

            if (_prevState != _currentState)
            {
                Plugin.Log.LogInfo($"New state: {_currentState}");

                owner.InteractionsChangedHandler();
            }
            _prevState = _currentState;
        }

        private void UpdateState()
        {
            AnimatorStateInfo curState = animator.GetCurrentAnimatorStateInfo(0);

            if (catGraphTraverser.HasDestination)
            {
                _currentState = CatState.Moving;
                return;
            }
            if (animator.GetBool("Sleeping"))
            {
                _currentState = CatState.Sleeping;
                return;
            }
            if (animator.GetBool("Eating"))
            {
                _currentState = CatState.Eating;
                return;
            }
            if (animator.GetBool("Defecating"))
            {
                _currentState = CatState.Defecating;
                return;
            }
            if (animator.GetBool("LyingSide"))
            {
                _currentState = CatState.LyingSide;
                return;
            }
            if (animator.GetBool("LyingBelly"))
            {
                _currentState = CatState.LyingBelly;
                return;
            }
            if (animator.GetBool("Sitting"))
            {
                _currentState = CatState.Sitting;
                return;
            }
            if (animator.GetBool("ScratchingHorizontal") || animator.GetBool("ScratchingVertical"))
            {
                _currentState = CatState.Scratching;
                return;
            }
            if (animator.GetBool("Grooming"))
            {
                _currentState = CatState.Grooming;
                return;
            }

            _currentState = CatState.Idle;
        }

        private void HandleBehavior()
        {
            switch (_currentState)
            {
                case CatState.Idle:
                    HandleIdleState();
                    break;
                case CatState.Moving:
                    HandleMovingState();
                    break;
                case CatState.WaitingByDoor:
                    HandleWaitingByDoor();
                    break;
                case CatState.Sitting:
                    HandleSittingState();
                    break;
                case CatState.LyingSide:
                case CatState.LyingBelly:
                    HandleLyingState();
                    break;
                case CatState.Sleeping:
                    HandleSleepingState();
                    break;
                case CatState.Eating:
                    HandleEatingState();
                    break;
                case CatState.Defecating:
                    HandleDefecatingState();
                    break;
                case CatState.Scratching:
                    HandleScratchingState();
                    break;
                case CatState.Grooming:
                    HandleGroomingState();
                    break;
            }
        }

        private void HandleMovingState()
        {
            if (catGraphTraverser.doorInTheWay != null)
            {
                _currentState = CatState.WaitingByDoor;
            }
            else
            {
                ResetAnimatorParameters();
            }
        }

        private void HandleIdleState()
        {
            if (UnityExtensions.RandomShouldOccur(10f))
            {
                Fidget();
            }

            if (UnityExtensions.RandomShouldOccur(3f))
            {
                animator.SetBool("Sitting", true);
            }

            if (UnityExtensions.RandomShouldOccur(10f))
                GoToRandomArea();
        }

        private void HandleWaitingByDoor()
        {
            animator.SetBool("Sitting", true);
            lookAt.SetLookTarget(catGraphTraverser.doorInTheWay.transform.parent);

            if (UnityExtensions.RandomShouldOccur(3f))
                Meow();

            if (UnityExtensions.RandomShouldOccur(20f))
                GoToRandomArea();
        }

        private void HandleSittingState()
        {
            if (UnityExtensions.RandomShouldOccur(20f))
                Fidget();
            else if (UnityExtensions.RandomShouldOccur(3f) && !IsFidgeting())
                lookAt.SetLookAtPlayer();
            else if (UnityExtensions.RandomShouldOccur(20f))
                GoToRandomArea();
        }

        void OnGUI()
        {
            IMGUIDebugDraw.Draw.Label(GetPlayerCam().GetComponent<Camera>(), transform.position, _currentState.ToString());
        }

        private void HandleLyingState()
        {
            if (UnityExtensions.RandomShouldOccur(60f))
            {
                animator.SetBool("Sleeping", true);
                lookAt.SetLookTarget(null);
            }
            else if (UnityExtensions.RandomShouldOccur(20f))
            {
                GoToRandomArea();
            }
            else if (UnityExtensions.RandomShouldOccur(20f))
            {
                Fidget();
            }
            else if (UnityExtensions.RandomShouldOccur(5f) && !IsFidgeting())
            {
                lookAt.SetLookAtPlayer();
            }
        }

        private void HandleSleepingState()
        {
            if (UnityExtensions.RandomShouldOccur(40f))
            {
                animator.SetBool("Sleeping", false);
                Fidget();
            }
        }

        private void HandleEatingState()
        {
            if (UnityExtensions.RandomShouldOccur(15f))
                GoToClosestWaypoint();
        }

        private void HandleDefecatingState()
        {
            if (UnityExtensions.RandomShouldOccur(15f))
                GoToClosestWaypoint();
        }

        private void HandleGroomingState()
        {
            if (UnityExtensions.RandomShouldOccur(35f))
                GoToClosestWaypoint();
        }

        private void HandleScratchingState()
        {
            if (UnityExtensions.RandomShouldOccur(15f))
                GoToClosestWaypoint();
        }

        float DistanceToPlayer()
        {
            return Vector3.Distance(transform.position, GetPlayerCam().position);
        }

        void HandlePlayerInteraction()
        {
            bool playerNearby = DistanceToPlayer() < 3f;
            bool lookingAtPlayer = lookAt.IsLookingAtPlayer();

            if (lookingAtPlayer && playerNearby)
            {
                if (UnityExtensions.RandomShouldOccur(5f))
                    Meow();
            }

            if (UnityExtensions.RandomShouldOccur(65f))
                Meow();

            if (IsPlayerShiningFlashlightAtFace())
            {
                eyelids.SetClamp(0.5f);
                pupils.SetDilation(0f);
            }
            else
            {
                if (eyelids.mode != CatEyelids.Mode.None)
                    eyelids.Release();

                pupils.SetDilation(lookingAtPlayer ? 0.6f : 0.4f);
            }

            bool playerInTheWay = IsPlayerInTheWay();
            if (playerInTheWay && animator.GetCurrentAnimatorStateInfo(0).IsName("Movement"))
            {
                catGraphTraverser.ForgetDestination();

                if (!IsFidgeting())
                { 
                    lookAt.SetLookAtPlayer();
                }

                if (UnityExtensions.RandomShouldOccur(4f))
                    animator.SetBool("Sitting", true);
            }
        }

        bool IsPlayerShiningFlashlightAtFace()
        {
            if (CameraClass.Instance == null || CameraClass.Instance.Flashlight == null)
                return false;
            return CameraClass.Instance.Flashlight.IsActive;
        }

        bool IsPlayerInTheWay()
        {
            if (Singleton<GameWorld>.Instance.MainPlayer.PointOfView != EPointOfView.FirstPerson)
                return false;

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
            animator.SetBool("SharpenHorizontal", false);
            animator.SetBool("SharpenVertical", false);
            animator.SetBool("Grooming", false);
            animator.SetBool("RunningInCircles", false);
            animator.ResetTrigger("Fidget");
            animator.Update(0);
        }

        bool IsBusy()
        {
            return 
                _currentState == CatState.Sleeping || 
                _currentState == CatState.Eating || 
                _currentState == CatState.Defecating;
        }

        void GoToRandomArea()
        {
            if (IsPlayerInTheWay())
                return;

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
