using UnityEngine;

#if UNITY_EDITOR
using Vertx.Debugging;
#endif

namespace tarkin.hideoutcat
{
    public static class LedgeDetector
    {
        private static float forwardOffsetSurfaceSearch = 0.75f;
        private static float forwardOffsetLedgeFaceCheck = 0.25f;
        private static float verticalOffset = 1.0f;
        private static float ledgeProbeWidth = 0.1f;
        private static float ledgeThicknessCheckDepth = 0.02f;
        private static LayerMask layerMask = 1 << 12;

        public static bool Detect(Transform transform, out Vector3 ledgeCenter, out Vector3 ledgeForward)
        {
            ledgeCenter = Vector3.zero;
            ledgeForward = Vector3.zero;

            if (!FindLedgeSurface(transform, out RaycastHit surfaceHit))
                return false;
            DrawHit(surfaceHit.point, surfaceHit.normal, Color.white);

            if (!CheckLedgeFace(transform, surfaceHit.point, -transform.right, out RaycastHit leftFaceHit))
                return false;
            DrawHit(leftFaceHit.point, leftFaceHit.normal, Color.red);

            if (!CheckLedgeFace(transform, surfaceHit.point, transform.right, out RaycastHit rightFaceHit))
                return false;
            DrawHit(rightFaceHit.point, rightFaceHit.normal, Color.blue);
            
            ledgeCenter = Vector3.Lerp(leftFaceHit.point, rightFaceHit.point, 0.5f);

            Vector3 ledgeEdge = (rightFaceHit.point - leftFaceHit.point).normalized;
            ledgeForward = Vector3.Cross(ledgeEdge, Vector3.up);

#if UNITY_EDITOR
            D.raw(new Shape.Line(leftFaceHit.point, rightFaceHit.point), Color.cyan);
            D.raw(new Shape.Arrow(ledgeCenter, Quaternion.LookRotation(ledgeForward), length: 0.1f, arrowheadScale: 3f), Color.green);
#endif
            return true;
        }

        private static bool FindLedgeSurface(Transform transform, out RaycastHit surfaceHit)
        {
            Vector3 origin = transform.position + (transform.forward * forwardOffsetSurfaceSearch) + (transform.up * verticalOffset);

#if UNITY_EDITOR
            D.raw(new Shape.Ray(origin, Vector3.down * verticalOffset), Color.yellow);
#endif

            return Physics.Raycast(origin, Vector3.down, out surfaceHit, verticalOffset, layerMask);
        }

        private static bool CheckLedgeFace(Transform transform, Vector3 surfaceHitPoint, Vector3 horizontalDirection, out RaycastHit faceHit)
        {
            Vector3 origin = transform.position
                           + (transform.forward * forwardOffsetLedgeFaceCheck)
                           + (horizontalDirection * ledgeProbeWidth)
                           + (Vector3.down * ledgeThicknessCheckDepth);

            origin.y = surfaceHitPoint.y - ledgeThicknessCheckDepth;

#if UNITY_EDITOR
            D.raw(new Shape.Ray(origin, transform.forward * forwardOffsetSurfaceSearch), Color.yellow);
#endif
            return Physics.Raycast(origin, transform.forward, out faceHit, forwardOffsetSurfaceSearch, layerMask);
        }

        private static void DrawHit(Vector3 position, Vector3 normal, Color color)
        {
#if UNITY_EDITOR
            D.raw(new Shape.Circle(position, normal, 0.02f), Color.white);
#endif
        }
    }
}
