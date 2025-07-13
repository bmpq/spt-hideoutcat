using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.UnityConverters.Math;
using UnityEngine;
using System.IO;
using tarkin.hideoutcat.Pathfinding;

namespace tarkin.hideoutcat.editor
{
    public static class LegacyJsonHandler
    {
        [System.Serializable]
        public class NodeJsonData
        {
            public string name;
            public Vector3 position;

            [JsonProperty("connectedTo")]
            public List<string> ConnectionNames { get; set; }

            public bool forwardJump;
            public EAreaType areaType;
            public int areaLevel;
            public float poseRotation;
            public Node.Pose pose;
        }

        public static void ImportJson()
        {
            string filePath2 = System.IO.Path.Combine(System.IO.Path.GetFullPath("E:\\Games\\SPT_3.11\\BepInEx\\plugins\\tarkin\\bundles"), "CatNodeGraph.json");

            JsonSerializerSettings settings = new JsonSerializerSettings
            {
                Converters = new List<JsonConverter> { new Vector3Converter() }
            };

            List<NodeJsonData> jsonNodes = JsonConvert.DeserializeObject<List<NodeJsonData>>(File.ReadAllText(filePath2), settings);

            DeserializeJsonNodes(jsonNodes);
        }

        public static void DeserializeJsonNodes(List<NodeJsonData> jsonNodes)
        {
            Transform root = new GameObject().transform;

            Dictionary<EAreaType, Transform> areaParents = new Dictionary<EAreaType, Transform>();
            Transform GetAreaParent(EAreaType areaType, int level)
            {
                if (areaType == EAreaType.NotSet)
                    return root;

                Transform area = null;
                areaParents.TryGetValue(areaType, out area);
                if (area == null)
                {
                    area = new GameObject($"{(int)areaType}_{areaType.ToString()}").transform;
                    area.SetParent(root, false);
                    areaParents[areaType] = area;
                }

                string levelstring = $"level{level}";
                for (int i = 0; i < area.childCount; i++)
                {
                    if (area.GetChild(i).name == levelstring)
                    {
                        return area.GetChild(i);
                    }
                }

                Transform levelTransform = new GameObject(levelstring).transform;
                levelTransform.SetParent(area, false);
                return levelTransform;
            }

            // Create Nodes based on Nodes
            foreach (var node in jsonNodes)
            {
                GameObject nodeObj = new GameObject(node.name);
                nodeObj.transform.SetParent(GetAreaParent(node.areaType, node.areaLevel));
                nodeObj.transform.position = node.position;

                Node goNode = nodeObj.AddComponent<Node>();
                goNode.areaLevel = node.areaLevel;
                goNode.areaType = node.areaType;
                goNode.forwardJump = node.forwardJump;
                goNode.pose = node.pose;
                goNode.transform.eulerAngles = new Vector3(0, node.poseRotation, 0);
            }

            Node[] goNodes = root.GetComponentsInChildren<Node>(); 
            
            foreach (var goNode in goNodes)
            {
                NodeJsonData jsonNode = jsonNodes.FirstOrDefault(n => n.name == goNode.name);
                foreach (var stringNode in jsonNode.ConnectionNames)
                {
                    Node goTarget = goNodes.FirstOrDefault(n => n.name == stringNode);
                    goNode.connectedTo.Add(goTarget);
                }
            }

            Debug.Log("Graph Deserialized!");
        }
    }
}
