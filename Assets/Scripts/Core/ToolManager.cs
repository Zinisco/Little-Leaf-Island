using UnityEngine;

public class ToolManager : MonoBehaviour
{
    [System.Serializable]
    public class CursorData
    {
        public Texture2D texture;
        public Vector2 hotspot;
    }

    public static ToolManager I;

    public enum Tool { Selection, Shovel, Water, Seed, Axe, Pickaxe }

    public Tool currentTool = Tool.Selection;

    [Header("Cursor Icons")]
    public Texture2D cursorDefault;
    public Texture2D cursorSelection;
    public Texture2D cursorShovel;
    public Texture2D cursorWater;
    public Texture2D cursorSeed;
    public Texture2D cursorAxe;
    public Texture2D cursorPickaxe;

    [Header("Camera Grab Cursor")]
    public Texture2D cursorGrabClosed;

    // Optional fallbacks if no marker found
    [SerializeField] Vector2 fallbackHotspot = Vector2.zero;
    [SerializeField] Vector2 grabFallbackHotspot = new Vector2(16, 16);

    void Awake()
    {
        I = this;
        ApplyCursor();
    }

    void Update()
    {
        // Prevent tool switching while in expansion mode
        if (ExpansionModeManager.I != null && ExpansionModeManager.I.IsActive)
            return;

        // Stop tool switching when inventory is open
        if (InventoryUIController.IsOpen)
            return;

        if (Input.GetKeyDown(KeyCode.Alpha1)) SetTool(Tool.Selection);
        if (Input.GetKeyDown(KeyCode.Alpha2)) SetTool(Tool.Shovel);
        if (Input.GetKeyDown(KeyCode.Alpha3)) SetTool(Tool.Water);
        if (Input.GetKeyDown(KeyCode.Alpha4)) SetTool(Tool.Seed);
        if (Input.GetKeyDown(KeyCode.Alpha5)) SetTool(Tool.Axe);
        if (Input.GetKeyDown(KeyCode.Alpha6)) SetTool(Tool.Pickaxe);
    }

    void LateUpdate()
    {
        // If inventory is open, do NOT change the cursor at all
        if (InventoryUIController.IsOpen)
            return;

        // Prevent cursor switching in expansion mode
        if (ExpansionModeManager.I != null && ExpansionModeManager.I.IsActive)
            return;

        if (PlaceableItemPlacer.I && PlaceableItemPlacer.I.IsPlacing)
            return;

        // RMB or MMB held = Camera grab cursor
        if (Input.GetMouseButton(1) || Input.GetMouseButton(2))
        {
            // Try to detect hotspot on grab icon; fall back if no marker
            Vector2 hotspot = TryDetectHotspot(cursorGrabClosed, grabFallbackHotspot);
            Cursor.SetCursor(cursorGrabClosed, hotspot, CursorMode.Auto);
            return;
        }

        ApplyCursor();
    }

    // -----------------------------
    // Public API
    // -----------------------------
    public void CycleTool(float direction)
    {
        if (ExpansionModeManager.I != null && ExpansionModeManager.I.IsActive)
            return;

        if(CraftingUI.I != null && CraftingUI.IsOpen)
            return;

        if (PlaceableItemPlacer.I && PlaceableItemPlacer.I.IsPlacing)
            return;

        int toolCount = System.Enum.GetValues(typeof(Tool)).Length;
        int index = (int)currentTool;
        index = (index + (direction > 0 ? -1 : 1) + toolCount) % toolCount;

        SetTool((Tool)index);
    }

    public void SetTool(Tool newTool)
    {
        currentTool = newTool;
        if (!InventoryUIController.IsOpen)
            ApplyCursor();
    }

    public void ApplyCursor()
    {
        Texture2D tex = cursorDefault;

        switch (currentTool)
        {
            case Tool.Selection: tex = cursorSelection; break;
            case Tool.Shovel: tex = cursorShovel; break;
            case Tool.Water: tex = cursorWater; break;
            case Tool.Seed: tex = cursorSeed; break;
            case Tool.Axe: tex = cursorAxe; break;
            case Tool.Pickaxe: tex = cursorPickaxe; break;
        }

        Vector2 hotspot = TryDetectHotspot(tex, fallbackHotspot);
        Cursor.SetCursor(tex, hotspot, CursorMode.Auto);
    }

    public void ForceDefaultCursor()
    {
        Vector2 hotspot = TryDetectHotspot(cursorDefault, fallbackHotspot);
        Cursor.SetCursor(cursorDefault, hotspot, CursorMode.Auto);
    }

    // -----------------------------
    // Hotspot detection
    // -----------------------------

    // Tries to detect a single pure-white (255,255,255) pixel and converts to Unity hotspot coords.
    // Falls back to provided default if none found or texture unreadable.
    Vector2 TryDetectHotspot(Texture2D tex, Vector2 fallback)
    {
        if (tex == null) return fallback;

        // If not readable, we can't scan pixels; return fallback.
        if (!tex.isReadable)
            return fallback;

        // Scan for the first pure white marker.
        // We’ll scan left->right, top->bottom so artists can place a known "tip" near top-left if desired.
        // Note: GetPixels32() returns pixels with origin at bottom-left, so we’ll convert properly below.
        Color32[] pixels;
        try
        {
            pixels = tex.GetPixels32();
        }
        catch
        {
            return fallback;
        }

        int w = tex.width;
        int h = tex.height;

        // Iterate in "top-left to bottom-right" visual order:
        // visualY = top row (h-1) down to 0; x = 0..w-1
        for (int visualY = h - 1; visualY >= 0; visualY--)
        {
            for (int x = 0; x < w; x++)
            {
                // Convert visual coords to array index (array origin is bottom-left)
                int arrayY = visualY; // because pixels[] origin is bottom-left (y=0), visualY already matches that base index
                int idx = arrayY * w + x;

                Color32 c = pixels[idx];
                if (c.r == 255 && c.g == 255 && c.b == 255 && c.a > 0)
                {
                    // Convert pixel-space (origin bottom-left) to Unity hotspot (origin TOP-left)
                    // Unity expects (0,0) = TOP-left
                    int unityY = (h - 1) - arrayY;

                    return new Vector2(x, unityY);
                }
            }
        }

        // No marker found
        return fallback;
    }
}
