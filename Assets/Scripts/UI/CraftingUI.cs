using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.Rendering.DebugUI;

public class CraftingUI : MonoBehaviour
{
    public static CraftingUI I;

    public static bool IsOpen { get; private set; }

    [SerializeField] private GameObject panel; // your crafting UI root

    [Header("List Setup")]
    public GameObject recipeSlotPrefab;    // Prefab with CraftingRecipeSlotUI
    public Transform recipeListParent;     // ScrollView/Viewport/Content
    public List<CraftingRecipe> recipes;   // Drag all recipe assets here

    [Header("Details Panel")]
    public CraftingDetailsUI detailsPanel; // Reference to the always-visible details panel

    private CraftingRecipe _selected;

    void Awake() => I = this;

    void Update()
    {
        // Close crafting with Escape key
        if (IsOpen && Input.GetKeyDown(KeyCode.Escape))
            Toggle();
    }

    void OnEnable()
    {
        BuildList();
        AutoSelectFirst();
        InventorySystem.OnInventoryChanged += HandleInventoryChanged;
    }

    void OnDisable()
    {
        InventorySystem.OnInventoryChanged -= HandleInventoryChanged;
    }

    void HandleInventoryChanged()
    {
        // Refresh details counts/live interactivity
        if (detailsPanel && _selected) detailsPanel.RefreshCounts(_selected);

        // Optional: keep rows’ highlight/interactable states fresh—cheap rebuild
        RepaintSelection();
    }

    void BuildList()
    {
        foreach (Transform c in recipeListParent) Destroy(c.gameObject);

        foreach (var r in recipes)
        {
            var go = Instantiate(recipeSlotPrefab, recipeListParent);
            var slot = go.GetComponent<CraftingRecipeSlotUI>();
            slot.SetupForList(r, OnRowClicked, IsSelected(r));
        }
    }

    void AutoSelectFirst()
    {
        if (recipes == null || recipes.Count == 0) { _selected = null; if (detailsPanel) detailsPanel.Clear(); return; }
        _selected = recipes[0];
        RepaintSelection();
        if (detailsPanel) detailsPanel.Show(_selected);
    }

    void OnRowClicked(CraftingRecipe r)
    {
        _selected = r;
        RepaintSelection();
        if (detailsPanel) detailsPanel.Show(_selected);
    }

    void RepaintSelection()
    {
        // Iterate children and update highlight state
        foreach (Transform c in recipeListParent)
        {
            var slot = c.GetComponent<CraftingRecipeSlotUI>();
            if (slot && slot.BoundRecipe != null)
                slot.UpdateHighlight(IsSelected(slot.BoundRecipe));
        }
    }

    bool IsSelected(CraftingRecipe r) => _selected == r;

    public void Toggle()
    {
        bool open = !IsOpen;
        panel.SetActive(open);
        IsOpen = open;

        if (TimeManager.I != null)
            TimeManager.I.SetPaused(open);

        if (open)
        {
            // Prevent expansion mode toggle while crafting
            if (ExpansionModeManager.I != null && ExpansionModeManager.I.IsActive)
                Debug.Log("Crafting opened while expansion active (still allowed, just blocks toggle key).");

            ToolManager.I.ForceDefaultCursor();
        }
        else if (!ExpansionModeManager.I || !ExpansionModeManager.I.IsActive)
        {
            ToolManager.I.ApplyCursor();
        }
    }

}
