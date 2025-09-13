using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using Vertx.Debugging;
using tarkin.hideoutcat.Pathfinding;

namespace tarkin.hideoutcat.editor
{
    public class GraphEditorWindow : EditorWindow
    {
        private Graph graphEditor;

        private Node _connectionSourceNode;

        [MenuItem("Tools/Graph Editor")]
        public static void ShowWindow()
        {
            GetWindow<GraphEditorWindow>("Graph Editor");
        }

        private void OnEnable()
        {
            SceneView.duringSceneGui += OnSceneGUI;
        }

        private void OnDisable()
        {
            SceneView.duringSceneGui -= OnSceneGUI;//
        }

        private void OnSelectionChange()
        {
            graphEditor = null;
            foreach (var item in Selection.gameObjects)
            {
                Graph _ = item.GetComponentInParent<Graph>();
                if (_ != null)
                {
                    graphEditor = _;
                    break;
                }
            }

            Repaint();
        }

        public void OnGUI()
        {
            if (graphEditor == null)
            {
                EditorGUILayout.HelpBox($"No active graph", MessageType.Warning);
                return;
            }

            EditorGUILayout.HelpBox($"(Left Click) on a ball to select node, (SHIFT + Left Click) to start connecting, (CTRL + Left Click) to create new node (auto connect to nearest)", MessageType.Info);

            if (_connectionSourceNode != null)
            {
                if (GUILayout.Button("Cancel Connection"))
                {
                    _connectionSourceNode = null;
                }
            }
        }

        private void OnSceneGUI(SceneView sceneView)
        {
            if (graphEditor == null)
                return;

            Node[] Nodes = graphEditor.GetComponentsInChildren<Node>();
            if (Nodes == null) return;

            HandleInput(Nodes);

            DrawNodesInfo(Nodes);
            DrawNodeButtons(Nodes);
            DrawNodesConnections(Nodes);
            DrawConnectionRemoveButtons(Nodes);
            DrawPendingConnection();

            SceneView.RepaintAll();
        }

        private void HandleInput(Node[] nodes)
        {
            if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Escape)
            {
                if (_connectionSourceNode != null)
                {
                    _connectionSourceNode = null;
                    Event.current.Use();
                }
            }

            if (GUIUtility.hotControl != 0 || Event.current.alt)
            {
                return;
            }

            if (Event.current.type == EventType.MouseDown && Event.current.button == 0)
            {
                if (Event.current.control)
                {
                    CreateNewNodeAtMousePosition(nodes);
                    Event.current.Use();
                }
            }
        }

        private void DrawPendingConnection()
        {
            if (_connectionSourceNode == null) return;

            Handles.color = Color.yellow;
            Ray ray = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
            Plane plane = new Plane(Vector3.up, _connectionSourceNode.transform.position);
            if (plane.Raycast(ray, out float distance))
            {
                Vector3 worldMousePosition = ray.GetPoint(distance);
                Handles.DrawLine(_connectionSourceNode.transform.position, worldMousePosition);
            }
        }

        private void DrawNodeButtons(Node[] nodes)
        {
            foreach (Node node in nodes)
            {
                if (!node.gameObject.activeInHierarchy) continue;

                Handles.color = Color.green;
                float handleSize = HandleUtility.GetHandleSize(node.transform.position) * 0.5f;

                if (Handles.Button(node.transform.position, Quaternion.identity, handleSize, handleSize, Handles.SphereHandleCap))
                {
                    if (Event.current.shift)
                    {
                        if (_connectionSourceNode == null)
                        {
                            _connectionSourceNode = node;
                        }
                        else if (_connectionSourceNode != node)
                        {
                            ConnectNodes(_connectionSourceNode, node);
                            _connectionSourceNode = null;
                        }
                        else
                        {
                            _connectionSourceNode = null;
                        }
                    }
                    else
                    {
                        Selection.activeGameObject = node.gameObject;
                    }
                }
            }
        }

        private void DrawConnectionRemoveButtons(Node[] nodes)
        {
            List<(Node, Node)> nodesToDisconnect = new List<(Node, Node)>();

            foreach (Node node in nodes)
            {
                if (!node.gameObject.activeInHierarchy || node.connectedTo == null) continue;

                foreach (Node connectedNode in new List<Node>(node.connectedTo))
                {
                    if (connectedNode == null || !connectedNode.gameObject.activeInHierarchy) continue;

                    if (node.GetInstanceID() > connectedNode.GetInstanceID()) continue;

                    Vector3 midpoint = (node.transform.position + connectedNode.transform.position) / 2f;
                    float handleSize = HandleUtility.GetHandleSize(midpoint) * 0.1f;

                    float distToCam = Vector3.Distance(SceneView.currentDrawingSceneView.camera.transform.position, midpoint);
                    float alpha = Mathf.Clamp01(1 - (distToCam / 20f));
                    if (alpha <= 0.1f) continue;

                    Handles.color = new Color(1, 0, 0, alpha);
                    if (Handles.Button(midpoint, Quaternion.identity, handleSize, handleSize, Handles.CubeHandleCap))
                    {
                        nodesToDisconnect.Add((node, connectedNode));
                    }
                }
            }

            // Perform disconnections after iterating through all connections
            foreach (var (nodeA, nodeB) in nodesToDisconnect)
            {
                DisconnectNodes(nodeA, nodeB);
            }
        }

        private void DrawNodesInfo(Node[] nodes)
        {
            foreach (Node node in nodes)
            {
                if (!node.gameObject.activeInHierarchy)
                    continue;

                if (node.purpose != Purpose.None)
                {
                    Handles.Label(node.transform.position + Vector3.up * 0.1f, $"{node.purpose}", EditorStyles.helpBox);
                }

                Handles.color = Color.blue;
                Handles.DrawWireDisc(node.transform.position, Vector3.up, 0.2f);
            }
        }

        private void DrawNodesConnections(Node[] nodes)
        {
            Handles.color = Color.blue;

            foreach (Node node in nodes)
            {
                if (!node.gameObject.activeInHierarchy || node.connectedTo == null)
                    continue;

                foreach (var connection in node.connectedTo)
                {
                    if (connection != null && connection.gameObject.activeInHierarchy)
                    {
                        Handles.DrawLine(node.transform.position, connection.transform.position);

                        Vector3 direction = (connection.transform.position - node.transform.position).normalized;
                        Vector3 arrowHeadPoint = connection.transform.position - direction * 0.5f;
                        Handles.ArrowHandleCap(0, arrowHeadPoint, Quaternion.LookRotation(direction), 0.4f, EventType.Repaint);
                    }
                }
            }
        }

        private void CreateNewNodeAtMousePosition(Node[] existingNodes)
        {
            Ray ray = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
            Plane plane = new Plane(Vector3.up, graphEditor.transform.position);
            if (!plane.Raycast(ray, out float distance)) return;

            Vector3 worldMousePosition = ray.GetPoint(distance);

            Node newNode = CreateNode(worldMousePosition);
            Selection.activeGameObject = newNode.gameObject;

            // Auto-connect to nearest node
            if (existingNodes != null && existingNodes.Length > 0)
            {
                Node closestNode = null;
                float minSqrDist = float.MaxValue;

                foreach (Node node in existingNodes)
                {
                    if (!node.gameObject.activeInHierarchy) continue;
                    float sqrDist = (node.transform.position - worldMousePosition).sqrMagnitude;
                    if (sqrDist < minSqrDist)
                    {
                        minSqrDist = sqrDist;
                        closestNode = node;
                    }
                }

                if (closestNode != null)
                {
                    ConnectNodes(closestNode, newNode);
                }
            }
        }

        private Node CreateNode(Vector3 position)
        {
            var go = new GameObject("Node", typeof(Node));
            Undo.RegisterCreatedObjectUndo(go, "Create Node");
            go.transform.position = position;
            go.transform.SetParent(graphEditor.transform);
            return go.GetComponent<Node>();
        }

        private void ConnectNodes(Node nodeA, Node nodeB)
        {
            if (nodeA == null || nodeB == null || nodeA == nodeB) return;

            if (nodeA.connectedTo == null)
                nodeA.connectedTo = new List<Node>();
            if (nodeB.connectedTo == null)
                nodeB.connectedTo = new List<Node>();

            Undo.RecordObjects(new Object[] { nodeA, nodeB }, "Connect Nodes");
            bool changed = false;
            if (!nodeA.connectedTo.Contains(nodeB))
            {
                nodeA.connectedTo.Add(nodeB);
                changed = true;
            }
            if (!nodeB.connectedTo.Contains(nodeA))
            {
                nodeB.connectedTo.Add(nodeA);
                changed = true;
            }

            if (changed)
            {
                EditorUtility.SetDirty(nodeA);
                EditorUtility.SetDirty(nodeB);
            }
        }

        private void DisconnectNodes(Node nodeA, Node nodeB)
        {
            if (nodeA == null || nodeB == null) return;
            Undo.RecordObjects(new Object[] { nodeA, nodeB }, "Disconnect Nodes");

            bool changed = false;
            if (nodeA.connectedTo.Contains(nodeB))
            {
                nodeA.connectedTo.Remove(nodeB);
                changed = true;
            }
            if (nodeB.connectedTo.Contains(nodeA))
            {
                nodeB.connectedTo.Remove(nodeA);
                changed = true;
            }

            if (changed)
            {
                EditorUtility.SetDirty(nodeA);
                EditorUtility.SetDirty(nodeB);
            }
        }
    }
}