using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Simple mesher: emits one quad per exposed voxel face (no greedy merging).
/// Straightforward and easy to debug; acceptable for small worlds / prototypes.
/// </summary>
public static class SimpleMesher
{
    // For each face we provide vertex offsets (in voxel coords, 4 corners).
    // Order is chosen for consistent winding (clockwise when looking at the face).
    static readonly Vector3Int[] faceDirs = {
        new Vector3Int(0, 0, 1),  // +Z (forward)
        new Vector3Int(0, 0, -1), // -Z (back)
        new Vector3Int(0, 1, 0),  // +Y (up)
        new Vector3Int(0, -1, 0), // -Y (down)
        new Vector3Int(1, 0, 0),  // +X (right)
        new Vector3Int(-1, 0, 0)  // -X (left)
    };

    // For each face (same order as faceDirs) the four corner offsets (relative to voxel origin).
    // Voxel origin is the minimum corner (x,y,z) of that voxel.
    static readonly Vector3[,] faceCorners = new Vector3[6,4]
    {
        // +Z face (front)
        { new Vector3(0,0,1), new Vector3(1,0,1), new Vector3(1,1,1), new Vector3(0,1,1) },
        // -Z face (back)
        { new Vector3(1,0,0), new Vector3(0,0,0), new Vector3(0,1,0), new Vector3(1,1,0) },
        // +Y face (top)
        { new Vector3(0,1,1), new Vector3(1,1,1), new Vector3(1,1,0), new Vector3(0,1,0) },
        // -Y face (bottom)
        { new Vector3(0,0,0), new Vector3(1,0,0), new Vector3(1,0,1), new Vector3(0,0,1) },
        // +X face (right)
        { new Vector3(1,0,1), new Vector3(1,0,0), new Vector3(1,1,0), new Vector3(1,1,1) },
        // -X face (left)
        { new Vector3(0,0,0), new Vector3(0,0,1), new Vector3(0,1,1), new Vector3(0,1,0) }
    };

    // Normals for faces in same order
    static readonly Vector3[] faceNormals = {
        Vector3.forward,
        Vector3.back,
        Vector3.up,
        Vector3.down,
        Vector3.right,
        Vector3.left
    };

    // Simple UVs for a full-face square (0..1)
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

        // helper to check bounds
        bool InBounds(int x, int y, int z) => x >= 0 && x < sx && y >= 0 && y < sy && z >= 0 && z < sz;

        for (int x = 0; x < sx; x++)
        {
            for (int y = 0; y < sy; y++)
            {
                for (int z = 0; z < sz; z++)
                {
                    if (!voxels[x,y,z]) continue; // skip air

                    // for each of the 6 directions, if neighbor is empty/outside, emit that face
                    for (int f = 0; f < 6; f++)
                    {
                        Vector3Int d = faceDirs[f];
                        int nx = x + d.x;
                        int ny = y + d.y;
                        int nz = z + d.z;

                        bool neighborSolid = InBounds(nx, ny, nz) && voxels[nx, ny, nz];
                        if (neighborSolid) continue; // face hidden

                        // add face
                        int baseIndex = verts.Count;
                        for (int i = 0; i < 4; i++)
                        {
                            Vector3 corner = faceCorners[f, i];
                            Vector3 pos = new Vector3(x + corner.x, y + corner.y, z + corner.z) * voxelScale;
                            verts.Add(pos);
                            normals.Add(faceNormals[f]);
                            uvs.Add(quadUVs[i]);
                        }

                        // two triangles (0,1,2) and (0,2,3)
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

        Mesh mesh = new Mesh();
        // if mesh potentially large:
        if (verts.Count > 65000) mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        mesh.SetVertices(verts);
        mesh.SetTriangles(tris, 0);
        mesh.SetNormals(normals);
        mesh.SetUVs(0, uvs);
        mesh.RecalculateBounds();
        // RecalculateNormals() not needed because we provided normals.
        return mesh;
    }
}
