// Copyright (C) Torchbearer Interactive, Ltd. - All Rights Reserved

// TBUnityLib v1.6.0

using UnityEngine;
using System.Collections.Generic;
using System.Reflection;
using System;

namespace TBUnityLib
{
    // Functions for generating textures
    namespace TextureTools
    {
        public struct TextureGenerator
        {
            // Generate 2D texture providing only a color.
            public static Texture2D GenerateTexture2D(Color color)
            {
                return GenerateTexture2D(color, 4, 4);
            }

            // Generate a 2D texture with custom dimensions.
            public static Texture2D GenerateTexture2D(Color color, int width, int height)
            {
                var returnTexture = new Texture2D(width, height);

                for (var x = 0; x < width; x++)
                {
                    for (var y = 0; y < height; y++)
                    {
                        returnTexture.SetPixel(x, y, color);
                    }
                }
                returnTexture.Apply();

                return returnTexture;
            }
        }
    }

    // Functions for handling terrain coordinates and chunks
    namespace TerrainTools
    {
        public struct Coordinates
        {
            public static Vector2 WorldToTerrainCoords(Vector3 nodeLocation, Terrain terrain)
            {

                return WorldToTerrainCoords(nodeLocation, terrain.terrainData.size, terrain.terrainData.alphamapResolution);
            }

            public static Vector2 WorldToTerrainCoords(Vector3 nodeLocation, Vector3 terrainSize, int terrainAlphaMapResolution)
            {
                var terrainCoordX = nodeLocation.z / terrainSize.x * terrainAlphaMapResolution;
                var terrainCoordY = nodeLocation.x / terrainSize.z * terrainAlphaMapResolution;

                return new Vector2((int)terrainCoordX, (int)terrainCoordY);
            }

            public static Vector3 TerrainToWorldCoords(int x, int y, Terrain terrain, bool mapToTerrainHeight)
            {
                var posX = (float)y / terrain.terrainData.alphamapResolution * terrain.terrainData.size.x;
                var posZ = (float)x / terrain.terrainData.alphamapResolution * terrain.terrainData.size.z;

                if (!mapToTerrainHeight) return new Vector3(posX, 0.0f, posZ);

                var posY = terrain.terrainData.GetHeight(x, y);

                return new Vector3(posX, posY, posZ);
            }

            public static Vector3 TerrainToWorldCoords(int x, int y, int alphaMapResolution, Vector3 terrainSize)
            {
                var posX = (float)y / alphaMapResolution * terrainSize.x;
                var posZ = (float)x / alphaMapResolution * terrainSize.z;

                return new Vector3(posX, 0.0f, posZ);

            }

            public static Vector3 TerrainToWorldCoords(int x, int y, Terrain terrain)
            {
                return TerrainToWorldCoords(x, y, terrain, false);
            }
        }

        public struct Chunks
        {
            // Get the alphaMaps for a specified area of the terarin.
            public static float[,,] GetAlphaMapChunk(float[,,] alphaMap, int x, int y, int width, int height)
            {
                var mapChunk = new float[width, height, alphaMap.GetLength(2)];

                for (int i = 0, v = x; i < width; i++, v++)
                {
                    for (int j = 0, w = y; j < height; j++, w++)
                    {
                        for (var k = 0; k < alphaMap.GetLength(2); k++)
                        {
                            mapChunk[j, i, k] = alphaMap[w, v, k];
                        }
                    }
                }

                return mapChunk;
            }
        }
    }

    // Functions for generating meshes
    namespace MeshTools
    {
        public struct Geometry
        {
            // Generate plane mesh
            public static Mesh GeneratePlaneMesh(int xSegments, int zSegments, float xSize, float zSize)
            {
                Mesh m = new Mesh();

                // Create variables for storing all the ffmesh information.
                List<Vector3> verts = new List<Vector3>();
                List<Vector3> norms = new List<Vector3>();
                List<Vector2> uvs = new List<Vector2>();
                List<int> tris = new List<int>();

                // Generate all of the vertex needed to construct the mesh.
                for (int x = 0; x <= xSegments; x++)
                {
                    for (int z = 0; z <= zSegments; z++)
                    {

                        // Calculate the vertex X and Z positions.
                        float xPos = 0.0f + (xSize / xSegments) * x;
                        float zPos = 0.0f + (zSize / zSegments) * z;

                        // Assign the vertex Y Position.
                        float yPos = 0.0f;

                        xPos -= (xSize / 2);
                        zPos -= (zSize / 2);

                        verts.Add(new Vector3(xPos, yPos, zPos));

                        // After the mesh has been added, add the UVs.
                        float u = 1.0f - (x / xSegments);
                        float v = 1.0f - (z / zSegments);
                        uvs.Add(new Vector2(u, v));

                        norms.Add(Vector3.up);
                    }
                }

                // Generate all the triangles.
                for (int x = 0, y = 0; x < (xSegments * zSegments); x++)
                {

                    // First Triangle.
                    tris.Add(x + y + 1);
                    tris.Add(zSegments + x + y + 1);
                    tris.Add(x + y);

                    // Second Triangle.
                    tris.Add(x + y + 1);
                    tris.Add(zSegments + x + y + 2);
                    tris.Add(zSegments + x + 1 + y);

                    // We need to skip the a quad at the end of the row.
                    if (x % zSegments + 1 == zSegments)
                    {
                        y++;
                    }
                }

                // Assign all the data to the mesh object.
                m.vertices = verts.ToArray();
                m.normals = norms.ToArray();
                m.uv = uvs.ToArray();
                m.triangles = tris.ToArray();

                // Recalculate the bounds so the viewing frustrum doesn't cull this object.
                m.RecalculateBounds();
                m.RecalculateNormals();


                return m;
            }

            public static Mesh GeneratePlaneMesh(int xSegments, int zSegments, Vector2 planeSize)
            {
                return GeneratePlaneMesh(xSegments, zSegments, planeSize.x, planeSize.y);
            }

            public static Mesh GeneratePlaneMesh(Vector2 planeSize)
            {
                return GeneratePlaneMesh(1, 1, planeSize.x, planeSize.y);
            }

            public static Mesh GeneratePlaneMesh(float xSize, float zSize)
            {
                return GeneratePlaneMesh(1, 1, xSize, zSize);
            }

            public static Mesh GeneratePlaneMesh(Vector3 planeSize)
            {
                return GeneratePlaneMesh(1, 1, planeSize.x, planeSize.z);
            }

            public static Mesh GeneratePlaneMesh(Vector3 topLeft, Vector3 topRight, Vector3 bottomLeft, Vector3 bottomRight, bool flipNormals)
            {
                Mesh m = new Mesh();

                List<Vector3> verts = new List<Vector3>();
                List<int> tris = new List<int>();
                List<Vector2> uvs = new List<Vector2>();

                verts.Add(topLeft);
                verts.Add(topRight);
                verts.Add(bottomLeft);
                verts.Add(bottomRight);

                tris.Add(0);
                tris.Add(1);
                tris.Add(2);
                tris.Add(2);
                tris.Add(1);
                tris.Add(3);

                uvs.Add(new Vector2(1.0f, 1.0f));
                uvs.Add(new Vector2(1.0f, 0.0f));
                uvs.Add(new Vector2(0.0f, 1.0f));
                uvs.Add(new Vector2(0.0f, 0.0f));


                if (flipNormals) tris.Reverse();

                m.vertices = verts.ToArray();
                m.triangles = tris.ToArray();
                m.uv = uvs.ToArray();

                m.RecalculateBounds();
                m.RecalculateNormals();



                return m;
            }

            public static Mesh GeneratePlaneMesh(Vector3 topLeft, Vector3 topRight, Vector3 bottomLeft, Vector3 bottomRight)
            {
                return GeneratePlaneMesh(topLeft, topRight, bottomLeft, bottomRight, false);
            }

            // Generate plane edge
            public static Mesh GeneratePlaneEdge(Vector2 planeSize, float depth)
            {
                return GenerateHalfCube(planeSize.x, planeSize.y, depth, false);
            }

            public static Mesh GeneratePlaneEdge(float planeSizeX, float planeSizeY, float depth)
            {
                return GenerateHalfCube(planeSizeX, planeSizeY, depth, false);
            }

            // Generate a tetrahedron
            public static Mesh GenerateTetrahedron(float topPlaneSizeX, float topPlaneSizeZ, float depth, float bottomPlaneSizeX, float bottomPlaneSizeZ, bool capTop = true, bool capBottom = true)
            {
                Mesh m = new Mesh();

                List<Vector3> verticies = new List<Vector3>();
                List<int> triangles = new List<int>();
                List<Vector2> uvs = new List<Vector2>();

                //Side X+
                verticies.Add(new Vector3(topPlaneSizeX / 2, 0.0f, topPlaneSizeZ / 2));
                verticies.Add(new Vector3(topPlaneSizeX / 2, 0.0f, -topPlaneSizeZ / 2));
                verticies.Add(new Vector3(bottomPlaneSizeX / 2, -depth, bottomPlaneSizeZ / 2));
                verticies.Add(new Vector3(bottomPlaneSizeX / 2, -depth, -bottomPlaneSizeZ / 2));
                uvs.Add(new Vector2(1.0f, 1.0f));
                uvs.Add(new Vector2(0.0f, 1.0f));
                uvs.Add(new Vector2(1.0f, 0.0f));
                uvs.Add(new Vector2(0.0f, 0.0f));
                triangles.Add(1);
                triangles.Add(0);
                triangles.Add(2);
                triangles.Add(2);
                triangles.Add(3);
                triangles.Add(1);

                //Side Z+
                verticies.Add(new Vector3(topPlaneSizeX / 2, 0.0f, topPlaneSizeZ / 2));
                verticies.Add(new Vector3(-topPlaneSizeX / 2, 0.0f, topPlaneSizeZ / 2));
                verticies.Add(new Vector3(bottomPlaneSizeX / 2, -depth, bottomPlaneSizeZ / 2));
                verticies.Add(new Vector3(-bottomPlaneSizeX / 2, -depth, bottomPlaneSizeZ / 2));
                uvs.Add(new Vector2(1.0f, 1.0f));
                uvs.Add(new Vector2(0.0f, 1.0f));
                uvs.Add(new Vector2(1.0f, 0.0f));
                uvs.Add(new Vector2(0.0f, 0.0f));
                triangles.Add(6);
                triangles.Add(4);
                triangles.Add(5);
                triangles.Add(5);
                triangles.Add(7);
                triangles.Add(6);

                //Size X-
                verticies.Add(new Vector3(-topPlaneSizeX / 2, 0.0f, topPlaneSizeZ / 2));
                verticies.Add(new Vector3(-topPlaneSizeX / 2, 0.0f, -topPlaneSizeZ / 2));
                verticies.Add(new Vector3(-bottomPlaneSizeX / 2, -depth, bottomPlaneSizeZ / 2));
                verticies.Add(new Vector3(-bottomPlaneSizeX / 2, -depth, -bottomPlaneSizeZ / 2));
                uvs.Add(new Vector2(1.0f, 1.0f));
                uvs.Add(new Vector2(0.0f, 1.0f));
                uvs.Add(new Vector2(1.0f, 0.0f));
                uvs.Add(new Vector2(0.0f, 0.0f));
                triangles.Add(10);
                triangles.Add(8);
                triangles.Add(9);
                triangles.Add(9);
                triangles.Add(11);
                triangles.Add(10);

                //Side Z-
                verticies.Add(new Vector3(topPlaneSizeX / 2, 0.0f, -topPlaneSizeZ / 2));
                verticies.Add(new Vector3(-topPlaneSizeX / 2, 0.0f, -topPlaneSizeZ / 2));
                verticies.Add(new Vector3(bottomPlaneSizeX / 2, -depth, -bottomPlaneSizeZ / 2));
                verticies.Add(new Vector3(-bottomPlaneSizeX / 2, -depth, -bottomPlaneSizeZ / 2));
                uvs.Add(new Vector2(1.0f, 1.0f));
                uvs.Add(new Vector2(0.0f, 1.0f));
                uvs.Add(new Vector2(1.0f, 0.0f));
                uvs.Add(new Vector2(0.0f, 0.0f));
                triangles.Add(13);
                triangles.Add(12);
                triangles.Add(14);
                triangles.Add(14);
                triangles.Add(15);
                triangles.Add(13);

                // Cap Top
                if (capTop)
                {
                    verticies.Add(new Vector3(topPlaneSizeX / 2, 0.0f, -topPlaneSizeZ / 2));
                    verticies.Add(new Vector3(-topPlaneSizeX / 2, 0.0f, -topPlaneSizeZ / 2));
                    verticies.Add(new Vector3(topPlaneSizeX / 2, 0.0f, topPlaneSizeZ / 2));
                    verticies.Add(new Vector3(-topPlaneSizeX / 2, 0.0f, topPlaneSizeZ / 2));
                    uvs.Add(new Vector2(1.0f, 1.0f));
                    uvs.Add(new Vector2(0.0f, 1.0f));
                    uvs.Add(new Vector2(1.0f, 0.0f));
                    uvs.Add(new Vector2(0.0f, 0.0f));
                    triangles.Add(17);
                    triangles.Add(19);
                    triangles.Add(18);
                    triangles.Add(18);
                    triangles.Add(16);
                    triangles.Add(17);
                }
                if (capBottom)
                {
                    int capTopAdder = capTop ? 4 : 0;
                    verticies.Add(new Vector3(bottomPlaneSizeX / 2, -depth, -bottomPlaneSizeZ / 2));
                    verticies.Add(new Vector3(-bottomPlaneSizeX / 2, -depth, -bottomPlaneSizeZ / 2));
                    verticies.Add(new Vector3(bottomPlaneSizeX / 2, -depth, bottomPlaneSizeZ / 2));
                    verticies.Add(new Vector3(-bottomPlaneSizeX / 2, -depth, bottomPlaneSizeZ / 2));
                    uvs.Add(new Vector2(1.0f, 1.0f));
                    uvs.Add(new Vector2(0.0f, 1.0f));
                    uvs.Add(new Vector2(1.0f, 0.0f));
                    uvs.Add(new Vector2(0.0f, 0.0f));
                    triangles.Add(19 + capTopAdder);
                    triangles.Add(17 + capTopAdder);
                    triangles.Add(18 + capTopAdder);

                    triangles.Add(17 + capTopAdder);
                    triangles.Add(16 + capTopAdder);
                    triangles.Add(18 + capTopAdder);
                }



                m.vertices = verticies.ToArray();
                m.triangles = triangles.ToArray();
                m.uv = uvs.ToArray();


                m.RecalculateBounds();
                m.RecalculateNormals();

                return m;

            }

            // Generate a half cube
            public static Mesh GenerateHalfCube(float planeSizeX, float planeSizeY, float depth, bool cap)
            {
                Mesh m = new Mesh();

                List<Vector3> verticies = new List<Vector3>();
                List<int> triangles = new List<int>();
                List<Vector2> uvs = new List<Vector2>();

                //Side X+
                verticies.Add(new Vector3(planeSizeX / 2, 0.0f, planeSizeY / 2));
                verticies.Add(new Vector3(planeSizeX / 2, 0.0f, -planeSizeY / 2));
                verticies.Add(new Vector3(planeSizeX / 2, -depth, planeSizeY / 2));
                verticies.Add(new Vector3(planeSizeX / 2, -depth, -planeSizeY / 2));
                uvs.Add(new Vector2(1.0f, 1.0f));
                uvs.Add(new Vector2(0.0f, 1.0f));
                uvs.Add(new Vector2(1.0f, 0.0f));
                uvs.Add(new Vector2(0.0f, 0.0f));
                triangles.Add(1);
                triangles.Add(0);
                triangles.Add(2);
                triangles.Add(2);
                triangles.Add(3);
                triangles.Add(1);

                //Side Z+
                verticies.Add(new Vector3(planeSizeX / 2, 0.0f, planeSizeY / 2));
                verticies.Add(new Vector3(-planeSizeX / 2, 0.0f, planeSizeY / 2));
                verticies.Add(new Vector3(planeSizeX / 2, -depth, planeSizeY / 2));
                verticies.Add(new Vector3(-planeSizeX / 2, -depth, planeSizeY / 2));
                uvs.Add(new Vector2(1.0f, 1.0f));
                uvs.Add(new Vector2(0.0f, 1.0f));
                uvs.Add(new Vector2(1.0f, 0.0f));
                uvs.Add(new Vector2(0.0f, 0.0f));
                triangles.Add(6);
                triangles.Add(4);
                triangles.Add(5);
                triangles.Add(5);
                triangles.Add(7);
                triangles.Add(6);

                //Size X-
                verticies.Add(new Vector3(-planeSizeX / 2, 0.0f, planeSizeY / 2));
                verticies.Add(new Vector3(-planeSizeX / 2, 0.0f, -planeSizeY / 2));
                verticies.Add(new Vector3(-planeSizeX / 2, -depth, planeSizeY / 2));
                verticies.Add(new Vector3(-planeSizeX / 2, -depth, -planeSizeY / 2));
                uvs.Add(new Vector2(1.0f, 1.0f));
                uvs.Add(new Vector2(0.0f, 1.0f));
                uvs.Add(new Vector2(1.0f, 0.0f));
                uvs.Add(new Vector2(0.0f, 0.0f));
                triangles.Add(10);
                triangles.Add(8);
                triangles.Add(9);
                triangles.Add(9);
                triangles.Add(11);
                triangles.Add(10);

                //Side Z-
                verticies.Add(new Vector3(planeSizeX / 2, 0.0f, -planeSizeY / 2));
                verticies.Add(new Vector3(-planeSizeX / 2, 0.0f, -planeSizeY / 2));
                verticies.Add(new Vector3(planeSizeX / 2, -depth, -planeSizeY / 2));
                verticies.Add(new Vector3(-planeSizeX / 2, -depth, -planeSizeY / 2));
                uvs.Add(new Vector2(1.0f, 1.0f));
                uvs.Add(new Vector2(0.0f, 1.0f));
                uvs.Add(new Vector2(1.0f, 0.0f));
                uvs.Add(new Vector2(0.0f, 0.0f));
                triangles.Add(13);
                triangles.Add(12);
                triangles.Add(14);
                triangles.Add(14);
                triangles.Add(15);
                triangles.Add(13);

                //Cap
                if (cap)
                {
                    verticies.Add(new Vector3(planeSizeX / 2, 0.0f, planeSizeY / 2));
                    verticies.Add(new Vector3(planeSizeX / 2, 0.0f, -planeSizeY / 2));
                    verticies.Add(new Vector3(-planeSizeX / 2, 0.0f, planeSizeY / 2));
                    verticies.Add(new Vector3(-planeSizeX / 2, 0.0f, -planeSizeY / 2));
                    uvs.Add(new Vector2(1.0f, 1.0f));
                    uvs.Add(new Vector2(0.0f, 1.0f));
                    uvs.Add(new Vector2(1.0f, 0.0f));
                    uvs.Add(new Vector2(0.0f, 0.0f));
                    triangles.Add(18);
                    triangles.Add(16);
                    triangles.Add(17);
                    triangles.Add(17);
                    triangles.Add(19);
                    triangles.Add(18);
                }





                m.vertices = verticies.ToArray();
                m.triangles = triangles.ToArray();
                m.uv = uvs.ToArray();
                m.normals = verticies.ToArray();

                m.RecalculateNormals();
                m.RecalculateBounds();

                return m;
            }

            public static Mesh GenerateHalfCube(Vector2 planeSize, float depth)
            {
                return GenerateHalfCube(planeSize.x, planeSize.y, depth, true);
            }

            public static Mesh GenerateHalfCube(float planeSizeX, float planeSizeY, float depth)
            {
                return GenerateHalfCube(planeSizeX, planeSizeY, depth, true);
            }

            // Generate a mesh strip
            public static Mesh GenerateStrip(Generic.Pair<Vector3>[] vertexData, Transform transform, String meshName)
            {
                Vector3[] vertexDataFirst = new Vector3[vertexData.Length];
                Vector3[] vertexDataSecond = new Vector3[vertexData.Length];

                Generic.Helper.SplitPairArray(vertexData, out vertexDataFirst, out vertexDataSecond);

                return GenerateStrip(vertexDataFirst, vertexDataSecond, transform, false, null, meshName);
            }

            public static Mesh GenerateStrip(Generic.Pair<Vector3>[] vertexData, Transform transform)
            {
                Vector3[] vertexDataFirst = new Vector3[vertexData.Length];
                Vector3[] vertexDataSecond = new Vector3[vertexData.Length];

                Generic.Helper.SplitPairArray(vertexData, out vertexDataFirst, out vertexDataSecond);

                return GenerateStrip(vertexDataFirst, vertexDataSecond, transform, false, null, "");
            }

            public static Mesh GenerateStrip(Generic.Pair<Vector3>[] vertexData, Transform transform, bool flipNormals, bool? offsetMode, string meshName)
            {
                Vector3[] vertexDataFirst = new Vector3[vertexData.Length];
                Vector3[] vertexDataSecond = new Vector3[vertexData.Length];

                Generic.Helper.SplitPairArray(vertexData, out vertexDataFirst, out vertexDataSecond);

                return GenerateStrip(vertexDataFirst, vertexDataSecond, transform, flipNormals, offsetMode, meshName);
            }

            public static Mesh GenerateStrip(Vector3[] vertexDataFirst, Vector3[] vertexDataSecond, Transform transform, bool flipNormals, bool? offsetMode, string meshName)
            {
                Mesh m = new Mesh();

                // Define mesh information lists
                List<Vector3> verts = new List<Vector3>();
                List<Vector3> norms = new List<Vector3>();
                List<Vector2> uvs = new List<Vector2>();
                List<int> tris = new List<int>();

                // Add vertex data, normals and uv information to lists
                for (int x = 0, y = 0; x < vertexDataFirst.Length; x++)
                {
                    y = x % 2;

                    Vector3 firstVertex, secondVertex;
                    Quaternion qAngle = Quaternion.AngleAxis(transform.rotation.y, Vector3.up);
                    firstVertex = qAngle * vertexDataFirst[x];
                    secondVertex = qAngle * vertexDataSecond[x];
                    if (offsetMode != null)
                    {
                        if (offsetMode == false)
                        {
                            firstVertex -= transform.position;
                            secondVertex -= transform.position;
                        }
                        else if (offsetMode == true)
                        {
                            firstVertex += transform.position;
                            secondVertex += transform.position;
                        }
                    }

                    //First Vertex
                    verts.Add(firstVertex);
                    norms.Add(Vector3.up);
                    if (y == 0)
                    {
                        uvs.Add(new Vector2(0.0f, 0.0f));
                    }
                    else
                    {
                        uvs.Add(new Vector2(1.0f, 0.0f));
                    }

                    //Second vertex
                    verts.Add(secondVertex);
                    norms.Add(Vector3.up);
                    if (y == 0)
                    {
                        uvs.Add(new Vector2(0.0f, 1.0f));
                    }
                    else
                    {
                        uvs.Add(new Vector2(1.0f, 1.0f));
                    }

                }

                // Add triangles to list
                for (int x = 0, y = 0; y < vertexDataFirst.Length - 1; y++)
                {
                    //First Triangle
                    tris.Add(x);
                    tris.Add(x + 2);
                    tris.Add(x + 1);

                    //Second Triangle
                    tris.Add(x + 1);
                    tris.Add(x + 2);
                    tris.Add(x + 3);
                    x += 2;
                }

                // Assign data to mesh
                if (flipNormals) tris.Reverse();

                m.vertices = verts.ToArray();
                m.normals = norms.ToArray();
                m.triangles = tris.ToArray();
                m.uv = uvs.ToArray();
                m.name = meshName;
                m.RecalculateBounds();
                m.RecalculateNormals();

                return m;
            }
            //Dynamic Intersection Additions
            public static Mesh GeneratePlaneWithCenterMesh(Vector3[] vertArray,Vector3 centerVector, bool inverseNormals = false)
            {
                Mesh mesh = new Mesh();
                List<Vector3> verts = new List<Vector3>(vertArray);
                List<int> tris = new List<int>(vertArray.Length * 3);
                List<Vector2> uvs = new List<Vector2>(vertArray.Length * 3);
                int vertsPerHalf = (verts.Count) / 2;
                float uvAddPerVert = 2f / verts.Count;
                //UV fix for odd numbered pollys to stop first and last UV's to equal 0.0f
                if ((verts.Count &1) == 1)
                {
                    vertsPerHalf = (verts.Count+1) / 2;
                    uvAddPerVert = 2f / (verts.Count+1);
                }
                float halfPoint = vertsPerHalf * uvAddPerVert;

                //Inserts the center vert at the start
                verts.Insert(0, centerVector);
                //Adds the inital UV's
                uvs.Add(new Vector2(0.0f, 0.0f));
                uvs.Add(new Vector2(0.0f, 1.0f));
                for (int i = 1; i < verts.Count-1; i++)
                {
                    tris.Add(0);
                    tris.Add(i);
                    tris.Add(i+1);

                    if(i<=vertsPerHalf)
                        uvs.Add(new Vector2(uvAddPerVert*(float)i, 1.0f));
                    else
                        uvs.Add(new Vector2(halfPoint - (uvAddPerVert * (float)(i - vertsPerHalf)) , 1.0f));
                }
                //Add the last tri from the begining to end
                tris.Add(0);
                tris.Add(verts.Count - 1);
                tris.Add(1);

                if (inverseNormals) tris.Reverse();
                mesh.vertices = verts.ToArray();
                mesh.triangles = tris.ToArray();
                mesh.uv = uvs.ToArray();
                mesh.RecalculateBounds();
                mesh.RecalculateNormals();
                return mesh;
            }

        }
    }

    // Generic functions for everyday use
    namespace Generic
    {
        public struct Pair<T>
        {
            private T first;
            private T second;

            public T First
            {
                get { return first; }
                set { first = value; }
            }

            public T Second
            {
                get { return second; }
                set { second = value; }
            }

            public Pair(T first, T second)
            {
                this.first = first;
                this.second = second;
            }

            public override string ToString()
            {
                return "[" + First + "," + Second + "]";
            }
        }

        public struct Helper
        {
            public static void SplitPairArray<T>(Pair<T>[] vector3Pairs, out T[] outFirstVector3, out T[] outSecondVector3)
            {
                var firstVector3 = new T[vector3Pairs.Length];
                var secondVector3 = new T[vector3Pairs.Length];

                for (var index = 0; index < vector3Pairs.Length; index++)
                {
                    firstVector3[index] = vector3Pairs[index].First;
                    secondVector3[index] = vector3Pairs[index].Second;
                }

                outFirstVector3 = firstVector3;
                outSecondVector3 = secondVector3;

            }

            public static void SetActiveRecursively(GameObject rootObject, bool active)
            {
                rootObject.SetActive(active);
                foreach (Transform childTransform in rootObject.transform)
                {
                    SetActiveRecursively(childTransform.gameObject, active);
                }
            }
        }

        public struct FunctionAsParameter
        {
            // Name to indentify the action
            private readonly string name;

            // The method to be called
            private readonly MethodInfo methodInfo;

            // Specific parameters for the methdo to be called
            private readonly object[] methodParameters;

            // The type of the methods parent class
            private readonly object classObject;

            public FunctionAsParameter(object classObject, MethodInfo methodInfo, object[] methodParameters)
            {
                this.classObject = classObject;
                this.methodInfo = methodInfo;
                this.methodParameters = methodParameters;
                name = "";
            }

            public FunctionAsParameter(object classObject, MethodInfo methodInfo, object[] methodParameters, string name)
            {
                this.classObject = classObject;
                this.methodInfo = methodInfo;
                this.methodParameters = methodParameters;
                this.name = name;
            }

            public string GetName()
            {
                return name;
            }

            public object Invoke()
            {
                return methodInfo.Invoke(classObject, methodParameters);
            }
        }

        [Serializable]
        public struct BoundedValue
        {
            [SerializeField]
            private double value;

            [SerializeField]
            private double lowerBound;

            [SerializeField]
            private double upperBound;

            public BoundedValue(double value, double lowerBound, double upperBound)
            {
                this.value = value;
                this.lowerBound = lowerBound;
                this.upperBound = upperBound;
            }

            public double UpperBound
            {
                get { return upperBound; }
                set { upperBound = value; }
            }

            public double LowerBound
            {
                get { return lowerBound; }
                set { lowerBound = value; }
            }

            public double Value
            {
                get
                {
                    if (value.CompareTo(lowerBound) < 0) value = lowerBound;
                    else if (value.CompareTo(upperBound) > 0) value = upperBound;
                    return value;
                }
                set
                {
                    if (value.CompareTo(lowerBound) < 0) this.value = lowerBound;
                    else if (value.CompareTo(upperBound) > 0) this.value = upperBound;
                    else
                    {
                        this.value = value;
                    }
                }
            }
        }

        public struct StringFactory
        {
            public static bool RemoveDirectoriesBefore(string path, string removeThisDirectoryAndAllParentDirectorys, out string trimmedPath)
            {
                var tempPath = "";
                var returnPath = "";

                for (int pathIndex = 0, removeThisDirectoryIndex = 0; pathIndex < path.Length; pathIndex++)
                {
                    if (path[pathIndex] != removeThisDirectoryAndAllParentDirectorys[removeThisDirectoryIndex])
                    {
                        continue;
                    }

                    tempPath += path[pathIndex];
                    removeThisDirectoryIndex++;

                    if (tempPath != removeThisDirectoryAndAllParentDirectorys)
                    {
                        continue;
                    }

                    for (var i = pathIndex + 1; i < path.Length; i++)
                    {
                        returnPath += path[i];
                    }

                    break;
                }

                trimmedPath = returnPath;

                return returnPath != "";
            }

            public static string RemoveDirectoriesBefore(string path, string removeThisDirectoryAndAllParentDirectorys)
            {
                var trimmedpath = "";

                return RemoveDirectoriesBefore(path, removeThisDirectoryAndAllParentDirectorys, out trimmedpath) ? trimmedpath : "";
            }

            public static string RemoveFileNameExtension(string Path)
            {
                var returnString = "";

                var fileExtensionStartIndex = 0;

                for (var index = Path.Length - 1; index > 0; index--)
                {
                    if (Path[index].ToString() == ".") fileExtensionStartIndex = index;
                }

                for (var index = 0; index < fileExtensionStartIndex; index++)
                {
                    returnString += Path[index];
                }

                if (returnString == "") returnString = Path;

                return returnString;
            }
        }
    }
}