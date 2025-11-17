using UnityEngine;

public class PlaceableItemPlacer : MonoBehaviour
{
    [Header("Settings")]
    public LayerMask groundMask;
    public float maxPlaceDistance = 100f;
    public float rotationSpeed = 90f;

    private GameObject ghostInstance;
    private ItemDefinition currentItem;
    private bool isPlacing = false;
    private float currentRotationY = 0f;
    private float cachedBottomOffset = 0f;


    public static PlaceableItemPlacer I;


    void Awake() => I = this;

    void Update()
    {
        if (!isPlacing) return;

        HandlePlacementPreview();
        HandleRotation();
        HandleClickInputs();

        Debug.DrawRay(Camera.main.transform.position, Camera.main.transform.forward * maxPlaceDistance, Color.red);
    }

    void HandlePlacementPreview()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

        if (!Physics.Raycast(ray, out RaycastHit hit, maxPlaceDistance, groundMask))
            return;

        Tile tile = hit.collider.GetComponent<Tile>() ?? hit.collider.GetComponentInParent<Tile>();
        if (!tile || !tile.placementAnchor) return;

        Vector3 pos = tile.placementAnchor.position;

        // Apply precomputed bottom offset
        pos.y += cachedBottomOffset;

        // Apply final position and rotation
        ghostInstance.transform.position = pos;
        ghostInstance.transform.rotation = Quaternion.Euler(0f, currentRotationY, 0f);
    }





    void HandleRotation()
    {
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(scroll) > 0.01f)
        {
            currentRotationY += scroll * rotationSpeed;
            if (ghostInstance)
                ghostInstance.transform.rotation = Quaternion.Euler(0f, currentRotationY, 0f);
        }
    }

    void HandleClickInputs()
    {
        if (Input.GetMouseButtonDown(0))
            TryPlace();

        if (Input.GetMouseButtonDown(1))
            CancelPlacement();
    }

    void TryPlace()
    {
        if (ghostInstance == null || currentItem == null) return;

        Vector3 placePos = ghostInstance.transform.position;

        // Spawn real object
        GameObject real = Instantiate(currentItem.placeablePrefab, placePos, ghostInstance.transform.rotation);

        InventorySystem.I.RemoveItem(currentItem, 1);
        CancelPlacement();
    }


    public void StartPlacing(ItemDefinition item)
    {
        if (item == null || !item.isPlaceable || item.placeablePrefab == null)
            return;

        currentItem = item;
        isPlacing = true;
        currentRotationY = 0f;

        ghostInstance = Instantiate(item.placeablePrefab);
        foreach (var c in ghostInstance.GetComponentsInChildren<Collider>())
            c.enabled = false;

        // ---- CACHE BOTTOM OFFSET ----
        Renderer[] rends = ghostInstance.GetComponentsInChildren<Renderer>();
        float minLocalY = float.MaxValue;

        foreach (var r in rends)
        {
            // Convert world min to local space once
            Vector3 localMin = ghostInstance.transform.InverseTransformPoint(r.bounds.min);
            minLocalY = Mathf.Min(minLocalY, localMin.y);
        }

        // Store offset (absolute) to bring bottom to y=0
        cachedBottomOffset = -minLocalY + currentItem.placementYOffset;
    }


    void CancelPlacement()
    {
        isPlacing = false;
        if (ghostInstance != null)
            Destroy(ghostInstance);
        ghostInstance = null;
        currentItem = null;
    }
}
