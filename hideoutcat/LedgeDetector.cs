using UnityEngine;

#if UNITY_EDITOR
using Vertx.Debugging;
#endif

namespace tarkin.hideoutcat
{
    public static class LedgeDetector
    {
        [System.Serializable]
        public struct Config
        {
            public float ForwardOffsetSurfaceSearch;
            public float ForwardOffsetLedgeFaceCheck;
            public float VerticalOffset;
            public float LedgeProbeWidth;
            public float LedgeThicknessCheckDepth;
            public float AngleRelativeToTransformLimit;
            public LayerMask LayerMask;

            public static readonly Config Default = new Config
            {
                ForwardOffsetSurfaceSearch = 0.75f,
                ForwardOffsetLedgeFaceCheck = 0.25f,
                VerticalOffset = 1.0f,
                LedgeProbeWidth = 0.1f,
                LedgeThicknessCheckDepth = 0.02f,
                AngleRelativeToTransformLimit = 30f,
                LayerMask = 1 << 12
            };
        }

        public static bool Detect(
            Transform transform,
            in Config config,
            out Vector3 ledgeCenter,
            out Vector3 ledgeForward)
        {
            ledgeCenter = Vector3.zero;
            ledgeForward = Vector3.zero;

            if (!FindLedgeSurface(transform, in config, out RaycastHit surfaceHit))
                return false;
            DrawHit(surfaceHit.point, surfaceHit.normal, Color.white);

            if (!CheckLedgeFace(transform, in config, surfaceHit.point, -transform.right, out RaycastHit leftFaceHit))
                return false;
            DrawHit(leftFaceHit.point, leftFaceHit.normal, Color.red);

            if (!CheckLedgeFace(transform, in config, surfaceHit.point, transform.right, out RaycastHit rightFaceHit))
                return false;
            DrawHit(rightFaceHit.point, rightFaceHit.normal, Color.blue);
            
            ledgeCenter = Vector3.Lerp(leftFaceHit.point, rightFaceHit.point, 0.5f);

            Vector3 ledgeEdge = (rightFaceHit.point - leftFaceHit.point).normalized;
            ledgeForward = Vector3.Cross(ledgeEdge, Vector3.up);

            bool angleLimitExceeded = Vector3.Angle(transform.forward, ledgeForward) > config.AngleRelativeToTransformLimit;

#if UNITY_EDITOR
            Color lineColor = angleLimitExceeded ? Color.red : Color.green;
            D.raw(new Shape.Line(leftFaceHit.point, rightFaceHit.point), lineColor);
            D.raw(new Shape.Arrow(ledgeCenter, Quaternion.LookRotation(ledgeForward), length: 0.1f, arrowheadScale: 3f), lineColor);
#endif
            return !angleLimitExceeded;
        }

        public static bool Detect(Transform transform, out Vector3 ledgeCenter, out Vector3 ledgeForward)
        {
            return Detect(transform, in Config.Default, out ledgeCenter, out ledgeForward);
        }

        private static bool FindLedgeSurface(Transform transform, in Config config, out RaycastHit surfaceHit)
        {
            Vector3 origin = transform.position
                           + (transform.forward * config.ForwardOffsetSurfaceSearch)
                           + (transform.up * config.VerticalOffset);

#if UNITY_EDITOR
            D.raw(new Shape.Ray(origin, Vector3.down * config.VerticalOffset), Color.yellow);
#endif

            return Physics.Raycast(origin, Vector3.down, out surfaceHit, config.VerticalOffset, config.LayerMask);
        }

        private static bool CheckLedgeFace(Transform transform, in Config config, Vector3 surfaceHitPoint, Vector3 horizontalDirection, out RaycastHit faceHit)
        {
            Vector3 origin = transform.position
                           + (transform.forward * config.ForwardOffsetLedgeFaceCheck)
                           + (horizontalDirection * config.LedgeProbeWidth)
                           + (Vector3.down * config.LedgeThicknessCheckDepth);

            origin.y = surfaceHitPoint.y - config.LedgeThicknessCheckDepth;

#if UNITY_EDITOR
            D.raw(new Shape.Ray(origin, transform.forward * config.ForwardOffsetSurfaceSearch), Color.yellow);
#endif
            return Physics.Raycast(origin, transform.forward, out faceHit, config.ForwardOffsetSurfaceSearch, config.LayerMask);
        }

        private static void DrawHit(Vector3 position, Vector3 normal, Color color)
        {
#if UNITY_EDITOR
            D.raw(new Shape.Circle(position, normal, 0.02f), Color.white);
#endif
        }
    }
}
