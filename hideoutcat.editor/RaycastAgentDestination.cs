using System.Collections;
using System.Collections.Generic;
using tarkin.hideoutcat;
using tarkin.hideoutcat.States;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;

public class RaycastAgentDestination : MonoBehaviour
{
#if UNITY_EDITOR
    public LayerMask layerMask;

    HideoutCat cat;

    Transform target;

    Mesh dotmesh;

    [SerializeField] Material dotMat;

    void Start()
    {
        target = new GameObject("target").transform;
        cat = FindObjectOfType<HideoutCat>();

        dotmesh = new Mesh
        {
            name = "LaserBeam _pointMesh",
            vertices = new Vector3[]
            {
                new Vector3(-1f, -1f),
                new Vector3(1f, -1f),
                new Vector3(-1f, 1f),
                new Vector3(1f, 1f)
            },
            uv = new Vector2[]
            {
                new Vector2(0f, 0f),
                new Vector2(1f, 0f),
                new Vector2(0f, 1f),
                new Vector2(1f, 1f)
            },
            triangles = new int[] { 0, 1, 3, 3, 2, 0 }
        };
        dotmesh.RecalculateBounds();
    }

    private void OnEnable()
    {

        HideoutCat.OnRequestPotentialTargets += HideoutCat_OnRequestPotentialTargets;
    }

    private void OnDisable()
    {

        HideoutCat.OnRequestPotentialTargets -= HideoutCat_OnRequestPotentialTargets;
    }

    private void HideoutCat_OnRequestPotentialTargets(List<Transform> obj)
    {
        obj.Add(target);
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetMouseButton(4))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit, 100, layerMask))
            {
                target.position = hit.point;
                target.rotation = Quaternion.LookRotation(hit.normal);

                Graphics.DrawMesh(dotmesh, hit.point + hit.normal * 0.01f, Quaternion.LookRotation(hit.normal), dotMat, LayerMask.NameToLayer("Default"));
                //cat.SetAttackTarget(target);
            }
        }
    }

    private void OnDestroy()
    {
        Destroy(dotmesh);
    }

    void OnDrawGizmos()
    {
        if (target == null) return;

        float size = 0.02f;

        Handles.color = Color.red;
        Handles.DrawSolidDisc(target.position, target.forward, size);
    }
#endif
}
