using System.Collections.Generic;
using UnityEngine;

public class CraftingUI : MonoBehaviour
{
    public static CraftingUI I;

    [Header("List Setup")]
    public GameObject recipeSlotPrefab;    // Prefab with CraftingRecipeSlotUI
    public Transform recipeListParent;     // ScrollView/Viewport/Content
    public List<CraftingRecipe> recipes;   // Drag all recipe assets here

    [Header("Details Panel")]
    public CraftingDetailsUI detailsPanel; // Reference to the always-visible details panel

    private CraftingRecipe _selected;

    void Awake() => I = this;

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

    // Called from CraftingManager (press C)
    public void Toggle()
    {
        bool next = !gameObject.activeSelf;
        gameObject.SetActive(next);

        if (next)
        {
            BuildList();
            if (_selected == null) AutoSelectFirst();
            else
            {
                RepaintSelection();
                if (detailsPanel) detailsPanel.Show(_selected); // <-- ensures ingredients appear
            }
        }
    }

}
