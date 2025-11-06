using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class InventoryDragController : MonoBehaviour
{
    public static InventoryDragController I;

    [Header("Prefab & Parent")]
    public GameObject dragIconPrefab;
    public Transform dragLayer; // the overlay canvas layer

    RectTransform dragRect;
    Image dragImage;
    TMP_Text dragQty;

    public int draggedFromIndex = -1;

    void Awake()
    {
        I = this;
    }

    void Update()
    {
        if (dragRect != null)
            dragRect.position = Input.mousePosition;
    }

    public void BeginDrag(Sprite icon, int quantity, int fromIndex)
    {
        if (dragIconPrefab == null || dragLayer == null) return;

        draggedFromIndex = fromIndex;

        GameObject go = Instantiate(dragIconPrefab, dragLayer);
        dragRect = go.GetComponent<RectTransform>();
        dragImage = go.GetComponentInChildren<Image>();
        dragQty = go.GetComponentInChildren<TMP_Text>();

        dragImage.sprite = icon;
        dragQty.text = quantity > 1 ? quantity.ToString() : "";
    }

    public void EndDrag()
    {
        if (dragLayer.childCount > 0)
            Destroy(dragLayer.GetChild(0).gameObject);

        dragRect = null;
        dragImage = null;
        dragQty = null;
        draggedFromIndex = -1;
    }

    public bool IsDragging => dragRect != null;
}
