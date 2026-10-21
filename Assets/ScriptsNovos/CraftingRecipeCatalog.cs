using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "CatalogoDeReceitas", menuName = "Crafting/Catálogo de receitas")]
public sealed class CraftingRecipeCatalog : ScriptableObject
{
    [SerializeField] private List<CraftingRecipeData> recipes = new List<CraftingRecipeData>();
    public IReadOnlyList<CraftingRecipeData> Recipes => recipes;
}
