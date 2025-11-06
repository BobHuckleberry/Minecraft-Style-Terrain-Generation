using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Makes a mesh by adding a quad for every visible voxel face.
/// Easy to understand, good for testing or small maps.
/// </summary>
public static class SimpleMesher
{
    // Directions for each face (+Z, -Z, +Y, -Y, +X, -X)
    static readonly Vector3Int[] faceDirs = {
        new Vector3Int(0, 0, 1),
        new Vector3Int(0, 0, -1),
        new Vector3Int(0, 1, 0),
        new Vector3Int(0, -1, 0),
        new Vector3Int(1, 0, 0),
        new Vector3Int(-1, 0, 0)
    };

    // Corner points for each face
    static readonly Vector3[,] faceCorners = new Vector3[6,4]
    {
        { new Vector3(0,0,1), new Vector3(1,0,1), new Vector3(1,1,1), new Vector3(0,1,1) }, // +Z
        { new Vector3(1,0,0), new Vector3(0,0,0), new Vector3(0,1,0), new Vector3(1,1,0) }, // -Z
        { new Vector3(0,1,1), new Vector3(1,1,1), new Vector3(1,1,0), new Vector3(0,1,0) }, // +Y
        { new Vector3(0,0,0), new Vector3(1,0,0), new Vector3(1,0,1), new Vector3(0,0,1) }, // -Y
        { new Vector3(1,0,1), new Vector3(1,0,0), new Vector3(1,1,0), new Vector3(1,1,1) }, // +X
        { new Vector3(0,0,0), new Vector3(0,0,1), new Vector3(0,1,1), new Vector3(0,1,0) }  // -X
    };

    // Normal direction for each face
    static readonly Vector3[] faceNormals = {
        Vector3.forward,
        Vector3.back,
        Vector3.up,
        Vector3.down,
        Vector3.right,
        Vector3.left
    };

    // UVs for one square
    static readonly Vector2[] quadUVs = {
        new Vector2(0,0),
        new Vector2(1,0),
        new Vector2(1,1),
        new Vector2(0,1)
    };

    public static Mesh GenerateMesh(bool[,,] voxels, float voxelScale)
    {
        int sx = voxels.GetLength(0);
        int sy = voxels.GetLength(1);
        int sz = voxels.GetLength(2);

        List<Vector3> verts = new List<Vector3>();
        List<int> tris = new List<int>();
        List<Vector3> normals = new List<Vector3>();
        List<Vector2> uvs = new List<Vector2>();

        // Checks if voxel is inside array
        bool InBounds(int x, int y, int z) => x >= 0 && x < sx && y >= 0 && y < sy && z >= 0 && z < sz;

        // Go through all voxels
        for (int x = 0; x < sx; x++)
        {
            for (int y = 0; y < sy; y++)
            {
                for (int z = 0; z < sz; z++)
                {
                    if (!voxels[x,y,z]) continue; // skip empty blocks

                    // Check all 6 faces
                    for (int f = 0; f < 6; f++)
                    {
                        Vector3Int d = faceDirs[f];
                        int nx = x + d.x;
                        int ny = y + d.y;
                        int nz = z + d.z;

                        bool neighborSolid = InBounds(nx, ny, nz) && voxels[nx, ny, nz];
                        if (neighborSolid) continue; // hidden face

                        // Add one quad
                        int baseIndex = verts.Count;
                        for (int i = 0; i < 4; i++)
                        {
                            Vector3 corner = faceCorners[f, i];
                            Vector3 pos = new Vector3(x + corner.x, y + corner.y, z + corner.z) * voxelScale;
                            verts.Add(pos);
                            normals.Add(faceNormals[f]);
                            uvs.Add(quadUVs[i]);
                        }

                        // Two triangles
                        tris.Add(baseIndex + 0);
                        tris.Add(baseIndex + 1);
                        tris.Add(baseIndex + 2);
                        tris.Add(baseIndex + 0);
                        tris.Add(baseIndex + 2);
                        tris.Add(baseIndex + 3);
                    }
                }
            }
        }

        // Build mesh
        Mesh mesh = new Mesh();
        if (verts.Count > 65000) mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        mesh.SetVertices(verts);
        mesh.SetTriangles(tris, 0);
        mesh.SetNormals(normals);
        mesh.SetUVs(0, uvs);
        mesh.RecalculateBounds();
        return mesh;
    }
}