using UnityEngine;

public class TileSelector : MonoBehaviour
{
    public LayerMask tileLayer;
    public GameObject highlightPrefab;
    Tile hoveredTile;

    void Update()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit, 100f, tileLayer))
        {
            Tile tile = hit.collider.GetComponentInParent<Tile>();
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

        if (Input.GetMouseButtonDown(0) && hoveredTile != null)
        {
            hoveredTile.OnClicked();
        }

        float scroll = Input.mouseScrollDelta.y;
        if (Mathf.Abs(scroll) > 0.01f && !Input.GetMouseButton(1)) // Scroll only
        {
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
