using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NovaReceita", menuName = "Crafting/Receita")]
public sealed class CraftingRecipeData : ScriptableObject
{
    [Serializable]
    public sealed class Ingredient
    {
        public ItemType item;
        [Min(1)] public int quantity = 1;
    }

    [SerializeField] private string recipeName = "Nova receita";
    [SerializeField] private ItemType output;
    [Min(1), SerializeField] private int outputCount = 1;
    [SerializeField] private List<Ingredient> ingredients = new List<Ingredient>();

    public string RecipeName => string.IsNullOrWhiteSpace(recipeName) ? output.ToString() : recipeName;
    public ItemType Output => output;
    public int OutputCount => Mathf.Max(1, outputCount);
    public IReadOnlyList<Ingredient> Ingredients => ingredients;

    public Dictionary<ItemType, int> GetRequirements()
    {
        Dictionary<ItemType, int> result = new Dictionary<ItemType, int>();
        if (ingredients == null) return result;
        foreach (Ingredient ingredient in ingredients)
        {
            if (ingredient == null || ingredient.quantity < 1) continue;
            if (result.TryGetValue(ingredient.item, out int current))
                result[ingredient.item] = current + ingredient.quantity;
            else
                result.Add(ingredient.item, ingredient.quantity);
        }
        return result;
    }
}
