// Copyright (C) Torchbearer Interactive, Ltd. - All Rights Reserved

// This class is used as the structure for any saved road(s)/intersection(s)
// when saving into XML format

using System.IO;
using System.Xml;
using System.Xml.Serialization;
using UnityEngine;

namespace MagnetRoads
{
    [XmlRoot("MagnetRoadCollection")]
    public class MagnetRoadCollection
    {
        public class MagnetRoadData
        {
            // Road-specific data
            [XmlAttribute("name")]
            public string name;
            public string uniqueId;
            public Vector3 location;
            public Vector3 rotation;
            public Vector3 scale;
            public string surfaceMaterial;
            public string sideMaterial;
            public string sidewalkMaterial;
            public float roadWidth;
            public float sidewalkWidth;
            public float sidewalkHeight;
            public float sideDepth;
            public float slopeWidth;
            public int stepsPerCurve;
            public bool isEditableAtRuntime;
            [XmlArray("HandlePoints")]
            [XmlArrayItem("HPoint")]
            public Vector3[] handlePoints;
            public bool snapToTerrain;
            public float distanceFromTerrain;
            public string positiveConnectionId;
            public string negativeConnectionId;

            // Additional road geometry
            public Mesh roadsideFencePanelMesh;
            public Vector2 roadsideFencePanelScaling;
            public Vector3 roadsideFencePanelRotation;
            public float roadsideFenceDistanceFromRoad;
            public string roadsideFencePanelMaterial;
            public Mesh roadsideFencePostMesh;
            public Vector3 roadsideFencePostScaling;
            public Vector3 roadsideFencePostRotation;
            public string roadsideFencePostMaterial;
            public Mesh centerFencePanelMesh;
            public Vector2 centerFencePanelScaling;
            public Vector3 centerFencePanelRotation;
            public float centerFenceDistanceFromRoad;
            public string centerFencePanelMaterial;
            public Mesh centerFencePostMesh;
            public Vector3 centerFencePostScaling;
            public Vector3 centerFencePostRotation;
            public string centerFencePostMaterial;
            public Vector2 reservationDimensions;
            public float reservationSlope;
            public string reservationTopMaterial;
            public string reservationSideMaterial;
            public Mesh centerObjectMesh;
            public int centerObjectsToSpawn;
            public Vector3 centerObjectScaling;
            public Vector3 centerObjectRotation;
            public string centerObjectMaterial;
            //Decals
            [XmlArray("RoadDecals")]
            [XmlArrayItem("Decal")]
            public RoadDecalsData[] roadDecals;
            //Bipolar Connections
            public bool positiveConnectionType;
            public bool negativeConnectionType;
        }

        public class IntersectionData
        {
            [XmlAttribute("name")]
            public string name;
            public Vector3 location;
            public Vector3 rotation;
            public Vector3 scale;
            public string surfaceMaterial;
            public string sideMaterial;
            public float roadWidth;
            public float sideDepth;
            public float slopeWidth;
            public bool isEditableAtRuntime;
            public Intersection.IntersectionType intersectionType;
            public string connectedUniqueId0;
            public string connectedUniqueId1;
            public string connectedUniqueId2;
            public string connectedUniqueId3;
            public float sidewalkWidht;
            public float sidewalkHeight;
            public string sidewalkMaterial;
        }

        public class DynamicIntersectionData
        {
            [XmlAttribute("name")]
            public string name;
            public Vector3 location;
            public Vector3 rotation;
            public Vector3 scale;
            public string surfaceMaterial;
            public string sideMaterial;
            public float roadWidth;
            public float sideDepth;
            public float slopeWidth;
            public bool isEditableAtRuntime;
            public int intersectitionAmmount;
            [XmlArray("ConnectionIDs")]
            [XmlArrayItem("ID")]
            public string[] ConnectionIDs;
        }

        public class RoadDecalsData
        {
            public float roadlocation;
            public Vector3 positionOffset;
            public Vector3 rotationOffset;
            public Vector3 scale;
            public string sprite;
        }

        [XmlArray("MagnetRoads")]
        [XmlArrayItem("MagnetRoad")]
        public MagnetRoadData[] magnetRoadData;
        [XmlArray("Intersections")]
        [XmlArrayItem("Intersection")]
        public IntersectionData[] intersectionData;
        [XmlArray("DynamicIntersections")]
        [XmlArrayItem("DynamicIntersection")]
        public DynamicIntersectionData[] dynamicIntersectionData;

        public void PrepareMagnetRoadData(MagnetRoad[] input)
        {
            magnetRoadData = new MagnetRoadData[input.Length];

            for (var i = 0; i < input.Length; i++)
            {
                magnetRoadData[i] = new MagnetRoadData
                {
                    // Core Magnet Road data
                    name = input[i].name,
                    uniqueId = input[i].uniqueConnectionId,
                    location = input[i].transform.position,
                    rotation = input[i].transform.rotation.eulerAngles,
                    scale = input[i].transform.localScale,
                    surfaceMaterial = input[i].surfaceMaterial ? input[i].surfaceMaterial.name : null,
                    sideMaterial = input[i].sideMaterial ? input[i].sideMaterial.name : null,
                    sidewalkMaterial = input[i].sidewalkMaterial ? input[i].sidewalkMaterial.name : null,
                    roadWidth = input[i].roadWidth,
                    sidewalkWidth = input[i].sidewalkWidth,
                    sidewalkHeight = input[i].sidewalkHeight,
                    sideDepth = input[i].sideDepth,
                    slopeWidth = input[i].slopeWidth,
                    stepsPerCurve = input[i].stepsPerCurve,
                    isEditableAtRuntime = input[i].IsEditableAtRuntime,
                    handlePoints = new Vector3[input[i].splineSource.ControlPointCount],
                    snapToTerrain = input[i].snapRoadToTerrain,
                    distanceFromTerrain = input[i].distanceFromTerrain,
                    positiveConnectionId = input[i].PositiveConnectionUniqueId,
                    negativeConnectionId = input[i].NegativeConnectionUniqueId,

                    // Additional road mesh data
                    roadsideFencePanelMesh = input[i].roadsideFencePanelMesh ? input[i].roadsideFencePanelMesh : null,
                    roadsideFencePanelScaling = input[i].roadsideFencePanelScaling,
                    roadsideFencePanelRotation = input[i].roadsideFencePostRotation,
                    roadsideFencePanelMaterial = input[i].roadsideFencePanelMaterial ? input[i].roadsideFencePanelMaterial.name : null,
                    roadsideFencePostMesh = input[i].roadsideFencePostMesh ? input[i].roadsideFencePostMesh : null,
                    roadsideFencePostScaling = input[i].roadsideFencePostScaling,
                    roadsideFencePostRotation = input[i].roadsideFencePostRotation,
                    roadsideFencePostMaterial = input[i].roadsideFencePostMaterial ? input[i].roadsideFencePostMaterial.name : null,
                    roadsideFenceDistanceFromRoad = input[i].fenceDistanceFromRoad,
                    centerFencePanelMesh = input[i].centerFencePanelMesh ? input[i].centerFencePanelMesh : null,
                    centerFencePanelScaling = input[i].centerFencePanelScaling,
                    centerFencePanelRotation = input[i].centerFencePanelRotation,
                    centerFencePanelMaterial = input[i].centerFencePanelMaterial ? input[i].centerFencePanelMaterial.name : null,
                    centerFencePostMesh = input[i].centerFencePostMesh ? input[i].centerFencePostMesh : null,
                    centerFencePostScaling = input[i].centerFencePostScaling,
                    centerFencePostRotation = input[i].centerFencePostRotation,
                    centerFencePostMaterial = input[i].centerFencePostMaterial ? input[i].centerFencePostMaterial.name : null,
                    reservationDimensions = input[i].reservationDimensions,
                    reservationSlope = input[i].reservationSlope,
                    reservationTopMaterial = input[i].reservationTopMaterial ? input[i].reservationTopMaterial.name : null,
                    reservationSideMaterial = input[i].reservationSideMaterial ? input[i].reservationSideMaterial.name : null,
                    centerObjectMesh = input[i].centerObjectMesh ? input[i].centerObjectMesh : null,
                    centerObjectScaling = input[i].centerObjectScaling,
                    centerObjectRotation = input[i].centerObjectRotation,
                    centerObjectsToSpawn = input[i].centerObjectsToSpawn,
                    centerObjectMaterial = input[i].centerObjectMaterial ? input[i].centerObjectMaterial.name : null,
                    // Decals
                    roadDecals = new RoadDecalsData[input[i].roadDecals.Count],
                    // BipolarConnections
                    negativeConnectionType = input[i].NegativeRoadConnection.positiveConnection,
                    positiveConnectionType = input[i].PositiveRoadConnection.positiveConnection,
                };
                for (var j = 0; j < input[i].splineSource.ControlPointCount; j++) magnetRoadData[i].handlePoints[j] = input[i].transform.TransformPoint(input[i].splineSource.GetControlPoint(j));
                // Parse Down Decals
                for(int k = 0; k < input[i].roadDecals.Count;k++)
                {
                    magnetRoadData[i].roadDecals[k] = new RoadDecalsData() {
                        positionOffset = input[i].roadDecals[k].possitionOffset,
                        roadlocation = input[i].roadDecals[k].locationOnRoad,
                        rotationOffset = input[i].roadDecals[k].rotationOffset,
                        scale = input[i].roadDecals[k].transform.localScale,
                        sprite = input[i].roadDecals[k].Decal != null ? input[i].roadDecals[k].Decal.name : null

                    };
                }
            }
        }

        public void PrepareIntersectionData(Intersection[] input)
        {
            intersectionData = new IntersectionData[input.Length];

            for (var i = 0; i < input.Length; i++)
            {
                intersectionData[i] = new IntersectionData
                {
                    name = input[i].name,
                    location = input[i].transform.position,
                    rotation = input[i].transform.rotation.eulerAngles,
                    scale = input[i].transform.localScale,
                    surfaceMaterial = input[i].surfaceMaterial ? input[i].surfaceMaterial.name : null,
                    sideMaterial = input[i].sideMaterial ? input[i].sideMaterial.name : null,
                    roadWidth = input[i].roadWidth,
                    sideDepth = input[i].sideDepth,
                    slopeWidth = input[i].slopeWidth,
                    isEditableAtRuntime = input[i].IsEditableAtRuntime,
                    intersectionType = input[i].CurrentIntersectionType,
                    connectedUniqueId0 = input[i].GetConnectionUniqueId(0),
                    connectedUniqueId1 = input[i].GetConnectionUniqueId(1),
                    connectedUniqueId2 = input[i].GetConnectionUniqueId(2),
                    connectedUniqueId3 = input[i].GetConnectionUniqueId(3),
                    sidewalkHeight = input[i].sidewalkHeight,
                    sidewalkWidht = input[i].sidewalkWidth,
                    sidewalkMaterial = input[i].sideWalkMaterial ? input[i].sideWalkMaterial.name : null
                };
            }
        }

        public void PrepareDynamicIntersectionData(DynamicIntersection[] input)
        {
            dynamicIntersectionData = new DynamicIntersectionData[input.Length];
            for (var i = 0; i < input.Length; i++)
            {
                dynamicIntersectionData[i] = new DynamicIntersectionData
                {
                    name = input[i].name,
                    location = input[i].transform.position,
                    rotation = input[i].transform.rotation.eulerAngles,
                    scale = input[i].transform.localScale,
                    surfaceMaterial = input[i].surfaceMaterial ? input[i].surfaceMaterial.name : null,
                    sideMaterial = input[i].sideMaterial ? input[i].sideMaterial.name : null,
                    roadWidth = input[i].roadWidth,
                    sideDepth = input[i].sideDepth,
                    slopeWidth = input[i].slopeWidth,
                    isEditableAtRuntime = input[i].IsEditableAtRuntime,
                    intersectitionAmmount = input[i].connectionAmount,
                    ConnectionIDs = new string[input[i].connectionAmount],
                };
                for(int k = 0; k < input[i].Connections.Length; k++)
                {
                    dynamicIntersectionData[i].ConnectionIDs[k] = input[i].ConnectionsUniqueIDs[k];
                }
            
            }
        }

        public void Save(string path)
        {
            var testPath = path.Trim(' ');
            if (string.IsNullOrEmpty(testPath)) return;

            Debug.Log("Saving road(s) to " + path);
            var serializer = new XmlSerializer(typeof(MagnetRoadCollection));
            var stream = new FileStream(path, FileMode.Create);
            try
            {
                serializer.Serialize(stream, this);
            }
            finally
            {
                stream.Close();
            }
        }

        public static MagnetRoadCollection Load(string path)
        {
            var testPath = path.Trim(' ');
            if (string.IsNullOrEmpty(testPath)) return null;

            Debug.Log("Loading road(s) from " + path);
            var serializer = new XmlSerializer(typeof(MagnetRoadCollection));
            var stream = new FileStream(path, FileMode.Open);
            try
            {
                return serializer.Deserialize(stream) as MagnetRoadCollection;
            }
            finally
            {
                stream.Close();
            }
        }
    }
}