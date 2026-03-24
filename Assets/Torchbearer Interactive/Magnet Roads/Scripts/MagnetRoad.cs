// Copyright (C) Torchbearer Interactive, Ltd. - All Rights Reserved

// This class handles the specic mesh generation and information neccecary to 
// create and retrieve information from spline roads as well as perform saving
// and loading functions.

using UnityEngine;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TBUnityLib.Generic;
using TBUnityLib.MeshTools;
using RuntimeGizmo;
using BezierSplines;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace MagnetRoads
{
    [Serializable] [ExecuteInEditMode] [AddComponentMenu("")]
    [RequireComponent(typeof(MeshFilter))] [RequireComponent(typeof(MeshRenderer))] [RequireComponent(typeof(BezierSpline))] [RequireComponent(typeof(MeshCollider))]
    public class MagnetRoad : MonoBehaviour
    {
        public const string VERSION_NUMBER = "v3.0.0";
        public const string VERSION_DESCRIPTION = "Magnet Roads for Unity 2017";
        [Serializable]
        public struct RoadConnection
        {
            public GameObject gameObject;
            public bool positiveConnection;
        }

        [HideInInspector]
        public BezierSpline splineSource;
        [Tooltip("Road Material")]
        public Material surfaceMaterial;
        [Tooltip("Roadside Material")]
        public Material sideMaterial;
        [Tooltip("Sidewalk Material")]
        public Material sidewalkMaterial;
        public float roadWidth = 0.5f;
        public float sidewalkWidth;
        public float sidewalkHeight;
        [Tooltip("Depth of the road's sides")]
        public float sideDepth = 0.2f;
        [Tooltip("The distance from the bottom of the side ramp to the road side")]
        public float slopeWidth;
        [Tooltip("Steps per curve/mesh resolution")]
        public int stepsPerCurve = 20;
        [Tooltip("Show road outline")]
        public bool showRoadOutline = true;
        [Tooltip("Buffer space at edge of road before lanes start")]
        public float roadsideMargin;
        [Tooltip("Total no. of car lanes on this road")]
        public int totalCarLanes = 2;
        [Tooltip("Show car routes")]
        public bool showCarRoutes = true;
        [Tooltip("Toggle road snapping to terrain")] 
        public bool snapRoadToTerrain;
        [Tooltip("Terrain to snap road to")]
        public Terrain terrain;
        [Tooltip("Distance road will be from terrain when snapping")]
        public float distanceFromTerrain = 0.05f;
        [Tooltip("Toggle road snapping to collider")]
        public bool snapRoadToCollider;
        [Tooltip("Collider to snap road to")]
        public MeshCollider snapCollider;
        [Tooltip("Distance road will be from collider when snapping")]
        public float distanceFromCollider = 0.05f;
        [Tooltip("Distance of the raycast to the collider")]
        public float distanceRaycastCollider = 500f;
        [HideInInspector]
        public string uniqueConnectionId;

        [HideInInspector]
        public bool shouldShowAdvancedTools;
        [HideInInspector]
        public Mesh roadsideFencePanelMesh;
        [HideInInspector]
        public Vector2 roadsideFencePanelScaling = new Vector2(1, 1);
        [HideInInspector]
        public Vector3 roadsideFencePanelRotation;
        [HideInInspector]
        public Material roadsideFencePanelMaterial;
        [HideInInspector]
        public Mesh roadsideFencePostMesh;
        [HideInInspector]
        public Vector3 roadsideFencePostScaling = new Vector3(1, 1, 1);
        [HideInInspector]
        public Vector3 roadsideFencePostRotation;
        [HideInInspector]
        public Material roadsideFencePostMaterial;
        [HideInInspector]
        public float fenceDistanceFromRoad;
        [HideInInspector]
        public Mesh centerFencePanelMesh;
        [HideInInspector]
        public Vector2 centerFencePanelScaling = new Vector2(1, 1);
        [HideInInspector]
        public Vector3 centerFencePanelRotation;
        [HideInInspector]
        public Material centerFencePanelMaterial;
        [HideInInspector]
        public Mesh centerFencePostMesh;
        [HideInInspector]
        public Vector3 centerFencePostScaling = new Vector3(1, 1, 1);
        [HideInInspector]
        public Vector3 centerFencePostRotation;
        [HideInInspector]
        public Material centerFencePostMaterial;
        [HideInInspector]
        public Vector2 reservationDimensions = new Vector3(0, 0, 0);
        [HideInInspector]
        public float reservationSlope;
        [HideInInspector]
        public Material reservationTopMaterial;
        [HideInInspector]
        public Material reservationSideMaterial;
        [HideInInspector]
        public Mesh centerObjectMesh;
        [HideInInspector]
        public int centerObjectsToSpawn;
        [HideInInspector]
        public Vector3 centerObjectScaling = new Vector3(0, 0, 0);
        [HideInInspector]
        public Vector3 centerObjectRotation;
        [HideInInspector]
        public Material centerObjectMaterial;
        [HideInInspector]
        public bool shouldShowDecalList;
        [HideInInspector]
        public List<MagnetDecal> roadDecals;


        [SerializeField] [HideInInspector]
        private string positiveConnectionUniqueId;
        [SerializeField] [HideInInspector]
        private string negativeConnectionUniqueId;
        private static Material sDefaultRoadMat;
        private static Material sDefaultSideMat;
        private RoadConnection positiveConnection;
        private RoadConnection negativeConnection;
        private GameObject leftSide;
        private GameObject rightSide;
        private GameObject frontSide;
        private GameObject backSide;
        private GameObject underSide;
        private GameObject leftSidewalk;
        private GameObject rightSidewalk;
        private Material cachedSideMaterial;
        private Mesh mesh;
        private MeshFilter meshFilter;
        private MeshCollider meshCollider;
        private Gizmo gizmo;
        private GizmoSelectable gizmoSelect;
        private LineRenderer runtimeCurveLine;
        private LineRenderer[] runtimeHandleLines;
        private GameObject[] runtimeHandles;
        private Vector3[] cachedPointVectors;
        private Vector3 cachedTransformPosition;
        [SerializeField] [Tooltip("Set this Magnet Road to be editable at runtime")]
        private bool editAtRuntime;
        [SerializeField] [Tooltip("Show child objects, like road sides etc. in the hierarchy")]
        private bool showChildObjects = false;

        public string PositiveConnectionUniqueId
        {
            get
            {
                return positiveConnectionUniqueId;
            }
            private set
            {
                if (positiveConnectionUniqueId == null)
                {
                    positiveConnectionUniqueId = value;
                }
            }
        }

        public string NegativeConnectionUniqueId
        {
            get
            {
                return negativeConnectionUniqueId;
            }
            private set
            {
                if (negativeConnectionUniqueId == null)
                {
                    negativeConnectionUniqueId = value;
                }
            }
        }

        public GameObject SnapNodeNegative
        {
            get;
            private set;
        }

        public GameObject SnapNodePositive
        {
            get;
            private set;
        }

        public bool IsEditableAtRuntime
        {
            get
            {
                return editAtRuntime;
            } 
        }

        public RoadConnection PositiveRoadConnection
        {
            get { return positiveConnection; }
        }

        public RoadConnection NegativeRoadConnection
        {
            get { return negativeConnection; }
        }

        protected void Awake()
        {
            if (splineSource == null)
            {
                // Create spline or throw error
                try
                {
                    splineSource = GetComponent<BezierSpline>();
                }
                catch (NullReferenceException)
                {
                    Debug.LogWarning("Spline Road missing Bezier Spline! Component added automatically.");
                    splineSource = gameObject.AddComponent<BezierSpline>();
                }

#if UNITY_EDITOR
                // Perform some inspector formatting
                for (var i = 0; i < 10; i++) UnityEditorInternal.ComponentUtility.MoveComponentDown(this);
                for (var i = 0; i < 10; i++) UnityEditorInternal.ComponentUtility.MoveComponentDown(splineSource);
#endif
            }

            if (uniqueConnectionId == null) uniqueConnectionId = Guid.NewGuid().ToString();
            if (!sDefaultRoadMat) sDefaultRoadMat = Resources.Load<Material>("Materials/asphalt_road");
            if (!sDefaultSideMat) sDefaultSideMat = Resources.Load<Material>("Materials/road_sides");
            if (sDefaultRoadMat) surfaceMaterial = sDefaultRoadMat;
            if (sDefaultSideMat) sideMaterial = sDefaultSideMat;
            runtimeHandles = new GameObject[0];
            runtimeHandleLines = new LineRenderer[0];
            cachedPointVectors = new Vector3[0];
            cachedTransformPosition = transform.position;
            if (roadDecals == null) roadDecals = new List<MagnetDecal>();
        }

        protected void Start()
        {
            CleanupRuntimeHandles();
            InitializeRuntimeHandles();
        }

        protected void OnDrawGizmos()
        {
            // Draw road lanes
            if (showCarRoutes) DrawCarPaths();

            // Draw road outlines
            if (!showRoadOutline) return;
            DrawRoadOutline(GenerateRoadVertexOutput(roadWidth));
            DrawRoadOutline(GenerateLeftRoadSideVectors(GenerateRoadVertexOutput(roadWidth + sidewalkWidth * 2)));
            DrawRoadOutline(GenerateRightRoadSideVectors(GenerateRoadVertexOutput(roadWidth + sidewalkWidth * 2)));
        }

        protected void Update()
        {
            // Clamp variables
            if (!Application.isPlaying || IsEditableAtRuntime)
            {
                roadWidth = Mathf.Clamp(roadWidth, 0.00001f, float.MaxValue);
                stepsPerCurve = Mathf.Clamp(stepsPerCurve, 1, int.MaxValue);
                totalCarLanes = Mathf.Clamp(totalCarLanes, 1, int.MaxValue);
                roadsideMargin = Mathf.Clamp(roadsideMargin, 0.0f, roadWidth / 2);
            }

            // Check for missing connections - if missing assume invalid & remove
            if (!positiveConnection.gameObject && !string.IsNullOrEmpty(positiveConnectionUniqueId))
            {
                positiveConnection.gameObject = FindGameObjectWithUniqueConnectionId(positiveConnectionUniqueId);
                if (!positiveConnection.gameObject) positiveConnectionUniqueId = "";
            }
            if (!negativeConnection.gameObject && !string.IsNullOrEmpty(negativeConnectionUniqueId))
            {
                negativeConnection.gameObject = FindGameObjectWithUniqueConnectionId(negativeConnectionUniqueId);
                if (!negativeConnection.gameObject) positiveConnectionUniqueId = "";
            }

            // Find snap nodes if lost
            if (!SnapNodeNegative || !SnapNodePositive)
            {
                try
                {
                    SnapNodeNegative = transform.Find("SnapNodeNegative").gameObject;
                    SnapNodePositive = transform.Find("SnapNodePositive").gameObject;
                }
                catch (NullReferenceException e)
                {
                    Debug.LogError(name + " has no Snap Nodes attached! Please regenerate the road mesh! " + e);
                    throw;
                }
            }

            // Lock transform rotation & scale
            transform.rotation = Quaternion.Euler(0, 0, 0);
            transform.localScale = new Vector3(1, 1, 1);

            // Check for position changes
            if (transform.position != cachedTransformPosition)
            {
                cachedTransformPosition = transform.position;
                UpdateConnections();
            }

            // Terrain snapping for bezier points
            if (snapRoadToTerrain && terrain)
            {
                for (var i = 0; i < splineSource.ControlPointCount; i++)
                {
                    if (i <= 1 && GetPositiveConnection_Intersection() != null) continue;
                    if (i >= splineSource.ControlPointCount - 2 && GetNegativeConnection_Intersection() != null) continue;
                    if (i % 3 != 0) continue;

                    var newPointPos = splineSource.transform.TransformPoint(splineSource.GetControlPoint(i));
                    newPointPos.y = terrain.SampleHeight(newPointPos) + distanceFromTerrain;
                    newPointPos = transform.InverseTransformPoint(newPointPos);
                    splineSource.SetControlPoint(i, newPointPos);
                }
            }

            if (snapRoadToCollider && snapCollider)
            {
                for (var i = 0; i < splineSource.ControlPointCount; i++)
                {
                    if (i <= 1 && GetPositiveConnection_Intersection() != null) continue;
                    if (i >= splineSource.ControlPointCount - 2 && GetNegativeConnection_Intersection() != null) continue;
                    if (i % 3 != 0) continue;

                    var newPointPos = splineSource.transform.TransformPoint(splineSource.GetControlPoint(i));
                    RaycastHit hit;
                    Ray ray = new Ray(new Vector3(newPointPos.x, distanceRaycastCollider, newPointPos.z), Vector3.down);
                    if (snapCollider.Raycast(ray, out hit, 2.0f * distanceRaycastCollider))
                    {
                        newPointPos.y = hit.point.y + distanceFromCollider;
                    }

                    newPointPos = transform.InverseTransformPoint(newPointPos);
                    splineSource.SetControlPoint(i, newPointPos);
                }
            }

            // Update the snap nodes runtime editing state
            if (SnapNodePositive.GetComponent<SnapPoint>()) SnapNodePositive.GetComponent<SnapPoint>().isEditableAtRuntime = editAtRuntime;
            if (SnapNodeNegative.GetComponent<SnapPoint>()) SnapNodeNegative.GetComponent<SnapPoint>().isEditableAtRuntime = editAtRuntime;

            //UpdateConnectedRoads();

            // Runtime editor functions
            if (editAtRuntime && Application.isPlaying)
            {
                if (!gizmo)
                {
                    if (FindObjectOfType<Gizmo>()) gizmo = FindObjectOfType<Gizmo>();
                }
                if (runtimeCurveLine && !runtimeCurveLine.enabled) runtimeCurveLine.enabled = true;
                if (GetComponent<GizmoSelectable>()) gizmoSelect = GetComponent<GizmoSelectable>();
                if (!gizmoSelect || !gizmoSelect.GetComponent<GizmoSelectable>()) gizmoSelect = gameObject.AddComponent<GizmoSelectable>();
                if (runtimeHandles.Length < 1)
                {
                    CleanupRuntimeHandles();
                    InitializeRuntimeHandles();
                }
#if UNITY_EDITOR
                if (Selection.activeGameObject == this)
                {
                    GenerateRoadMesh(GenerateRoadVertexOutput(roadWidth));
                }
#endif
                if (gizmo && gizmo.selectedObject==transform)
                {
                    GenerateRoadMesh(GenerateRoadVertexOutput(roadWidth));
                }
            }
            else
            {
                CleanupRuntimeHandles();
                if (gizmo) DestroyImmediate(gizmo);
            }
            UpdateRuntimeHandles();

            // Update cached point vectors for file saving
            cachedPointVectors = new Vector3[splineSource.ControlPointCount];
            for (var i = 0; i < splineSource.ControlPointCount; i++) cachedPointVectors[i] = splineSource.GetControlPoint(i);
        }

        public Pair<Vector3>[] GenerateRoadVertexOutput(float width)
        {
            var vertexOutput = new Pair<Vector3>[stepsPerCurve * splineSource.CurveCount + 1];
            var index = 0;
            var roadOffset = width / 2;
            var point = splineSource.GetPoint(0f);
            var current = new Pair<Vector3>();
            var offsetRotation = new Vector3(0, 90, 0);
            var vaTemp = point + splineSource.GetRotation(0f, offsetRotation) * roadOffset;
            current.First = new Vector3(vaTemp.x, point.y, vaTemp.z);
            var vbTemp = point + splineSource.GetRotation(0f, offsetRotation) * -roadOffset;
            current.Second = new Vector3(vbTemp.x, point.y, vbTemp.z);
            vertexOutput[index] = current;
            var steps = stepsPerCurve * splineSource.CurveCount;
            for (var i = 0; i <= steps; i++, index++)
            {
                point = splineSource.GetPoint(i / (float)steps);
                vaTemp = point + splineSource.GetRotation(i / (float)steps, offsetRotation) * roadOffset;
                if (terrain && snapRoadToTerrain) vaTemp.y = terrain.SampleHeight(new Vector3(vaTemp.x, point.y, vaTemp.z)) + distanceFromTerrain;
                else vaTemp.y = point.y;
                current.First = new Vector3(vaTemp.x, vaTemp.y, vaTemp.z);
                vbTemp = point + splineSource.GetRotation(i / (float)steps, offsetRotation) * -roadOffset;
                if (terrain && snapRoadToTerrain) vbTemp.y = terrain.SampleHeight(new Vector3(vbTemp.x, point.y, vbTemp.z)) + distanceFromTerrain;
                else vbTemp.y = point.y;
                current.Second = new Vector3(vbTemp.x, vbTemp.y, vbTemp.z);
                vertexOutput[index] = current;
            }

            // Physically connect roads to other roads regardless of terrain snapping settings
            var road = GetNegativeConnection_MagnetRoad();
            if (road && (road.snapRoadToTerrain || snapRoadToTerrain))
            {
                var isSnapToTerrainOnThisRoad = snapRoadToTerrain;
                var terrainRoad = isSnapToTerrainOnThisRoad ? this : road;

                var point1 = vertexOutput[vertexOutput.Length - 1].First;
                point1.y = vaTemp.y = terrainRoad.terrain.SampleHeight(new Vector3(point1.x, point1.y, point1.z)) + terrainRoad.distanceFromTerrain;
                vertexOutput[vertexOutput.Length - 1].First = point1;

                var point2 = vertexOutput[vertexOutput.Length - 1].Second;
                point2.y = vaTemp.y = terrainRoad.terrain.SampleHeight(new Vector3(point2.x, point2.y, point2.z)) + terrainRoad.distanceFromTerrain;
                vertexOutput[vertexOutput.Length - 1].Second = point2;
            }
            road = GetPositiveConnection_MagnetRoad();
            if (road && (road.snapRoadToTerrain || snapRoadToTerrain))
            {
                var isSnapToTerrainOnThisRoad = snapRoadToTerrain;
                var terrainRoad = isSnapToTerrainOnThisRoad ? this : road;

                var point1 = vertexOutput[0].First;
                point1.y = vaTemp.y = terrainRoad.terrain.SampleHeight(new Vector3(point1.x, point1.y, point1.z)) + terrainRoad.distanceFromTerrain;
                vertexOutput[0].First = point1;

                var point2 = vertexOutput[0].Second;
                point2.y = vaTemp.y = terrainRoad.terrain.SampleHeight(new Vector3(point2.x, point2.y, point2.z)) + terrainRoad.distanceFromTerrain;
                vertexOutput[0].Second = point2;
            }

            // Physically connect intersections to roads regardless of terrain snapping settings
            var intersection = GetNegativeConnection_Intersection();
            if (intersection)
            {
                for (var i = 0; i < intersection.Connections.Length; i++)
                {
                    if (!intersection.Connections[i]) continue;
                    if (intersection.Connections[i] != gameObject) continue;
                    var snapPoint = intersection.SnapNodes[i];
                    vertexOutput[vertexOutput.Length - 1].First = snapPoint.transform.position - snapPoint.transform.right * (width / 2);
                    vertexOutput[vertexOutput.Length - 1].Second = snapPoint.transform.position + snapPoint.transform.right * (width / 2);
                }
            }
            intersection = GetPositiveConnection_Intersection();
            if (!intersection) return vertexOutput;
            {
                for (var i = 0; i < intersection.Connections.Length; i++)
                {
                    if (!intersection.Connections[i]) continue;
                    if (intersection.Connections[i] != gameObject) continue;
                    var snapPoint = intersection.SnapNodes[i];
                    vertexOutput[0].First = snapPoint.transform.position + (snapPoint.transform.right * (width / 2));
                    vertexOutput[0].Second = snapPoint.transform.position - (snapPoint.transform.right * (width / 2));
                }
            }

            return vertexOutput;
        }

        private Pair<Vector3>[] GenerateLeftRoadSideVectors(Pair<Vector3>[] vertexData)
        {
            var leftRoadSide = vertexData;
            var leftRoadSideSlope = GenerateRoadVertexOutput(roadWidth + slopeWidth);
            for (var i = 0; i < vertexData.Length; i++)
            {
                leftRoadSide[i].Second = new Vector3(leftRoadSideSlope[i].First.x, leftRoadSideSlope[i].First.y - sideDepth, leftRoadSideSlope[i].First.z);
            }
            return leftRoadSide;
        }

        private Pair<Vector3>[] GenerateRightRoadSideVectors(Pair<Vector3>[] vertexData)
        {
            var rightRoadSide = vertexData;
            var rightRoadSideSlope = GenerateRoadVertexOutput(roadWidth + slopeWidth);
            for (var i = 0; i < vertexData.Length; i++)
            {
                rightRoadSide[i].First = new Vector3(rightRoadSideSlope[i].Second.x, rightRoadSideSlope[i].Second.y - sideDepth, rightRoadSideSlope[i].Second.z);
            }
            return rightRoadSide;
        }

        private void GenerateSidewalkVectors(out Pair<Vector3>[] leftSideVectors, out Pair<Vector3>[] rightSideVectors)
        {
            var roadExtentVectors = GenerateRoadVertexOutput(roadWidth);
            var sidewalkExtentVectors = GenerateRoadVertexOutput(roadWidth + sidewalkWidth * 2);

            var leftSideVectorList = new List<Pair<Vector3>>();
            var rightSideVectorList = new List<Pair<Vector3>>();
            //Generates Top
            for (var i = 0; i < roadExtentVectors.Length; i++)
            {
                leftSideVectorList.Add(new Pair<Vector3>(roadExtentVectors[i].First + Vector3.up * sidewalkHeight, sidewalkExtentVectors[i].First + Vector3.up * (sidewalkHeight - 0.005f)));
                rightSideVectorList.Add(new Pair<Vector3>(roadExtentVectors[i].Second + Vector3.up * sidewalkHeight, sidewalkExtentVectors[i].Second + Vector3.up * (sidewalkHeight - 0.005f)));
            }

            leftSideVectorList.Add(new Pair<Vector3>(roadExtentVectors[roadExtentVectors.Length - 1].First, sidewalkExtentVectors[sidewalkExtentVectors.Length - 1].First));
            rightSideVectorList.Add(new Pair<Vector3>(roadExtentVectors[roadExtentVectors.Length - 1].Second, sidewalkExtentVectors[sidewalkExtentVectors.Length - 1].Second));
            //Generates Sides
            for (var i = sidewalkExtentVectors.Length - 1; i >= 0; i--)
            {
                leftSideVectorList.Add(new Pair<Vector3>(sidewalkExtentVectors[i].First, sidewalkExtentVectors[i].First + Vector3.up * (sidewalkHeight - 0.005f)));
                rightSideVectorList.Add(new Pair<Vector3>(sidewalkExtentVectors[i].Second, sidewalkExtentVectors[i].Second + Vector3.up * (sidewalkHeight - 0.005f)));
            }
            
            foreach (var extentVector in roadExtentVectors)
            {
                leftSideVectorList.Add(new Pair<Vector3>(extentVector.First, extentVector.First + Vector3.up * sidewalkHeight));
                rightSideVectorList.Add(new Pair<Vector3>(extentVector.Second, extentVector.Second + Vector3.up * sidewalkHeight));
            }

            leftSideVectors = leftSideVectorList.ToArray();
            rightSideVectors = rightSideVectorList.ToArray();
        }

        public void GenerateRoadMesh(Pair<Vector3>[] vertexData)
        {
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

            mesh = new Mesh();
            meshFilter.mesh = Geometry.GenerateStrip(vertexData, transform, true, false, "Procedural Road");
#if UNITY_2017_3_OR_NEWER
            meshCollider.cookingOptions = MeshColliderCookingOptions.None;
#endif
            meshCollider.sharedMesh = meshFilter.sharedMesh;
            if (surfaceMaterial) gameObject.GetComponent<Renderer>().sharedMaterial = surfaceMaterial;

            if (!SnapNodeNegative || !SnapNodePositive) GenerateSnapPoints(splineSource);
            UpdateSnapPoints();

            GenerateSideMeshes(
                GenerateLeftRoadSideVectors(GenerateRoadVertexOutput(roadWidth + sidewalkWidth * 2)),
                GenerateRightRoadSideVectors(GenerateRoadVertexOutput(roadWidth + sidewalkWidth * 2))
            );
            UpdateDecalPositions();
        }

        private void GenerateSideMeshes(Pair<Vector3>[] leftSideVectors, Pair<Vector3>[] rightSideVectors)
        {
            // Clear existing child mesh game objects from the road and...
            if (transform.Find("Road Side One"))
            {
                cachedSideMaterial = transform.Find("Road Side One").gameObject.GetComponent<Renderer>().sharedMaterial;
                DestroyImmediate(transform.Find("Road Side One").gameObject);
            }
            if (transform.Find("Road Left Sidewalk")) DestroyImmediate(transform.Find("Road Left Sidewalk").gameObject);
            if (transform.Find("Road Right Sidewalk")) DestroyImmediate(transform.Find("Road Right Sidewalk").gameObject);
            if (transform.Find("Road Side Two")) DestroyImmediate(transform.Find("Road Side Two").gameObject);
            if (transform.Find("Road Underside")) DestroyImmediate(transform.Find("Road Underside").gameObject);
            if (transform.Find("Road Side Three")) DestroyImmediate(transform.Find("Road Side Three").gameObject);
            if (transform.Find("Road Side Four")) DestroyImmediate(transform.Find("Road Side Four").gameObject);

            // ...generate new ones
            rightSide = new GameObject("Road Side One");
            rightSide.transform.parent = gameObject.transform;
            leftSide = new GameObject("Road Side Two");
            leftSide.transform.parent = gameObject.transform;
            if (sideDepth >= 0)
            {
                frontSide = new GameObject("Road Side Three");
                frontSide.transform.parent = gameObject.transform;
                backSide = new GameObject("Road Side Four");
                backSide.transform.parent = gameObject.transform;
                underSide = new GameObject("Road Underside");
                underSide.transform.parent = gameObject.transform;
            }
            if (sidewalkWidth > 0)
            {
                leftSidewalk = new GameObject("Road Left Sidewalk");
                leftSidewalk.transform.parent = gameObject.transform;
                rightSidewalk = new GameObject("Road Right Sidewalk");
                rightSidewalk.transform.parent = gameObject.transform;
            }

            // Show or hide sub-objects based on user settings
            if (!showChildObjects)
            {
                if (sideDepth >= 0)
                {
                    underSide.hideFlags = HideFlags.HideInHierarchy;
                    backSide.hideFlags = HideFlags.HideInHierarchy;
                    frontSide.hideFlags = HideFlags.HideInHierarchy;
                }
                if (sidewalkWidth > 0)
                {
                    //leftSidewalk.hideFlags = HideFlags.HideInHierarchy;
                    //rightSidewalk.hideFlags = HideFlags.HideInHierarchy;
                }
                leftSide.hideFlags = HideFlags.HideInHierarchy;
                rightSide.hideFlags = HideFlags.HideInHierarchy;
            }

            // Create RoadSideOne's mesh and mesh filter components
            var rsOneMf = rightSide.AddComponent<MeshFilter>();
            rightSide.AddComponent<MeshRenderer>();
            rsOneMf.mesh = Geometry.GenerateStrip(rightSideVectors, transform, false, null, "Road Side One");
#if UNITY_2017_3_OR_NEWER
            rightSide.AddComponent<MeshCollider>().cookingOptions = MeshColliderCookingOptions.None;
#endif
            rightSide.AddComponent<MeshCollider>().sharedMesh = rsOneMf.sharedMesh;
            rightSide.GetComponent<Renderer>().sharedMaterial = !sideMaterial ? cachedSideMaterial : sideMaterial;

            // Do the same for RoadSideTwo
            var rsTwoMf = leftSide.AddComponent<MeshFilter>();
            leftSide.AddComponent<MeshRenderer>();
            rsTwoMf.mesh = Geometry.GenerateStrip(leftSideVectors, transform, false, null, "Road Side Two");
#if UNITY_2017_3_OR_NEWER
            leftSide.AddComponent<MeshCollider>().cookingOptions = MeshColliderCookingOptions.None;
#endif
            leftSide.AddComponent<MeshCollider>().sharedMesh = rsTwoMf.sharedMesh;
            leftSide.GetComponent<Renderer>().sharedMaterial = !sideMaterial ? cachedSideMaterial : sideMaterial;

            // Check the underside is actually below the road
            if (!(sideDepth > 0)) return;

            // Generate the mesh for the front cap of the road
            var rsFrontMf = frontSide.AddComponent<MeshFilter>();
            frontSide.AddComponent<MeshRenderer>();
            rsFrontMf.mesh = Geometry.GeneratePlaneMesh(leftSideVectors[0].First, rightSideVectors[0].Second, leftSideVectors[0].Second, rightSideVectors[0].First, true);
#if UNITY_2017_3_OR_NEWER
            frontSide.AddComponent<MeshCollider>().cookingOptions = MeshColliderCookingOptions.None;
#endif
            frontSide.AddComponent<MeshCollider>().sharedMesh = rsFrontMf.sharedMesh; 
            rsFrontMf.GetComponent<Renderer>().sharedMaterial = !sideMaterial ? cachedSideMaterial : sideMaterial;

            // Generate the mesh for the back cap of the road
            var rsBackMf = backSide.AddComponent<MeshFilter>();
            backSide.AddComponent<MeshRenderer>();
            rsBackMf.mesh = Geometry.GeneratePlaneMesh(leftSideVectors[leftSideVectors.Length - 1].First, rightSideVectors[rightSideVectors.Length - 1].Second, leftSideVectors[leftSideVectors.Length - 1].Second, rightSideVectors[rightSideVectors.Length - 1].First, false);
#if UNITY_2017_3_OR_NEWER
            backSide.AddComponent<MeshCollider>().cookingOptions = MeshColliderCookingOptions.None;
#endif
            backSide.AddComponent<MeshCollider>().sharedMesh = rsBackMf.sharedMesh;
            rsBackMf.GetComponent<Renderer>().sharedMaterial = !sideMaterial ? cachedSideMaterial : sideMaterial;

            // Pull the bottom vertexes out of the left and right side vectors
            var underSideVectors = new Pair<Vector3>[leftSideVectors.Length];
            for (var i = 0; i < leftSideVectors.Length; i++)
            {
                underSideVectors[i].First = leftSideVectors[i].Second;
                underSideVectors[i].Second = rightSideVectors[i].First;
            }

            // Create the components for the underside of the road
            var rsUnderMf = underSide.AddComponent<MeshFilter>();
            underSide.AddComponent<MeshRenderer>();
            rsUnderMf.mesh = Geometry.GenerateStrip(underSideVectors, transform, false, null, "Road Underside");
#if UNITY_2017_3_OR_NEWER
            underSide.AddComponent<MeshCollider>().cookingOptions = MeshColliderCookingOptions.None;
#endif
            underSide.AddComponent<MeshCollider>().sharedMesh = rsUnderMf.sharedMesh;
            underSide.GetComponent<Renderer>().sharedMaterial = !sideMaterial ? cachedSideMaterial : sideMaterial;

            // Check we've actually got sidewalks
            if (sidewalkWidth <= 0) return;

            // Generate the sidewalk mesh data
            Pair<Vector3>[] leftSidewalkVectors, rightSidewalkVectors;
            GenerateSidewalkVectors(out leftSidewalkVectors, out rightSidewalkVectors);

            // Generate the mesh for the left sidewalk
            var sidewalkLeftMf = leftSidewalk.AddComponent<MeshFilter>();
            leftSidewalk.AddComponent<MeshRenderer>();
            sidewalkLeftMf.mesh = Geometry.GenerateStrip(leftSidewalkVectors, transform, false, null, "Road Left Sidewalk");
#if UNITY_2017_3_OR_NEWER
            leftSidewalk.AddComponent<MeshCollider>().cookingOptions = MeshColliderCookingOptions.None;
#endif
            leftSidewalk.AddComponent<MeshCollider>().sharedMesh = sidewalkLeftMf.sharedMesh;
            leftSidewalk.GetComponent<Renderer>().sharedMaterial = sidewalkMaterial;
            
            // Generate the mesh for the right sidewalk
            var sidewalkRightMf = rightSidewalk.AddComponent<MeshFilter>();
            rightSidewalk.AddComponent<MeshRenderer>();
            sidewalkRightMf.mesh = Geometry.GenerateStrip(rightSidewalkVectors, transform, true, null, "Road Right Sidewalk");
#if UNITY_2017_3_OR_NEWER
            rightSidewalk.AddComponent<MeshCollider>().cookingOptions = MeshColliderCookingOptions.None;
#endif
            rightSidewalk.AddComponent<MeshCollider>().sharedMesh = sidewalkRightMf.sharedMesh;
            rightSidewalk.GetComponent<Renderer>().sharedMaterial = sidewalkMaterial;
        }

        private void GenerateSnapPoints(BezierSpline spline)
        {
            // Check for existing snap points then see if they've moved, if so return
            if (SnapNodeNegative || SnapNodePositive)
            {
                if (SnapNodeNegative.transform.position == splineSource.GetPoint(stepsPerCurve * spline.CurveCount) && SnapNodePositive.transform.position == splineSource.GetPoint(0))
                    return;
            }

            // Destroy old snap points if needed and create new ones
            try
            {
                if (transform.Find("SnapNodeNegative") || transform.Find("SnapNodePositive"))
                {
                    if (transform.Find("SnapNodeNegative")) DestroyImmediate(transform.Find("SnapNodeNegative").gameObject); // destroy the old road pieces
                    if (transform.Find("SnapNodePositive")) DestroyImmediate(transform.Find("SnapNodePositive").gameObject);
                }
            }
            catch (NullReferenceException)
            {
                // Something we don't need already doesn't exist, yay! 
            }
            var posLeft = spline.GetPoint(stepsPerCurve * spline.CurveCount);
            var posRight = spline.GetPoint(0f);
            SnapNodeNegative = new GameObject("SnapNodeNegative");
            SnapNodePositive = new GameObject("SnapNodePositive");
            SnapNodeNegative.transform.parent = gameObject.transform;
            SnapNodeNegative.AddComponent<SnapPoint>().SetUp(SnapPoint.PointEnd.Negative, roadWidth);
            SnapNodeNegative.transform.position = posLeft;
            SnapNodePositive.transform.parent = gameObject.transform;
            SnapNodePositive.AddComponent<SnapPoint>().SetUp(SnapPoint.PointEnd.Positive, roadWidth);
            SnapNodePositive.transform.position = posRight;
        }

        private void UpdateSnapPoints()
        {
            if (!SnapNodeNegative || !SnapNodePositive) GenerateSnapPoints(splineSource);
            else
            {
                var posLeft = splineSource.GetPoint(stepsPerCurve * splineSource.CurveCount);
                var posRight = splineSource.GetPoint(0f);
                SnapNodeNegative.GetComponent<SnapPoint>().SetUp(SnapPoint.PointEnd.Negative, roadWidth);
                SnapNodeNegative.transform.position = posLeft;
                SnapNodePositive.GetComponent<SnapPoint>().SetUp(SnapPoint.PointEnd.Positive, roadWidth);
                SnapNodePositive.transform.position = posRight;
            }
        }

        public void ClearRoadMesh()
        {
            try
            {
                if (mesh) mesh.Clear();
                meshFilter.sharedMesh.Clear();
                meshCollider.sharedMesh.Clear();
                if (leftSide) DestroyImmediate(leftSide);
                if (rightSide) DestroyImmediate(rightSide);
                if (leftSidewalk) DestroyImmediate(leftSidewalk);
                if (rightSidewalk) DestroyImmediate(rightSidewalk);
                if (frontSide) DestroyImmediate(frontSide);
                if (backSide) DestroyImmediate(backSide);
                if (underSide) DestroyImmediate(underSide);
            }
            catch (Exception e)
            {
                if(e is NullReferenceException || e is UnassignedReferenceException)
                {
                    meshFilter = GetComponent<MeshFilter>();
                    meshCollider = GetComponent<MeshCollider>();
                    if (transform.childCount > 0)
                    {
                        if (transform.Find("Road Side One")) leftSide = transform.Find("Road Side One").gameObject;
                        if (transform.Find("Road Side Two")) rightSide = transform.Find("Road Side Two").gameObject;
                        if (transform.Find("Road Left Sidewalk")) leftSidewalk = transform.Find("Road Left Sidewalk").gameObject;
                        if (transform.Find("Road Right Sidewalk")) rightSidewalk = transform.Find("Road Right Sidewalk").gameObject;
                        if (transform.Find("Road Side Three")) frontSide = transform.Find("Road Side Three").gameObject;
                        if (transform.Find("Road Side Four")) backSide = transform.Find("Road Side Four").gameObject;
                        if (transform.Find("Road Underside")) underSide = transform.Find("Road Underside").gameObject;
                    }
                    ClearRoadMesh();
                    return;
                }

                Debug.LogWarning("MESH FAILED TO CLEAR: " + e);
            }
        }

        public void AddCurve(bool atPositive = false)
        {
            CleanupRuntimeHandles();
            splineSource.AddCurve(stepsPerCurve, atPositive, roadWidth * 2);
            InitializeRuntimeHandles();
            GenerateRoadMesh(GenerateRoadVertexOutput(roadWidth));
        }

        public void RemoveCurve(bool atPositive)
        {
            CleanupRuntimeHandles();
            splineSource.RemoveCurve(atPositive);
            InitializeRuntimeHandles();
            GenerateRoadMesh(GenerateRoadVertexOutput(roadWidth));
        }

        public void AttachIntersection(bool atPositive, bool threeLane = true)
        {
            Vector3 forwardDir;
            Vector3 intersectionPosition;
            Vector3 intersectionRotationTarget;

            if (!atPositive)
            {
                forwardDir = splineSource.GetDirection(splineSource.CurveCount * stepsPerCurve);
                intersectionPosition = SnapNodeNegative.transform.position + forwardDir * (roadWidth / 2);
                intersectionRotationTarget = SnapNodeNegative.transform.position + forwardDir * roadWidth;
            }
            else
            {
                forwardDir = splineSource.GetDirection(0f);
                intersectionPosition = SnapNodePositive.transform.position - forwardDir * (roadWidth / 2);
                intersectionRotationTarget = SnapNodePositive.transform.position - forwardDir * roadWidth;
            }

            forwardDir.y = 0;

            var intersection = threeLane ? CreateNewThreeLane().GetComponent<Intersection>() : CreateNewFourLane().GetComponent<Intersection>();
            intersection.roadWidth = roadWidth;
            intersection.transform.position = intersectionPosition;
            intersection.sideDepth = sideDepth;
            intersection.slopeWidth = slopeWidth;
            intersection.uniqueConnectionId = Guid.NewGuid().ToString();

            var q = Quaternion.LookRotation(intersectionRotationTarget - intersection.transform.position);
            intersection.transform.rotation = Quaternion.RotateTowards(intersection.transform.rotation, q, 360);
            if (!atPositive) intersection.transform.Rotate(Vector3.up, 90);
            else intersection.transform.Rotate(Vector3.up, -90);
            intersection.transform.rotation = new Quaternion(0, intersection.transform.rotation.y, 0, intersection.transform.rotation.w);
            intersection.GenerateIntersectionMesh();

            if (!atPositive) SnapFirstAndLastPoints(splineSource.ControlPointCount - 1);
            else SnapFirstAndLastPoints(0);

            SnapFirstAndLastPoints(atPositive ? 0 : splineSource.ControlPointCount - 1);
            GenerateRoadMesh(GenerateRoadVertexOutput(roadWidth));
        }

        public void AttachMagnetRoad(bool atPositive)
        {
            Vector3 forwardDir;
            Vector3 roadCentrePosition;
            var pointPositions = new Vector3[4];
            var roadLength = roadWidth * 2 * 3;
            if (!atPositive)
            {
                forwardDir = splineSource.GetDirection(splineSource.CurveCount * stepsPerCurve);
                roadCentrePosition = SnapNodeNegative.transform.position + forwardDir * (roadLength / 2);
                for (var i = 0; i < pointPositions.Length; i++)
                {
                    pointPositions[i] = SnapNodeNegative.transform.position + forwardDir * (roadLength / 3 * i);
                }
            }
            else
            {
                forwardDir = splineSource.GetDirection(0);
                roadCentrePosition = SnapNodePositive.transform.position - forwardDir * (roadLength / 2);
                for (var i = 0; i < pointPositions.Length; i++)
                {
                    pointPositions[i] = SnapNodePositive.transform.position - forwardDir * (roadLength / 3 * i);
                }
            }

            var newRoad = CreateNewSplineRoad().GetComponent<MagnetRoad>();
            newRoad.transform.position = roadCentrePosition;
            newRoad.splineSource.SetControlPoint(0, newRoad.splineSource.transform.InverseTransformPoint(pointPositions[0]));
            newRoad.splineSource.SetControlPoint(3, newRoad.splineSource.transform.InverseTransformPoint(pointPositions[3]));
            newRoad.splineSource.SetControlPoint(1, newRoad.splineSource.transform.InverseTransformPoint(pointPositions[1]));
            newRoad.splineSource.SetControlPoint(2, newRoad.splineSource.transform.InverseTransformPoint(pointPositions[2]));
            newRoad.roadWidth = roadWidth;
            newRoad.roadsideMargin = roadsideMargin;
            newRoad.totalCarLanes = totalCarLanes;
            newRoad.distanceFromTerrain = distanceFromTerrain;
            newRoad.snapRoadToTerrain = snapRoadToTerrain;
            newRoad.terrain = terrain;
            newRoad.showCarRoutes = showCarRoutes;
            newRoad.showRoadOutline = showRoadOutline;
            newRoad.sideDepth = sideDepth;
            newRoad.slopeWidth = slopeWidth;
            newRoad.GenerateRoadMesh(newRoad.GenerateRoadVertexOutput(newRoad.roadWidth));
            newRoad.uniqueConnectionId = Guid.NewGuid().ToString();
            AddConnection(atPositive, newRoad,!atPositive);
        }

        public void EnableRuntimeEditing()
        {
            editAtRuntime = true;
            CleanupRuntimeHandles();
            InitializeRuntimeHandles();
        }

        public void DisableRuntimeEditing()
        {
            editAtRuntime = false;
            CleanupRuntimeHandles();
        }

        private void InitializeRuntimeHandles()
        {
            if (runtimeHandles.Length != splineSource.ControlPointCount && editAtRuntime)
            {
                // Generate runtime handles
                runtimeHandles = new GameObject[splineSource.ControlPointCount];
                for (var i = 0; i < splineSource.ControlPointCount; i++)
                {
                    // Create point 'stems'
                    var position = splineSource.transform.TransformPoint(splineSource.GetControlPoint(i));
                    runtimeHandles[i] = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                    runtimeHandles[i].transform.position = new Vector3(position.x, position.y + roadWidth / 4, position.z);
                    runtimeHandles[i].transform.localScale = new Vector3(roadWidth / 15, roadWidth / 4, roadWidth / 15);
                    runtimeHandles[i].GetComponent<Renderer>().material = Resources.Load<Material>("Materials/_RuntimeGizmo/HandleColor");
                    runtimeHandles[i].GetComponent<Renderer>().sharedMaterial.color = Color.yellow;
                    runtimeHandles[i].name = "RuntimeControlPin";

                    // Create selectable 'tops'
                    var handleTop = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    handleTop.transform.position = new Vector3(position.x, position.y + roadWidth / 2, position.z);
                    handleTop.transform.localScale = new Vector3(roadWidth / 4, roadWidth / 4, roadWidth / 4);
                    //handleTop.transform.parent = runtimeHandles[i].transform;
                    handleTop.GetComponent<Renderer>().material = Resources.Load<Material>("Materials/_RuntimeGizmo/HandleColor");
                    handleTop.GetComponent<Renderer>().sharedMaterial.color = Color.yellow;
                    handleTop.AddComponent<GizmoSelectable>();
                    handleTop.name = "RuntimeControlHandle" + i;
                    //handleTop.transform.parent = transform;

                    runtimeHandles[i].transform.parent = handleTop.transform;
                }

                // Create bezier curve line on road
                if (!GetComponent<LineRenderer>())
                {
                    runtimeCurveLine = gameObject.AddComponent<LineRenderer>();
                }
                runtimeCurveLine = GetComponent<LineRenderer>();
#if UNITY_2017_1_OR_NEWER
                runtimeCurveLine.positionCount = (stepsPerCurve * splineSource.CurveCount + 1);
#endif
                var temp = GetCentreWaypoints();
                for (var i = 0; i < temp.Length; i++) temp[i].y += 0.05f;
                runtimeCurveLine.SetPositions(temp);
                runtimeCurveLine.material = Resources.Load<Material>("Materials/_RuntimeGizmo/RoadLineColor");
                runtimeCurveLine.sharedMaterial.color = Color.white;
#if UNITY_2017_1_OR_NEWER
                runtimeCurveLine.startWidth = roadWidth/10;
#else
                runtimeCurveLine.SetWidth(roadWidth / 10f, roadWidth / 10f);
#endif
                runtimeCurveLine.transform.parent = transform;

                // Create Line Renderers for handle directions
                runtimeHandleLines = null;
                runtimeHandleLines = new LineRenderer[splineSource.ControlPointCount];
                for (var i = 1; i < splineSource.ControlPointCount; i += 3)
                {
                    CreateUpdateHandleLine(i - 1, i);
                    CreateUpdateHandleLine(i + 1, i + 2);
                }
            }
            else if (runtimeHandles.Length > 1 && !editAtRuntime)
            {
                // Remove runtime handles & lines
                CleanupRuntimeHandles();
            }
        }

        private void CreateUpdateHandleLine(int startHandleIndex, int lineIndex)
        {
            // Assert that the handle line is valid
            try
            {
                // Try to set the vertex count of this line
#if UNITY_2017_1_OR_NEWER
                runtimeHandleLines[lineIndex].positionCount = 2;
#else
                runtimeHandleLines[lineIndex].SetWidth(roadWidth / 10f, roadWidth / 10f);
#endif
            }
            catch (Exception)
            {
                // Operation failed, assumed new LineRenderer needed  
                try
                {
                    // Try to set the vertex count again
                    runtimeHandleLines[lineIndex] = runtimeHandles[startHandleIndex].AddComponent<LineRenderer>();
#if UNITY_2017_1_OR_NEWER
                    runtimeHandleLines[lineIndex].positionCount = 2;
#else
                    runtimeHandleLines[lineIndex].SetWidth(roadWidth / 10f, roadWidth / 10f);
#endif
                }
                catch (Exception e)
                {
                    // line index invalid - only show during play mode
                    if (Application.isPlaying) Debug.LogError("ERROR ("+e+") when creating handle lines!");
                    return;
                }
            }

            // Update line data
            var tempArray = new Vector3[2];
            var positions = new Pair<Vector3>
            {
                First = splineSource.transform.TransformPoint(splineSource.GetControlPoint(startHandleIndex)),
                Second = splineSource.transform.TransformPoint(splineSource.GetControlPoint(startHandleIndex + 1))
            };
            tempArray[0] = new Vector3(positions.First.x, positions.First.y + 0.05f, positions.First.z);
            tempArray[1] = new Vector3(positions.Second.x, positions.Second.y + 0.05f, positions.Second.z);
            runtimeHandleLines[lineIndex].SetPositions(tempArray);
            runtimeHandleLines[lineIndex].material = Resources.Load<Material>("Materials/_RuntimeGizmo/HandleColor");
            runtimeHandleLines[lineIndex].sharedMaterial.color = Color.yellow;
#if UNITY_2017_1_OR_NEWER
            runtimeHandleLines[lineIndex].startWidth = roadWidth / 10;
#else
            runtimeHandleLines[lineIndex].SetWidth(roadWidth / 10f, roadWidth / 10f);
#endif
        }

        private void UpdateRuntimeHandles()
        {
            if (!editAtRuntime) return;

            // Update any selected handles & the other handle positions
            for (var i = 0; i < splineSource.ControlPointCount; i += 1) RuntimeUpdatePoint(i);

            // Update the line renderers
            if (runtimeHandleLines.Length != splineSource.ControlPointCount) runtimeHandleLines = new LineRenderer[splineSource.ControlPointCount];
            for (var i = 1; i < splineSource.ControlPointCount; i += 3)
            {
                CreateUpdateHandleLine(i - 1, i);
                CreateUpdateHandleLine(i + 1, i + 2);
            }

            // Update the curve line if needed
            var isHandleSelected = false;
            for (var i = 0; i < runtimeHandles.Length; i++)
            {
                if (gizmo && gizmo.selectedObject == runtimeHandles[i].transform.parent)
                {
                    isHandleSelected = true;
                    break;
                }
            }
            if (runtimeCurveLine && (isHandleSelected || gizmo.selectedObject == transform))
            {
#if UNITY_2017_1_OR_NEWER
                if (runtimeCurveLine.positionCount != (stepsPerCurve * splineSource.CurveCount + 1)) runtimeCurveLine.positionCount = (stepsPerCurve * splineSource.CurveCount + 1);
#endif
                var temp = GetCentreWaypoints();
                for (var i = 0; i < temp.Length; i++) temp[i].y += 0.05f;
                runtimeCurveLine.SetPositions(temp);
            }
        }

        private void RuntimeUpdatePoint(int index)
        {
            if (runtimeHandles.Length == 0) return;
            if (index > runtimeHandles.Length || !runtimeHandles[index]) return;

            var handlePos = runtimeHandles[index].transform.position;
            var point = splineSource.transform.InverseTransformPoint(new Vector3(handlePos.x, handlePos.y - roadWidth / 4, handlePos.z));
            if (gizmo == null) return;
            if (gizmo.selectedObject == runtimeHandles[index].transform.parent)
            {
                // Update the spline control point of the selected handle only
                splineSource.SetControlPoint(index, point);
                if (Application.isPlaying) SnapFirstAndLastPoints(index);
                GenerateRoadMesh(GenerateRoadVertexOutput(roadWidth));
            }
            // Update the handle object position
            runtimeHandles[index].transform.parent.position = splineSource.transform.TransformPoint(splineSource.GetControlPoint(index) + new Vector3(0, (roadWidth / 2), 0));
        }

        public void SnapFirstAndLastPoints(int selectedIndex)
        {
            // Get required vectors and arrays
            var selectedVector = splineSource.GetControlPoint(selectedIndex);
            var allRoads = FindObjectsOfType<MagnetRoad>();
            var allIntersections = FindObjectsOfType<Intersection>();
            var allDynamicIntersections = FindObjectsOfType<DynamicIntersection>();
            // Perform all snapping checks and finaly the snapping itself
            if  (selectedVector == splineSource.GetControlPoint(splineSource.ControlPointCount -1) || selectedVector == splineSource.GetControlPoint(0))
            {
                foreach (var road in allRoads)
                {
                    var pointA = splineSource.gameObject.GetComponent<MagnetRoad>().GetClosestSnapPointFromVector(transform.TransformPoint(selectedVector));
                    var pointB = road.GetClosestSnapPointFromVector(transform.TransformPoint(selectedVector));
                    if (!pointA || !pointB) continue;
                    
                    // Check all Magnet Roads in the scene
                    if (pointA.PointType != pointB.PointType) // Check for the polar Connections
                    {
                        if (road.gameObject != splineSource.gameObject)
                        {
                            if (Vector3.Distance(transform.TransformPoint(selectedVector), road.SnapNodeNegative.transform.position) <= road.roadWidth / 3)
                            {
                                splineSource.SetControlPoint(selectedIndex, transform.InverseTransformPoint(road.SnapNodeNegative.transform.position));
                                if (selectedVector == splineSource.GetControlPoint(splineSource.ControlPointCount - 1))
                                {
                                    var distance = Vector3.Distance(transform.TransformPoint(selectedVector), transform.TransformPoint(splineSource.GetControlPoint(splineSource.ControlPointCount - 2)));
                                    splineSource.SetControlPoint(selectedIndex - 1, transform.InverseTransformPoint(road.SnapNodeNegative.transform.position + (road.splineSource.GetDirection(0f) * -distance)));
                                    AddConnection(false, road,true);
                                    road.AddConnection(true, this,false);
                                }
                                if (selectedVector == splineSource.GetControlPoint(0))
                                {
                                    var distance = Vector3.Distance(transform.TransformPoint(selectedVector), transform.TransformPoint(splineSource.GetControlPoint(1)));
                                    splineSource.SetControlPoint(1, transform.InverseTransformPoint(road.SnapNodeNegative.transform.position + (road.splineSource.GetDirection(road.splineSource.CurveCount * road.stepsPerCurve) * distance)));
                                    AddConnection(true, road,false);
                                    road.AddConnection(false, this,true);
                                }
                            }
                            if (Vector3.Distance(transform.TransformPoint(selectedVector), road.SnapNodePositive.transform.position) <= road.roadWidth / 3)
                            {
                                splineSource.SetControlPoint(selectedIndex, transform.InverseTransformPoint(road.SnapNodePositive.transform.position));
                                if (selectedVector == splineSource.GetControlPoint(splineSource.ControlPointCount - 1))
                                {
                                    var distance = Vector3.Distance(transform.TransformPoint(selectedVector), transform.TransformPoint(splineSource.GetControlPoint(splineSource.ControlPointCount - 2)));
                                    splineSource.SetControlPoint(selectedIndex - 1, transform.InverseTransformPoint(road.SnapNodePositive.transform.position + (road.splineSource.GetDirection(0f) * -distance)));
                                    AddConnection(false, road,true);
                                    road.AddConnection(true, this,false);
                                }
                                if (selectedVector == splineSource.GetControlPoint(0))
                                {
                                    var distance = Vector3.Distance(transform.TransformPoint(selectedVector), transform.TransformPoint(splineSource.GetControlPoint(1)));
                                    splineSource.SetControlPoint(1, transform.InverseTransformPoint(road.SnapNodePositive.transform.position + (road.splineSource.GetDirection(road.splineSource.CurveCount * road.stepsPerCurve) * distance)));
                                    AddConnection(true, road,false);
                                    road.AddConnection(false, this,true);
                                }
                            }
                        }
                    }
                    else
                    {//Check for bipolar connections. Has to be sepprate because of small math diffrances and overlapping checks
                        if (road.gameObject != splineSource.gameObject)
                        {
                            if (Vector3.Distance(transform.TransformPoint(selectedVector), road.SnapNodePositive.transform.position) <= road.roadWidth / 3)
                            {
                                splineSource.SetControlPoint(selectedIndex, transform.InverseTransformPoint(road.SnapNodePositive.transform.position));
                                if (selectedVector == splineSource.GetControlPoint(splineSource.ControlPointCount - 1))
                                {
                                    var distance = Vector3.Distance(transform.TransformPoint(selectedVector), transform.TransformPoint(splineSource.GetControlPoint(splineSource.ControlPointCount - 2)));
                                    splineSource.SetControlPoint(selectedIndex - 1, transform.InverseTransformPoint(road.SnapNodePositive.transform.position - (road.splineSource.GetDirection(0f) * distance)));
                                    AddConnection(true, road, true);
                                    road.AddConnection(true, this, true);
                                }
                                if (selectedVector == splineSource.GetControlPoint(0))
                                {
                                    var distance = Vector3.Distance(transform.TransformPoint(selectedVector), transform.TransformPoint(splineSource.GetControlPoint(1)));
                                    splineSource.SetControlPoint(1, transform.InverseTransformPoint(road.SnapNodeNegative.transform.position + (road.splineSource.GetDirection(road.splineSource.CurveCount * road.stepsPerCurve) * distance)));
                                    AddConnection(true, road, true);
                                    road.AddConnection(true, this, true);
                                }
                            }
                            if (Vector3.Distance(transform.TransformPoint(selectedVector), road.SnapNodeNegative.transform.position) <= road.roadWidth / 3)
                            {
                                splineSource.SetControlPoint(selectedIndex, transform.InverseTransformPoint(road.SnapNodeNegative.transform.position));
                                if (selectedVector == splineSource.GetControlPoint(splineSource.ControlPointCount - 1))
                                {
                                    var distance = Vector3.Distance(transform.TransformPoint(selectedVector), transform.TransformPoint(splineSource.GetControlPoint(splineSource.ControlPointCount - 2)));
                                    splineSource.SetControlPoint(selectedIndex - 1, transform.InverseTransformPoint(road.SnapNodeNegative.transform.position + (road.splineSource.GetDirection(0f) * distance)));
                                    AddConnection(false, road, false);
                                    road.AddConnection(false, this, false);
                                }
                                if (selectedVector == splineSource.GetControlPoint(0))
                                {
                                    var distance = Vector3.Distance(transform.TransformPoint(selectedVector), transform.TransformPoint(splineSource.GetControlPoint(1)));
                                    splineSource.SetControlPoint(1, transform.InverseTransformPoint(road.SnapNodePositive.transform.position + (road.splineSource.GetDirection(road.splineSource.CurveCount * road.stepsPerCurve) * distance)));
                                    AddConnection(false, road, false);
                                    road.AddConnection(false, this, false);
                                }
                            }
                        }
                    }

                    // Check all Intersections in the scene
                    foreach (var intersection in allIntersections)
                    {
                        var snapNodeNumber = 0;
                        foreach (var snapNode in intersection.SnapNodes)
                        {
                            if (Vector3.Distance(transform.TransformPoint(selectedVector), snapNode.transform.position) < intersection.roadWidth / 3)
                            {
                                splineSource.SetControlPoint(selectedIndex, transform.InverseTransformPoint(snapNode.transform.position));
                                if (selectedVector == splineSource.GetControlPoint(splineSource.ControlPointCount - 1))
                                {
                                    var distance = Vector3.Distance(transform.TransformPoint(selectedVector), transform.TransformPoint(splineSource.GetControlPoint(splineSource.ControlPointCount - 2)));
                                    splineSource.SetControlPoint(selectedIndex - 1, transform.InverseTransformPoint(snapNode.transform.position + (snapNode.transform.forward * distance)));
                                    AddConnection(false, intersection, snapNodeNumber);
                                }
                                if (selectedVector == splineSource.GetControlPoint(0))
                                {
                                    var distance = Vector3.Distance(transform.TransformPoint(selectedVector), transform.TransformPoint(splineSource.GetControlPoint(1)));
                                    splineSource.SetControlPoint(1, transform.InverseTransformPoint(snapNode.transform.position + (snapNode.transform.forward * distance)));
                                    AddConnection(true, intersection, snapNodeNumber);
                                }
                            }
                            snapNodeNumber++;
                        }
                    }

                    // Check all Intersections in the scene
                    foreach (var intersection in allDynamicIntersections)
                    {
                        var snapNodeNumber = 0;
                        foreach (var snapNode in intersection.SnapNodes)
                        {
                            if (Vector3.Distance(transform.TransformPoint(selectedVector), snapNode.transform.position) < intersection.roadWidth / 3)
                            {
                                splineSource.SetControlPoint(selectedIndex, transform.InverseTransformPoint(snapNode.transform.position));
                                if (selectedVector == splineSource.GetControlPoint(splineSource.ControlPointCount - 1))
                                {
                                    var distance = Vector3.Distance(transform.TransformPoint(selectedVector), transform.TransformPoint(splineSource.GetControlPoint(splineSource.ControlPointCount - 2)));
                                    splineSource.SetControlPoint(selectedIndex - 1, transform.InverseTransformPoint(snapNode.transform.position + (snapNode.transform.forward * distance)));
                                    AddConnection(false, intersection, snapNodeNumber);
                                }
                                if (selectedVector == splineSource.GetControlPoint(0))
                                {
                                    var distance = Vector3.Distance(transform.TransformPoint(selectedVector), transform.TransformPoint(splineSource.GetControlPoint(1)));
                                    splineSource.SetControlPoint(1, transform.InverseTransformPoint(snapNode.transform.position + (snapNode.transform.forward * distance)));
                                    AddConnection(true, intersection, snapNodeNumber);
                                }
                            }
                            snapNodeNumber++;
                        }
                    }
                }
            }

            // Test that any active connections are still within range and disconnect if needed
            UpdateConnections(); 
        }

        private void CleanupRuntimeHandles()
        {
            if (!Application.isPlaying || !editAtRuntime) DestroyImmediate(runtimeCurveLine);
            if (runtimeHandles == null) return;
            if (runtimeHandles.Length <= 0) return;

            if (gizmo)
            {
                gizmo.ClearSelection();
                gizmo.Hide();
            }

            foreach (var line in runtimeHandleLines) DestroyImmediate(line);
            foreach (var handle in runtimeHandles)
            {
                if (handle) if (handle.transform.parent.gameObject) DestroyImmediate(handle.transform.parent.gameObject);
                if (handle) DestroyImmediate(handle);
            }

            runtimeHandles = new GameObject[0];
        }

        public void UpdateConnections()
        {
            CheckForDisconnect(true);
            CheckForDisconnect(false);
        }

        private void CheckForDisconnect(bool atPositive)
        {
            var connection = atPositive ? positiveConnection : negativeConnection;
            if (!connection.gameObject) return;

            var intersection = connection.gameObject.GetComponent<Intersection>();
            var road = connection.gameObject.GetComponent<MagnetRoad>();

            int controlPoint;
            if (atPositive) controlPoint = 0;
            else controlPoint = splineSource.ControlPointCount - 1;

            if (intersection)
            {
                for (var i = 0; i < intersection.Connections.Length; i++)
                {
                    if (!intersection.Connections[i]) continue;
                    if (intersection.Connections[i] != gameObject) continue;
                    var distance = Vector3.Distance(intersection.SnapNodes[i].transform.position, transform.TransformPoint(splineSource.GetControlPoint(controlPoint)));
                    if (!(distance > roadWidth / 3)) continue;

                    RemoveConnection(atPositive);
                    intersection.RemoveConnection(i);
                }
            }

            if (!road) return;
            {
                var distance = Vector3.Distance(connection.positiveConnection ? road.SnapNodePositive.transform.position : road.SnapNodeNegative.transform.position, transform.TransformPoint(splineSource.GetControlPoint(controlPoint)));

                if (!(distance > roadWidth / 3)) return;

                RemoveConnection(atPositive);
                road.RemoveConnection(connection.positiveConnection);
            }
        }

        public void AddConnection(bool atPositive, MagnetRoad connection, bool otherPositive)
        {
            if (atPositive)
            {
                positiveConnection.gameObject = connection.gameObject;
                positiveConnection.positiveConnection = otherPositive;
                positiveConnectionUniqueId = connection.uniqueConnectionId;
            }
            else
            {
                negativeConnection.gameObject = connection.gameObject;
                negativeConnection.positiveConnection = otherPositive;
                negativeConnectionUniqueId = connection.uniqueConnectionId;
            }
        }

        public void AddConnection(bool atPositive, Intersection connection, int snapNodeNumber)
        {
            if (atPositive)
            {
                positiveConnection.gameObject = connection.gameObject;
                positiveConnectionUniqueId = connection.uniqueConnectionId;
            }
            else
            {
                negativeConnection.gameObject = connection.gameObject;
                negativeConnectionUniqueId = connection.uniqueConnectionId;
            }
            connection.AddConnection(snapNodeNumber, this);
        }
        public void AddConnection(bool atPositive, DynamicIntersection connection, int snapNodeNumber)
        {
            if (atPositive)
            {
                positiveConnection.gameObject = connection.gameObject;
                positiveConnectionUniqueId = connection.uniqueConnectionId;
            }
            else
            {
                negativeConnection.gameObject = connection.gameObject;
                negativeConnectionUniqueId = connection.uniqueConnectionId;
            }
            connection.AddConnection(snapNodeNumber, this);
        }

        public void RemoveConnection(bool atPositive)
        {
            if (atPositive)
            {
                positiveConnection.gameObject = null;
                positiveConnectionUniqueId = "";
            }
            else
            {
                negativeConnection.gameObject = null;
                negativeConnectionUniqueId = "";
            }
        }

        public static void DrawRoadOutline(IList<Pair<Vector3>> vertexData)
        {
            if (vertexData.Count == 0)
                return;
            Gizmos.color = Color.grey;
            var current = vertexData[0];
            Gizmos.DrawLine(current.First, current.Second);
            var last = current;
            for (var i = 1; i <= vertexData.Count - 1; i++)
            {
                current = vertexData[i];
                Gizmos.DrawLine(current.First, current.Second);
                Gizmos.DrawLine(current.First, last.First);
                Gizmos.DrawLine(current.Second, last.Second);
                last = current;
            }
        }

        private void DrawCarPaths()
        {
            if (totalCarLanes <= 0) return;

            var startNumber = totalCarLanes % 2;
            var laneInterval = (roadWidth - roadsideMargin * 2) / totalCarLanes;

            for (var i = startNumber; i < totalCarLanes + 1; i += 2)
            {
                if (i == 1) DrawCarPath(GetCentreWaypoints(), Color.blue);
                else if (i > 1)
                {
                    var width = laneInterval * (i - 1);
                    Vector3[] outputOne, outputTwo;
                    Helper.SplitPairArray(GenerateRoadVertexOutput(width), out outputOne, out outputTwo);
                    DrawCarPath(outputOne, Color.blue);
                    DrawCarPath(outputTwo, Color.blue);
                }
            }
        }

        private static void DrawCarPath(ICollection<Vector3> path, Color color)
        {
            Gizmos.color = color;
            for (var i = 0; i < path.Count - 1; i++)
            {
                Gizmos.DrawLine(path.ElementAt(i), path.ElementAt(i+1));
            }
        }

        public void UpdateDecalPositions()
        {
           foreach(MagnetDecal decal in roadDecals)
            {
                decal.transform.position = splineSource.GetPoint(decal.locationOnRoad) + decal.possitionOffset;
                Vector3 direction = splineSource.GetDirection(decal.locationOnRoad);

                decal.transform.rotation = Quaternion.LookRotation(direction);
                decal.transform.Rotate(decal.rotationOffset);
            }
        }

        public Vector3[] GetLaneWaypoints(int laneNo)
        {
            var startNumber = totalCarLanes % 2;
            var laneInterval = (roadWidth - roadsideMargin * 2) / totalCarLanes;
            var middleValue = totalCarLanes / 2 + (startNumber == 0 ? -1 : 0);
            int lowerValue = middleValue, upperValue = middleValue;
            
            for (var i = startNumber; i < totalCarLanes + 1; i += 2)
            {
                if (startNumber == 0)
                {
                    // Even
                    if (i != 0) lowerValue--;
                    upperValue++;
                }
                else
                {
                    // Odd
                    if (laneNo == middleValue) return GetCentreWaypoints();
                    lowerValue--;
                    upperValue++;
                }

                var width = laneInterval * (i + 1);
                Vector3[] outputOne, outputTwo;
                Helper.SplitPairArray(GenerateRoadVertexOutput(width), out outputOne, out outputTwo);
                if (laneNo == lowerValue) return outputOne;
                if (laneNo == upperValue) return outputTwo;
            }

            return GetCentreWaypoints();
        }

        public Vector3[] GetCentreWaypoints()
        {
            Vector3[] outputOne;
            Helper.SplitPairArray(GenerateRoadVertexOutput(0), out outputOne, out outputOne);
            return outputOne;
        }

        private SnapPoint GetClosestSnapPointFromVector(Vector3 vector)
        {
            if (!SnapNodeNegative || !SnapNodePositive) return null;
            var distLeft = Vector3.Distance(vector, SnapNodeNegative.transform.position);
            var distRight = Vector3.Distance(vector, SnapNodePositive.transform.position);
            if (distLeft > distRight) return SnapNodePositive.gameObject.GetComponent<SnapPoint>();
            return distRight >= distLeft ? SnapNodeNegative.gameObject.GetComponent<SnapPoint>() : null;
        }

        // Repositions a specific road spline handle given a valid index
        public void SetRoadControlPointPosition(int handleIndex, Vector3 newPosition, bool shouldUpdateMesh = true)
        {
            if (handleIndex < 0 || handleIndex >= splineSource.ControlPointCount)
            {
                throw new IndexOutOfRangeException("Control point handle index out of range!");
            }

            splineSource.SetControlPoint(handleIndex, splineSource.transform.InverseTransformPoint(newPosition));
            SnapFirstAndLastPoints(handleIndex);
            cachedPointVectors[handleIndex] = newPosition;
            if (!shouldUpdateMesh) { return; }
            GenerateRoadMesh(GenerateRoadVertexOutput(roadWidth));
            CleanupRuntimeHandles();
            InitializeRuntimeHandles();
        }

        public GameObject GetPositiveConnection()
        {
            return positiveConnection.gameObject;
        }

        public MagnetRoad GetPositiveConnection_MagnetRoad()
        {
            if (!positiveConnection.gameObject || !positiveConnection.gameObject.GetComponent<MagnetRoad>()) return null;
            return positiveConnection.gameObject.GetComponent<MagnetRoad>();
        }

        public Intersection GetPositiveConnection_Intersection()
        {
            if (!positiveConnection.gameObject || !positiveConnection.gameObject.GetComponent<Intersection>()) return null;
            return positiveConnection.gameObject.GetComponent<Intersection>();
        }

        public GameObject GetNegativeConnection()
        {
            return negativeConnection.gameObject;
        }

        public MagnetRoad GetNegativeConnection_MagnetRoad()
        {
            if (!negativeConnection.gameObject || !negativeConnection.gameObject.GetComponent<MagnetRoad>()) return null;
            return negativeConnection.gameObject.GetComponent<MagnetRoad>();
        }

        public Intersection GetNegativeConnection_Intersection()
        {
            if (!negativeConnection.gameObject || !negativeConnection.gameObject.GetComponent<Intersection>()) return null;
            return negativeConnection.gameObject.GetComponent<Intersection>();
        }

        public static GameObject FindGameObjectWithUniqueConnectionId(string uniqueId)
        {
            foreach (var road in FindObjectsOfType<MagnetRoad>())
            {
                if (road.uniqueConnectionId.Equals(uniqueId)) return road.gameObject;
            }

            return (from intersection in FindObjectsOfType<Intersection>() where intersection.uniqueConnectionId.Equals(uniqueId) select intersection.gameObject).FirstOrDefault();
        }

        public void SaveRoadToXml(string path = "DEFAULT_LOCATION")
        {
            try
            {
                var collection = new MagnetRoadCollection();
                var magnetRoads = new MagnetRoad[1];
                magnetRoads[0] = this;
                collection.PrepareMagnetRoadData(magnetRoads);
                collection.Save(path == "DEFAULT_LOCATION"
                    ? Path.Combine(Application.persistentDataPath, "RoadData.xml")
                    : path);
            }
            catch (IOException)
            {
                Debug.LogWarning("Failed to save the Magnet Road to a file, check the selected path.");
            }
        }

        public static void SaveRoadsToXml(string path = "DEFAULT_LOCATION")
        {
            try
            {
                var collection = new MagnetRoadCollection();
                collection.PrepareMagnetRoadData(FindObjectsOfType<MagnetRoad>());
                collection.PrepareIntersectionData(FindObjectsOfType<Intersection>());
                collection.PrepareDynamicIntersectionData(FindObjectsOfType<DynamicIntersection>());
                collection.Save(path == "DEFAULT_LOCATION"
                    ? Path.Combine(Application.persistentDataPath, "RoadData.xml")
                    : path);
            }
            catch (IOException)
            {
                Debug.LogWarning("Failed to save the Magnet Roads to a file, check the selected path.");
            }
        }

        public static void LoadRoadsFromXml(string path)
        {
            // Store a list of recently spawned roads
            var spawnedRoads = new List<MagnetRoad>();

            // Get the files
            var files = Directory.GetFiles(path);
            if (files.Length > 0)
            {
                foreach (var file in files)
                {
                    // Load the saved data
                    var collection = MagnetRoadCollection.Load(file);

                    // Create saved Magnet Roads
                    var roadDataArray = collection.magnetRoadData;
                    if (collection.magnetRoadData != null)
                    {
                        foreach (var roadData in roadDataArray)
                        {
                            // Load the saved data into a new Magnet Road
                            var newMagnetRoad = new GameObject().AddComponent<MagnetRoad>();
                            newMagnetRoad.name = roadData.name;
                            newMagnetRoad.transform.position = roadData.location;
                            newMagnetRoad.transform.Rotate(Vector3.right, roadData.rotation.x);
                            newMagnetRoad.transform.Rotate(Vector3.up, roadData.rotation.y);
                            newMagnetRoad.transform.Rotate(Vector3.forward, roadData.rotation.z);
                            newMagnetRoad.transform.localScale = new Vector3(1, 1, 1);
                            newMagnetRoad.surfaceMaterial = (Material)Resources.Load("Materials/" + roadData.surfaceMaterial, typeof(Material));
                            newMagnetRoad.sideMaterial = (Material)Resources.Load("Materials/" + roadData.sideMaterial, typeof(Material));
                            newMagnetRoad.roadWidth = roadData.roadWidth;
                            newMagnetRoad.sideDepth = roadData.sideDepth;
                            newMagnetRoad.slopeWidth = roadData.slopeWidth;
                            newMagnetRoad.stepsPerCurve = roadData.stepsPerCurve;
                            if (roadData.isEditableAtRuntime) newMagnetRoad.EnableRuntimeEditing();
                            newMagnetRoad.NegativeConnectionUniqueId = roadData.negativeConnectionId;
                            newMagnetRoad.PositiveConnectionUniqueId = roadData.positiveConnectionId;

                            // Create the req. number of curves for the road
                            while (roadData.handlePoints.Length > newMagnetRoad.splineSource.ControlPointCount) newMagnetRoad.AddCurve();

                            // Place handle points in req. order
                            // Curve start & end points first
                            for (var i = 0; i < roadData.handlePoints.Length; i += 3) newMagnetRoad.splineSource.SetControlPoint(i, newMagnetRoad.transform.InverseTransformPoint(roadData.handlePoints[i]));
                            // Then the mid points
                            for (var i = 1; i < roadData.handlePoints.Length - 1; i += 3)
                            {
                                newMagnetRoad.splineSource.SetControlPoint(i, newMagnetRoad.transform.InverseTransformPoint(roadData.handlePoints[i]));
                                newMagnetRoad.splineSource.SetControlPoint(i + 1, newMagnetRoad.transform.InverseTransformPoint(roadData.handlePoints[i + 1]));
                            }

                            // Load terrain snapping data
                            newMagnetRoad.snapRoadToTerrain = roadData.snapToTerrain;
                            newMagnetRoad.terrain = FindObjectOfType<Terrain>();
                            newMagnetRoad.distanceFromTerrain = roadData.distanceFromTerrain;

                            // Generate the loaded road into the scene
                            newMagnetRoad.GenerateRoadMesh(newMagnetRoad.GenerateRoadVertexOutput(newMagnetRoad.roadWidth));
                            spawnedRoads.Add(newMagnetRoad);

                            // Check for additional road meshes
                            newMagnetRoad.fenceDistanceFromRoad = roadData.roadsideFenceDistanceFromRoad;
                            if (roadData.roadsideFencePanelMesh)
                            {
                                newMagnetRoad.roadsideFencePanelMesh = roadData.roadsideFencePanelMesh;
                                newMagnetRoad.roadsideFencePanelScaling = roadData.roadsideFencePanelScaling;
                                newMagnetRoad.roadsideFencePanelRotation = roadData.roadsideFencePanelRotation;
                                newMagnetRoad.roadsideFencePanelMaterial = (Material)Resources.Load("Materials/" + roadData.roadsideFencePanelMaterial, typeof(Material));
                                MeshGenerator.GenerateRoadFencePanels(newMagnetRoad, newMagnetRoad.roadsideFencePanelMesh, newMagnetRoad.fenceDistanceFromRoad, newMagnetRoad.roadsideFencePanelScaling, newMagnetRoad.roadsideFencePanelRotation, newMagnetRoad.roadsideFencePanelMaterial);
                            }
                            if (roadData.roadsideFencePostMesh)
                            {
                                newMagnetRoad.roadsideFencePostMesh = roadData.roadsideFencePostMesh;
                                newMagnetRoad.roadsideFencePostScaling = roadData.roadsideFencePostScaling;
                                newMagnetRoad.roadsideFencePostRotation = roadData.roadsideFencePostRotation;
                                newMagnetRoad.roadsideFencePostMaterial = (Material)Resources.Load("Materials/" + roadData.roadsideFencePostMaterial, typeof(Material));
                                MeshGenerator.GenerateRoadFencePosts(newMagnetRoad, newMagnetRoad.roadsideFencePostMesh, newMagnetRoad.fenceDistanceFromRoad, newMagnetRoad.roadsideFencePostScaling, newMagnetRoad.roadsideFencePostRotation, newMagnetRoad.roadsideFencePostMaterial);
                            }
                            if (roadData.centerFencePanelMesh)
                            {
                                newMagnetRoad.centerFencePanelMesh = roadData.centerFencePanelMesh;
                                newMagnetRoad.centerFencePanelScaling = roadData.centerFencePanelScaling;
                                newMagnetRoad.centerFencePanelRotation = roadData.centerFencePanelRotation;
                                newMagnetRoad.centerFencePanelMaterial = (Material)Resources.Load("Materials/" + roadData.centerFencePanelMaterial, typeof(Material));
                                MeshGenerator.GenerateRoadFencePanels(newMagnetRoad, newMagnetRoad.centerFencePanelMesh, newMagnetRoad.fenceDistanceFromRoad, newMagnetRoad.centerFencePanelScaling, newMagnetRoad.centerFencePanelRotation, newMagnetRoad.centerFencePanelMaterial, true);
                            }
                            if (roadData.centerFencePostMesh)
                            {
                                newMagnetRoad.centerFencePostMesh = roadData.centerFencePostMesh;
                                newMagnetRoad.centerFencePostScaling = roadData.centerFencePostScaling;
                                newMagnetRoad.centerFencePostRotation = roadData.centerFencePostRotation;
                                newMagnetRoad.centerFencePostMaterial = (Material)Resources.Load("Materials/" + roadData.centerFencePostMaterial, typeof(Material));
                                MeshGenerator.GenerateRoadFencePosts(newMagnetRoad, newMagnetRoad.centerFencePostMesh, newMagnetRoad.fenceDistanceFromRoad, newMagnetRoad.centerFencePostScaling, newMagnetRoad.centerFencePostRotation, newMagnetRoad.centerFencePostMaterial, true);
                            }
                            if (roadData.reservationDimensions.x > 0)
                            {
                                newMagnetRoad.reservationDimensions = roadData.reservationDimensions;
                                newMagnetRoad.reservationSlope = roadData.reservationSlope;
                                newMagnetRoad.reservationSideMaterial = (Material)Resources.Load("Materials/" + roadData.reservationSideMaterial, typeof(Material));
                                newMagnetRoad.reservationTopMaterial = (Material)Resources.Load("Materials/" + roadData.reservationTopMaterial, typeof(Material));
                                MeshGenerator.GenerateCentralReservation(newMagnetRoad);
                            }
                            if (roadData.centerObjectMesh)
                            {
                                newMagnetRoad.centerObjectMesh = roadData.centerObjectMesh;
                                newMagnetRoad.centerObjectScaling = roadData.centerObjectScaling;
                                newMagnetRoad.centerObjectRotation = roadData.centerObjectRotation;
                                newMagnetRoad.centerObjectsToSpawn = roadData.centerObjectsToSpawn;
                                newMagnetRoad.centerObjectMaterial = (Material)Resources.Load("Materials/" + roadData.centerObjectMaterial, typeof(Material));
                                MeshGenerator.GenerateCentralReservationObjects(newMagnetRoad);
                            }
                            if(roadData.roadDecals != null)
                            {
                                newMagnetRoad.roadDecals = new List<MagnetDecal>(roadData.roadDecals.Length);
                                for(int i = 0; i < roadData.roadDecals.Length; i++)
                                {
                                    GameObject gameObject = new GameObject("Decal " + newMagnetRoad.roadDecals.Count);
                                    gameObject.transform.parent = newMagnetRoad.transform;
                                    gameObject.AddComponent<SpriteRenderer>();
                                    MagnetDecal decal = gameObject.AddComponent<MagnetDecal>();
                                    decal.roadID = newMagnetRoad.uniqueConnectionId;
                                    decal.locationOnRoad = roadData.roadDecals[i].roadlocation;
                                    decal.possitionOffset = roadData.roadDecals[i].positionOffset;
                                    decal.rotationOffset = roadData.roadDecals[i].rotationOffset;
                                    decal.transform.localScale = roadData.roadDecals[i].scale;
                                    decal.Decal = (Sprite)Resources.Load("Decals/" + roadData.roadDecals[i].sprite, typeof(Sprite));
                                    newMagnetRoad.roadDecals.Add(decal);
                                }
                                newMagnetRoad.UpdateDecalPositions();
                            }

                        }
                    }

                    // Create saved Intersections
                    var intersectionDataArray = collection.intersectionData;
                    if (collection.intersectionData != null)
                    {
                        foreach (var intersectionData in intersectionDataArray)
                        {
                            // Load the saved data into a new Intersection
                            var newIntersection = new GameObject().AddComponent<Intersection>();
                            newIntersection.name = intersectionData.name;
                            newIntersection.transform.position = intersectionData.location;
                            newIntersection.transform.Rotate(Vector3.right, intersectionData.rotation.x);
                            newIntersection.transform.Rotate(Vector3.up, intersectionData.rotation.y);
                            newIntersection.transform.Rotate(Vector3.forward, intersectionData.rotation.z);
                            newIntersection.transform.localScale = intersectionData.scale;
                            newIntersection.surfaceMaterial = (Material)Resources.Load("Materials/" + intersectionData.surfaceMaterial, typeof(Material));
                            newIntersection.sideMaterial = (Material)Resources.Load("Materials/" + intersectionData.sideMaterial, typeof(Material));
                            newIntersection.roadWidth = intersectionData.roadWidth;
                            newIntersection.sideDepth = intersectionData.sideDepth;
                            newIntersection.slopeWidth = intersectionData.slopeWidth;
                            if (intersectionData.isEditableAtRuntime) newIntersection.EnableRuntimeEditing();

                            newIntersection.sidewalkHeight = intersectionData.sidewalkHeight;
                            newIntersection.sidewalkWidth = intersectionData.sidewalkWidht;
                            newIntersection.sideWalkMaterial = (Material)Resources.Load("Materials/" + intersectionData.sideMaterial, typeof(Material));

                            // Generate the loaded Intersection into the scene
                            newIntersection.SetUp(intersectionData.intersectionType);

                            // Load connections
                            newIntersection.SetConnectionUniqueId(0, intersectionData.connectedUniqueId0);
                            newIntersection.SetConnectionUniqueId(1, intersectionData.connectedUniqueId1);
                            newIntersection.SetConnectionUniqueId(2, intersectionData.connectedUniqueId2);
                            newIntersection.SetConnectionUniqueId(3, intersectionData.connectedUniqueId3);

                        }
                    }

                    // Create saved Intersections
                    var DynamicIntersectionDataArray = collection.dynamicIntersectionData;
                    if (DynamicIntersectionDataArray != null)
                    {
                        foreach (var intersectionData in DynamicIntersectionDataArray)
                        {
                            // Load the saved data into a new Intersection
                            var newIntersection = new GameObject().AddComponent<DynamicIntersection>();
                            newIntersection.name = intersectionData.name;
                            newIntersection.transform.position = intersectionData.location;
                            newIntersection.transform.Rotate(Vector3.right, intersectionData.rotation.x);
                            newIntersection.transform.Rotate(Vector3.up, intersectionData.rotation.y);
                            newIntersection.transform.Rotate(Vector3.forward, intersectionData.rotation.z);
                            newIntersection.transform.localScale = intersectionData.scale;
                            newIntersection.surfaceMaterial = (Material)Resources.Load("Materials/" + intersectionData.surfaceMaterial, typeof(Material));
                            newIntersection.sideMaterial = (Material)Resources.Load("Materials/" + intersectionData.sideMaterial, typeof(Material));
                            newIntersection.roadWidth = intersectionData.roadWidth;
                            newIntersection.sideDepth = intersectionData.sideDepth;
                            newIntersection.slopeWidth = intersectionData.slopeWidth;
                            newIntersection.IsEditableAtRuntime = intersectionData.isEditableAtRuntime;
                            newIntersection.connectionAmount = intersectionData.intersectitionAmmount;
                            newIntersection.SetUp();

                            newIntersection.SetConnectionIDs(intersectionData.ConnectionIDs);
                        }
                    }


                    // Check for possible snap points
                    if (spawnedRoads.Count <= 0) continue;
                    foreach (var road in spawnedRoads)
                    {
                        road.SnapFirstAndLastPoints(0);
                        road.SnapFirstAndLastPoints(road.splineSource.ControlPointCount - 1);
                    }
                }
            }
            else
            {
                // No files selected - return null
                Debug.LogWarning("No file(s) selected to load!");
            }
        }

#if UNITY_EDITOR
        [MenuItem("Tools/Magnet Roads/Regenerate all Magnet Roads", false, 98)]
#endif
        public static void RegenerateAllRoadsAndIntersections()
        {
            foreach (var road in FindObjectsOfType<MagnetRoad>())
            {
                road.GenerateRoadMesh(road.GenerateRoadVertexOutput(road.roadWidth));
            }
            foreach (var intersection in FindObjectsOfType<Intersection>())
            {
                intersection.GenerateIntersectionMesh();
            }
        }

#if UNITY_EDITOR
        [MenuItem("Tools/Magnet Roads/New Magnet Road")]
#endif
        public static GameObject CreateNewSplineRoad()
        {
            var newOne = new GameObject
            {
                name = "Magnet Road"
            };
            var mr = newOne.AddComponent<MagnetRoad>();
            mr.GenerateRoadMesh(mr.GenerateRoadVertexOutput(mr.roadWidth));
            return newOne;
        }

#if UNITY_EDITOR
        [MenuItem("Tools/Magnet Roads/New Intersection/Two-lane")]
#endif
        public static GameObject CreateNewTwoLane()
        {
            var newOne = new GameObject
            {
                name = "Two-lane Intersection"
            };
            newOne.AddComponent<Intersection>().SetUp(Intersection.IntersectionType.TwoLane);
            return newOne;
        }

#if UNITY_EDITOR
        [MenuItem("Tools/Magnet Roads/New Intersection/Three-lane")]
#endif
        public static GameObject CreateNewThreeLane()
        {
            var newOne = new GameObject
            {
                name = "Three-lane Intersection"
            };
            newOne.AddComponent<Intersection>().SetUp(Intersection.IntersectionType.ThreeLane);
            return newOne;
        }

#if UNITY_EDITOR
        [MenuItem("Tools/Magnet Roads/New Intersection/Four-lane")] 
#endif
        public static GameObject CreateNewFourLane()
        {
            var newOne = new GameObject
            {
                name = "Four-lane Intersection"
            };
            newOne.AddComponent<Intersection>().SetUp(Intersection.IntersectionType.FourLane);
            return newOne;
        }

#if UNITY_EDITOR
        [MenuItem("Tools/Magnet Roads/New DynamicIntersection")]
#endif
        public static GameObject CreateNewDynamicIntersection()
        {
            var newOne = new GameObject
            {
                name = "Dynamic Intersection"
            };
            newOne.AddComponent<DynamicIntersection>().SetUp();
            return newOne;
        }

#if UNITY_EDITOR
        [MenuItem("Tools/Magnet Roads/Save Current Road(s) as .xml", false, 99)]
        protected static void SaveRoadsToFile()
        {
            // Get the desired file path
            var path = EditorUtility.SaveFilePanel("Save Magnet Roads as XML", "", "UntitledRoads", "xml");

            // Try to save the roads
            try
            {
                SaveRoadsToXml(path);
            }
            catch (ArgumentException)
            {
                // No file selected
            }
        }
#endif

#if UNITY_EDITOR
        [MenuItem("Tools/Magnet Roads/Load Road(s) from .xml", false, 99)]
        protected static void LoadRoadsFromFile()
        {
            // Get the desired file path
            var path = EditorUtility.OpenFilePanel("Load Magnet Road XML file", "", "xml");

            // Load the roads
            try
            {
                LoadRoadsFromXml(path);
            }
            catch (ArgumentException)
            {
                // User has likely closed the file explorer - do nothing. 
            }
        }
#endif
    }

#if UNITY_EDITOR
    [CustomEditor(typeof(MagnetRoad))]
    public class SplineRoadEditorInspector : Editor
    {
        private MagnetRoad road;
        private Texture logo;
        private float roadFollowerSpeed;
        private int roadFollowerLane;

        protected void OnEnable()
        {
            logo = (Texture)Resources.Load("logo", typeof(Texture));
        }

        protected void OnSceneGUI()
        {
            if (road == target as MagnetRoad)
            {
                if (Tools.current == Tool.Rotate || Tools.current == Tool.Scale) Tools.hidden = true;
                else Tools.hidden = false;
            }
            else Tools.hidden = false;
        }

        public override void OnInspectorGUI()
        {
            road = target as MagnetRoad;

            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.Label(logo, GUILayout.Width((float)logo.width/2), GUILayout.Height((float)logo.height/2));
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            HorizontalLine();

            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.Label(MagnetRoad.VERSION_NUMBER + " - " + MagnetRoad.VERSION_DESCRIPTION, EditorStyles.miniBoldLabel);
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            HorizontalLine();

            // Road editing
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.Label("ROAD DATA", EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            DrawDefaultInspector();
            var oldColor = GUI.color;
            GUI.color = new Color(1, 0.5f, 0.0f);

            HorizontalLine();

            // Road generation
            if (road.IsEditableAtRuntime) GUI.enabled = false;
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.Label("GENERATION", EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            if (GUILayout.Button("Generate Road Mesh"))
            {
                Undo.RecordObject(road, "Generate Road Mesh");
                EditorUtility.SetDirty(road);
                road.GenerateRoadMesh(road.GenerateRoadVertexOutput(road.roadWidth));
            }
            GUI.color = new Color(.2f, .55f, 1);
            if (GUILayout.Button("Clear Road Mesh"))
            {
                Undo.RecordObject(road, "Clear Road Mesh");
                EditorUtility.SetDirty(road);
                road.ClearRoadMesh();
            }
            GUI.color = oldColor;
            if (!GUI.enabled) GUI.enabled = true;

            HorizontalLine();

            // Curve editing
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.Label("EDIT ROAD", EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            // Add curve points
            GUILayout.Label("Add curve points to");
            GUILayout.BeginHorizontal();
            Undo.RecordObject(road.splineSource, "Extend Road");
            if (GUILayout.Button("Negative", EditorStyles.miniButtonLeft)) road.AddCurve();
            if (GUILayout.Button("Positive", EditorStyles.miniButtonRight)) road.AddCurve(true);
            EditorUtility.SetDirty(road.splineSource);
            GUILayout.EndHorizontal();

            // Remove curve points
            GUILayout.Label("Remove curve points from");
            GUILayout.BeginHorizontal();
            Undo.RecordObject(road.splineSource, "Shorten Road");
            if (GUILayout.Button("Negative", EditorStyles.miniButtonLeft)) road.splineSource.RemoveCurve(false);
            if (GUILayout.Button("Positive", EditorStyles.miniButtonRight)) road.splineSource.RemoveCurve(true);
            EditorUtility.SetDirty(road.splineSource);
            GUILayout.EndHorizontal();

            // Add magnet road
            GUILayout.Label("Add new Magnet Road to");
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Negative", EditorStyles.miniButtonLeft)) road.AttachMagnetRoad(false);
            if (GUILayout.Button("Positive", EditorStyles.miniButtonRight)) road.AttachMagnetRoad(true);
            GUILayout.EndHorizontal();

            // Add three-way intersection
            GUILayout.Label("Add Three-way Intersection to");
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Negative", EditorStyles.miniButtonLeft)) road.AttachIntersection(false);
            if (GUILayout.Button("Positive", EditorStyles.miniButtonRight)) road.AttachIntersection(true);
            GUILayout.EndHorizontal();

            // Add four-lane intersection
            GUILayout.Label("Add Four-way Intersection to");
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Negative", EditorStyles.miniButtonLeft)) road.AttachIntersection(false, false);
            if (GUILayout.Button("Positive", EditorStyles.miniButtonRight)) road.AttachIntersection(true, false);
            GUILayout.EndHorizontal();

            HorizontalLine();

            // Connection data
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.Label("CONNECTIONS", EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            GUI.enabled = false;
            EditorGUILayout.ObjectField("Positive:", road.GetPositiveConnection(), typeof(GameObject), true);
            EditorGUILayout.ObjectField("Negative:", road.GetNegativeConnection(), typeof(GameObject), true);
            GUI.enabled = true;
            GUI.color = oldColor;

            HorizontalLine();

            // Additional road objects
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.Label("ADDITIONAL ROAD OBJECTS", EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            // Toggle advanced tools
            road.shouldShowAdvancedTools = EditorGUILayout.Foldout(road.shouldShowAdvancedTools, "Advanced Road Elements");
            EditorGUILayout.HelpBox("Additional road meshes (center reservation & road fences) will automatically be generated when loading from XML - leave the mesh & reservation size fields blank before saving to stop geometry generation on loading", MessageType.Warning);
            if (road.shouldShowAdvancedTools)
            {
                // Fences
                GUILayout.Label("Roadside Fences:", EditorStyles.boldLabel);
                road.roadsideFencePanelMesh = (Mesh)EditorGUILayout.ObjectField("Fence Panel Mesh", road.roadsideFencePanelMesh, typeof(Mesh), true);
                road.roadsideFencePanelScaling = EditorGUILayout.Vector2Field("Fence Panel Scale", road.roadsideFencePanelScaling);
                road.roadsideFencePanelRotation = EditorGUILayout.Vector3Field("Fence Panel Rotation", road.roadsideFencePanelRotation);
                road.roadsideFencePanelMaterial = (Material)EditorGUILayout.ObjectField("Fence Panel Material", road.roadsideFencePanelMaterial, typeof(Material), true);
                GUILayout.Space(12);
                road.roadsideFencePostMesh = (Mesh)EditorGUILayout.ObjectField("Fence Post Mesh", road.roadsideFencePostMesh, typeof(Mesh), true);
                road.roadsideFencePostScaling = EditorGUILayout.Vector3Field("Fence Post Scale", road.roadsideFencePostScaling);
                road.roadsideFencePostRotation = EditorGUILayout.Vector3Field("Fence Post Rotation", road.roadsideFencePostRotation);
                road.roadsideFencePostMaterial = (Material)EditorGUILayout.ObjectField("Fence Post Material", road.roadsideFencePostMaterial, typeof(Material), true);
                GUILayout.Space(12);
                road.fenceDistanceFromRoad = EditorGUILayout.FloatField("Distance From Road", road.fenceDistanceFromRoad);
                GUILayout.Label("Center Fence:", EditorStyles.boldLabel);
                road.centerFencePanelMesh = (Mesh)EditorGUILayout.ObjectField("Fence Panel Mesh", road.centerFencePanelMesh, typeof(Mesh), true);
                road.centerFencePanelScaling = EditorGUILayout.Vector2Field("Fence Panel Scale", road.centerFencePanelScaling);
                road.centerFencePanelRotation = EditorGUILayout.Vector3Field("Fence Panel Rotation", road.centerFencePanelRotation);
                road.centerFencePanelMaterial = (Material)EditorGUILayout.ObjectField("Fence Panel Material", road.centerFencePanelMaterial, typeof(Material), true);
                GUILayout.Space(12);
                road.centerFencePostMesh = (Mesh)EditorGUILayout.ObjectField("Fence Post Mesh", road.centerFencePostMesh, typeof(Mesh), true);
                road.centerFencePostScaling = EditorGUILayout.Vector3Field("Fence Post Scale", road.centerFencePostScaling);
                road.centerFencePostRotation = EditorGUILayout.Vector3Field("Fence Post Rotation", road.centerFencePostRotation);
                road.centerFencePostMaterial = (Material)EditorGUILayout.ObjectField("Fence Post Material", road.centerFencePostMaterial, typeof(Material), true);
                GUI.enabled = road.roadsideFencePanelMesh || road.roadsideFencePostMesh || road.centerFencePanelMesh || road.centerFencePostMesh;
                if (GUILayout.Button("Generate All Fences"))
                {
                    if (road.roadsideFencePanelMesh) MeshGenerator.GenerateRoadFencePanels(road, road.roadsideFencePanelMesh, road.fenceDistanceFromRoad, road.roadsideFencePanelScaling, road.roadsideFencePanelRotation, road.roadsideFencePanelMaterial);
                    if (road.roadsideFencePostMesh) MeshGenerator.GenerateRoadFencePosts(road, road.roadsideFencePostMesh, road.fenceDistanceFromRoad, road.roadsideFencePostScaling, road.roadsideFencePostRotation, road.roadsideFencePostMaterial);
                    if (road.centerFencePanelMesh) MeshGenerator.GenerateRoadFencePanels(road, road.centerFencePanelMesh, road.fenceDistanceFromRoad, road.centerFencePanelScaling, road.centerFencePanelRotation, road.centerFencePanelMaterial, true);
                    if (road.centerFencePanelMesh) MeshGenerator.GenerateRoadFencePosts(road, road.centerFencePostMesh, road.fenceDistanceFromRoad, road.centerFencePostScaling, road.centerFencePostRotation, road.centerFencePostMaterial, true);
                }
                GUI.enabled = true;

                // Central reservation
                GUILayout.Label("Central Reservation:", EditorStyles.boldLabel);
                road.reservationDimensions = EditorGUILayout.Vector2Field("Reservation Size", road.reservationDimensions);
                road.reservationSlope = EditorGUILayout.FloatField("Reservation Slope", road.reservationSlope);
                road.reservationTopMaterial = (Material)EditorGUILayout.ObjectField("Top Material", road.reservationTopMaterial, typeof(Material), true);
                road.reservationSideMaterial = (Material)EditorGUILayout.ObjectField("Side Material", road.reservationSideMaterial, typeof(Material), true);
                GUI.enabled = road.reservationDimensions.x > 0 || road.reservationDimensions.y > 0;
                if (GUILayout.Button("Generate Central Reservation"))
                {
                    MeshGenerator.GenerateCentralReservation(road);
                }
                GUI.enabled = true;

                // Central objects
                GUILayout.Label("Central Objects:", EditorStyles.boldLabel);
                road.centerObjectMesh = (Mesh)EditorGUILayout.ObjectField("Object Mesh", road.centerObjectMesh, typeof(Mesh), true);
                road.centerObjectScaling = EditorGUILayout.Vector3Field("Object Scale", road.centerObjectScaling);
                road.centerObjectRotation = EditorGUILayout.Vector3Field("Object Rotation", road.centerObjectRotation);
                road.centerObjectMaterial = (Material)EditorGUILayout.ObjectField("Object Material", road.centerObjectMaterial, typeof(Material), true);
                road.centerObjectsToSpawn = EditorGUILayout.IntField("Total Objects to Spawn", road.centerObjectsToSpawn);
                GUI.enabled = road.centerObjectMesh;
                if (GUILayout.Button("Generate Center Objects"))
                {
                    MeshGenerator.GenerateCentralReservationObjects(road);
                }
                GUI.enabled = true;
            }

            // Decals
            road.shouldShowDecalList = EditorGUILayout.Foldout(road.shouldShowDecalList, "Show Decal List");
            if (road.shouldShowDecalList)
            {
                EditorGUILayout.LabelField("Number of Decals: " + road.roadDecals.Count);
                for(int i = 0; i<road.roadDecals.Count;i++)
                {
                    MagnetDecal decal = road.roadDecals[i];
                    decal.Decal = (Sprite)EditorGUILayout.ObjectField("Decal Sprite", decal.Decal, typeof(Sprite), true);
                    decal.locationOnRoad = EditorGUILayout.Slider("Decal Location", decal.locationOnRoad, 0.0f, 1.0f);
                    decal.possitionOffset = EditorGUILayout.Vector3Field("Offset Position", decal.possitionOffset);
                    decal.transform.localScale = EditorGUILayout.Vector3Field("Object Scale", decal.transform.localScale);
                    decal.rotationOffset =  EditorGUILayout.Vector3Field("Offset Rotation", decal.rotationOffset);

                    road.UpdateDecalPositions();
                    if (GUILayout.Button("Remove Decal"))
                    {
                        road.roadDecals.Remove(decal);
                        DestroyImmediate(decal.gameObject);
                        i--;
                    }
                    HorizontalLine();
                }
            }
            if (GUILayout.Button("Add New Decal"))
            {
                GameObject gameObject = new GameObject("Decal " + road.roadDecals.Count);
                gameObject.transform.parent = road.transform;
                gameObject.AddComponent<SpriteRenderer>();
                MagnetDecal decal = gameObject.AddComponent<MagnetDecal>();
                decal.roadID = road.uniqueConnectionId;
                road.roadDecals.Add(decal);
                road.UpdateDecalPositions();
            }

            HorizontalLine();

            // Save road
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.Label("SAVE", EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            if (GUILayout.Button("Save Selected Road to XML"))
            {
                var path = EditorUtility.SaveFilePanel("Save Magnet Roads as XML", "", "UntitledRoad", "xml");
                try
                {
                    road.SaveRoadToXml(path);
                }
                catch (ArgumentException)
                {
                    Debug.LogWarning("Road saving failed for " + road.name);
                }
            }

            HorizontalLine();

            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.Label("Copyright \u00A9 2017 - Torchbearer Interactive, Ltd.", EditorStyles.miniBoldLabel);
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            GUILayout.Space(5);
        }

        public static void HorizontalLine()
        {
            var old = GUI.color;
            GUI.color = new Color(.7f,.7f,.7f);
            GUILayout.Space(5);
            GUILayout.Box("", GUILayout.ExpandWidth(true), GUILayout.Height(1));
            GUILayout.Space(5);
            GUI.color = old;
        }
    }
#endif
}