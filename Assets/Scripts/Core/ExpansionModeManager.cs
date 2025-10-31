using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ExpansionModeManager : MonoBehaviour
{
    public static ExpansionModeManager I;

    [Header("Expansion Visuals")]
    public GameObject expansionHighlightPrefab; // glowing ring / outline prefab
    public GameObject tileHighlightPrefab; // same one used by TileSelector

    [Header("Settings")]
    public KeyCode toggleKey = KeyCode.Tab;

    [Header("UI")]
    public GameObject expansionBannerUI; // assign a UI panel or TMP label

    private bool isActive = false;
    private Dictionary<Vector2Int, GameObject> highlights = new();
    private GameObject hoverHighlightInstance;
    private Vector2Int? currentHoveredSpot = null;

    Camera mainCam;

    public bool IsActive => isActive; // publicly readable flag

    void Awake()
    {
        I = this;
    }

    void Start()
    {
        mainCam = Camera.main;
        if (expansionBannerUI != null)
            expansionBannerUI.SetActive(false);
    }

    void Update()
    {
        if (Input.GetKeyDown(toggleKey))
            ToggleExpansionMode();

        if (!isActive) return;

        HandleHoverHighlight();
        HandleTileClick();
    }

    void ToggleExpansionMode()
    {
        isActive = !isActive;

        if (isActive)
        {
            ShowAllHighlights();
            if (expansionBannerUI != null)
                expansionBannerUI.SetActive(true);

            Debug.Log("Entered expansion mode!");
        }
        else
        {
            ClearHighlights();
            ClearHoverHighlight();
            if (expansionBannerUI != null)
                expansionBannerUI.SetActive(false);

            Debug.Log("Exited expansion mode!");
        }
    }

    void ShowAllHighlights()
    {
        ClearHighlights();

        // Get both owned and expandable tiles
        List<Vector2Int> expandableSpots = TileManager.I.GetExpandableTiles();

        // Owned tiles
        foreach (var owned in TileManager.I.GetAllTiles().Keys)
        {
            Vector3 pos = new Vector3(owned.Item1 * TileManager.I.tileSize, 0f, owned.Item2 * TileManager.I.tileSize);
            GameObject highlight = Instantiate(expansionHighlightPrefab, pos, Quaternion.identity);
            highlight.transform.SetParent(TileManager.I.islandRoot);
            highlights[new Vector2Int(owned.Item1, owned.Item2)] = highlight;
        }

        // Expansion tiles
        foreach (var spot in expandableSpots)
        {
            Vector3 pos = new Vector3(spot.x * TileManager.I.tileSize, 0f, spot.y * TileManager.I.tileSize);
            GameObject highlight = Instantiate(expansionHighlightPrefab, pos, Quaternion.identity);
            highlight.transform.SetParent(TileManager.I.islandRoot);
            highlights[spot] = highlight;
        }
    }

    void HandleHoverHighlight()
    {
        Ray ray = mainCam.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit, 100f))
        {
            Vector3 worldPos = hit.point;
            int x = Mathf.RoundToInt(worldPos.x / TileManager.I.tileSize);
            int y = Mathf.RoundToInt(worldPos.z / TileManager.I.tileSize);
            Vector2Int target = new(x, y);

            // Check if this is an expansion tile spot
            if (highlights.ContainsKey(target) && !TileManager.I.HasTile(x, y))
            {
                // Show highlight if not already there
                if (currentHoveredSpot == null || currentHoveredSpot.Value != target)
                {
                    ClearHoverHighlight();
                    currentHoveredSpot = target;

                    // find the expansion tile’s mesh height
                    GameObject expansionTile = highlights[target];
                    MeshRenderer mr = expansionTile.GetComponentInChildren<MeshRenderer>();
                    float topY = mr != null ? mr.bounds.max.y : 0f;

                    // spawn the visual highlight (the same ring from TileSelector)
                    Vector3 pos = new Vector3(
                        x * TileManager.I.tileSize,
                        topY + 0.02f,
                        y * TileManager.I.tileSize
                    );

                    hoverHighlightInstance = Instantiate(tileHighlightPrefab, pos, Quaternion.identity);
                    hoverHighlightInstance.transform.SetParent(expansionTile.transform);
                }
            }
            else
            {
                ClearHoverHighlight();
            }
        }
        else
        {
            ClearHoverHighlight();
        }
    }


    void ClearHoverHighlight()
    {
        if (hoverHighlightInstance != null)
        {
            Destroy(hoverHighlightInstance);
            hoverHighlightInstance = null;
        }
        currentHoveredSpot = null;
    }

    void HandleTileClick()
    {
        if (!Input.GetMouseButtonDown(0)) return;

        Ray ray = mainCam.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit, 100f))
        {
            Vector3 worldPos = hit.point;
            int x = Mathf.RoundToInt(worldPos.x / TileManager.I.tileSize);
            int y = Mathf.RoundToInt(worldPos.z / TileManager.I.tileSize);

            Vector2Int target = new(x, y);

            // If it’s one of the expansion tiles, allow purchase
            List<Vector2Int> expandableSpots = TileManager.I.GetExpandableTiles();
            if (expandableSpots.Contains(target))
            {
                bool success = TileManager.I.TryBuyTile(x, y);
                if (success)
                {
                    Destroy(highlights[target]);
                    highlights.Remove(target);
                    ShowAllHighlights();
                }
            }
        }
    }

    void ClearHighlights()
    {
        foreach (var kvp in highlights)
        {
            if (kvp.Value != null)
                Destroy(kvp.Value);
        }
        highlights.Clear();
    }
}
