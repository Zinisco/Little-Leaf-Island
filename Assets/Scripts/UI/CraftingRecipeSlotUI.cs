using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

public class CraftingRecipeSlotUI : MonoBehaviour, IPointerClickHandler
{
    [Header("UI References")]
    public TextMeshProUGUI nameText;     // recipe.outputItem.displayName
    public Image highlightBG;            // semi-transparent selection background

    public CraftingRecipe BoundRecipe { get; private set; }

    System.Action<CraftingRecipe> onClicked;

    public void SetupForList(CraftingRecipe recipe, System.Action<CraftingRecipe> onClicked, bool isSelected)
    {
        BoundRecipe = recipe;
        this.onClicked = onClicked;

        if (nameText) nameText.text = recipe.outputItem ? recipe.outputItem.displayName : "(null)";

        UpdateHighlight(isSelected);
    }

    public void UpdateHighlight(bool on)
    {
        if (highlightBG) highlightBG.enabled = on;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        onClicked?.Invoke(BoundRecipe);
    }
}
