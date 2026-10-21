using System.IO;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class CraftingRecipeCatalogAutoSetup
{
    static CraftingRecipeCatalogAutoSetup()
    {
        EditorApplication.delayCall += EnsureDefaultCatalog;
    }

    private static void EnsureDefaultCatalog()
    {
        if (AssetDatabase.LoadAssetAtPath<CraftingRecipeCatalog>("Assets/Resources/CraftingRecipeCatalog.asset") != null)
            return;
        CraftingRecipeCatalogSetup.CreateDefaults();
    }
}

public static class CraftingRecipeCatalogSetup
{
    private const string CatalogPath = "Assets/Resources/CraftingRecipeCatalog.asset";
    private const string RecipeFolder = "Assets/Data/Crafting/Recipes";

    [MenuItem("Tools/Crafting NPC/Create editable default recipe catalog")]
    public static void CreateDefaults()
    {
        CraftingRecipeCatalog existing = AssetDatabase.LoadAssetAtPath<CraftingRecipeCatalog>(CatalogPath);
        if (existing != null)
        {
            Selection.activeObject = existing;
            Debug.Log("O catálogo já existe. Selecione-o e edite a lista Recipes no Inspector.");
            return;
        }

        Directory.CreateDirectory("Assets/Resources");
        Directory.CreateDirectory(RecipeFolder);
        CraftingRecipeCatalog catalog = ScriptableObject.CreateInstance<CraftingRecipeCatalog>();
        AssetDatabase.CreateAsset(catalog, CatalogPath);

        CraftingRecipeData[] recipes =
        {
            Make("Sopa de cogumelo", ItemType.SopaDeCogumelo, 1, (ItemType.FungoComum, 2), (ItemType.AguaPotavel, 1)),
            Make("Carne cozida", ItemType.CarneCozida, 1, (ItemType.CarneCrua, 1), (ItemType.Wood, 1)),
            Make("Substancia calmante", ItemType.SubistanciaCalmante, 1, (ItemType.FungoLunaSonos, 1), (ItemType.Frasco, 1)),
            Make("Comunicador", ItemType.Comunicador, 1, (ItemType.Componentes, 2), (ItemType.FiosDeCobre, 1), (ItemType.Battery, 1)),
            Make("Garrafa termica", ItemType.GarrafaTermica, 1, (ItemType.Vidro, 2), (ItemType.Tecido, 1), (ItemType.Fio, 1)),
            Make("Cilindro de oxigenio", ItemType.CilindroDeOxigenio, 1, (ItemType.Ferro, 2), (ItemType.Componentes, 1), (ItemType.FiosDeCobre, 1))
        };

        SerializedObject serializedCatalog = new SerializedObject(catalog);
        SerializedProperty recipeList = serializedCatalog.FindProperty("recipes");
        recipeList.arraySize = recipes.Length;
        for (int i = 0; i < recipes.Length; i++)
            recipeList.GetArrayElementAtIndex(i).objectReferenceValue = recipes[i];
        serializedCatalog.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(catalog);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Selection.activeObject = catalog;
        Debug.Log("Catálogo editável criado. Edite as receitas no Inspector; o NPC o carrega automaticamente em execução.");
    }

    private static CraftingRecipeData Make(string recipeName, ItemType output, int outputCount,
        params (ItemType item, int quantity)[] ingredients)
    {
        string assetName = MakeSafeName(recipeName);
        CraftingRecipeData data = ScriptableObject.CreateInstance<CraftingRecipeData>();
        SerializedObject serialized = new SerializedObject(data);
        serialized.FindProperty("recipeName").stringValue = recipeName;
        serialized.FindProperty("output").enumValueIndex = (int)output;
        serialized.FindProperty("outputCount").intValue = outputCount;
        SerializedProperty list = serialized.FindProperty("ingredients");
        list.arraySize = ingredients.Length;
        for (int i = 0; i < ingredients.Length; i++)
        {
            SerializedProperty element = list.GetArrayElementAtIndex(i);
            element.FindPropertyRelative("item").enumValueIndex = (int)ingredients[i].item;
            element.FindPropertyRelative("quantity").intValue = ingredients[i].quantity;
        }
        serialized.ApplyModifiedPropertiesWithoutUndo();
        string path = AssetDatabase.GenerateUniqueAssetPath(RecipeFolder + "/" + assetName + ".asset");
        AssetDatabase.CreateAsset(data, path);
        return data;
    }

    private static string MakeSafeName(string value)
    {
        foreach (char invalid in Path.GetInvalidFileNameChars()) value = value.Replace(invalid, '_');
        return value.Replace(' ', '_');
    }
}
