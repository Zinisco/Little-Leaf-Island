using UnityEngine;

public class ToolManager : MonoBehaviour
{
    public static ToolManager I;

    public enum Tool { Selection, Shovel, Water, Seed }
    public Tool currentTool = Tool.Selection;

    [Header("Cursor Icons")]
    public Texture2D cursorDefault;
    public Texture2D cursorSelection;
    public Texture2D cursorShovel;
    public Texture2D cursorWater;
    public Texture2D cursorSeed;


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
        if (Input.GetKeyDown(KeyCode.Alpha1)) SetTool(Tool.Selection);
        if (Input.GetKeyDown(KeyCode.Alpha2)) SetTool(Tool.Shovel);
        if (Input.GetKeyDown(KeyCode.Alpha3)) SetTool(Tool.Water);
        if (Input.GetKeyDown(KeyCode.Alpha4)) SetTool(Tool.Seed);
    }

    void LateUpdate()
    {
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
        int toolCount = System.Enum.GetValues(typeof(Tool)).Length;
        int index = (int)currentTool;
        index = (index + (direction > 0 ? -1 : 1) + toolCount) % toolCount;

        SetTool((Tool)index);
    }

    void SetTool(Tool newTool)
    {
        currentTool = newTool;
        ApplyCursor();
    }

    void ApplyCursor()
    {
        Texture2D tex = cursorDefault;

        switch (currentTool)
        {
            case Tool.Selection: tex = cursorSelection; break;
            case Tool.Shovel: tex = cursorShovel; break;
            case Tool.Water: tex = cursorWater; break;
            case Tool.Seed: tex = cursorSeed; break;
        }

        Cursor.SetCursor(tex, cursorHotspot, CursorMode.Auto);
    }
}
