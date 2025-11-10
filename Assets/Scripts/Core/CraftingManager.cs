using UnityEngine;

public class CraftingManager : MonoBehaviour
{
    public static CraftingManager I;

    [Header("References")]
    [SerializeField] private CraftingUI craftingUI;   // <-- assign in Inspector

    void Awake()
    {
        I = this;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.C))
        {
            if (craftingUI != null)
            {
                craftingUI.Toggle();
                Debug.Log("Toggling Crafting UI on: " + craftingUI.gameObject.name);
            }
            else
            {
                Debug.LogWarning("CraftingManager has no CraftingUI reference assigned!");
            }
        }
    }

    public bool CanCraft(CraftingRecipe recipe)
    {
        if (!InventorySystem.I || !recipe || recipe.ingredients == null) return false;

        foreach (var ing in recipe.ingredients)
        {
            if (!ing.item || ing.amount <= 0) return false;
            if (InventorySystem.I.CountOf(ing.item) < ing.amount)
                return false;
        }
        return true;
    }

    public bool Craft(CraftingRecipe recipe)
    {
        if (!CanCraft(recipe))
        {
            Debug.Log("Not enough materials!");
            return false;
        }

        foreach (var ing in recipe.ingredients)
            InventorySystem.I.RemoveItem(ing.item, ing.amount);

        if (recipe.outputItem && recipe.outputAmount > 0)
            InventorySystem.I.AddItem(recipe.outputItem, recipe.outputAmount);

        Debug.Log($"Crafted {recipe.outputAmount}x {recipe.outputItem?.displayName}");
        return true;
    }
}
