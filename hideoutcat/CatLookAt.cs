using UnityEngine;

namespace tarkin.hideoutcat
{
    internal class CatLookAt : MonoBehaviour
    {
        [SerializeField] BoneLookAt constraintHead;
        [SerializeField] BoneLookAt constraintNeck;

        Transform targetLookAt;
        Transform targetLookAtDummy;

        Camera cameraMain;

        public bool tracking => constraintNeck != null && constraintNeck.targetLookAt != null;
        
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
            constraintNeck.targetLookAt = targetLookAt;
            constraintHead.targetLookAt = targetLookAt;
        }

        public void LookAt(Vector3 worldPos)
        {
            if (targetLookAtDummy == null)
                targetLookAtDummy = new GameObject("CatLookTarget").transform;

            targetLookAtDummy.position = worldPos;

            constraintNeck.targetLookAt = targetLookAtDummy;
            constraintHead.targetLookAt = targetLookAtDummy;
        }
    }
}