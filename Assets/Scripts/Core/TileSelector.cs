using UnityEngine;

public class TileSelector : MonoBehaviour
{
    public LayerMask tileLayer;
    public GameObject highlightPrefab;
    Tile hoveredTile;

    void Update()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

        if (Physics.Raycast(ray, out RaycastHit hit, 100f))
        {
            // If hovering over an Expansion Tile, clear highlight (don’t let it “stick”)
            if (hit.collider.gameObject.layer == LayerMask.NameToLayer("ExpansionTile"))
            {
                ClearHighlight();
                return;
            }

            // Only proceed if we hit something in the tile layer mask
            if (((1 << hit.collider.gameObject.layer) & tileLayer) != 0)
            {
                Tile tile = hit.collider.GetComponentInParent<Tile>();
                if (tile == null)
                    return;

                if (tile != hoveredTile)
                {
                    ClearHighlight();
                    hoveredTile = tile;
                    hoveredTile.ShowHighlight(highlightPrefab);
                }
            }
            else
            {
                ClearHighlight();
            }
        }
        else
        {
            ClearHighlight();
        }

        // Only allow tool use when not in expansion mode
        if (ExpansionModeManager.I == null || !ExpansionModeManager.I.IsActive)
        {
            if (Input.GetMouseButtonDown(0) && hoveredTile != null)
                hoveredTile.OnClicked();

            float scroll = Input.mouseScrollDelta.y;
            if (Mathf.Abs(scroll) > 0.01f && !Input.GetMouseButton(1))
                ToolManager.I.CycleTool(scroll);
        }
    }



    void ClearHighlight()
    {
        if (hoveredTile != null)
        {
            hoveredTile.HideHighlight();
            hoveredTile = null;
        }
    }
}
