using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Creates the initial editable inventory UI prefab. It never overwrites an existing prefab.</summary>
[InitializeOnLoad]
public static class InventoryUIPrefabBuilder
{
    private const string PrefabPath = "Assets/Resources/UI/InventoryUI.prefab";

    static InventoryUIPrefabBuilder() { EditorApplication.delayCall += EnsurePrefabExists; }

    [MenuItem("Tools/UI/Create Inventory UI Prefab (if missing)")]
    public static void EnsurePrefabExists()
    {
        if (File.Exists(PrefabPath))
        {
            EnsureSlotRaycasts();
            return;
        }
        Directory.CreateDirectory(Path.GetDirectoryName(PrefabPath));
        GameObject root = Build();
        try
        {
            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            AssetDatabase.SaveAssets();
            Debug.Log("Prefab editável do inventário criado em " + PrefabPath);
        }
        finally { Object.DestroyImmediate(root); }
        EnsureSlotRaycasts();
    }

    private static void EnsureSlotRaycasts()
    {
        GameObject root = PrefabUtility.LoadPrefabContents(PrefabPath);
        bool changed = false;
        try
        {
            string[] paths = { "InventoryScreen/SlotsPanel/SlotTemplate", "EquipmentHud/Slots/EquipmentSlotTemplate" };
            foreach (string path in paths)
            {
                Transform slot = root.transform.Find(path);
                Image image = slot != null ? slot.GetComponent<Image>() : null;
                if (image != null && !image.raycastTarget)
                {
                    image.raycastTarget = true;
                    changed = true;
                }
            }
            if (changed)
            {
                EditorUtility.SetDirty(root);
                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
                AssetDatabase.SaveAssets();
            }
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
    }

    private static GameObject Build()
    {
        GameObject root = Rect("InventoryUI", null, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        GameObject screen = Rect("InventoryScreen", root.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        AddImage(screen, new Color(0.055f, 0.07f, 0.075f, 0.98f), true);
        Text("Title", screen.transform, "INVENTÁRIO", 30, FontStyles.Bold, new Color(0.82f, 0.64f, 0.36f),
            new Vector2(0.035f, 0.84f), new Vector2(0.58f, 0.97f), TextAlignmentOptions.Left);
        Text("Summary", screen.transform, "0 tipo(s) de item • 24 espaços", 16, FontStyles.Normal, Color.white,
            new Vector2(0.62f, 0.875f), new Vector2(0.965f, 0.95f), TextAlignmentOptions.Right);
        Text("Hint", screen.transform, "Arraste para reorganizar ou equipar na área MÃO • Clique para ver detalhes", 14,
            FontStyles.Italic, new Color(0.78f, 0.8f, 0.79f), new Vector2(0.035f, 0.79f), new Vector2(0.965f, 0.85f), TextAlignmentOptions.Left);

        GameObject grid = Rect("SlotsPanel", screen.transform, new Vector2(0.035f, 0.07f), new Vector2(0.66f, 0.77f), Vector2.zero, Vector2.zero);
        AddImage(grid, new Color(0.08f, 0.095f, 0.095f, 0.7f));
        GridLayoutGroup layout = grid.AddComponent<GridLayoutGroup>();
        layout.cellSize = new Vector2(76f, 76f); layout.spacing = new Vector2(10f, 10f);
        layout.constraint = GridLayoutGroup.Constraint.FixedColumnCount; layout.constraintCount = 6;
        layout.padding = new RectOffset(14, 14, 14, 14); layout.childAlignment = TextAnchor.UpperLeft;
        GameObject slot = BuildSlot("SlotTemplate", grid.transform, false);
        slot.SetActive(false);

        GameObject details = Rect("ItemDetails", screen.transform, new Vector2(0.685f, 0.07f), new Vector2(0.965f, 0.77f), Vector2.zero, Vector2.zero);
        AddImage(details, new Color(0.1f, 0.115f, 0.115f, 0.95f));
        Text("DetailsTitle", details.transform, "DETALHES DO ITEM", 17, FontStyles.Bold, new Color(0.82f, 0.64f, 0.36f),
            new Vector2(0.07f, 0.83f), new Vector2(0.93f, 0.96f), TextAlignmentOptions.Left);
        Text("ItemName", details.transform, "Nenhum item selecionado", 24, FontStyles.Bold, Color.white,
            new Vector2(0.07f, 0.66f), new Vector2(0.93f, 0.82f), TextAlignmentOptions.Left);
        Text("ItemQuantity", details.transform, "", 17, FontStyles.Bold, new Color(0.82f, 0.64f, 0.36f),
            new Vector2(0.07f, 0.53f), new Vector2(0.93f, 0.64f), TextAlignmentOptions.Left);
        Text("ItemDescription", details.transform, "Selecione um item na grade para ler sua descrição.", 16, FontStyles.Normal,
            new Color(0.88f, 0.89f, 0.86f), new Vector2(0.07f, 0.08f), new Vector2(0.93f, 0.49f), TextAlignmentOptions.TopLeft);

        GameObject hud = Rect("EquipmentHud", root.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
            new Vector2(-235f, 18f), new Vector2(235f, 150f));
        AddImage(hud, new Color(0.045f, 0.055f, 0.06f, 0.97f));
        Outline outline = hud.AddComponent<Outline>(); outline.effectColor = new Color(0.82f, 0.64f, 0.36f, 0.48f);
        outline.effectDistance = new Vector2(1.5f, 1.5f);
        Text("Title", hud.transform, "ITENS EQUIPADOS", 16, FontStyles.Bold, new Color(0.82f, 0.64f, 0.36f),
            new Vector2(0f, 0.78f), new Vector2(1f, 0.98f), TextAlignmentOptions.Center);
        GameObject equipmentSlots = Rect("Slots", hud.transform, new Vector2(0f, 0f), Vector2.one, Vector2.zero, Vector2.zero);
        HorizontalLayoutGroup equipmentLayout = equipmentSlots.AddComponent<HorizontalLayoutGroup>();
        equipmentLayout.childAlignment = TextAnchor.MiddleCenter; equipmentLayout.spacing = 8f;
        equipmentLayout.padding = new RectOffset(12, 12, 18, 0);
        equipmentLayout.childControlWidth = false; equipmentLayout.childControlHeight = false;
        equipmentLayout.childForceExpandWidth = false; equipmentLayout.childForceExpandHeight = false;
        GameObject equipmentSlot = BuildSlot("EquipmentSlotTemplate", equipmentSlots.transform, true);
        equipmentSlot.SetActive(false);
        return root;
    }

    private static GameObject BuildSlot(string name, Transform parent, bool equipment)
    {
        GameObject slot = Rect(name, parent, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        if (equipment)
        {
            RectTransform slotRect = slot.GetComponent<RectTransform>();
            slotRect.anchorMin = slotRect.anchorMax = new Vector2(0.5f, 0.5f);
            slotRect.sizeDelta = new Vector2(62f, 62f);
            LayoutElement element = slot.AddComponent<LayoutElement>();
            element.preferredWidth = 62f; element.preferredHeight = 62f;
        }
        // Slot graphics must receive the pointer raycast for click, drag and drop.
        AddImage(slot, equipment ? new Color(0.19f, 0.17f, 0.12f, 0.98f) : new Color(0.09f, 0.11f, 0.13f, 0.74f), true);
        InventorySlotView behavior = slot.AddComponent<InventorySlotView>();
        TextMeshProUGUI item = Text("ItemLabel", slot.transform, "", 15, FontStyles.Normal, Color.white,
            new Vector2(0.05f, equipment ? 0.28f : 0.25f), new Vector2(0.95f, equipment ? 0.78f : 0.92f), TextAlignmentOptions.Center);
        item.enableAutoSizing = true; item.fontSizeMin = 8; item.fontSizeMax = 15;
        TextMeshProUGUI quantity = Text("QuantityLabel", slot.transform, "", 13, FontStyles.Normal, Color.white,
            new Vector2(0.05f, equipment ? 0.02f : 0.04f), new Vector2(0.95f, equipment ? 0.28f : 0.32f), TextAlignmentOptions.BottomRight);
        quantity.enableAutoSizing = true; quantity.fontSizeMin = 8; quantity.fontSizeMax = 13;
        TextMeshProUGUI number = null;
        if (equipment)
        {
            GameObject badge = Rect("NumberBadge", slot.transform, new Vector2(0.07f, 0.64f), new Vector2(0.35f, 0.92f), Vector2.zero, Vector2.zero);
            AddImage(badge, new Color(0.30f, 0.19f, 0.08f, 0.98f));
            badge.GetComponent<Image>().raycastTarget = false;
            number = Text("NumberLabel", badge.transform, "1", 11, FontStyles.Bold, new Color(1f, 0.9f, 0.72f),
                Vector2.zero, Vector2.one, TextAlignmentOptions.Center);
        }
        SerializedObject serialized = new SerializedObject(behavior);
        serialized.FindProperty("background").objectReferenceValue = slot.GetComponent<Image>();
        serialized.FindProperty("itemLabel").objectReferenceValue = item;
        serialized.FindProperty("quantityLabel").objectReferenceValue = quantity;
        if (number != null) serialized.FindProperty("numberLabel").objectReferenceValue = number;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        return slot;
    }

    private static GameObject Rect(string name, Transform parent, Vector2 min, Vector2 max, Vector2 offsetMin, Vector2 offsetMax)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        if (parent != null) go.transform.SetParent(parent, false);
        RectTransform rect = go.GetComponent<RectTransform>(); rect.anchorMin = min; rect.anchorMax = max;
        rect.pivot = new Vector2(0.5f, 0.5f); rect.offsetMin = offsetMin; rect.offsetMax = offsetMax;
        return go;
    }

    private static void AddImage(GameObject go, Color color, bool raycast = false)
    {
        Image image = go.AddComponent<Image>(); image.color = color; image.raycastTarget = raycast;
    }

    private static TextMeshProUGUI Text(string name, Transform parent, string value, float size, FontStyles style, Color color,
        Vector2 min, Vector2 max, TextAlignmentOptions alignment)
    {
        GameObject go = Rect(name, parent, min, max, Vector2.zero, Vector2.zero);
        TextMeshProUGUI text = go.AddComponent<TextMeshProUGUI>(); text.font = TMP_Settings.defaultFontAsset;
        text.text = value; text.fontSize = size; text.fontStyle = style; text.color = color;
        text.alignment = alignment; text.raycastTarget = false; return text;
    }
}
