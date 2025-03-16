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
        CatEyelids eyelids;
        CatPupils pupils;
        CatAudio audio;

        CatGraphTraverser catGraphTraverser;

        private enum CatState
        {
            Idle,
            Moving,
            Sitting,
            Lying,
            Sleeping,
            Eating,
            Defecating,
            Sharpening,
            WaitingByDoor,
            Grooming
        }

        bool lastLyingPoseBelly;
        
        private CatState _currentState = CatState.Idle;
        private CatState _prevState;

        float fidgetingTime;
        bool fidgeting => fidgetingTime > 0;

        GamePlayerOwner owner;

        Door doorGym;

        float meowCooldown;

        Camera playerCam;


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

            ResetAnimatorParameters();
        }

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
            if (nodesLeft.Count > 1)
            {
                lookAt.LookAt(nodesLeft[Mathf.Min(1, nodesLeft.Count - 1)].position + new Vector3(0, 0.3f, 0));
            }
            else
            {
                lookAt.SetLookTarget(null);
            }
        }

        private void OnDestinationReached(Node node)
        {
            foreach (var parameter in node.poseParameters)
            {
                parameter.Apply(animator);
            }

            lookAt.SetLookTarget(null);

            // the node dictates the animator parameters, so we work backwards in this case, setting the state from parsed parameters and not the other way around
            if (node.poseParameters.Any(p => p.Name == "Sitting"))
            {
                SetState(CatState.Sitting, false);
            }
            else if (node.poseParameters.Any(p => p.Name == "LyingSide") || node.poseParameters.Any(p => p.Name == "LyingBelly"))
            {
                lastLyingPoseBelly = node.poseParameters.Any(p => p.Name == "LyingBelly");

                SetState(CatState.Lying, false);
            }
            else if (node.poseParameters.Any(p => p.Name == "Eating"))
            {
                SetState(CatState.Eating, false);
            }
            else if (node.poseParameters.Any(p => p.Name == "Defecating"))
            {
                SetState(CatState.Defecating, false);
            }
            else if (node.poseParameters.Any(p => p.Name == "Grooming"))
            {
                SetState(CatState.Grooming, false);
            }
            else
            {
                SetState(CatState.Idle, false);
            }
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
            SetState(CatState.Idle, true);
        }

        void GoToClosestWaypoint()
        {
            ResetAnimatorParameters();
            SetTargetNode(Plugin.CatGraph.GetNodeClosestWaypoint(transform.position)); // GoToClosestWaypoint now sets the state
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

            SetState(CatState.Grooming, true);
        }

        public bool IsSleeping()
        {
            return _currentState == CatState.Sleeping;
        }

        public void WakeUp()
        {
            if (_currentState == CatState.Sleeping)
            {
                SetState(CatState.Lying, true);
                StartFidget();
                audio.Meow(CatAudio.MeowType.Short);
            }
        }

        public void Pet()
        {
            if (IsPettable())
            {
                animator.SetTrigger("Caress");
                audio.Purr();
            }
        }

        public bool IsPettable()
        {
            return _currentState == CatState.Idle ||
                   _currentState == CatState.Sitting ||
                   _currentState == CatState.Lying;
        }

        public void Meow()
        {
            if (IsBusy() || fidgeting) return;

            if (meowCooldown > 0f)
                return;
            meowCooldown = 2f;

            animator.SetTrigger("Meow");

            if (DistanceToPlayer() < 5f)
                audio.Meow(CatAudio.MeowType.Address);
            else
                audio.Meow(CatAudio.MeowType.Far);
        }

        private void StartFidget()
        {
            if (fidgeting)
                return;
            fidgetingTime = 6f;
            animator.SetTrigger("Fidget");
            lookAt.SetLookTarget(null);
        }

        void FixedUpdate()
        {
            meowCooldown -= Time.fixedDeltaTime;

            animator.SetFloat("Random", Random.value); // used for different variants of fidgeting

            HandleState();
            HandlePlayerInteraction();

            if (_prevState != _currentState)
            {
                Plugin.Log.LogInfo($"New state: {_currentState}");
                owner.InteractionsChangedHandler();
            }
            _prevState = _currentState;
        }

        private void SetState(CatState newState, bool setAnimatorParameters)
        {
            if (_currentState == newState)
                return;

            _prevState = _currentState;
            _currentState = newState;

            owner.InteractionsChangedHandler();

            if (!setAnimatorParameters)
                return;

            ResetAnimatorParameters();
            switch (newState)
            {
                case CatState.Idle:
                    break;
                case CatState.Moving:
                    break;
                case CatState.Sitting:
                    animator.SetBool("Sitting", true);
                    break;
                case CatState.Lying:
                    animator.SetBool(lastLyingPoseBelly ? "LyingBelly" : "LyingSide", true);
                    break;
                case CatState.Sleeping:
                    animator.SetBool(lastLyingPoseBelly ? "LyingBelly" : "LyingSide", true);
                    animator.SetBool("Sleeping", true);
                    break;
                case CatState.Eating:
                    animator.SetBool("Eating", true);
                    break;
                case CatState.Defecating:
                    animator.SetBool("Defecating", true);
                    break;
                case CatState.Sharpening:
                    break;
                case CatState.WaitingByDoor:
                    animator.SetBool("Sitting", true);
                    lookAt.SetLookTarget(catGraphTraverser.doorInTheWay.transform.parent);
                    break;
                case CatState.Grooming:
                    animator.SetBool("Grooming", true);
                    break;
            }

            animator.Update(0);
        }

        private void HandleState()
        {
            if (catGraphTraverser.HasDestination)
            {
                ResetAnimatorParameters();
                _currentState = CatState.Moving;
                return;
            }
            if (fidgeting)
            {
                HandleFidgetingState();
                return;
            }

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
                case CatState.Lying:
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
                case CatState.Sharpening:
                    HandleSharpeningState();
                    break;
                case CatState.Grooming:
                    HandleGroomingState();
                    break;
            }
        }
        private void HandleFidgetingState()
        {
            fidgetingTime -= Time.fixedDeltaTime;
            if (fidgetingTime < 2f)
            {
                animator.ResetTrigger("Fidget");
            }
        }

        private void HandleMovingState()
        {
            if (catGraphTraverser.doorInTheWay != null)
            {
                SetState(CatState.WaitingByDoor, true);
            }
        }

        private void HandleIdleState()
        {
            if (UnityExtensions.RandomShouldOccur(20f))
            {
                StartFidget();
            }
            else if (UnityExtensions.RandomShouldOccur(3f))
            {
                SetState(CatState.Sitting, true);
            }
            else if (UnityExtensions.RandomShouldOccur(10f))
            {
                GoToRandomArea();
            }
        }

        private void HandleWaitingByDoor()
        {
            if (UnityExtensions.RandomShouldOccur(3f))
                Meow();
            else if (UnityExtensions.RandomShouldOccur(20f))
                GoToRandomArea();

            // catGraphTraverser checks if the door is open and continues moving if so
        }

        private void HandleSittingState()
        {
            if (UnityExtensions.RandomShouldOccur(30f))
                StartFidget();
            else if (UnityExtensions.RandomShouldOccur(3f) && !fidgeting)
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
                SetState(CatState.Sleeping, true);
                lookAt.SetLookTarget(null);
            }
            else if (UnityExtensions.RandomShouldOccur(20f))
            {
                GoToRandomArea();
            }
            else if (UnityExtensions.RandomShouldOccur(30f))
            {
                StartFidget();
            }
            else if (UnityExtensions.RandomShouldOccur(5f) && !fidgeting)
            {
                lookAt.SetLookAtPlayer();
            }
        }

        private void HandleSleepingState()
        {
            if (UnityExtensions.RandomShouldOccur(40f))
            {
                WakeUp();
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

        private void HandleSharpeningState()
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
                eyelids.SetClamp(0.8f);
                pupils.SetDilation(DistanceToPlayer().RemapClamped(0.3f, 2f, 0, 0.6f));
            }
            else
            {
                if (eyelids.mode != CatEyelids.Mode.None)
                    eyelids.Release();

                pupils.SetDilation(lookingAtPlayer ? 0.6f : 0.4f);
            }

            if (IsPlayerInTheWay() && _currentState == CatState.Moving)
            {
                catGraphTraverser.ForgetDestination();
                SetState(CatState.Idle, true);

                lookAt.SetLookAtPlayer();

                if (UnityExtensions.RandomShouldOccur(4f))
                    SetState(CatState.Sitting, true);
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
            animator.SetBool("SharpeningHorizontal", false);
            animator.SetBool("SharpeningVertical", false);
            animator.SetBool("Grooming", false);
            animator.SetBool("RunningInCircles", false);
            animator.ResetTrigger("Fidget");
            animator.ResetTrigger("Meow");
            animator.ResetTrigger("Caress");
        }

        bool IsBusy()
        {
            return _currentState == CatState.Sleeping ||
                   _currentState == CatState.Eating ||
                   _currentState == CatState.Defecating;
        }

        void GoToRandomArea()
        {
            if (IsBusy() || IsPlayerInTheWay())
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

            Node targetNode = targetNodes[Random.Range(0, targetNodes.Count)];
            SetTargetNode(targetNode);
        }
    }
}