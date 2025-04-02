using UnityEngine;

namespace hideoutcat
{
    internal class CatLookAt : MonoBehaviour
    {
        BoneLookAt constraintHead;
        BoneLookAt constraintNeck;

        Transform targetLookAt;
        Transform targetLookAtDummy;

        Camera cameraMain;

        public bool tracking => constraintNeck != null && constraintNeck.targetLookAt != null;

        void Init()
        {
            Transform boneHead = transform.Find("RootNode/Arm_Cat/Skeleton/root_bone_01/Spine_base_02/spine_02_03/spine_03_04/spine_04_05/spine_05_06/neck_07/head_08");
            Transform boneNeck = transform.Find("RootNode/Arm_Cat/Skeleton/root_bone_01/Spine_base_02/spine_02_03/spine_03_04/spine_04_05/spine_05_06/neck_07");

            if (boneHead == null)
            {
                Debug.LogError($"Error init {nameof(CatLookAt)}! cant find armature bone");
                return;
            }

            // the order of adding components matters! (LateUpdate() call, neck rot has to affect head rot)
            constraintNeck = gameObject.AddComponent<BoneLookAt>();
            constraintNeck.bone = boneNeck;
            constraintNeck.weight = 0.6f;
            constraintNeck.rotationOffsetEuler = new Vector3(-15f, 0, 0f);
            constraintNeck.customUpVector = Vector3.up;
            constraintNeck.useAngleLimits = true;
            constraintNeck.maxAngleLimits = new Vector3(80, 20, 20);
            constraintNeck.minAngleLimits = new Vector3(-50, -20, -20);

            constraintHead = gameObject.AddComponent<BoneLookAt>();
            constraintHead.bone = boneHead;
            constraintHead.weight = 1f;
            constraintHead.rotationOffsetEuler = new Vector3(-15f, 0, 0f);
            constraintHead.customUpVector = Vector3.up;
            constraintHead.useAngleLimits = true;
            constraintHead.maxAngleLimits = new Vector3(80, 20, 20);
            constraintHead.minAngleLimits = new Vector3(-40, -20, -20);
        }

        public void SetLookAtPlayer()
        {
            if (cameraMain == null)
                cameraMain = Camera.main;

            SetLookTarget(cameraMain.transform);
        }

        public bool IsLookingAtPlayer()
        {
            if (constraintNeck == null)
                return false;

            if (cameraMain == null)
                return false;

            return constraintNeck.targetLookAt == cameraMain.transform;
        }

        public void SetLookTarget(Transform targetLookAt)
        {
            if (constraintNeck == null)
                Init();

            constraintNeck.targetLookAt = targetLookAt;
            constraintHead.targetLookAt = targetLookAt;
        }

        public void LookAt(Vector3 worldPos)
        {
            if (constraintNeck == null)
                Init();

            if (targetLookAtDummy == null)
                targetLookAtDummy = new GameObject("CatLookTarget").transform;

            targetLookAtDummy.position = worldPos;

            constraintNeck.targetLookAt = targetLookAtDummy;
            constraintHead.targetLookAt = targetLookAtDummy;
        }
    }
}