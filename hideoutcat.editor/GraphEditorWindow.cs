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
            foreach (var item in Selection.gameObjects)
            {
                Graph _ = item.GetComponentInParent<Graph>();
                if (_ == null)
                    continue;
                graphEditor = _;
                break;
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
            if (Event.current.type == EventType.MouseDrag)
            {
                return;
            }

            if (graphEditor == null)
                return;

            Node[] Nodes = graphEditor.GetComponentsInChildren<Node>();
            if (Nodes == null || Nodes.Length == 0) return;

            if (Event.current.keyCode == KeyCode.Escape)
            {
                _connectionSourceNode = null;
            }
            //
            // Ctrl + Click to create new node
            if (Event.current.control && Event.current.type == EventType.MouseDown && Event.current.button == 0)
            {
                CreateNewNodeAtMousePosition(Nodes);
                Event.current.Use(); // Consume the event to prevent default behavior
                return; // Early return to avoid node selection/connection logic
            }

            DrawNodesInfo(Nodes);
            DrawNodesConnections(Nodes);

            DrawNodeButtons(Nodes);
            DrawConnectionRemoveButtons(Nodes);

            if (_connectionSourceNode != null)
            {
                Handles.color = Color.yellow;
                Ray ray = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
                Plane plane = new Plane(Vector3.up, Vector3.zero);
                float distance;
                Vector3 worldMousePosition = Vector3.zero;
                if (plane.Raycast(ray, out distance))
                {
                    worldMousePosition = ray.GetPoint(distance);
                }
                Handles.DrawLine(_connectionSourceNode.transform.position, worldMousePosition);
            }

            SceneView.RepaintAll();
        }


        private static bool WorldToGUIPoint(Vector3 query, out Vector2 point, out float distance, Camera camera)
        {
            Vector3 viewPos = camera.WorldToViewportPoint(query);
            distance = viewPos.z;
            if (distance < 0)
            {
                point = Vector2.zero;
                return false;
            }

            var viewScreenVector = new Vector2(viewPos.x, 1 - viewPos.y);
            viewScreenVector /= EditorGUIUtility.pixelsPerPoint;
            point = new Vector2(viewScreenVector.x * camera.pixelWidth, viewScreenVector.y * camera.pixelHeight);
            return true;
        }

        private void DrawNodeButtons(Node[] nodes)
        {
            var view = SceneView.lastActiveSceneView;

            foreach (Node Node in nodes)
            {
                float distToSceneViewCamera = Vector3.Distance(view.camera.transform.position, Node.transform.position);

                Handles.color = Color.green;
                float handleSize = HandleUtility.GetHandleSize(Node.transform.position) * 0.25f;

                if (!WorldToGUIPoint(Node.transform.position, out Vector2 screenPos, out float distance, SceneView.currentDrawingSceneView?.camera))
                    continue;
                float alpha;
                float iconSize = GizmoUtility.iconSize * 1000;
                alpha = 1 - Mathf.InverseLerp(iconSize * 0.75f, iconSize, distance);
                if (alpha <= 0)
                    continue;

                if (Handles.Button(Node.transform.position, Quaternion.identity, handleSize * alpha, handleSize, Handles.SphereHandleCap))
                {
                    if (Event.current.shift)
                    {
                        if (_connectionSourceNode == null)
                        {
                            _connectionSourceNode = Node;
                        }
                        else if (_connectionSourceNode != Node)
                        {
                            ConnectNodes(_connectionSourceNode, Node);
                            _connectionSourceNode = null;
                            HandleUtility.Repaint();
                        }
                        else
                        {
                            _connectionSourceNode = null;
                            Repaint();
                        }
                    }
                    else
                    {
                        Selection.activeGameObject = Node.gameObject;
                    }
                }
            }
        }

        private void DrawConnectionRemoveButtons(Node[] nodes)
        {
            var view = SceneView.lastActiveSceneView;

            // Use a list to store disconnection actions to avoid modifying the collection during iteration
            List<(Node, Node)> nodesToDisconnect = new List<(Node, Node)>();

            foreach (Node Node in nodes)
            {
                if (!Node.gameObject.activeInHierarchy)
                    continue;

                if (Node.connectedTo != null)
                {
                    // Create a copy of the connections list to iterate over
                    List<Node> connectionsCopy = new List<Node>(Node.connectedTo);
                    foreach (Node connectedNode in connectionsCopy)
                    {
                        if (connectedNode != null && connectedNode.gameObject.activeInHierarchy)
                        {
                            // Calculate midpoint for the disconnect button
                            Vector3 midpoint = (Node.transform.position + connectedNode.transform.position) / 2f;
                            float handleSize = HandleUtility.GetHandleSize(midpoint) * 0.1f;

                            float distToSceneViewCamera = Vector3.Distance(view.camera.transform.position, midpoint);
                            if (distToSceneViewCamera > 10f)
                                continue;

                            if (!WorldToGUIPoint(midpoint, out Vector2 screenPos, out float distance, SceneView.currentDrawingSceneView?.camera))
                                continue;
                            float alpha;
                            float iconSize = GizmoUtility.iconSize * 1000;
                            float cursorDistance = Vector2.Distance(screenPos, Event.current.mousePosition);
                            alpha = 1 - Mathf.InverseLerp(iconSize * 0.75f, iconSize, distance);
                            alpha *= Mathf.Clamp01(1f - (cursorDistance / 200f));
                            if (alpha <= 0)
                                continue;

                            // Draw the disconnect button
                            Handles.color = Color.red;
                            if (Handles.Button(midpoint, Quaternion.identity, handleSize * alpha, handleSize, Handles.CubeHandleCap))
                            {
                                // Add the nodes to the disconnection list
                                nodesToDisconnect.Add((Node, connectedNode));
                            }
                        }
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

                        Handles.color = Color.blue;
                        Vector3 arrowHeadPoint = connection.transform.position - direction * 0.5f;
                        Handles.DrawLine(arrowHeadPoint + Quaternion.Euler(0, 45, 0) * (-direction * 0.2f), arrowHeadPoint);
                        Handles.DrawLine(arrowHeadPoint + Quaternion.Euler(0, -45, 0) * (-direction * 0.2f), arrowHeadPoint);
                    }
                }
            }
        }

        private void CreateNewNodeAtMousePosition(Node[] existingNodes)
        {
            if (existingNodes.Length == 0)
            {
                Ray ray = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
                Plane plane = new Plane(Vector3.up, Vector3.zero);
                float distance;
                if (plane.Raycast(ray, out distance))
                {
                    Vector3 worldMousePosition = ray.GetPoint(distance);
                    Node newNode = CreateNode(worldMousePosition);
                    Selection.activeGameObject = newNode.gameObject;
                }
                return;
            }

            Node closestNode = null;
            float minScreenDistSq = float.MaxValue;
            Vector2 mouseScreenPos = Event.current.mousePosition;

            foreach (Node node in existingNodes)
            {
                if (!node.gameObject.activeInHierarchy)
                    continue;

                Vector2 nodeScreenPos = HandleUtility.WorldToGUIPoint(node.transform.position);
                float distSq = Vector2.SqrMagnitude(mouseScreenPos - nodeScreenPos);
                if (distSq < minScreenDistSq)
                {
                    minScreenDistSq = distSq;
                    closestNode = node;
                }
            }

            if (closestNode != null)
            {
                Ray ray = HandleUtility.GUIPointToWorldRay(mouseScreenPos);
                Plane plane = new Plane(Vector3.up, new Vector3(0, closestNode.transform.position.y, 0));
                float distance;
                if (plane.Raycast(ray, out distance))
                {
                    Vector3 worldMousePosition = ray.GetPoint(distance);
                    Node newNode = CreateNode(worldMousePosition);
                    if (newNode != null && closestNode != null)
                    {
                        newNode.gameObject.transform.SetParent(closestNode.transform.parent, true);
                        ConnectNodes(closestNode, newNode);
                        Selection.activeGameObject = newNode.gameObject;
                    }
                }
            }
        }

        private Node CreateNode(Vector3 position)
        {
            GameObject newNodeGO = new GameObject();
            Undo.RegisterCreatedObjectUndo(newNodeGO, "Create Node");

            newNodeGO.transform.position = position;
            newNodeGO.transform.SetParent(graphEditor.transform);

            Node newNode = newNodeGO.AddComponent<Node>();
            if (newNode != null)
            {
                return newNode;
            }
            return null;
        }


        private void ConnectNodes(Node sourceNode, Node targetNode)
        {
            if (sourceNode == null || targetNode == null) return;

            SerializedObject serializedSourceNode = new SerializedObject(sourceNode);
            SerializedProperty sourceConnectionsProperty = serializedSourceNode.FindProperty("connectedTo");

            bool sourceAlreadyConnected = false;
            for (int i = 0; i < sourceConnectionsProperty.arraySize; ++i)
            {
                if (sourceConnectionsProperty.GetArrayElementAtIndex(i).objectReferenceValue == targetNode)
                {
                    sourceAlreadyConnected = true;
                    break;
                }
            }

            if (!sourceAlreadyConnected)
            {
                int newIndex = sourceConnectionsProperty.arraySize;
                sourceConnectionsProperty.arraySize++;
                sourceConnectionsProperty.GetArrayElementAtIndex(newIndex).objectReferenceValue = targetNode;
                serializedSourceNode.ApplyModifiedProperties();
                Debug.Log($"Connected {sourceNode.name} to {targetNode.name}");
            }
            else
            {
                Debug.LogWarning($"Nodes {sourceNode.name} and {targetNode.name} are already connected (source to target).");
            }
        }

        private void DisconnectNodes(Node nodeA, Node nodeB)
        {
            Undo.RecordObject(nodeA, "Disconnect Nodes");
            Undo.RecordObject(nodeB, "Disconnect Nodes");

            if (nodeA.connectedTo.Contains(nodeB))
            {
                nodeA.connectedTo.Remove(nodeB);
            }
            if (nodeB.connectedTo.Contains(nodeA))
            {
                nodeB.connectedTo.Remove(nodeA);
            }

            EditorUtility.SetDirty(nodeA);  // Important: Mark objects as dirty for undo/redo
            EditorUtility.SetDirty(nodeB);

            Debug.Log($"Disconnected {nodeA.name} from {nodeB.name}");
        }
    }
}