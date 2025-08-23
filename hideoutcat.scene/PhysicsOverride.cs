using UnityEngine;

namespace tarkin.hideoutcat.scene
{
    internal class PhysicsOverride : MonoBehaviour
    {
        public Vector3 physicsWorldCenter;
        public Vector3 physicsWorldSize = new Vector3(250, 250, 250);
        public int phsyicsWorldSubdivisions = 8;

        public int solverIterations = 6;
        public int defaultSolverVelocityIterations = 1;

        void Start()
        {
            Physics.RebuildBroadphaseRegions(new Bounds(physicsWorldCenter, physicsWorldSize), phsyicsWorldSubdivisions);
            Physics.defaultSolverIterations = solverIterations;
            Physics.defaultSolverVelocityIterations = defaultSolverVelocityIterations;

            Physics.simulationMode = SimulationMode.FixedUpdate;

            this.enabled = false;
        }
    }
}
