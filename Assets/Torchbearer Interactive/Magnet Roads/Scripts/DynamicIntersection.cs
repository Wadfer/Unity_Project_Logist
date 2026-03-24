// Copyright (C) Torchbearer Interactive, Ltd. - All Rights Reserved

//Handels the creation of Intersections with varible amount of intersections.

using System;
using UnityEngine;
using System.IO;
using TBUnityLib.MeshTools;
using RuntimeGizmo;

#if UNITY_EDITOR
using UnityEditor;
#endif
namespace MagnetRoads
{
    [Serializable]
    [ExecuteInEditMode]
    [AddComponentMenu("")]
    [RequireComponent(typeof(MeshFilter))]
    [RequireComponent(typeof(MeshRenderer))]
    [RequireComponent(typeof(MeshCollider))]
    public class DynamicIntersection : MonoBehaviour
    {
        [Tooltip("Road Material")]
        public Material surfaceMaterial;
        [Tooltip("Roadside Material")]
        public Material sideMaterial;
        [Tooltip("Road width value")] 
        public float roadWidth = 0.5f;
        [Tooltip("Depth of the road's sides")]
        public float sideDepth = 0.2f;
        [Tooltip("Slope of the road's sides")]
        public float slopeWidth;
        [Tooltip("The Amount of connections Generated")] 
        public int connectionAmount;
        [HideInInspector]
        public string uniqueConnectionId;
        [SerializeField] [Tooltip("Set this Intersection to be editable at runtime")]
        private bool isEditableAtRuntime;
        [SerializeField] [Tooltip("Sets if the outline and vert lines are drawn")]
        private bool drawOutlines;
        private Vector3 currentCenter;

        public SnapPoint[] SnapNodes { get { return snapNodes; } }
        public GameObject[] Connections { get { return connections; } }
        public bool IsEditableAtRuntime { get { return isEditableAtRuntime; }  set { isEditableAtRuntime = value; } }
        public string[] ConnectionsUniqueIDs { get { return connectionUniqueIDs; } }

        private static Material sDefaultRoadMat;
        private static Material sDefaultSideMat;
        [SerializeField][HideInInspector]
        private string[] connectionUniqueIDs;
        [SerializeField][HideInInspector]
        private GameObject[] connections ;
        [SerializeField] [HideInInspector]
        private SnapPoint[] snapNodes;
        private Vector3 cachedPosition;
        private Quaternion cachedRotation;
        private Vector3 cachedCenter;
        private float cachedRoadWidth;
        private float cachedSideDepth;
        private float cachedSlopeWidth;
        private int cachedConnectionAmmout;
        private Mesh mesh;
        private MeshFilter meshFilter;
        private MeshCollider meshCollider;
        private GameObject snapNodeParent;
        private Gizmo gizmo;
        private GizmoSelectable gizmoSelect;

        void Awake()
        {
            if (connectionUniqueIDs == null) connectionUniqueIDs = new string[0];
            if (snapNodes == null) snapNodes = new SnapPoint[0];
            if (connections == null) connections = new GameObject[0];
        }

        void Update()
        {
            // Clamp Values
            if (!Application.isPlaying || isEditableAtRuntime)
            {
                roadWidth = Mathf.Clamp(roadWidth, 0.1f, float.MaxValue);
                connectionAmount = Mathf.Clamp(connectionAmount, 3, int.MaxValue);
                sideDepth = Mathf.Clamp(sideDepth, 0.0f, float.MaxValue);
            }

            // Store an instance of the gizmo if possible
            if (isEditableAtRuntime && !gizmo)
            {
                gizmo = FindObjectOfType<Gizmo>();
            }
            // Check for disscontects if moved
            if (!cachedPosition.Equals(transform.position) || !cachedRotation.Equals(transform.rotation))
            {
                CheckForDisconnect();
                cachedRotation = transform.rotation;
                cachedPosition = transform.position;
            }
            // edit at run time
            if (Application.isPlaying && isEditableAtRuntime && (!roadWidth.Equals(cachedRoadWidth) || !sideDepth.Equals(cachedSideDepth) || !slopeWidth.Equals(cachedSlopeWidth) || !connectionAmount.Equals(cachedConnectionAmmout)))
                SetUp();


            // Check if this intersection needs to be made selectable or remove the selectable gizmo
            if (isEditableAtRuntime && Application.isPlaying)
            {
                gizmoSelect = GetComponent<GizmoSelectable>() ? GetComponent<GizmoSelectable>() : gameObject.AddComponent<GizmoSelectable>();
            }
            else
            {
                // Check for the existing gizmo and clear its data
                if (gizmo)
                {
                    if (gizmo.selectedObject == gameObject)
                    {
                        gizmo.ClearSelection();
                        gizmo.Hide();
                    }
                }
                DestroyImmediate(gizmoSelect);
            }

            // Ensure snap node's isEditableAtRuntime matches their parent
            foreach (SnapPoint node in SnapNodes)
            {
                if (!node) continue;
                if (node.isEditableAtRuntime != IsEditableAtRuntime)
                {
                    node.isEditableAtRuntime = IsEditableAtRuntime;
                }
            }

            // When selected by the editor gizmo hide the snapPoints
            if (!gizmo) return;
            foreach (var point in SnapNodes)
            {
                if (!point) continue;
                if (point.GetComponent<Renderer>()) point.GetComponent<Renderer>().enabled = !gizmo.selectedObject == transform;
            }

        }

        public void SetUp()
        {
            roadWidth = Mathf.Clamp(roadWidth, 0.1f, float.MaxValue);
            connectionAmount = Mathf.Clamp(connectionAmount, 3, int.MaxValue);
            sideDepth = Mathf.Clamp(sideDepth, 0.0f, float.MaxValue);
            cachedRoadWidth = roadWidth;
            cachedSideDepth = sideDepth;
            cachedSlopeWidth = slopeWidth;
            cachedConnectionAmmout = connectionAmount;
            if (uniqueConnectionId == null) uniqueConnectionId = Guid.NewGuid().ToString();
            if (!sDefaultRoadMat)
            {
                sDefaultRoadMat = Resources.Load<Material>("Materials/asphalt_road");
            }
            if (!sDefaultSideMat)
            {
                sDefaultSideMat = Resources.Load<Material>("Materials/road_sides");
            }
            if (sDefaultRoadMat && !surfaceMaterial) surfaceMaterial = sDefaultRoadMat;
            if (sDefaultSideMat && !sideMaterial) sideMaterial = sDefaultSideMat;
            

            GenerateMesh();
        }
        
        private void GenerateMesh()
        {
            // Cleanup all the previus Intersection data
            ClearIntersection();
            snapNodes = new SnapPoint[cachedConnectionAmmout];
            connections = new GameObject[cachedConnectionAmmout];
            connectionUniqueIDs = new string[cachedConnectionAmmout];
            // Generate Top
            Vector3[] topPlaneVerts = GenerateTopPlaneVerts(cachedRoadWidth, connectionAmount);
            cachedCenter = currentCenter;
            mesh = Geometry.GeneratePlaneWithCenterMesh(topPlaneVerts,cachedCenter);
            mesh.name = "Dynamic Top Plane";
            meshFilter.mesh = mesh;
            meshCollider.sharedMesh = meshFilter.sharedMesh;
            GetComponent<Renderer>().sharedMaterial = surfaceMaterial;
            if (sideDepth != 0.0f)
            {
                // Generate bottem (compensate for slope)
                Vector3[] bottemPlaneVerts = GenerateBottemPlaneVerts(cachedRoadWidth, connectionAmount, cachedCenter, cachedSlopeWidth, Vector3.down * cachedSideDepth);
                Mesh bottemMesh = Geometry.GeneratePlaneWithCenterMesh(bottemPlaneVerts, cachedCenter, true);
                bottemMesh.name = "Intersection Underside";
                var bottemGo = new GameObject("Intersection Underside");
                bottemGo.AddComponent<MeshFilter>().mesh = bottemMesh;
                bottemGo.AddComponent<MeshRenderer>();
                bottemGo.AddComponent<MeshCollider>().sharedMesh = bottemMesh;
                bottemGo.GetComponent<Renderer>().sharedMaterial = sideMaterial;
                bottemGo.transform.position = transform.position;
                bottemGo.transform.SetParent(transform);
                // Generate sides using top and bottem
                Array.Resize(ref topPlaneVerts, topPlaneVerts.Length + 1);
                Array.Resize(ref bottemPlaneVerts, bottemPlaneVerts.Length + 1);
                topPlaneVerts[topPlaneVerts.Length - 1] = topPlaneVerts[0];
                bottemPlaneVerts[bottemPlaneVerts.Length - 1] = bottemPlaneVerts[0];
                Mesh sideMesh = Geometry.GenerateStrip(bottemPlaneVerts, topPlaneVerts, transform, false, null, "Intersection Sides");
                var sideGo = new GameObject("Intersection Sides");
                sideGo.AddComponent<MeshFilter>().mesh = sideMesh;
                sideGo.AddComponent<MeshRenderer>();
                sideGo.AddComponent<MeshCollider>().sharedMesh = sideMesh;
                sideGo.GetComponent<Renderer>().sharedMaterial = sideMaterial;
                sideGo.transform.position = transform.position;
                sideGo.transform.SetParent(transform);
            }
            // Generated SnapNodes
            snapNodeParent = new GameObject();
            snapNodeParent.transform.position = transform.position;
            snapNodeParent.transform.parent = transform;
            snapNodeParent.name = "Snap Points";
            // Creation of SnapPoints
            {
                Vector3 snapPointPos;
                
                for (int i = 0; i < cachedConnectionAmmout - 1; i++)
                {
                    snapPointPos = Vector3.Lerp(topPlaneVerts[i], topPlaneVerts[i + 1], 0.5f);
                    snapNodes[i]= CreateSnapPoint(snapPointPos, Quaternion.LookRotation(snapPointPos - cachedCenter), SnapPoint.PointEnd.Bipolar, "SnapPoint" + i);
                }
                snapPointPos = Vector3.Lerp(topPlaneVerts[cachedConnectionAmmout-1], topPlaneVerts[0], 0.5f);
                snapNodes[cachedConnectionAmmout-1] = CreateSnapPoint(snapPointPos, Quaternion.LookRotation(snapPointPos - cachedCenter), SnapPoint.PointEnd.Bipolar, "SnapPoint" + (cachedConnectionAmmout - 1));
            }

        }

        private Vector3[] GenerateTopPlaneVerts(float width, int connectionAmmount, bool offsetByTransform = false)
        {
            Quaternion rotation = Quaternion.AngleAxis(((float)1 / connectionAmmount) * 360f, Vector3.up);
            Vector3 movmentVector = new Vector3(width, 0.0f);
            Vector3[] Points = new Vector3[connectionAmmount];
            Vector3 currentLocation = new Vector3(0.0f,0.0f);
            //Center calcuation
            {
                Quaternion apothemRot = Quaternion.AngleAxis(90f, Vector3.up);
                Vector3 CenterLine = currentLocation + (rotation * movmentVector) * 0.5f;
                Vector3 lenght = new Vector3(width / (2f * Mathf.Tan(Mathf.PI / connectionAmmount)),0.0f);
                currentCenter = CenterLine + ((apothemRot * rotation) * lenght);
            }
            if (offsetByTransform) currentLocation += transform.position;
            Points[0] = currentLocation;
            for (int i = 1; i < connectionAmmount; i++)
            {
                movmentVector = rotation * movmentVector;
                currentLocation += movmentVector;
                Points[i] = currentLocation;
            }
            return Points;
        }

        private Vector3[] GenerateBottemPlaneVerts(float width, int connectionAmmount,Vector3 center,float slopeWidth, Vector3 offset)
        {
            Quaternion rotation = Quaternion.AngleAxis(((float)1 / connectionAmmount) * 360f, Vector3.up);
            Vector3 movmentVector = new Vector3(width, 0.0f);
            Vector3[] Points = new Vector3[connectionAmmount];
            Vector3 currentLocation = new Vector3(0.0f, 0.0f);

            Points[0] = currentLocation + (currentLocation -center) *slopeWidth + offset;
            for (int i = 1; i < connectionAmmount; i++)
            {
                movmentVector = rotation * movmentVector;
                currentLocation += movmentVector;
                Points[i] = currentLocation + (currentLocation - center) * slopeWidth + offset;
            }
            return Points;
        }

        private SnapPoint CreateSnapPoint(Vector3 offset, Quaternion rotation, SnapPoint.PointEnd polarity, string pointName)
        {
            var snapPointGo = new GameObject();
            var snapPoint = snapPointGo.AddComponent<SnapPoint>();
            snapPoint.SetUp(polarity, cachedRoadWidth);
            snapPointGo.transform.position = transform.position + offset;
            snapPointGo.transform.rotation = rotation;
            snapPointGo.transform.parent = snapNodeParent.transform;
            snapPointGo.name = pointName;
            return snapPoint;
        }

        public void AddConnection(int entranceNo, MagnetRoad connection)
        {
            connections[entranceNo] = connection.gameObject;
            connectionUniqueIDs[entranceNo] = connection.uniqueConnectionId;
        }

        // TODO: Update for Dynamic Intersection
        public void ClearIntersection()
        {
            // Store roadSide texture
            cachedRotation = transform.rotation;
            cachedPosition = transform.position;

            // Refresh object information
            foreach (var node in snapNodes)
            {
                if(node!= null)
                    DestroyImmediate(node.gameObject);
            }
            if (snapNodeParent) DestroyImmediate(snapNodeParent);
            if (transform.Find("Intersection Underside")) DestroyImmediate(transform.Find("Intersection Underside").gameObject);
            if (transform.Find("Intersection Sides")) DestroyImmediate(transform.Find("Intersection Sides").gameObject);
            if (transform.Find("Snap Points")) DestroyImmediate(transform.Find("Snap Points").gameObject);
            transform.rotation = Quaternion.Euler(0, 0, 0); // reset any rotation

            // Set-up mesh components
            try
            {
                meshFilter = GetComponent<MeshFilter>();
            }
            catch (NullReferenceException)
            {
                meshFilter = gameObject.AddComponent<MeshFilter>();
            }
            try
            {
                GetComponent<MeshRenderer>();
            }
            catch (NullReferenceException)
            {
                gameObject.AddComponent<MeshRenderer>();
            }
            try
            {
                meshCollider = GetComponent<MeshCollider>();
            }
            catch (NullReferenceException)
            {
                meshCollider = gameObject.AddComponent<MeshCollider>();
            }

            if (meshFilter.sharedMesh != null)
                DestroyImmediate(meshFilter.sharedMesh);
            if (meshCollider.sharedMesh != null)
                DestroyImmediate(meshCollider.sharedMesh);
            connections = new GameObject[0];
            snapNodes = new SnapPoint[0];
            connectionUniqueIDs = new string[0];

        }
        
        public void AttachMagnetRoad(int entranceNo)
        {
            if (entranceNo < 0 || entranceNo > snapNodes.Length-1 || snapNodes[0] == null)
            {
                throw new IndexOutOfRangeException("Entrance number out of range!");
            }

            var pointPositions = new Vector3[4];
            var roadLength = roadWidth * 2 * 3;
            var forwardDir = snapNodes[entranceNo].transform.forward;
            var roadCentrePosition = snapNodes[entranceNo].transform.position + forwardDir * (roadLength / 2);
            for (var i = 0; i < pointPositions.Length; i++)
            {
                pointPositions[i] = snapNodes[entranceNo].transform.position + forwardDir * (roadLength / 3 * i);
            }

            var newRoad = MagnetRoad.CreateNewSplineRoad().GetComponent<MagnetRoad>();
            newRoad.transform.position = roadCentrePosition;
            newRoad.splineSource.SetControlPoint(0, newRoad.splineSource.transform.InverseTransformPoint(pointPositions[0]));
            newRoad.splineSource.SetControlPoint(3, newRoad.splineSource.transform.InverseTransformPoint(pointPositions[3]));
            newRoad.splineSource.SetControlPoint(1, newRoad.splineSource.transform.InverseTransformPoint(pointPositions[1]));
            newRoad.splineSource.SetControlPoint(2, newRoad.splineSource.transform.InverseTransformPoint(pointPositions[2]));
            newRoad.roadWidth = roadWidth;
            newRoad.sideDepth = sideDepth;
            newRoad.slopeWidth = slopeWidth;
            newRoad.surfaceMaterial = surfaceMaterial;
            newRoad.sideMaterial = sideMaterial;
            newRoad.GenerateRoadMesh(newRoad.GenerateRoadVertexOutput(newRoad.roadWidth));
            newRoad.uniqueConnectionId = Guid.NewGuid().ToString();
            newRoad.AddConnection(true, this, entranceNo);
            AddConnection(entranceNo, newRoad);
        }

        public void SetConnectionIDs(string[] newConnections)
        {
            for (int i = 0; i < connections.Length; i++)
            {
                if (!connections[i] && !string.IsNullOrEmpty(newConnections[i]))
                {
                    connections[i] = MagnetRoad.FindGameObjectWithUniqueConnectionId(newConnections[i]);
                    if (!connections[i]) connectionUniqueIDs[i] = "";
                }
            }
        }

        protected void CheckForDisconnect()
        {
            for (int i = 0; i < connections.Length; i++)
            {
                var connection = connections[i];
                if (!connection) return;

                var magnetRoad = connection.GetComponent<MagnetRoad>();
                var isConnectionAtPositive = magnetRoad.GetPositiveConnection() == gameObject;
                var snapNode = isConnectionAtPositive ? magnetRoad.SnapNodePositive : magnetRoad.SnapNodeNegative;
                if (!snapNode) return;
                var roadEndVector = snapNode.transform.position;
                var distance = Vector3.Distance(roadEndVector, SnapNodes[i].transform.position);
                if (!(distance > roadWidth / 3)) return;

                RemoveConnection(i);
                magnetRoad.RemoveConnection(isConnectionAtPositive);
            }
        }

        public void RemoveConnection(int entranceNo)
        {
            if (!connections[entranceNo]) return;
            connections[entranceNo] = null;
            connectionUniqueIDs[entranceNo] = "";
        }

        public void SaveIntersectionToXml(string path = "DEFAULT_LOCATION")
        {
            try
            {
                var collection = new MagnetRoadCollection();
                var intersection = new DynamicIntersection[1];
                intersection[0] = this;
                collection.PrepareDynamicIntersectionData(intersection);
                collection.Save(path == "DEFAULT_LOCATION" ? Path.Combine(Application.persistentDataPath, "RoadData.xml") : path);
            }
            catch (IOException)
            {
                Debug.LogWarning("Failed to save the Intersection to a file, check the selected path.");
            }
        }

        private void OnDrawGizmos()
        {
#if UNITY_EDITOR
            var number = 0;
            foreach (var snapPoint in snapNodes)
            {
                if (!snapPoint) continue;
                if (Camera.current)
                {
                    var screenPoint = Camera.current.WorldToViewportPoint(snapPoint.transform.position);
                    var onScreen = screenPoint.z > 0 && screenPoint.x > 0 && screenPoint.x < 1 && screenPoint.y > 0 &&
                                   screenPoint.y < 1;
                    if (onScreen)
                    {
                        Handles.Label(snapPoint.transform.position + snapPoint.transform.forward * roadWidth / 2, number.ToString());
                    }
                }
                number++;
            }
#endif
            if (!drawOutlines) return;
            Vector3[] topVerts = GenerateTopPlaneVerts(roadWidth, connectionAmount, true);
            Vector3[] bottemVerts = null;
            if (sideDepth != 0.0f)
                bottemVerts = GenerateBottemPlaneVerts(roadWidth, connectionAmount, currentCenter, slopeWidth, Vector3.down * sideDepth);
            Gizmos.color = Color.gray;
            DrawVerts(topVerts,bottemVerts);
        }

        private void DrawVerts(Vector3[] topVerts, Vector3[] bottemVerts)
        {
            Vector3 possition = transform.position;
            for (int i = 1; i <topVerts.Length; i++)
            {
                Gizmos.DrawLine(topVerts[i - 1], topVerts[i]);
            }
            //Draw from last to first
            Gizmos.DrawLine(topVerts[topVerts.Length - 1], topVerts[0]);
            if (bottemVerts != null)
            {
                for (int i = 1; i < bottemVerts.Length; i++)
                {
                    Gizmos.DrawLine(bottemVerts[i - 1]+ possition, bottemVerts[i] + possition);
                }
                Gizmos.DrawLine(bottemVerts[topVerts.Length - 1] + possition, bottemVerts[0] + possition);
            }
            Gizmos.color = Color.blue;

            foreach (var vector in topVerts)
            {
                Gizmos.DrawLine(currentCenter+ possition, vector);
            }

            if (bottemVerts != null)
            {
                for (int i = 0; i < topVerts.Length; i++)
                {
                    Gizmos.DrawLine(topVerts[i], bottemVerts[i] + possition);
                }
            }
        }
    }


#if UNITY_EDITOR
    [CustomEditor(typeof(DynamicIntersection))]
    public class DynamicIntersectionEditorInspector : Editor
    {
        private DynamicIntersection dynamicIntersection;
        private Texture logo;
        private bool showAddRoadButtons;

        protected void OnEnable()
        {
            logo = (Texture)Resources.Load("logo", typeof(Texture));
        }

        public override void OnInspectorGUI()
        {
            dynamicIntersection =  target as DynamicIntersection;
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.Label(logo, GUILayout.Width((float)logo.width / 2), GUILayout.Height((float)logo.height / 2));
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            HorizontalLine();

            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.Label(MagnetRoad.VERSION_NUMBER + " - " + MagnetRoad.VERSION_DESCRIPTION, EditorStyles.miniBoldLabel);
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            HorizontalLine();

            // Default Inspector Stuff
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.Label("INTERSECTION DATA", EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            DrawDefaultInspector();
            var oldColor = GUI.color;
            GUI.color = new Color(1, 0.5f, 0.0f);

            HorizontalLine();

            if (GUILayout.Button("Generate Road Mesh"))
            {
                Undo.RecordObject(dynamicIntersection, "Generate Road Mesh");
                EditorUtility.SetDirty(dynamicIntersection);
                dynamicIntersection.SetUp();
            }
            GUI.color = new Color(.2f, .55f, 1);
            if (GUILayout.Button("Clear Road Mesh"))
            {
                Undo.RecordObject(dynamicIntersection, "Clear Road Mesh");
                EditorUtility.SetDirty(dynamicIntersection);
                dynamicIntersection.ClearIntersection();
            }

            HorizontalLine();

            GUI.color = oldColor;
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.Label("ADD ROADS", EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            showAddRoadButtons = EditorGUILayout.Foldout(showAddRoadButtons, "Road Selection");
            if (showAddRoadButtons)
            {
                GUI.color = new Color(1, 0.5f, 0.0f);
                if (GUILayout.Button("Add Roads to all connections"))
                {
                    for (int i = 0; i < dynamicIntersection.SnapNodes.Length; i++)
                        dynamicIntersection.AttachMagnetRoad(i);
                }
                GUI.color = oldColor;
                for (int i = 0; i < dynamicIntersection.SnapNodes.Length; i++)
                {
                    if (GUILayout.Button("Add Road At Point " + i))
                    {
                        dynamicIntersection.AttachMagnetRoad(i);
                    }
                }
            }
            HorizontalLine();

            // Save road
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.Label("SAVE", EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            GUI.color = new Color(.2f, .55f, 1);
            if (GUILayout.Button("Save Selected Intersection to XML"))
            {
                var path = EditorUtility.SaveFilePanel("Save Magnet Roads as XML", "", "UntitledDynamicIntersection", "xml");
                try
                {
                    dynamicIntersection.SaveIntersectionToXml(path);
                }
                catch (ArgumentException)
                {
                    // No folder selected - ignore
                }
            }

            HorizontalLine();

            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.Label("Copyright \u00A9 2017 - Torchbearer Interactive, Ltd.", EditorStyles.miniBoldLabel);
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            GUILayout.Space(5);
            ClampValues();
        }

        public static void HorizontalLine()
        {
            var old = GUI.color;
            GUI.color = new Color(.7f, .7f, .7f);
            GUILayout.Space(5);
            GUILayout.Box("", GUILayout.ExpandWidth(true), GUILayout.Height(1));
            GUILayout.Space(5);
            GUI.color = old;
        }
        private void ClampValues()
        {
          
            dynamicIntersection.roadWidth = Mathf.Clamp(dynamicIntersection.roadWidth, 0.1f, float.MaxValue);
            dynamicIntersection.connectionAmount = Mathf.Clamp(dynamicIntersection.connectionAmount, 3, int.MaxValue);
            dynamicIntersection.sideDepth = Mathf.Clamp(dynamicIntersection.sideDepth, 0.0f, float.MaxValue);
        }
    }

#endif
}
