using UnityEngine;

[CreateAssetMenu(menuName = "LLI/Crafting Recipe")]
public class CraftingRecipe : ScriptableObject
{
    [Header("Output")]
    public ItemDefinition outputItem;
    public int outputAmount = 1;

    [Header("Ingredients")]
    public Ingredient[] ingredients;

    [System.Serializable]
    public struct Ingredient
    {
        public ItemDefinition item;
        public int amount;
    }
}
