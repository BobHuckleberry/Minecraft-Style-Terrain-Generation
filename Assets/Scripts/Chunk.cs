using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
[RequireComponent(typeof(MeshCollider))]
public class Chunk : MonoBehaviour
{
    // config (set during Init)
    int size = 16;
    float voxelScale = 1f;
    float noiseScale = 0.08f;
    float heightScale = 20f;
    int seed = 0;
    int chunkX = 0, chunkZ = 0;

    // runtime
    bool[,,] voxels;
    MeshFilter mf;
    MeshRenderer mr;
    MeshCollider mc;

    // allow external code to set precomputed voxels
    public void SetVoxelData(bool[,,] voxelData)
    {
        voxels = voxelData;
    }
    public void Init(int size, float voxelScale, Material material, int cx, int cz)
    {
        this.size = size;
        this.voxelScale = voxelScale;
        this.chunkX = cx;
        this.chunkZ = cz;

        mf = GetComponent<MeshFilter>();
        mr = GetComponent<MeshRenderer>();
        mc = GetComponent<MeshCollider>();

        if (material != null) mr.sharedMaterial = material;
        else mr.sharedMaterial = new Material(Shader.Find("Standard"));
    }
    public void BuildMeshFromVoxelData()
    {
        if (voxels == null) return;

        Mesh mesh = SimpleMesher.GenerateMesh(voxels, voxelScale);
        mf.sharedMesh = mesh;

        if (mc == null) mc = GetComponent<MeshCollider>();
        mc.sharedMesh = null;
        mc.sharedMesh = mesh;
    }

    public static bool[,,] GenerateVoxelArray(int size, float noiseScale, float heightScale, int seed, int chunkX, int chunkZ)
    {
        bool[,,] vox = new bool[size, size, size];
        float worldOffsetX = chunkX * size;
        float worldOffsetZ = chunkZ * size;

        for (int x = 0; x < size; x++)
        for (int z = 0; z < size; z++)
        {
            float noise = FractalNoiseStatic(worldOffsetX + x, worldOffsetZ + z, 3, noiseScale, 0.5f, 2.0f, seed);
            int height = Mathf.FloorToInt(noise * heightScale);

            for (int y = 0; y < size; y++)
                vox[x, y, z] = (y <= height);
        }
        return vox;
    }

    // make a CPU-only version of fractal noise (no Unity API usage except Mathf)
    static float FractalNoiseStatic(float x, float z, int octaves, float frequency, float persistence, float lacunarity, int seed)
    {
        float total = 0f;
        float amplitude = 1f;
        float maxValue = 0f;

        for (int i = 0; i < octaves; i++)
        {
            float sampleX = (x + seed * 100) * frequency;
            float sampleZ = (z + seed * 100) * frequency;
            float perlin = Mathf.PerlinNoise(sampleX, sampleZ);
            total += perlin * amplitude;
            maxValue += amplitude;
            amplitude *= persistence;
            frequency *= lacunarity;
        }
        return total / maxValue;
    }
    
}