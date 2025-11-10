using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CraftingDetailsUI : MonoBehaviour
{
    [Header("Refs")]
    public Image icon;
    public TextMeshProUGUI nameText;
    public Transform ingredientsParent;       // Vertical Layout Group
    public GameObject ingredientRowPrefab;    // Child must have "Icon" (Image) and "Text" (TMP) children
    public Button craftButton;

    private CraftingRecipe current;

    void Start()
    {

    }

    public void Show(CraftingRecipe recipe)
    {
        current = recipe;
        Build(recipe);
    }

    public void Clear()
    {
        current = null;
        if (icon) icon.sprite = null;
        if (nameText) nameText.text = "";
        ClearIngredients();
        if (craftButton)
        {
            craftButton.interactable = false;
            craftButton.onClick.RemoveAllListeners();
        }
    }

    void Build(CraftingRecipe recipe)
    {
        if (!recipe) { Clear(); return; }

        if (icon) icon.sprite = recipe.outputItem ? recipe.outputItem.icon : null;
        if (nameText) nameText.text = recipe.outputItem ? recipe.outputItem.displayName : "(null)";

        ClearIngredients();

        foreach (var ing in recipe.ingredients)
        {
            var row = Instantiate(ingredientRowPrefab, ingredientsParent);

            var iconImg = row.transform.Find("Icon")?.GetComponent<Image>();
            var textTMP = row.transform.Find("NameText")?.GetComponent<TextMeshProUGUI>();

            if (iconImg) iconImg.sprite = ing.item ? ing.item.icon : null;

            int have = InventorySystem.I ? InventorySystem.I.CountOf(ing.item) : 0;
            int need = Mathf.Max(1, ing.amount);

            if (textTMP)
            {
                string nm = ing.item ? ing.item.displayName : "(null)";
                textTMP.text = $"{nm}: {have}/{need}";
                textTMP.color = (have >= need) ? Color.white : new Color(1f, 0.45f, 0.45f);
            }
        }

        bool canCraft = CraftingManager.I && CraftingManager.I.CanCraft(recipe);
        if (craftButton)
        {
            craftButton.interactable = canCraft;
            craftButton.onClick.RemoveAllListeners();
            craftButton.onClick.AddListener(() => OnCraftClicked());
        }
    }

    public void RefreshCounts(CraftingRecipe recipe)
    {
        if (current != recipe) return;
        Build(recipe); // simple rebuild keeps it robust
    }

    void ClearIngredients()
    {
        for (int i = ingredientsParent.childCount - 1; i >= 0; i--)
            Destroy(ingredientsParent.GetChild(i).gameObject);
    }

    void OnCraftClicked()
    {
        if (!current) return;

        if (CraftingManager.I.Craft(current))
        {
            // Refresh counts + button interactivity after crafting
            RefreshCounts(current);
        }
    }
}
