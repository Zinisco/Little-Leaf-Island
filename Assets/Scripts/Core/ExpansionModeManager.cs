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

        // Get list of all potential expansion spots
        List<Vector2Int> expandableSpots = TileManager.I.GetExpandableTiles();

        foreach (var spot in expandableSpots)
        {
            Vector3 pos = new Vector3(
                spot.x * TileManager.I.tileSize,
                0f,
                spot.y * TileManager.I.tileSize
            );

            // Spawn highlight for expansion tiles only
            GameObject highlight = Instantiate(expansionHighlightPrefab, pos, Quaternion.identity, TileManager.I.islandRoot);

            // Hide cost UI until hover
            var costCanvas = highlight.transform.Find("CostCanvas");
            if (costCanvas != null)
                costCanvas.gameObject.SetActive(false);

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

            if (highlights.ContainsKey(target) && !TileManager.I.HasTile(x, y))
            {
                if (currentHoveredSpot == null || currentHoveredSpot.Value != target)
                {
                    ClearHoverHighlight();
                    currentHoveredSpot = target;

                    GameObject expansionTile = highlights[target];

                    // Highlight ring
                    MeshRenderer mr = expansionTile.GetComponentInChildren<MeshRenderer>();
                    float topY = mr != null ? mr.bounds.max.y : 0f;
                    Vector3 pos = new Vector3(x * TileManager.I.tileSize, topY + 0.02f, y * TileManager.I.tileSize);

                    hoverHighlightInstance = Instantiate(tileHighlightPrefab, pos, Quaternion.identity, expansionTile.transform);

                    var costCanvas = expansionTile.transform.Find("CostCanvas");
                    if (costCanvas != null)
                    {
                        int distance = Mathf.RoundToInt(Vector2Int.Distance(Vector2Int.zero, target));
                        int tileCost = Mathf.RoundToInt(TileManager.I.expansionCost * Mathf.Pow(1.2f, distance));

                        // Look specifically for the CostLabel child
                        var costLabel = costCanvas.transform.Find("CostLabel");
                        if (costLabel != null)
                        {
                            var text = costLabel.GetComponent<TMPro.TextMeshProUGUI>();
                            if (text != null)
                            {
                                text.text = $"{tileCost}";

                                if (EconomySystem.I.Coins >= tileCost)
                                {
                                    text.color = Color.black; // readable on bright ground
                                }
                                else
                                {
                                    text.color = new Color(0.3f, 0f, 0f); // dark red (less harsh, still readable)
                                }

                            }
                        }

                        costCanvas.gameObject.SetActive(true);
                    }
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

        if (currentHoveredSpot != null && highlights.ContainsKey(currentHoveredSpot.Value))
        {
            var expansionTile = highlights[currentHoveredSpot.Value];
            var costCanvas = expansionTile.transform.Find("CostCanvas");
            if (costCanvas != null)
                costCanvas.gameObject.SetActive(false);
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
