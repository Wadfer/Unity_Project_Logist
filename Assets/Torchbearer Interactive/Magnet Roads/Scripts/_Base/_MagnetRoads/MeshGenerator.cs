// Copyright (C) Torchbearer Interactive, Ltd. - All Rights Reserved

// This class is used to spawn and position custom roadside/central reservation objects
// on existing Magnet Roads; i.e. fences, streetlights etc.

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using TBUnityLib.Generic;
using TBUnityLib.MeshTools;

namespace MagnetRoads
{
    [AddComponentMenu("")]
    public class MeshGenerator : MonoBehaviour
    {
        public static GameObject GenerateRoadFencePanels(MagnetRoad parentRoad, Mesh fenceMesh, float distanceFromRoad, Vector2 fenceXyScales, Vector3 fenceRotationOffset, Material fenceMaterial, bool isCenterOnly = false)
        {
            if (parentRoad == null)
            {
                throw new NullReferenceException("Invalid MagnetRoad instance provided - road fences will not generate");
            }
            if (fenceMesh == null)
            {
                throw new NullReferenceException("Invalid fence mesh provided - road fences will not generate");
            }

            var extentLengths = fenceMesh.bounds.size;
            var roadSideVectors = parentRoad.GenerateRoadVertexOutput(!isCenterOnly ? parentRoad.roadWidth + distanceFromRoad : 0.0f);
            var parentGameObject = new GameObject(parentRoad.name + " - Fences");
            parentGameObject.transform.parent = parentRoad.transform;

            for (var i = 0; i < roadSideVectors.Length - 1; i++)
            {
                // Left side fence panel
                var newFence1 = GenerateObjectWithMesh(parentRoad.name + (!isCenterOnly ? " Left Fence " : " Center Fence ") + i, fenceMesh, fenceMaterial);
                var pointDistance1 = Vector3.Distance(roadSideVectors[i].First, roadSideVectors[i + 1].First);
                var newScale1 = newFence1.transform.localScale;
                newScale1.x = fenceXyScales.x;
                newScale1.y = fenceXyScales.y;
                newScale1.z = pointDistance1 * newScale1.z / extentLengths.z;
                newFence1.transform.localScale = newScale1;
                newFence1.transform.position = Vector3.Lerp(roadSideVectors[i].First, roadSideVectors[i + 1].First, 0.5f);
                newFence1.transform.LookAt(roadSideVectors[i + 1].First);
                newFence1.transform.rotation = Quaternion.Euler(newFence1.transform.rotation.eulerAngles + (i < roadSideVectors.Length - 1 ? fenceRotationOffset : -fenceRotationOffset));
                newFence1.transform.parent = parentGameObject.transform;

                if (isCenterOnly) continue;

                // Right side fence panel
                var newFence2 = GenerateObjectWithMesh(parentRoad.name + " Right Fence " + i, fenceMesh, fenceMaterial);
                var pointDistance2 = Vector3.Distance(roadSideVectors[i].Second, roadSideVectors[i + 1].Second);
                var newScale2 = newFence2.transform.localScale;
                newScale2.x = fenceXyScales.x;
                newScale2.y = fenceXyScales.y;
                newScale2.z = pointDistance2 * newScale2.z / extentLengths.z;
                newFence2.transform.localScale = newScale2;
                newFence2.transform.position = Vector3.Lerp(roadSideVectors[i].Second, roadSideVectors[i + 1].Second, 0.5f);
                newFence2.transform.LookAt(roadSideVectors[i + 1].Second);
                newFence2.transform.rotation = Quaternion.Euler(newFence2.transform.rotation.eulerAngles + (i < roadSideVectors.Length - 1 ? fenceRotationOffset : -fenceRotationOffset));
                newFence2.transform.parent = parentGameObject.transform;
            }

            return parentGameObject;
        }

        public static GameObject GenerateRoadFencePosts(MagnetRoad parentRoad, Mesh fenceMesh, float distanceFromRoad, Vector3 fencePostScales, Vector3 fenceRotationOffset, Material fenceMaterial, bool isCenterOnly = false)
        {
            if (parentRoad == null)
            {
                throw new NullReferenceException("Invalid MagnetRoad instance provided - road fence posts will not generate");
            }
            if (fenceMesh == null)
            {
                throw new NullReferenceException("Invalid fence post mesh provided - road fence posts will not generate");
            }

            var roadSideVectors = parentRoad.GenerateRoadVertexOutput(!isCenterOnly ? parentRoad.roadWidth + distanceFromRoad : 0.0f);
            var parentGameObject = new GameObject(parentRoad.name + " - Fence Posts");
            parentGameObject.transform.parent = parentRoad.transform;

            for (var i = 0; i < roadSideVectors.Length; i++)
            {
                // Left side fence post
                var newFence1 = GenerateObjectWithMesh(parentRoad.name + (!isCenterOnly ? " Left Fence Post " : " Center Fence Post ") + i, fenceMesh, fenceMaterial);
                newFence1.transform.localScale = fencePostScales;
                newFence1.transform.position = roadSideVectors[i].First; 
                newFence1.transform.LookAt(i < roadSideVectors.Length - 1 ? roadSideVectors[i+1].First : roadSideVectors[i-1].First);
                newFence1.transform.rotation = Quaternion.Euler(newFence1.transform.rotation.eulerAngles + (i < roadSideVectors.Length - 1 ? fenceRotationOffset : -fenceRotationOffset));
                newFence1.transform.parent = parentGameObject.transform;

                if (isCenterOnly) continue;

                // Right side fence post
                var newFence2 = GenerateObjectWithMesh(parentRoad.name + " Right Fence Post " + i, fenceMesh, fenceMaterial);
                newFence2.transform.localScale = fencePostScales;
                newFence2.transform.position = roadSideVectors[i].Second; 
                newFence2.transform.LookAt(i < roadSideVectors.Length - 1 ? roadSideVectors[i + 1].Second : roadSideVectors[i - 1].Second);
                newFence2.transform.rotation = Quaternion.Euler(newFence2.transform.rotation.eulerAngles + (i < roadSideVectors.Length - 1 ? fenceRotationOffset : -fenceRotationOffset));
                newFence2.transform.parent = parentGameObject.transform;
            }

            return parentGameObject;
        }

        public static GameObject GenerateCentralReservation(MagnetRoad parentRoad)
        {
            if (parentRoad == null)
            {
                throw new NullReferenceException("Invalid MagnetRoad instance provided - road central reservation will not generate");
            }

            // Generate main central reservation mesh vectors
            var reservationMeshVectors = parentRoad.GenerateRoadVertexOutput(parentRoad.reservationDimensions.x).ToList();
            for (var i = 0; i < reservationMeshVectors.Count; i++)
            {
                var pair = reservationMeshVectors[i];
                reservationMeshVectors[i] = new Pair<Vector3>(pair.First += new Vector3(0, parentRoad.reservationDimensions.y, 0), pair.Second += new Vector3(0, parentRoad.reservationDimensions.y, 0));
            }

            // Create object & mesh
            var parentGameObject = new GameObject(parentRoad.name + " - Central Reservation");
            var mesh = parentGameObject.AddComponent<MeshFilter>().sharedMesh = Geometry.GenerateStrip(reservationMeshVectors.ToArray(), parentGameObject.transform, true, null, "Central Reservation Strip");
            parentGameObject.AddComponent<MeshCollider>().sharedMesh = mesh;
            parentGameObject.AddComponent<MeshRenderer>().sharedMaterial = parentRoad.reservationTopMaterial;
            parentGameObject.transform.parent = parentRoad.transform;

            // Generate central reservation side mesh
            var sideMeshVectors = new List<Pair<Vector3>>();
            var sideBaseVectors = parentRoad.GenerateRoadVertexOutput(parentRoad.reservationDimensions.x += parentRoad.reservationSlope * 2);
            for (var index = 0; index < reservationMeshVectors.Count; index++)
            {
                var currentPair = reservationMeshVectors[index];
                sideMeshVectors.Insert(0, new Pair<Vector3>(currentPair.First, sideBaseVectors[index].First));
                sideMeshVectors.Insert(sideMeshVectors.Count, new Pair<Vector3>(currentPair.Second, sideBaseVectors[index].Second));
            }
            sideMeshVectors.Insert(0, sideMeshVectors[sideMeshVectors.Count - 1]);

            // Create side object & mesh
            var sideMeshObject = new GameObject(parentRoad.name + " - Central Reservation Sides");
            mesh = sideMeshObject.AddComponent<MeshFilter>().sharedMesh = Geometry.GenerateStrip(sideMeshVectors.ToArray(), sideMeshObject.transform, true, null, "Central Reservation Strip");
            sideMeshObject.AddComponent<MeshCollider>().sharedMesh = mesh;
            sideMeshObject.AddComponent<MeshRenderer>().sharedMaterial = parentRoad.reservationSideMaterial;
            sideMeshObject.transform.parent = parentGameObject.transform;

            return parentGameObject;
        }

        public static GameObject GenerateCentralReservationObjects(MagnetRoad parentRoad)
        {
            if (parentRoad == null)
            {
                throw new NullReferenceException("Invalid MagnetRoad instance provided - central reservation objects will not generate");
            }
            if (parentRoad.centerObjectMesh == null)
            {
                throw new NullReferenceException("Invalid central reservation object mesh provided - central reservation objects will not generate");
            }

            var centerVectorPoints = parentRoad.GetCentreWaypoints();
            var parentGameObject = new GameObject(parentRoad.name + " - Central Reservation Objects");
            parentGameObject.transform.parent = parentRoad.transform;

            var totalPoints = parentRoad.stepsPerCurve * parentRoad.splineSource.CurveCount;
            var pointInterval = totalPoints / parentRoad.centerObjectsToSpawn;
            var startPoint = pointInterval / 2;
            for (var i = 0; i < parentRoad.centerObjectsToSpawn; i++)
            {
                // Generate center objects
                var newObject = GenerateObjectWithMesh(parentRoad.name + " Center Object " + i, parentRoad.centerObjectMesh, parentRoad.centerObjectMaterial);
                newObject.transform.localScale = parentRoad.centerObjectScaling;
                var currentIndex = startPoint + i * pointInterval;
                newObject.transform.position = centerVectorPoints[currentIndex]; 
                newObject.transform.LookAt(i < totalPoints - 1 ? centerVectorPoints[currentIndex+1] : centerVectorPoints[currentIndex - 1]);
                newObject.transform.rotation = Quaternion.Euler(newObject.transform.rotation.eulerAngles + (i < totalPoints - 1 ? parentRoad.centerObjectRotation : -parentRoad.centerObjectRotation));
                newObject.transform.parent = parentGameObject.transform;
            }

            return parentGameObject;
        }

        private static GameObject GenerateObjectWithMesh(string objectName, Mesh objectMesh, Material objectMaterial)
        {
            var output = new GameObject(objectName);
            var outputMeshFilter = output.AddComponent<MeshFilter>();
            outputMeshFilter.sharedMesh = objectMesh;
            var outputMeshCollider = output.AddComponent<MeshCollider>();
            outputMeshCollider.sharedMesh = objectMesh;
            var outputMeshRenderer = output.AddComponent<MeshRenderer>();
            outputMeshRenderer.sharedMaterial = objectMaterial;

            return output;
        }
    }
}