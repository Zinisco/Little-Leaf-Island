using UnityEngine;

public class ToolManager : MonoBehaviour
{
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

    Vector2 cursorHotspot = new Vector2(8, 8); // tweak per texture
    Vector2 grabHotspot = new Vector2(16, 16); // tweak per grab icon

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

        // RMB or MMB held = Camera grab cursor
        if (Input.GetMouseButton(1) || Input.GetMouseButton(2))
        {
            Cursor.SetCursor(cursorGrabClosed, grabHotspot, CursorMode.Auto);
            return;
        }

        ApplyCursor();
    }


    public void CycleTool(float direction)
    {
        if (ExpansionModeManager.I != null && ExpansionModeManager.I.IsActive)
            return;

        int toolCount = System.Enum.GetValues(typeof(Tool)).Length;
        int index = (int)currentTool;
        index = (index + (direction > 0 ? -1 : 1) + toolCount) % toolCount;

        SetTool((Tool)index);
    }

    void SetTool(Tool newTool)
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

        Cursor.SetCursor(tex, cursorHotspot, CursorMode.Auto);
    }

    public void ForceDefaultCursor()
    {
        Cursor.SetCursor(cursorDefault, cursorHotspot, CursorMode.Auto);
    }

}
