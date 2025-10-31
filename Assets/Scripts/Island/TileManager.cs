using System;
using System.Collections.Generic;
using UnityEngine;

public class TileManager : MonoBehaviour
{
    public static TileManager I;

    [Header("Tile Settings")]
    public float tileSize = 2f;
    public GameObject grassPrefab;
    public GameObject soilPrefab;
    public GameObject wetSoilPrefab;
    public GameObject seededSoilDryPrefab;
    public GameObject seededSoilWetPrefab;

    [Header("Expansion Settings")]
    public int expansionCost = 50; // configurable cost per tile

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

    void Awake()
    {
        I = this;
    }

    void Start()
    {
        // Create a clean 3x3 starting island
        for (int x = -1; x <= 1; x++)
        {
            for (int y = -1; y <= 1; y++)
            {
                AddTile(x, y);
            }
        }
    }

    public void AddTile(int x, int y)
    {
        if (tiles.ContainsKey((x, y)))
            return;

        Vector3 pos = GridToWorld(x, y);

        // Create tile logic parent
        GameObject tileGO = new GameObject($"Tile_{x}_{y}");
        tileGO.transform.position = pos;
        tileGO.transform.parent = islandRoot;

        Tile tile = tileGO.AddComponent<Tile>();
        tile.x = x;
        tile.y = y;
        tile.state = Tile.State.Grass;

        // Spawn visual as child
        tile.InitializeVisual(
            Instantiate(grassPrefab, pos, Quaternion.identity, tileGO.transform)
        );

        tiles.Add((x, y), tile);
    }


    Vector3 GridToWorld(int x, int y)
    {
        return new Vector3(x * tileSize, 0f, y * tileSize);
    }

    public bool HasTile(int x, int y)
    {
        return tiles.ContainsKey((x, y));
    }

    public bool TryBuyTile(int x, int y)
    {
        // Make sure it's adjacent to existing land
        bool adjacentToOwned =
            tiles.ContainsKey((x + 1, y)) || tiles.ContainsKey((x - 1, y)) ||
            tiles.ContainsKey((x, y + 1)) || tiles.ContainsKey((x, y - 1));

        if (!adjacentToOwned)
        {
            Debug.Log("You can only buy land adjacent to your farm!");
            return false;
        }

        // Check if already owned
        if (tiles.ContainsKey((x, y)))
        {
            Debug.Log("Tile already owned!");
            return false;
        }

        // Spend coins
        if (!EconomySystem.I.SpendCoins(expansionCost))
            return false;

        AddTile(x, y);

        // Play a little FX for feedback
        if (hoeFXPrefab != null)
            Instantiate(hoeFXPrefab, GridToWorld(x, y) + Vector3.up * 0.1f, Quaternion.identity);

        Debug.Log($"Purchased tile at {x},{y}");
        return true;
    }

    public List<Vector2Int> GetExpandableTiles()
    {
        List<Vector2Int> result = new();

        foreach (var key in tiles.Keys)
        {
            Vector2Int pos = new(key.Item1, key.Item2);
            Vector2Int[] neighbors = {
            new(pos.x + 1, pos.y),
            new(pos.x - 1, pos.y),
            new(pos.x, pos.y + 1),
            new(pos.x, pos.y - 1)
        };

            foreach (var n in neighbors)
            {
                if (!tiles.ContainsKey((n.x, n.y)) && !result.Contains(n))
                    result.Add(n);
            }
        }

        return result;
    }


    public Dictionary<(int, int), Tile> GetAllTiles() => tiles;

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


}
