using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

[InitializeOnLoad]
public static class CraftingNpcUIPrefabBuilder
{
    private const string PrefabPath = "Assets/Resources/UI/CraftingNpcUI.prefab";

    static CraftingNpcUIPrefabBuilder()
    {
        EditorApplication.delayCall += EnsurePrefabExists;
    }

    [MenuItem("Tools/Crafting NPC/Create UI Prefab (if missing)")]
    public static void EnsurePrefabExists()
    {
        if (File.Exists(PrefabPath))
        {
            EnsureRecipeScrollStructure();
            return;
        }

        Directory.CreateDirectory(Path.GetDirectoryName(PrefabPath));
        GameObject root = CreateCanvas();
        try
        {
            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            AssetDatabase.SaveAssets();
            Debug.Log("Prefab de UI do NPC criado em " + PrefabPath + ". Ele pode ser editado normalmente na janela Prefab.");
        }
        finally
        {
            Object.DestroyImmediate(root);
        }
        EnsureRecipeScrollStructure();
    }

    private static void EnsureRecipeScrollStructure()
    {
        GameObject root = PrefabUtility.LoadPrefabContents(PrefabPath);
        try
        {
            Transform recipes = root.transform.Find("Panel/Screens/RecipesPanel");
            if (recipes == null) return;
            Transform viewport = recipes.Find("RecipeViewport");
            if (viewport == null)
            {
                GameObject viewportObject = new GameObject("RecipeViewport", typeof(RectTransform), typeof(RectMask2D));
                viewport = viewportObject.transform;
                viewport.SetParent(recipes, false);
                RectTransform rect = viewportObject.GetComponent<RectTransform>();
                rect.anchorMin = new Vector2(0.5f, 1f); rect.anchorMax = new Vector2(0.5f, 1f);
                rect.pivot = new Vector2(0.5f, 1f); rect.anchoredPosition = new Vector2(0f, -145f);
                rect.sizeDelta = new Vector2(640f, 350f);
            }
            Transform content = viewport.Find("RecipeContent");
            if (content == null)
            {
                GameObject contentObject = new GameObject("RecipeContent", typeof(RectTransform));
                content = contentObject.transform; content.SetParent(viewport, false);
                RectTransform rect = contentObject.GetComponent<RectTransform>();
                rect.anchorMin = new Vector2(0f, 1f); rect.anchorMax = new Vector2(1f, 1f);
                rect.pivot = new Vector2(0.5f, 1f); rect.anchoredPosition = Vector2.zero;
                rect.sizeDelta = Vector2.zero;
            }
            VerticalLayoutGroup layout = content.GetComponent<VerticalLayoutGroup>();
            if (layout == null)
            {
                layout = content.gameObject.AddComponent<VerticalLayoutGroup>();
                layout.spacing = 5f; layout.childAlignment = TextAnchor.UpperCenter;
                layout.childControlWidth = true; layout.childControlHeight = false;
                layout.childForceExpandWidth = true; layout.childForceExpandHeight = false;
            }
            ContentSizeFitter fitter = content.GetComponent<ContentSizeFitter>();
            if (fitter == null)
            {
                fitter = content.gameObject.AddComponent<ContentSizeFitter>();
                fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
                fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            }
            Transform template = recipes.Find("RecipeButton_0");
            if (template != null)
            {
                template.SetParent(content, false);
                LayoutElement element = template.GetComponent<LayoutElement>();
                if (element == null)
                {
                    element = template.gameObject.AddComponent<LayoutElement>();
                    element.preferredHeight = template.GetComponent<RectTransform>().sizeDelta.y > 0f
                        ? template.GetComponent<RectTransform>().sizeDelta.y : 46f;
                }
                template.gameObject.SetActive(false);
            }
            for (int i = recipes.childCount - 1; i >= 0; i--)
            {
                Transform child = recipes.GetChild(i);
                if (child.name.StartsWith("RecipeButton_") && child != template)
                    child.gameObject.SetActive(false);
            }
            ScrollRect scroll = recipes.GetComponent<ScrollRect>();
            if (scroll == null)
            {
                scroll = recipes.gameObject.AddComponent<ScrollRect>();
                scroll.horizontal = false; scroll.vertical = true;
                scroll.movementType = ScrollRect.MovementType.Clamped; scroll.inertia = false;
            }
            scroll.viewport = viewport.GetComponent<RectTransform>();
            scroll.content = content.GetComponent<RectTransform>();
            EditorUtility.SetDirty(root);
            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
    }

    private static GameObject CreateCanvas()
    {
        GameObject root = new GameObject("CraftingNpcUI", typeof(RectTransform), typeof(Canvas),
            typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;

        CanvasScaler scaler = root.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280f, 720f);
        scaler.matchWidthOrHeight = 0.5f;

        GameObject panel = CreateRect("Panel", root.transform,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            Vector2.zero, new Vector2(720f, 590f));
        Image panelImage = panel.AddComponent<Image>();
        panelImage.color = new Color(0.08f, 0.1f, 0.13f, 0.97f);

        CreateText("Title", panel.transform, "Posso ajudar?", 38f,
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0f, -52f), new Vector2(640f, 72f));
        CreateText("Message", panel.transform, "", 23f,
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0f, -112f), new Vector2(640f, 44f));

        GameObject screens = CreateRect("Screens", panel.transform, Vector2.zero, Vector2.one,
            new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        GameObject home = CreateRect("HomePanel", screens.transform, Vector2.zero, Vector2.one,
            new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        GameObject recipes = CreateRect("RecipesPanel", screens.transform, Vector2.zero, Vector2.one,
            new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        GameObject details = CreateRect("RecipeDetailsPanel", screens.transform, Vector2.zero, Vector2.one,
            new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        recipes.SetActive(false);
        details.SetActive(false);

        CreateButton(home.transform, "CraftButton", "Craftar", new Vector2(0f, -205f),
            new Vector2(340f, 64f), 25f);
        CreateButton(home.transform, "ExitButton", "Sair", new Vector2(0f, -295f),
            new Vector2(340f, 64f), 25f);

        for (int i = 0; i < 6; i++)
        {
            CreateButton(recipes.transform, "RecipeButton_" + i, "Receita " + (i + 1),
                new Vector2(0f, -170f - i * 51f), new Vector2(620f, 46f), 18f);
        }
        CreateButton(recipes.transform, "BackButton", "Voltar", new Vector2(0f, -520f),
            new Vector2(220f, 48f), 20f);

        CreateText("Ingredients", details.transform, "Materiais necessarios:", 23f,
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f),
            new Vector2(0f, -215f), new Vector2(600f, 220f));
        CreateButton(details.transform, "CraftButton", "Criar", new Vector2(0f, -415f),
            new Vector2(340f, 58f), 22f);
        CreateButton(details.transform, "BackButton", "Voltar", new Vector2(0f, -490f),
            new Vector2(220f, 48f), 20f);

        return root;
    }

    private static GameObject CreateButton(Transform parent, string name, string label,
        Vector2 position, Vector2 size, float fontSize)
    {
        GameObject buttonObject = CreateRect(name, parent,
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), position, size);
        Image image = buttonObject.AddComponent<Image>();
        image.color = new Color(0.22f, 0.3f, 0.36f, 1f);
        Button button = buttonObject.AddComponent<Button>();
        button.targetGraphic = image;
        ColorBlock colors = button.colors;
        colors.normalColor = image.color;
        colors.highlightedColor = new Color(0.32f, 0.43f, 0.5f, 1f);
        colors.pressedColor = new Color(0.15f, 0.22f, 0.27f, 1f);
        colors.selectedColor = colors.highlightedColor;
        colors.disabledColor = new Color(0.22f, 0.3f, 0.36f, 0.5f);
        button.colors = colors;

        GameObject labelObject = CreateRect("Label", buttonObject.transform,
            Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        TextMeshProUGUI text = labelObject.AddComponent<TextMeshProUGUI>();
        text.font = TMP_Settings.defaultFontAsset;
        text.fontSize = fontSize;
        text.color = Color.white;
        text.alignment = TextAlignmentOptions.Center;
        text.text = label;
        text.raycastTarget = false;
        text.margin = new Vector4(12f, 5f, 12f, 5f);
        return buttonObject;
    }

    private static TextMeshProUGUI CreateText(string name, Transform parent, string value, float fontSize,
        Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 position, Vector2 size)
    {
        GameObject textObject = CreateRect(name, parent, anchorMin, anchorMax, pivot, position, size);
        TextMeshProUGUI text = textObject.AddComponent<TextMeshProUGUI>();
        text.font = TMP_Settings.defaultFontAsset;
        text.fontSize = fontSize;
        text.color = Color.white;
        text.alignment = TextAlignmentOptions.Center;
        text.text = value;
        text.raycastTarget = false;
        return text;
    }

    private static GameObject CreateRect(string name, Transform parent, Vector2 anchorMin,
        Vector2 anchorMax, Vector2 pivot, Vector2 position, Vector2 size)
    {
        GameObject obj = new GameObject(name, typeof(RectTransform));
        obj.transform.SetParent(parent, false);
        RectTransform rect = obj.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = pivot;
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        return obj;
    }
}
