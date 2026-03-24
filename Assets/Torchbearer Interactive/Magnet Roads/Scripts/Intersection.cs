// Copyright (C) Torchbearer Interactive, Ltd. - All Rights Reserved

// This class handles the mesh and information generation required to generate
// snappable intersections that link to SplineRoads

using UnityEngine;
using System;
using System.IO;
using System.Collections.Generic;
using TBUnityLib.Generic;
using TBUnityLib.MeshTools;
using RuntimeGizmo;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace MagnetRoads
{
    [Serializable] [ExecuteInEditMode] [AddComponentMenu("")]
    [RequireComponent(typeof(MeshFilter))] [RequireComponent(typeof(MeshRenderer))] [RequireComponent(typeof(MeshCollider))]
    public class Intersection : MonoBehaviour
    {
        [Tooltip("Road Material")]
        public Material surfaceMaterial;
        [Tooltip("Roadside Material")]
        public Material sideMaterial;
        [Tooltip("Sidewalk Material")]
        public Material sideWalkMaterial;
        [Tooltip("Road width value")]
        public float roadWidth;
        [Tooltip("Depth of the road's sides")]
        public float sideDepth;
        [Tooltip("Slope of the road's sides")]
        public float slopeWidth;
        public float sidewalkWidth;
        public float sidewalkHeight;
        [HideInInspector]
        public string uniqueConnectionId;

        [SerializeField] [HideInInspector]
        protected string connectedUniqueId0;
        [SerializeField] [HideInInspector]
        protected string connectedUniqueId1;
        [SerializeField] [HideInInspector]
        protected string connectedUniqueId2;
        [SerializeField] [HideInInspector]
        protected string connectedUniqueId3;

        private static Material sDefaultRoadMat;
        private static Material sDefaultSideMat;
        private static Material sDefaultSidewalkMat;
        private GameObject[] connections = new GameObject[0];
        private Material cachedSideMaterial;
        private Vector3 cachedPosition;
        private Quaternion cachedRotation;
        private float cachedRoadWidth;
        private float cachedSideDepth;
        private float cachedSlopeWidth;
        private float cachedSidwalkHeight;
        private Mesh mesh;
        private MeshFilter meshFilter;
        private MeshCollider meshCollider;
        private GameObject snapNodeParent;
        [SerializeField] [HideInInspector]
        private IntersectionType intersectionType;
        private Gizmo gizmo;
        private GizmoSelectable gizmoSelect;
        [SerializeField] [Tooltip("Set this Intersection to be editable at runtime")]
        private bool isEditableAtRuntime;

        public GameObject[] Connections
        {
            get
            {
                return connections;
            }
        }

        public SnapPoint[] SnapNodes
        {
            get
            {
                return gameObject.GetComponentsInChildren<SnapPoint>();
            }
        }

        public enum IntersectionType
        {
            ThreeLane = 3,
            FourLane = 4,
            TwoLane = 2
        }

        public IntersectionType CurrentIntersectionType
        {
            get
            {
                return intersectionType;
            }
        }

        public bool IsEditableAtRuntime
        {
            get
            {
                return isEditableAtRuntime;
            }
        }

        public void SetUp(IntersectionType type)
        {
            intersectionType = type;
            if (roadWidth.Equals(0)) roadWidth = 0.5f;
            if (sideDepth.Equals(0)) sideDepth = 0.2f;
            cachedPosition = transform.position;
            cachedRotation = transform.rotation;
            cachedRoadWidth = roadWidth;
            cachedSideDepth = sideDepth;
            cachedSlopeWidth = slopeWidth;
            if (!sDefaultRoadMat)
            {
                sDefaultRoadMat = intersectionType == IntersectionType.ThreeLane
                    ? Resources.Load<Material>("Materials/asphalt_threeway")
                    : intersectionType == IntersectionType.FourLane ? 
                    Resources.Load<Material>("Materials/asphalt_fourway") :
                    Resources.Load<Material>("Materials/asphalt_twoway");
            }
            if (!sDefaultSideMat)
            {
                sDefaultSideMat = Resources.Load<Material>("Materials/road_sides");
            }
            if(!sDefaultSidewalkMat)
            {
                sDefaultSidewalkMat = Resources.Load<Material>("Materials/road_sidewalk");
            }
            if (sDefaultRoadMat && !surfaceMaterial) surfaceMaterial = sDefaultRoadMat;
            if (sDefaultSideMat && !sideMaterial) sideMaterial = sDefaultSideMat;
            if (uniqueConnectionId == null) uniqueConnectionId = Guid.NewGuid().ToString();
            GenerateIntersectionMesh();
            connections = new GameObject[(int)intersectionType];
        }

        protected void Start()
        {
            // Check connections exist - won't if cloned
            if (connections.Length <= 0)
            {
                SetUp(intersectionType);
            }
        }
        
        protected void Update()
        {
            // Check for missing connections - if missing assume invalid & remove
            if (connections.Length > 0)
            {
                if (!connections[0] && !string.IsNullOrEmpty(connectedUniqueId0))
                {
                    connections[0] = MagnetRoad.FindGameObjectWithUniqueConnectionId(connectedUniqueId0);
                    if (!connections[0]) connectedUniqueId0 = "";
                }
                if (!connections[1] && !string.IsNullOrEmpty(connectedUniqueId1))
                {
                    connections[1] = MagnetRoad.FindGameObjectWithUniqueConnectionId(connectedUniqueId1);
                    if (!connections[1]) connectedUniqueId1 = "";
                }
                if ((int)intersectionType > 2)
                {
                    if (!connections[2] && !string.IsNullOrEmpty(connectedUniqueId2))
                    {
                        connections[2] = MagnetRoad.FindGameObjectWithUniqueConnectionId(connectedUniqueId2);
                        if (!connections[2]) connectedUniqueId2 = "";
                    }
                }
                if ((int) intersectionType > 3)
                {
                    if (!connections[3] && !string.IsNullOrEmpty(connectedUniqueId3))
                    {
                        connections[3] = MagnetRoad.FindGameObjectWithUniqueConnectionId(connectedUniqueId3);
                        if (!connections[3]) connectedUniqueId3 = "";
                    }
                }
            }

            // Store an instance of the gizmo if possible
            if (isEditableAtRuntime && !gizmo)
            {
                gizmo = FindObjectOfType<Gizmo>();
            }

            // Check whether to update the intersection mesh
            if (isEditableAtRuntime)
            {
                if (transform.position != cachedPosition || transform.rotation != cachedRotation || !roadWidth.Equals(cachedRoadWidth) || !sideDepth.Equals(cachedSideDepth) || !slopeWidth.Equals(cachedSlopeWidth))
                {
                    GenerateIntersectionMesh();
                }
                cachedRoadWidth = roadWidth;
                cachedSideDepth = sideDepth;
                cachedSlopeWidth = slopeWidth;
            }

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

            // Constrain values
            if (roadWidth < 0) roadWidth = 0.01f; 
            if (slopeWidth < 0) slopeWidth = 0;

            // Cache required variables
            if (transform.position != cachedPosition || cachedRotation != transform.rotation)
            {
                for (var i = 0; i < Connections.Length; i++)
                {
                    CheckForDisconnect(i);
                }
                cachedRotation = transform.rotation;
                cachedPosition = transform.position;
            }

            // Ensure snap node's isEditableAtRuntime matches their parent
            foreach (SnapPoint node in SnapNodes)
            {
                if (node.isEditableAtRuntime != IsEditableAtRuntime)
                {
                    node.isEditableAtRuntime = IsEditableAtRuntime;
                }
            }

            // When selected by the editor gizmo hide the snapPoints
            if (!gizmo) return;
            foreach (var point in SnapNodes)
            {
                if (point.GetComponent<Renderer>()) point.GetComponent<Renderer>().enabled = !gizmo.selectedObject == transform;
            }
        }

        protected void OnDrawGizmos()
        {
#if UNITY_EDITOR
            var number = 0;
            foreach (var snapPoint in SnapNodes)
            {
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
        }
        
        public void GenerateIntersectionMesh()
        {
            cachedSidwalkHeight = -sidewalkHeight;
            // Store roadSide texture
            cachedRotation = transform.rotation;
            if (transform.Find("Intersection Sides"))
                cachedSideMaterial = transform.Find("Intersection Sides").gameObject.GetComponent<Renderer>().sharedMaterial;

            // Refresh object information
            foreach (var node in SnapNodes)
            {
                DestroyImmediate(node.gameObject);
            }
            if (snapNodeParent) DestroyImmediate(snapNodeParent);
            if (transform.Find("Intersection Underside")) DestroyImmediate(transform.Find("Intersection Underside").gameObject);
            if (transform.Find("Intersection Sides")) DestroyImmediate(transform.Find("Intersection Sides").gameObject);
            if (transform.Find("Snap Points")) DestroyImmediate(transform.Find("Snap Points").gameObject);
            if (transform.Find("Intersection SideWalk Path")) DestroyImmediate(transform.Find("Intersection SideWalk Path").gameObject);
            if (transform.Find("Intersection SideWalk Sides")) DestroyImmediate(transform.Find("Intersection SideWalk Sides").gameObject);
            transform.rotation = Quaternion.Euler(0, 0, 0); // reset any rotations

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

            // Generate road mesh
            mesh = Geometry.GeneratePlaneMesh(roadWidth, roadWidth);

            mesh.name = "Procedural Intersection";
            meshFilter.mesh = mesh;
            meshCollider.sharedMesh = mesh;
            if (surfaceMaterial) GetComponent<Renderer>().sharedMaterial = surfaceMaterial;

            // Create the SnapPoint parent object
            snapNodeParent = new GameObject();
            snapNodeParent.transform.position = transform.position;
            snapNodeParent.transform.parent = transform;
            snapNodeParent.name = "Snap Points";

            Pair<Vector3>[] sideVerts;
            Pair<Vector3>[] topVerts = GenerateSideWalkVerts(roadWidth, sidewalkWidth, cachedSidwalkHeight, out sideVerts);

            var sideWalksPath = new GameObject();
            var sideWalksPathMesh = Geometry.GenerateStrip(topVerts, transform, true, null, "Intersection Top");
            sideWalksPath.AddComponent<MeshFilter>().mesh = sideWalksPathMesh;
            sideWalksPath.AddComponent<MeshRenderer>();
            sideWalksPath.AddComponent<MeshCollider>().sharedMesh = sideWalksPathMesh;
            sideWalksPath.GetComponent<Renderer>().sharedMaterial = !sideWalkMaterial ? cachedSideMaterial : sideWalkMaterial;
            sideWalksPath.transform.position = new Vector3(transform.position.x, transform.position.y, transform.position.z);
            sideWalksPath.transform.Rotate(new Vector3(180, intersectionType == IntersectionType.TwoLane ? 90 : 180, 0));
            sideWalksPath.transform.SetParent(transform);
            //sideWalks.gameObject.hideFlags = HideFlags.HideInHierarchy;
            sideWalksPath.name = "Intersection SideWalk Path";
            if (cachedSidwalkHeight != 0.0f)
            {
                var sideWalksSides = new GameObject();
                var sideWalksSidesMesh = Geometry.GenerateStrip(sideVerts, transform, cachedSidwalkHeight > 0.0f, null, "Intersection Side");
                sideWalksSides.AddComponent<MeshFilter>().mesh = sideWalksSidesMesh;
                sideWalksSides.AddComponent<MeshRenderer>();
                sideWalksSides.AddComponent<MeshCollider>().sharedMesh = sideWalksSidesMesh;
                if (cachedSidwalkHeight < 0.0f)
                    sideWalksSides.GetComponent<Renderer>().sharedMaterial = !sideMaterial ? cachedSideMaterial : sideMaterial;
                else
                    sideWalksSides.GetComponent<Renderer>().sharedMaterial = Resources.Load<Material>("Materials/road_sides_both_faces");

                sideWalksSides.transform.position = new Vector3(transform.position.x, transform.position.y, transform.position.z);
                sideWalksSides.transform.Rotate(new Vector3(180, intersectionType == IntersectionType.TwoLane ? 90 : 180, 0));
                sideWalksSides.transform.SetParent(transform);
                //sideWalks.gameObject.hideFlags = HideFlags.HideInHierarchy;
                sideWalksSides.name = "Intersection SideWalk Sides";
            }

            // Generate side mesh & game object
            var sides = new GameObject();
            var sideMesh = Geometry.GenerateTetrahedron(roadWidth, roadWidth, sideDepth, roadWidth+slopeWidth, roadWidth+slopeWidth, false, false);
            sides.AddComponent<MeshFilter>().mesh = sideMesh;
            sides.AddComponent<MeshRenderer>();
            sides.AddComponent<MeshCollider>().sharedMesh = sideMesh;
            sides.GetComponent<Renderer>().sharedMaterial = !sideMaterial ? cachedSideMaterial : sideMaterial;
            sides.transform.position = transform.position;
            sides.transform.SetParent(transform);
            //sides.gameObject.hideFlags = HideFlags.HideInHierarchy;
            sides.name = "Intersection Sides";

            // Generate underside mesh
            var underSide = new GameObject();
            var underSideMesh = Geometry.GeneratePlaneMesh(roadWidth, roadWidth);
            underSide.AddComponent<MeshFilter>().mesh = underSideMesh;
            underSide.AddComponent<MeshRenderer>();
            underSide.AddComponent<MeshCollider>().sharedMesh = underSideMesh;
            underSide.GetComponent<Renderer>().sharedMaterial = !sideMaterial ? cachedSideMaterial : sideMaterial;
            underSide.transform.position = new Vector3(transform.position.x, transform.position.y - sideDepth, transform.position.z);
            underSide.transform.Rotate(new Vector3(180, 0, 0));
            underSide.transform.SetParent(transform);
            underSide.gameObject.hideFlags = HideFlags.HideInHierarchy;
            underSide.name = "Intersection Underside";

            // Generate snap points on each edge of the intersection
            switch (intersectionType)
            {
                case IntersectionType.TwoLane:
                    CreateSnapPoint(Vector3.left * (roadWidth / 2), Quaternion.Euler(0, -90, 0), SnapPoint.PointEnd.Bipolar, "SnapPoint1");
                    CreateSnapPoint(Vector3.forward * (roadWidth / 2), Quaternion.Euler(0, 0, 0), SnapPoint.PointEnd.Bipolar, "SnapPoint2");
                    //CreateSnapPoint(Vector3.right * (roadWidth / 2), Quaternion.Euler(0, 90, 0), SnapPoint.PointEnd.Bipolar, "SnapPoint3");
                    break;
                case IntersectionType.ThreeLane:
                    CreateSnapPoint(Vector3.left * (roadWidth / 2), Quaternion.Euler(0, -90, 0), SnapPoint.PointEnd.Bipolar, "SnapPoint1");
                    CreateSnapPoint(Vector3.forward * (roadWidth / 2), Quaternion.Euler(0, 0, 0), SnapPoint.PointEnd.Bipolar, "SnapPoint2");
                    CreateSnapPoint(Vector3.right * (roadWidth / 2), Quaternion.Euler(0, 90, 0), SnapPoint.PointEnd.Bipolar, "SnapPoint3");
                    break;
                case IntersectionType.FourLane:
                    CreateSnapPoint(Vector3.left * (roadWidth / 2), Quaternion.Euler(0, -90, 0), SnapPoint.PointEnd.Bipolar, "SnapPoint1");
                    CreateSnapPoint(Vector3.forward * (roadWidth / 2), Quaternion.Euler(0, 0, 0), SnapPoint.PointEnd.Bipolar, "SnapPoint2");
                    CreateSnapPoint(Vector3.right * (roadWidth / 2), Quaternion.Euler(0, 90, 0), SnapPoint.PointEnd.Bipolar, "SnapPoint3");
                    CreateSnapPoint(Vector3.back * (roadWidth / 2), Quaternion.Euler(0, 180, 0), SnapPoint.PointEnd.Bipolar, "SnapPoint4");
                    break;
            }

            // Rotate back into place
            transform.rotation = cachedRotation;
        }

        private Pair<Vector3>[] GenerateSideWalkVerts(float currnetRoadWidth , float currnetSidewalkWidth, float currnetSidewalkHeight, out Pair<Vector3>[] sideVectors)
        {
            List<Pair<Vector3>> vertPairs = new List<Pair<Vector3>>();
            if (intersectionType == IntersectionType.FourLane || currnetSidewalkWidth <= 0.0f)
            {
                sideVectors = vertPairs.ToArray();
                return vertPairs.ToArray();
            }
            var halfRoadWidth = currnetRoadWidth / 2f;
            //Generate Top
            vertPairs.Add(new Pair<Vector3>(new Vector3(-halfRoadWidth, currnetSidewalkHeight, -halfRoadWidth), new Vector3(-halfRoadWidth, currnetSidewalkHeight, -halfRoadWidth - currnetSidewalkWidth)));
            vertPairs.Add(new Pair<Vector3>(new Vector3(halfRoadWidth, currnetSidewalkHeight, -halfRoadWidth), new Vector3(halfRoadWidth, currnetSidewalkHeight, -halfRoadWidth - currnetSidewalkWidth)));
            if(intersectionType == IntersectionType.TwoLane)
            {
                vertPairs.Add(new Pair<Vector3>(new Vector3(halfRoadWidth, currnetSidewalkHeight, -halfRoadWidth), new Vector3(halfRoadWidth + currnetSidewalkWidth, currnetSidewalkHeight, -halfRoadWidth)));
                vertPairs.Add(new Pair<Vector3>(new Vector3(halfRoadWidth, currnetSidewalkHeight, halfRoadWidth), new Vector3(halfRoadWidth + currnetSidewalkWidth, currnetSidewalkHeight, halfRoadWidth)));
            }
            Pair<Vector3>[] topVerts = vertPairs.ToArray();
            vertPairs.Clear();
            Vector3 toBottem = new Vector3(0.0f, currnetSidewalkHeight);
            Vector3 zClipOffset;
            if (currnetSidewalkHeight > 0f)
                zClipOffset = new Vector3(0.0001f, 0.0f, 0.0001f);
            else
                zClipOffset = new Vector3();
            Vector3[] leftVerts, rightVerts;
            Helper.SplitPairArray(topVerts, out leftVerts, out rightVerts);
            List<Vector3> rightVersList = new List<Vector3>(rightVerts);
            rightVersList.Reverse();
            //Generate Siding
            foreach (var point in rightVersList)
            {
                vertPairs.Add(new Pair<Vector3>(point, point - toBottem));
            }
            foreach (var point in leftVerts)
            {
                vertPairs.Add(new Pair<Vector3>(point + zClipOffset, point - toBottem + zClipOffset));
            }
            vertPairs.Add(new Pair<Vector3>(rightVersList[0], rightVersList[0]-toBottem));
            sideVectors = vertPairs.ToArray();
            return topVerts;
        }

        private void DrawRoadOutline(IList<Pair<Vector3>> vertexData)
        {
            if (vertexData.Count == 0)
                return;
            Gizmos.color = Color.blue;
            var position = transform.position;
            var current = vertexData[0];
            Gizmos.DrawLine(current.First+ position, current.Second+ position);
            var last = current;
            for (var i = 1; i <= vertexData.Count - 1; i++)
            {
                current = vertexData[i];
                Gizmos.DrawLine(current.First+ position, current.Second+ position);
                Gizmos.DrawLine(current.First+ position, last.First+ position);
                Gizmos.DrawLine(current.Second+ position, last.Second+ position);
                last = current;
            }
        }
        private void CreateSnapPoint(Vector3 offset, Quaternion rotation, SnapPoint.PointEnd polarity, string pointName)
        {
            var snapPoint = new GameObject();
            snapPoint.AddComponent<SnapPoint>().SetUp(polarity, roadWidth);
            snapPoint.transform.position = transform.position + offset;
            snapPoint.transform.rotation = rotation;
            snapPoint.transform.parent = snapNodeParent.transform;
            snapPoint.name = pointName;
        }

        public void AttachMagnetRoad(int entranceNo)
        {
            if (entranceNo < 0 || entranceNo > (int) intersectionType - 1)
            {
                throw new IndexOutOfRangeException("Entrance number out of range!");
            }

            var pointPositions = new Vector3[4];
            var roadLength = roadWidth * 2 * 3;
            var forwardDir = SnapNodes[entranceNo].transform.forward;
            var roadCentrePosition = SnapNodes[entranceNo].transform.position + forwardDir * (roadLength / 2);
            for (var i = 0; i < pointPositions.Length; i++)
            {
                pointPositions[i] = SnapNodes[entranceNo].transform.position + forwardDir * (roadLength / 3 * i);
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

        public void CheckForDisconnect(int entranceNo)
        {
            var connection = connections[entranceNo];
            if (!connection) return;

            var magnetRoad = connection.GetComponent<MagnetRoad>();
            var isConnectionAtPositive = magnetRoad.GetPositiveConnection() == gameObject;
            var roadEndVector = isConnectionAtPositive ? magnetRoad.SnapNodePositive.transform.position : magnetRoad.SnapNodeNegative.transform.position;

            var distance = Vector3.Distance(roadEndVector, SnapNodes[entranceNo].transform.position);
            if (!(distance > roadWidth / 3)) return;

            RemoveConnection(entranceNo);
            magnetRoad.RemoveConnection(isConnectionAtPositive);
        }

        public void AddConnection(int entranceNo, MagnetRoad connection)
        {
            connections[entranceNo] = connection.gameObject;
            SetConnectionUniqueId(entranceNo, connection.uniqueConnectionId);
        }

        public void RemoveConnection(int entranceNo)
        {
            if (!connections[entranceNo]) return;
            connections[entranceNo] = null;
            SetConnectionUniqueId(entranceNo, "");
        }

        public void SetConnectionUniqueId(int entranceNo, string uniqueId)
        {
            if (entranceNo == 0) connectedUniqueId0 = uniqueId;
            if (entranceNo == 1) connectedUniqueId1 = uniqueId;
            if (entranceNo == 2) connectedUniqueId2 = uniqueId;
            if ((int) intersectionType < 4) return;
            if (entranceNo == 3) connectedUniqueId3 = uniqueId;
        }

        public string GetConnectionUniqueId(int connectionIndex)
        {
            connectionIndex = Mathf.Clamp(connectionIndex, 0, (int)intersectionType - 1);

            switch (connectionIndex)
            {
                case 0:
                    return connectedUniqueId0;
                case 1:
                    return connectedUniqueId1;
                case 2:
                    return connectedUniqueId2;
                case 3:
                    return connectedUniqueId3;
            }

            return null;
        }

        public void EnableRuntimeEditing()
        {
            isEditableAtRuntime = true;
        }

        public void DisableRuntimeEditing()
        {
            isEditableAtRuntime = false;
        }

        public void SaveIntersectionToXml(string path = "DEFAULT_LOCATION")
        {
            try
            {
                var collection = new MagnetRoadCollection();
                var intersection = new Intersection[1];
                intersection[0] = this;
                collection.PrepareIntersectionData(intersection);
                collection.Save(path == "DEFAULT_LOCATION" ? Path.Combine(Application.persistentDataPath, "RoadData.xml") : path);
            }
            catch (IOException)
            {
                Debug.LogWarning("Failed to save the Intersection to a file, check the selected path.");
            }
        }
    }

#if UNITY_EDITOR
    [CustomEditor(typeof(Intersection))]
    public class IntersectionEditorInspector : Editor
    {
        private Intersection intersection;
        private Texture logo;

        protected void OnEnable()
        {
            logo = (Texture)Resources.Load("logo", typeof(Texture));
        }

        public override void OnInspectorGUI()
        {
            intersection = target as Intersection;

            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.Label(logo, GUILayout.Width((float)logo.width / 2), GUILayout.Height((float)logo.height / 2));
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            SplineRoadEditorInspector.HorizontalLine();

            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.Label(MagnetRoad.VERSION_NUMBER + " - " + MagnetRoad.VERSION_DESCRIPTION, EditorStyles.miniBoldLabel);
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            SplineRoadEditorInspector.HorizontalLine();

            // Intersection editing
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.Label("INTERSECTION DATA", EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            DrawDefaultInspector();
            var oldColor = GUI.color;
            GUI.color = new Color(1, 0.5f, 0.0f);

            SplineRoadEditorInspector.HorizontalLine();

            // Intersection generation
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.Label("GENERATION", EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            if (intersection.IsEditableAtRuntime) GUI.enabled = false;
            if (GUILayout.Button("Regenerate Intersection Mesh"))
            {
                Undo.RecordObject(intersection, "Generate Intersection Mesh");
                EditorUtility.SetDirty(intersection);
                intersection.GenerateIntersectionMesh();
            }
            if (intersection.IsEditableAtRuntime) GUI.enabled = true;
            GUI.color = oldColor;

            SplineRoadEditorInspector.HorizontalLine();

            // Connection data
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.Label("CONNECTIONS", EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            GUI.enabled = false;
            for (var i = 0; i < intersection.Connections.Length; i++)
            {
                var temp = intersection.Connections[i];
                var connectionString = "Entrance " + i + ":";
                EditorGUILayout.ObjectField(connectionString, temp, typeof(MagnetRoad), true);
            }
            GUI.enabled = true;
            GUI.color = oldColor;

            SplineRoadEditorInspector.HorizontalLine();

            // Add roads
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.Label("ADD ROADS", EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            for (var i = 0; i < intersection.Connections.Length; i++)
            {
                if (GUILayout.Button("Add Magnet Road at Connection " + i))
                {
                    intersection.AttachMagnetRoad(i);
                }
            }

            SplineRoadEditorInspector.HorizontalLine();

            // Save road
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.Label("SAVE", EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            GUI.color = new Color(.2f, .55f, 1);
            if (GUILayout.Button("Save Selected Intersection to XML"))
            {
                var path = EditorUtility.SaveFilePanel("Save Magnet Roads as XML", "", "UntitledIntersection", "xml");
                try
                {
                    intersection.SaveIntersectionToXml(path);
                }
                catch (ArgumentException)
                {
                    // No folder selected - ignore
                }
            }

            SplineRoadEditorInspector.HorizontalLine();

            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.Label("Copyright \u00A9 2017 - Torchbearer Interactive, Ltd.", EditorStyles.miniBoldLabel);
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            GUILayout.Space(5);
        }
    }
#endif
}