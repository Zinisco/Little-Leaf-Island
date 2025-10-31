using UnityEngine;
using System.Collections.Generic;

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
}
