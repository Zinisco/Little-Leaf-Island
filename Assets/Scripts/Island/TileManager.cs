using System;
using System.Collections.Generic;
using UnityEngine;
using static UnityEditor.PlayerSettings;

public class TileManager : MonoBehaviour
{
    public static TileManager I;

    [Header("Seed Settings")]
    public int worldSeed;


    [Header("Tile Settings")]
    public float tileSize = 2f;
    public GameObject grassPrefab;
    public GameObject soilPrefab;
    public GameObject wetSoilPrefab;
    public GameObject seededSoilDryPrefab;
    public GameObject seededSoilWetPrefab;
    public GameObject decorGrassPrefab;


    [Header("HouseFeatures")]
    public GameObject housePrefab;
    public Vector2Int houseOrigin = new Vector2Int(2, -1); // bottom-left tile of house
    public Vector2Int houseSize = new Vector2Int(3, 3); // width = 3, height = 2
    public Vector3 houseOffset = new Vector3(-1, 1.2f, -1);
    public float houseRotationY = 90f;


    [Header("Expansion Settings")]
    public int expansionCost = 5;

    [Header("Resource Prefabs")]
    public GameObject[] treePrefabs;
    public GameObject[] rockPrefabs;
    public Material ghostMaterial;


    [Header("Resource Spawn Tuning")]
    [Range(0f, 1f)] public float spawnChance = 0.25f;  // Balanced: 1 in 4 tiles get a resource
    [Range(0f, 1f)] public float treeWeight = 0.66f;   // A,A,B feeling: ~2/3 trees, 1/3 rocks
    public int treeHP = 3;
    public int rockHP = 3;

    [Header("Crops")]
    public CropDefinition carrotCrop;

    [Header("Island Root")]
    public Transform islandRoot;

    [Header("FX")]
    public ParticleSystem hoeFXPrefab;
    public AudioClip hoeSound;
    public float hoeSoundVolume = 0.9f;
    public ParticleSystem waterFXPrefab;
    public AudioClip waterSound;
    public float waterSoundVolume = 0.9f;

    private Dictionary<(int, int), Tile> tiles = new();

    // Pending resources keyed by grid position, only for NOT-YET-OWNED tiles
    private Dictionary<Vector2Int, PendingResource> pending = new();

    [Serializable]
    public class PendingResource
    {
        public bool hasResource;
        public ResourceNode.ResourceType type;
        public int hp;
        public int prefabIndex; 
    }

    void Awake()
    {
        I = this;

        // If this is a NEW GAME, randomize a seed
        // Later, when loading a save, you'll overwrite this with the saved value
        worldSeed = UnityEngine.Random.Range(100000, 999999);

    }


    void Start()
    {
        for (int x = -1; x <= 1; x++)
            for (int y = -1; y <= 1; y++)
                AddTile(x, y);

        SpawnHouse();
    }

    public void AddTile(int x, int y)
    {
        if (tiles.ContainsKey((x, y))) return;

        Vector3 pos = GridToWorld(x, y);

        // --- Create EMPTY tile root ---
        GameObject tileGO = new GameObject($"Tile_{x}_{y}");
        tileGO.transform.SetParent(islandRoot);
        tileGO.transform.position = pos;
        tileGO.layer = LayerMask.NameToLayer("Tile");

        // --- Add Tile component ---
        Tile tile = tileGO.AddComponent<Tile>();
        tile.x = x;
        tile.y = y;
        tile.state = Tile.State.Grass;

        // --- Add the grass visual as child AND register it as currentVisual ---
        GameObject visual = Instantiate(grassPrefab, tileGO.transform);
        visual.transform.localPosition = Vector3.zero;

        foreach (Transform t in visual.GetComponentsInChildren<Transform>(true))
            t.gameObject.layer = LayerMask.NameToLayer("Tile");

        tile.currentVisual = visual;  
        tile.UpdateCropAnchor();
        tile.UpdatePlacementAnchor();

        // --- Generate surface collider ---
        Renderer r = visual.GetComponentInChildren<Renderer>();
        float topY = r.bounds.max.y;

        BoxCollider col = tileGO.AddComponent<BoxCollider>();
        col.size = new Vector3(tileSize, 0.05f, tileSize);
        col.center = new Vector3(0f, topY - tileGO.transform.position.y + 0.025f, 0f);

        // Store tile
        tiles.Add((x, y), tile);
    }



    void SpawnHouse()
    {
        // Create and mark the tiles under the house
        for (int x = 0; x < houseSize.x; x++)
        {
            for (int y = 0; y < houseSize.y; y++)
            {
                int gx = houseOrigin.x + x;
                int gy = houseOrigin.y + y;

                if (!tiles.ContainsKey((gx, gy)))
                    AddTile(gx, gy);

                tiles[(gx, gy)].state = Tile.State.Decor;
                tiles[(gx, gy)].UpdateVisual();
            }
        }

        // Position is the center of the footprint
        Vector3 basePos = GridToWorld(houseOrigin.x, houseOrigin.y);
        Vector3 centerOffset = new Vector3(
            (houseSize.x * tileSize) / 2f,
            0,
            (houseSize.y * tileSize) / 2f
        );

        Quaternion rot = Quaternion.Euler(0, houseRotationY, 0);

        Instantiate(
            housePrefab,
            basePos + centerOffset + houseOffset,
            rot,
            islandRoot
        );
    }



    public Vector3 GridToWorld(int x, int y) => new Vector3(x * tileSize, 0f, y * tileSize);
    public bool HasTile(int x, int y) => tiles.ContainsKey((x, y));
    public Dictionary<(int, int), Tile> GetAllTiles() => tiles;

    System.Random CoordRng(Vector2Int p)
    {
        int seed = worldSeed;
        unchecked
        {
            seed ^= p.x * 73856093;
            seed ^= p.y * 19349663;
        }
        return new System.Random(seed);
    }


    PendingResource GetOrGeneratePending(Vector2Int p)
    {
        if (pending.TryGetValue(p, out var pr)) return pr;

        var rng = CoordRng(p);
        bool spawns = rng.NextDouble() < spawnChance;

        var result = new PendingResource { hasResource = spawns, hp = 0, prefabIndex = -1 };

        if (spawns)
        {
            bool isTree = rng.NextDouble() < treeWeight;
            result.type = isTree ? ResourceNode.ResourceType.Tree : ResourceNode.ResourceType.Rock;
            result.hp = isTree ? treeHP : rockHP;

            // Choose prefab index once and store it
            var arr = isTree ? treePrefabs : rockPrefabs;
            if (arr != null && arr.Length > 0)
                result.prefabIndex = rng.Next(arr.Length);
        }

        pending[p] = result;
        return result;
    }

    public void SpawnGhostIfAny(Vector2Int p, Transform parent)
    {
        var pr = GetOrGeneratePending(p);
        if (!pr.hasResource) return;

        GameObject[] arr = pr.type == ResourceNode.ResourceType.Tree ? treePrefabs : rockPrefabs;
        if (arr == null || arr.Length == 0) return;

        int index = Mathf.Clamp(pr.prefabIndex, 0, arr.Length - 1);
        var prefab = arr[index];
        if (prefab == null) return;

        var go = Instantiate(prefab, parent);
        go.transform.localPosition = Vector3.zero;

        var rng = CoordRng(p);
        float randomY = (float)rng.NextDouble() * 360f;
        go.transform.rotation = Quaternion.Euler(0, randomY, 0);


        // Compute correct world pos for height offset
        var pos = GridToWorld(p.x, p.y);

        var renderers = go.GetComponentsInChildren<Renderer>();
        if (renderers.Length > 0)
        {
            float minY = float.MaxValue;
            float maxY = float.MinValue;

            foreach (var r in renderers)
            {
                minY = Mathf.Min(minY, r.bounds.min.y);
                maxY = Mathf.Max(maxY, r.bounds.max.y);
            }

            float height = maxY - minY;

            // Correct ghost placement
            float sinkPercent = (pr.type == ResourceNode.ResourceType.Rock) ? 0.50f : 0f;
            go.transform.position = pos + new Vector3(0, (height * 0.5f - minY) - (height * sinkPercent), 0);

        }

        // Convert prefab to ghost
        foreach (var childRenderer in go.GetComponentsInChildren<Renderer>(true))
            if (ghostMaterial != null) childRenderer.material = ghostMaterial;

        foreach (var col in go.GetComponentsInChildren<Collider>(true))
            col.enabled = false;

        foreach (var rn in go.GetComponentsInChildren<ResourceNode>(true))
            Destroy(rn);

        foreach (var cr in go.GetComponentsInChildren<ClickableResource>(true))
            Destroy(cr);

        go.name = "[Ghost]" + go.name;
    }



    // After purchase, spawn the actual node on the owned tile and clear pending
    void SpawnOwnedResourceIfAny(int x, int y)
    {
        var key = new Vector2Int(x, y);
        if (!pending.TryGetValue(key, out var pr) || !pr.hasResource) return;

        GameObject[] arr = pr.type == ResourceNode.ResourceType.Tree ? treePrefabs : rockPrefabs;
        if (arr == null || arr.Length == 0) return;

        int index = Mathf.Clamp(pr.prefabIndex, 0, arr.Length - 1);
        var prefab = arr[index];
        if (prefab == null) return;

        var pos = GridToWorld(x, y);
        var go = Instantiate(prefab, pos, Quaternion.identity, islandRoot);

        var rng = CoordRng(key);
        float randomY = (float)rng.NextDouble() * 360f;
        go.transform.rotation = Quaternion.Euler(0, randomY, 0);


        // Raise the real resource above the tile
        var renderers = go.GetComponentsInChildren<Renderer>();
        if (renderers.Length > 0)
        {
            // Calculate lowest and highest Y of all renderers
            float minY = float.MaxValue;
            float maxY = float.MinValue;

            foreach (var r in renderers)
            {
                minY = Mathf.Min(minY, r.bounds.min.y);
                maxY = Mathf.Max(maxY, r.bounds.max.y);
            }

            float height = maxY - minY;

            // Sinking amount: rocks embed slightly into ground, trees stay above
            float sinkPercent = (pr.type == ResourceNode.ResourceType.Rock) ? 0.50f : 0f;
            go.transform.position = pos + new Vector3(0, (height * 0.5f - minY) - (height * sinkPercent), 0);

        }



        // Attach essential components
        var rn = go.GetComponent<ResourceNode>();
        if (rn == null) rn = go.AddComponent<ResourceNode>();
        rn.type = pr.type;
        rn.hitPoints = pr.hp;

        if (go.GetComponent<ClickableResource>() == null)
            go.AddComponent<ClickableResource>();

        pending.Remove(key);
    }

    public bool TryBuyTile(int x, int y)
    {
        bool adjacentToOwned =
            tiles.ContainsKey((x + 1, y)) || tiles.ContainsKey((x - 1, y)) ||
            tiles.ContainsKey((x, y + 1)) || tiles.ContainsKey((x, y - 1));
        if (!adjacentToOwned) { Debug.Log("You can only buy adjacent land!"); return false; }

        if (tiles.ContainsKey((x, y))) { Debug.Log("Tile already owned!"); return false; }

        // Purchase-based cost curve
        int totalOwned = tiles.Count;
        float baseCost = expansionCost;   // start at 5
        float growthRate = 1.035f;
        float curveStrength = 0.9f;
        float earlyDiscount = 0.9f;
        float scaledCost = baseCost * earlyDiscount * Mathf.Pow(growthRate, Mathf.Pow(totalOwned, curveStrength));
        int tileCost = Mathf.Min(Mathf.RoundToInt(scaledCost), 50000);

        if (!EconomySystem.I.SpendCoins(tileCost)) return false;

        AddTile(x, y);

        if (hoeFXPrefab != null)
            Instantiate(hoeFXPrefab, GridToWorld(x, y) + Vector3.up * 0.1f, Quaternion.identity);

        // Now place any pending resource as a real node
        SpawnOwnedResourceIfAny(x, y);

        Debug.Log($"Purchased tile {x},{y} for {tileCost} coins.");
        return true;
    }

    public List<Vector2Int> GetExpandableTiles()
    {
        var result = new List<Vector2Int>();
        foreach (var key in tiles.Keys)
        {
            var pos = new Vector2Int(key.Item1, key.Item2);
            var neighbors = new[]
            {
                new Vector2Int(pos.x + 1, pos.y),
                new Vector2Int(pos.x - 1, pos.y),
                new Vector2Int(pos.x, pos.y + 1),
                new Vector2Int(pos.x, pos.y - 1),
            };
            foreach (var n in neighbors)
            {
                if (!tiles.ContainsKey((n.x, n.y)) && !result.Contains(n))
                    result.Add(n);
            }
        }
        return result;
    }

    public void ClearPending()
    {
        pending.Clear();
    }

    public void AddOrRestoreTile(TileSaveData t)
    {
        // Create if missing
        if (!tiles.ContainsKey((t.x, t.y)))
            AddTile(t.x, t.y);

        Tile tile = tiles[(t.x, t.y)];
        tile.state = Enum.Parse<Tile.State>(t.state);

        // If a crop exists
        if (!string.IsNullOrEmpty(t.cropName))
        {
            tile.crop = carrotCrop; // for now only carrots

            // Determine correct seeded prefab
            if (tile.currentVisual != null)
                UnityEngine.Object.Destroy(tile.currentVisual);

            GameObject seededPrefab = (tile.state == Tile.State.WetSoil)
                ? seededSoilWetPrefab
                : seededSoilDryPrefab;

            tile.InitializeVisual(UnityEngine.Object.Instantiate(
                seededPrefab,
                tile.transform.position,
                UnityEngine.Quaternion.identity,
                tile.transform
            ));

            // Ensure crop anchor and growth visuals restore properly
            tile.UpdateCropAnchor();
            tile.SetGrowthStage(t.growthStage, t.randomYRotation);
        }
        else
        {
            // No crop = normal soil/grass
            tile.UpdateVisual();
        }
    }

    public List<PendingSaveData> ExportPending()
    {
        var list = new List<PendingSaveData>();
        foreach (var kvp in pending)
        {
            var p = kvp.Key;
            var v = kvp.Value;
            list.Add(new PendingSaveData
            {
                x = p.x,
                y = p.y,
                hasResource = v.hasResource,
                type = v.type.ToString(),
                hp = v.hp,
                prefabIndex = v.prefabIndex
            });
        }
        return list;
    }

    public void ImportPending(List<PendingSaveData> entries)
    {
        pending.Clear();
        if (entries == null) return;

        foreach (var e in entries)
        {
            var key = new Vector2Int(e.x, e.y);
            var pr = new PendingResource
            {
                hasResource = e.hasResource,
                hp = e.hp,
                prefabIndex = e.prefabIndex
            };

            if (Enum.TryParse(e.type, out ResourceNode.ResourceType t))
                pr.type = t;

            pending[key] = pr;
        }
    }

    public bool HasTileAtWorld(Vector3 worldPos)
    {
        int x = Mathf.RoundToInt(worldPos.x / tileSize);
        int y = Mathf.RoundToInt(worldPos.z / tileSize);
        return HasTile(x, y);
    }

}
