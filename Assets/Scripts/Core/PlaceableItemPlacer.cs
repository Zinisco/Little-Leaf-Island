using UnityEngine;
using UnityEngine.UI;

public class PlaceableItemPlacer : MonoBehaviour
{
    [Header("Settings")]
    public LayerMask groundMask;
    public float maxPlaceDistance = 100f;
    public float rotationSpeed = 90f;

    [Header("Smooth Rotation")]
    public float rotationSmoothSpeed = 6f;

    public Image pickUpProgressRing;

    private GameObject ghostInstance;
    private ItemDefinition currentItem;
    private bool isPlacing = false;
    private float currentRotationY = 0f;
    private float cachedBottomOffset = 0f;

    private float targetRotationY = 0f;
    private float lastPlacementTime = 0f;
    private float reselectCooldown = 1.0f;
    private float pickUpHoldTime = 0.4f;
    private float pickUpHoldTimer = 0f;
    private GameObject hoveredPlaceable;
    private bool justPlacedOrCancelled = false;


    public static PlaceableItemPlacer I;


    void Awake() => I = this;

    void Update()
    {
        if (!isPlacing)
        {
            HandleHoldToPick();
        }

        if (!isPlacing) return;

        if (pickUpProgressRing && pickUpProgressRing.gameObject.activeSelf)
        {
            pickUpProgressRing.transform.position = Input.mousePosition;
        }

        HandlePlacementPreview();
        HandleRotation();
        HandleClickInputs();
    }


    void TrySelectPlacedItem()
    {
        // Cooldown: prevent re-picking up immediately after placing
        if (Time.time - lastPlacementTime < reselectCooldown)
            return;

        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

        if (Physics.Raycast(ray, out RaycastHit hit, maxPlaceDistance))
        {
            PlaceableInfo info = hit.collider.GetComponentInParent<PlaceableInfo>();
            if (info != null)
            {
                GameObject selected = info.gameObject;

                // Save info
                currentItem = info.definition;
                currentRotationY = selected.transform.rotation.eulerAngles.y;
                targetRotationY = currentRotationY;

                // Save world position and destroy original
                Vector3 prevPos = selected.transform.position;
                Destroy(selected);

                // Enter placement mode
                StartPlacing(currentItem);
                ghostInstance.transform.position = prevPos;  // optional

                Debug.Log("Repositioning started: " + currentItem.name);
            }
        }
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

        if (scroll > 0.01f)      // scroll up
            targetRotationY += 90f;
        else if (scroll < -0.01f) // scroll down
            targetRotationY -= 90f;

        // Optional: normalize to keep the value between 0–360 (cleaner debugging, same behavior)
        targetRotationY = Mathf.Repeat(targetRotationY, 360f);

        // Smooth out the rotation
        currentRotationY = Mathf.LerpAngle(currentRotationY, targetRotationY, Time.deltaTime * rotationSmoothSpeed);

        if (ghostInstance)
            ghostInstance.transform.rotation = Quaternion.Euler(0f, currentRotationY, 0f);
    }

    void HandleClickInputs()
    {
        // Disallow placing while rotating
        if (!IsRotationComplete())
            return;

        if (Input.GetMouseButtonDown(0))
            TryPlace();

        if (Input.GetMouseButtonDown(1))
            CancelPlacement();
    }


    void TryPlace()
    {
        if (ghostInstance == null || currentItem == null) return;

        Vector3 placePos = ghostInstance.transform.position;

        GameObject real = Instantiate(currentItem.placeablePrefab, placePos, ghostInstance.transform.rotation);

        lastPlacementTime = Time.time;

        InventorySystem.I.RemoveItem(currentItem, 1);
        CancelPlacement();

        justPlacedOrCancelled = true; // prevent UI flicker
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

        // ---- Hide progress ring when starting placement ----
        if (pickUpProgressRing != null)
        {
            pickUpProgressRing.gameObject.SetActive(false);
            pickUpProgressRing.fillAmount = 0f;
        }

    }

    void CancelPlacement()
    {
        isPlacing = false;
        if (ghostInstance != null)
            Destroy(ghostInstance);
        ghostInstance = null;
        currentItem = null;

        ResetPickUpUI(); // scoped UI clean
        justPlacedOrCancelled = true; // prevent UI flicker
    }

    bool IsRotationComplete()
    {
        // Avoid wrapping issues with angles >360 or negative
        float angleDiff = Mathf.Abs(Mathf.DeltaAngle(currentRotationY, targetRotationY));
        return angleDiff < 0.1f; // threshold for 'settled' rotation
    }

    void CacheBottomOffset(GameObject obj)
    {
        Renderer[] rends = obj.GetComponentsInChildren<Renderer>();
        float minLocalY = float.MaxValue;

        foreach (var r in rends)
        {
            Vector3 localMin = obj.transform.InverseTransformPoint(r.bounds.min);
            minLocalY = Mathf.Min(minLocalY, localMin.y);
        }

        cachedBottomOffset = -minLocalY + currentItem.placementYOffset;
    }
    void HandleHoldToPick()
    {
        if (isPlacing) return; // Safety: ignore hold logic during placement

        if (isPlacing || justPlacedOrCancelled) return; // latch prevents UI flashing

        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

        if (Physics.Raycast(ray, out RaycastHit hit, maxPlaceDistance))
        {
            PlaceableInfo info = hit.collider.GetComponentInParent<PlaceableInfo>();

            if (Input.GetMouseButton(0) && info != null)
            {
                hoveredPlaceable = info.gameObject;
                pickUpHoldTimer += Time.deltaTime;

                // Only show ring if NOT placing
                if (!isPlacing && pickUpProgressRing != null)
                {
                    pickUpProgressRing.gameObject.SetActive(true);
                    pickUpProgressRing.fillAmount = Mathf.Clamp01(pickUpHoldTimer / pickUpHoldTime);
                }

                if (pickUpHoldTimer >= pickUpHoldTime)
                {
                    // Trigger pick-up
                    currentItem = info.definition;
                    currentRotationY = hoveredPlaceable.transform.rotation.eulerAngles.y;
                    targetRotationY = currentRotationY;

                    Vector3 prevPos = hoveredPlaceable.transform.position;
                    Destroy(hoveredPlaceable);

                    // Hide progress ring before starting placement
                    ResetPickUpUI();

                    StartPlacing(currentItem);
                    ghostInstance.transform.position = prevPos;

                    pickUpHoldTimer = 0f;
                    hoveredPlaceable = null;
                }
            }
            else
            {
                ResetPickUpUI();
            }
        }
        else
        {
            ResetPickUpUI();
        }
    }


    void ResetPickUpUI()
    {
        pickUpHoldTimer = 0f;
        hoveredPlaceable = null;

        if (pickUpProgressRing != null)
        {
            pickUpProgressRing.gameObject.SetActive(false);
            pickUpProgressRing.fillAmount = 0f;
        }
    }

    void LateUpdate()
    {
        if (justPlacedOrCancelled)
            justPlacedOrCancelled = false; // ensure it stays active for only one frame
    }


    public bool IsPlacing => isPlacing;

}
