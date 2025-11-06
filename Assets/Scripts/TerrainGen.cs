using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using System.Threading.Tasks;

public class VoxelTerrain : MonoBehaviour
{
    public int chunksX = 4;
    public int chunksZ = 4;
    public int size = 16;          
    public float voxelScale = 1f;
    public float heightScale = 20f;
    public float noiseScale = 0.08f;
    public int seed = 1337;
    public Material chunkMaterial;   
    public int renderDistanceInChunks = 4;  

    public float updateInterval = 0.5f;

    public Transform playerTransform;

    //All active chunks
    Dictionary<Vector2Int, Chunk> chunks = new Dictionary<Vector2Int, Chunk>();
    private WaitForSeconds updateWait;

    void Start()
    {
        if (chunkMaterial == null)
        {
            Debug.LogWarning("VoxelTerrain: chunkMaterial not assigned. Assign one in the Inspector.");
        }

        Random.InitState(seed);
        updateWait = new WaitForSeconds(updateInterval);
        StartCoroutine(WorldUpdateLoop());
    }
    IEnumerator WorldUpdateLoop()
    {
        while (true)
        {
            UpdateVisibleChunks();
            yield return updateWait;
        }
    }

    public void UpdateVisibleChunks()
    {
        if (playerTransform == null) return;

        //Current Player position
        Vector3 playerPos = playerTransform.position;
        //Current Player chunk coordinates
        int playerChunkX = Mathf.FloorToInt(playerPos.x / (size * voxelScale));
        int playerChunkZ = Mathf.FloorToInt(playerPos.z / (size * voxelScale));

        var desired = new HashSet<Vector2Int>();

        for (int dz = -renderDistanceInChunks; dz <= renderDistanceInChunks; dz++)
        {
            for (int dx = -renderDistanceInChunks; dx <= renderDistanceInChunks; dx++)
            {
                Vector2Int coord = new Vector2Int(playerChunkX + dx, playerChunkZ + dz);

                // optional: skip if outside a circular radius (euclidean)
                if (dx*dx + dz*dz > renderDistanceInChunks * renderDistanceInChunks) continue;

                desired.Add(coord);
                if (!chunks.ContainsKey(coord))
                {
                    // spawn chunk (can be coroutine/task if generation heavy)
                    StartCoroutine(SpawnChunk(coord));
                }
            }
        }
    }

    IEnumerator SpawnChunk(Vector2Int coord)
    {
        // 1) start CPU voxel generation on a background thread
        Task<bool[,,]> genTask = Task.Run(() =>
            Chunk.GenerateVoxelArray(size, noiseScale, heightScale, seed, coord.x, coord.y)
        );

        // you can yield until the task completes (non-blocking)
        while (!genTask.IsCompleted)
            yield return null;

        bool[,,] voxelData = genTask.Result; // now on main thread

        // 2) create GameObject and chunk component on main thread
        var go = new GameObject($"Chunk_{coord.x}_{coord.y}");
        go.transform.position = new Vector3(coord.x * size, 0, coord.y * size);
        go.transform.parent = transform;
        go.AddComponent<MeshFilter>();
        go.AddComponent<MeshRenderer>();
        go.AddComponent<MeshCollider>();

        var chunkComponent = go.AddComponent<Chunk>();

        // 3) init Unity-side fields and assign material
        chunkComponent.Init(size, voxelScale, chunkMaterial, coord.x, coord.y);

        // 4) set voxel data (already computed)
        chunkComponent.SetVoxelData(voxelData);

        // 5) build/assign mesh & collider (main thread, quick)
        chunkComponent.BuildMeshFromVoxelData();

        chunks[coord] = chunkComponent;

        yield return null;
    }
    
    private GameObject CreateChunkGameObject(Vector2Int coord, object data)
    {
        GameObject chunkGO = new GameObject($"Chunk_{coord.x}_{coord.y}");
        chunkGO.transform.position = new Vector3(coord.x * size, 0, coord.y * size);
        // add MeshFilter/MeshRenderer etc. and build mesh from data
        var mf = chunkGO.AddComponent<MeshFilter>();
        var mr = chunkGO.AddComponent<MeshRenderer>();
        //mf.mesh = BuildMeshFromData(data); // create mesh on main thread
        chunkGO.transform.parent = transform;
        return chunkGO;
    }
    private object GenerateChunkData(Vector2Int coord)
    {
        // heavy CPU work could be done here or dispatched to a background thread
        return new { coord = coord }; // placeholder
    }
    private void RemoveChunk(Vector2Int coord)
    {
        if (!chunks.TryGetValue(coord, out var go)) return;
        Destroy(go);
        chunks.Remove(coord);
    }


}
