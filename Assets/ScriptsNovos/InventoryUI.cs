using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Inventory screen built from Inspector-configurable values. Existing scene references
/// (inventoryPanel and the legacy counters) remain supported for easy migration.
/// </summary>
public sealed class InventoryUI : MonoBehaviour
{
    [Serializable]
    public sealed class ItemDescription
    {
        public ItemType item;
        [TextArea(2, 5)] public string description;
    }

    [Header("Painel principal")]
    [Tooltip("Canvas da cena que receberá o prefab visual. O objeto antigo InventoryPanel pode continuar como referência do Canvas.")]
    public GameObject inventoryPanel;
    [Tooltip("Prefab visual editável em Assets/Resources/UI/InventoryUI.prefab")]
    public GameObject inventoryPrefab;
    [Tooltip("Tecla para abrir/fechar. I continua funcionando como atalho alternativo.")]
    public KeyCode toggleKey = KeyCode.Tab;

    [Header("Slots")]
    [Min(1)] public int slotCount = 24;

    [Header("Descrições editáveis")]
    [Tooltip("Edite estes textos no Inspector. Itens sem entrada usam uma descrição automática.")]
    public List<ItemDescription> itemDescriptions = new List<ItemDescription>();

    [Header("Compatibilidade com contadores antigos")]
    public TextMeshProUGUI batteryText;
    public TextMeshProUGUI woodText;
    public TextMeshProUGUI keysText;

    private PlayerInventory playerInventory;
    private bool isOpen;
    private GameObject runtimeUiRoot;
    private GameObject screenRoot;
    private GameObject slotTemplate;
    private GameObject equipmentSlotTemplate;
    private Transform equipmentHudRoot;
    private RectTransform gridRoot;
    private TextMeshProUGUI detailName;
    private TextMeshProUGUI detailDescription;
    private TextMeshProUGUI detailQuantity;
    private TextMeshProUGUI headerSummary;
    private readonly List<InventorySlotView> slotViews = new List<InventorySlotView>();
    private readonly List<InventorySlotView> equipmentSlotViews = new List<InventorySlotView>();
    private readonly List<ItemType?> slotOrder = new List<ItemType?>();
    private ItemType? selectedItem;

    private void Start()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null) playerInventory = player.GetComponentInParent<PlayerInventory>();
        if (playerInventory != null) playerInventory.OnInventoryChanged += UpdateUI;

        if (inventoryPanel != null)
        {
            BuildInterface();
            BuildEquipmentHud();
        }
        UpdateUI();
    }

    private void OnDestroy()
    {
        if (playerInventory != null) playerInventory.OnInventoryChanged -= UpdateUI;
    }

    private void Update()
    {
        if (PauseManager.IsPaused || CraftingNpc.IsMenuOpen) return;
        if (Input.GetKeyDown(toggleKey) || Input.GetKeyDown(KeyCode.I)) ToggleInventory();
    }

    public void ToggleInventory()
    {
        if (inventoryPanel == null) return;
        isOpen = !isOpen;
        if (screenRoot != null) screenRoot.SetActive(isOpen);
        if (isOpen) UpdateUI();
    }

    public void UpdateUI()
    {
        if (playerInventory == null) return;
        if (batteryText != null) batteryText.text = $"Baterias: {playerInventory.BatteryCount}";
        if (woodText != null) woodText.text = $"Madeira: {playerInventory.WoodCount}";
        if (keysText != null) keysText.text = playerInventory.GetKeyIDs().Count > 0
            ? "Chaves: " + string.Join(", ", playerInventory.GetKeyIDs()) : "Chaves: Nenhuma";
        if (gridRoot == null) return;

        Dictionary<ItemType, int> items = playerInventory.GetItemsSnapshot();
        List<ItemType> present = new List<ItemType>(items.Keys);
        present.Sort((a, b) => string.Compare(GetItemName(a), GetItemName(b), StringComparison.CurrentCulture));
        for (int i = 0; i < slotOrder.Count; i++)
            if (slotOrder[i].HasValue && !items.ContainsKey(slotOrder[i].Value)) slotOrder[i] = null;
        foreach (ItemType item in present)
            if (!slotOrder.Contains(item))
            {
                int emptyIndex = slotOrder.FindIndex(entry => !entry.HasValue);
                if (emptyIndex >= 0) slotOrder[emptyIndex] = item;
                else slotOrder.Add(item);
            }

        int totalKinds = items.Count + (playerInventory.GetKeyIDs().Count > 0 ? 1 : 0);
        if (headerSummary != null) headerSummary.text = $"{totalKinds} tipo(s) de item  •  {slotCount} espaços";
        for (int i = 0; i < slotViews.Count; i++)
        {
            if (i < slotOrder.Count && slotOrder[i].HasValue)
            {
                ItemType item = slotOrder[i].Value;
                slotViews[i].SetItem(item, items[item], GetItemName(item), i, selectedItem == item);
            }
            else if (i == slotOrder.Count && playerInventory.GetKeyIDs().Count > 0)
                slotViews[i].SetItem(ItemType.Key, playerInventory.GetKeyIDs().Count, "Chave", i, selectedItem == ItemType.Key);
            else slotViews[i].SetEmpty(i);
        }

        for (int i = 0; i < equipmentSlotViews.Count; i++)
        {
            ItemType? equipped = playerInventory.GetEquippedItem(i);
            if (equipped.HasValue)
                equipmentSlotViews[i].SetItem(equipped.Value, playerInventory.GetEquippedQuantity(i), GetItemName(equipped.Value), i, selectedItem == equipped.Value);
            else equipmentSlotViews[i].SetEmpty(i);
        }

        if (selectedItem.HasValue && (GetCount(selectedItem.Value) > 0 || IsEquipped(selectedItem.Value))) ShowDetails(selectedItem.Value);
        else if (!selectedItem.HasValue || GetCount(selectedItem.Value) <= 0) ShowEmptyDetails();
    }

    private int GetCount(ItemType type)
    {
        if (playerInventory == null) return 0;
        return type == ItemType.Key ? playerInventory.GetKeyIDs().Count : playerInventory.GetItemCount(type);
    }

    private void BuildInterface()
    {
        if (inventoryPrefab == null) inventoryPrefab = Resources.Load<GameObject>("UI/InventoryUI");
        Canvas canvas = inventoryPanel != null ? inventoryPanel.GetComponentInParent<Canvas>() : null;
        if (inventoryPrefab == null || canvas == null)
        {
            Debug.LogError("Atribua InventoryUI.prefab e o Canvas no componente InventoryUI.", this);
            return;
        }
        runtimeUiRoot = Instantiate(inventoryPrefab, canvas.transform, false);
        inventoryPanel.SetActive(false);
        runtimeUiRoot.name = "InventoryUI_Runtime";
        Transform screen = runtimeUiRoot.transform.Find("InventoryScreen");
        Transform slots = runtimeUiRoot.transform.Find("InventoryScreen/SlotsPanel");
        Transform details = runtimeUiRoot.transform.Find("InventoryScreen/ItemDetails");
        Transform equipment = runtimeUiRoot.transform.Find("EquipmentHud");
        if (screen == null || slots == null || details == null || equipment == null)
        {
            Debug.LogError("InventoryUI.prefab precisa conter InventoryScreen/SlotsPanel, ItemDetails e EquipmentHud.", this);
            Destroy(runtimeUiRoot); runtimeUiRoot = null; return;
        }
        screenRoot = screen.gameObject;
        screenRoot.SetActive(false);
        headerSummary = FindText(screen, "Summary");
        detailName = FindText(details, "ItemName");
        detailQuantity = FindText(details, "ItemQuantity");
        detailDescription = FindText(details, "ItemDescription");
        slotTemplate = slots.Find("SlotTemplate")?.gameObject;
        equipmentSlotTemplate = equipment.Find("Slots/EquipmentSlotTemplate")?.gameObject;
        if (headerSummary == null || detailName == null || detailQuantity == null || detailDescription == null || slotTemplate == null || equipmentSlotTemplate == null)
        {
            Debug.LogError("Confira os nomes e componentes do prefab InventoryUI conforme o guia de edição visual.", this);
            return;
        }
        gridRoot = slots.GetComponent<RectTransform>();
        slotViews.Clear();
        for (int i = 0; i < Mathf.Max(1, slotCount); i++)
        {
            GameObject slot = Instantiate(slotTemplate, slots, false);
            slot.name = $"Slot_{i + 1:00}"; slot.SetActive(true);
            InventorySlotView view = slot.GetComponent<InventorySlotView>();
            if (view == null) { Debug.LogError("SlotTemplate precisa de InventorySlotView.", slot); continue; }
            view.Configure(this, slot.GetComponent<Image>());
            slotViews.Add(view);
        }
    }

    private void BuildEquipmentHud()
    {
        if (runtimeUiRoot == null || equipmentSlotTemplate == null) return;
        Transform slots = runtimeUiRoot.transform.Find("EquipmentHud/Slots");
        if (slots == null) return;
        equipmentHudRoot = slots.parent;
        equipmentSlotViews.Clear();
        for (int i = 0; i < PlayerInventory.EquipmentSlotCount; i++)
        {
            GameObject equipSlot = Instantiate(equipmentSlotTemplate, slots, false);
            equipSlot.name = $"EquipmentSlot_{i + 1}"; equipSlot.SetActive(true);
            InventorySlotView view = equipSlot.GetComponent<InventorySlotView>();
            if (view == null) { Debug.LogError("EquipmentSlotTemplate precisa de InventorySlotView.", equipSlot); continue; }
            view.Configure(this, equipSlot.GetComponent<Image>(), true, i + 1);
            equipmentSlotViews.Add(view);
        }
    }

    private static TextMeshProUGUI FindText(Transform parent, string childName)
    {
        Transform child = parent.Find(childName);
        return child != null ? child.GetComponent<TextMeshProUGUI>() : null;
    }

    internal void SelectItem(ItemType item) { selectedItem = item; ShowDetails(item); UpdateUI(); }

    internal void MoveItem(int sourceIndex, int targetIndex)
    {
        if (sourceIndex < 0 || sourceIndex >= slotOrder.Count || targetIndex < 0 || targetIndex >= slotViews.Count ||
            !slotOrder[sourceIndex].HasValue) return;
        while (slotOrder.Count <= targetIndex) slotOrder.Add(null);
        ItemType? moving = slotOrder[sourceIndex];
        slotOrder[sourceIndex] = slotOrder[targetIndex];
        slotOrder[targetIndex] = moving;
        UpdateUI();
    }

    internal void HandleSlotDrop(InventorySlotView source, InventorySlotView destination)
    {
        if (playerInventory == null || source == null || destination == null || source == destination || !source.HasItem) return;
        bool changed;
        if (source.IsEquipmentSlot && destination.IsEquipmentSlot)
            changed = playerInventory.TrySwapEquippedItems(source.SlotIndex, destination.SlotIndex);
        else if (source.IsEquipmentSlot)
            changed = playerInventory.TrySetEquippedItem(source.SlotIndex, null);
        else if (destination.IsEquipmentSlot)
            changed = playerInventory.TrySetEquippedItem(destination.SlotIndex, source.Item);
        else
        {
            MoveItem(source.SlotIndex, destination.SlotIndex);
            return;
        }

        if (!changed && destination.IsEquipmentSlot)
            Debug.LogWarning("Não foi possível equipar este item. Confirme se ele ainda está no inventário.", this);
    }

    private void ShowDetails(ItemType item)
    {
        if (detailName == null || playerInventory == null) return;
        detailName.text = item == ItemType.Key ? "Chaves" : GetItemName(item);
        detailQuantity.text = $"Mochila: {GetCount(item)}    •    Equipado: {GetEquippedCount(item)}";
        string description = null;
        foreach (ItemDescription entry in itemDescriptions)
            if (entry != null && entry.item == item) { description = entry.description; break; }
        detailDescription.text = string.IsNullOrWhiteSpace(description) ? GetDefaultDescription(item) : description;
    }

    private bool IsEquipped(ItemType item) => GetEquippedCount(item) > 0;

    private int GetEquippedCount(ItemType item)
    {
        if (playerInventory == null) return 0;
        int count = 0;
        for (int i = 0; i < PlayerInventory.EquipmentSlotCount; i++)
            if (playerInventory.GetEquippedItem(i) == item) count += playerInventory.GetEquippedQuantity(i);
        return count;
    }

    private void ShowEmptyDetails()
    {
        if (detailName == null) return;
        detailName.text = "Nenhum item selecionado"; detailQuantity.text = "";
        detailDescription.text = "Selecione um item na grade para ler sua descrição. As descrições podem ser editadas no Inspector.";
    }

    private static string GetItemName(ItemType item) => item == ItemType.Key ? "Chaves" : Regex.Replace(item.ToString(), "([a-z])([A-Z])", "$1 $2");
    private string GetDefaultDescription(ItemType item)
    {
        switch (item)
        {
            case ItemType.Battery: return "Uma bateria portátil. Pode ser usada para alimentar dispositivos e também é necessária em algumas receitas.";
            case ItemType.Wood: return "Madeira recolhida durante a exploração. Serve como material para fogueiras e receitas.";
            case ItemType.FungoComum: return "Um fungo silvestre relativamente comum. Pode ser usado no preparo de alimentos e em receitas de fabricação.";
            case ItemType.FungoDeTronco: return "Fungo que cresce em troncos úmidos. Recurso natural útil para experimentos e fabricação.";
            case ItemType.FungoLunaSonos: return "Fungo raro associado a efeitos calmantes. É ingrediente de uma substância medicinal.";
            case ItemType.LuminousFungus: return "Fungo que emite um brilho suave no escuro. Um recurso incomum encontrado durante a exploração.";
            case ItemType.Fibra: return "Fibras vegetais resistentes, adequadas para criar cordas e outros materiais.";
            case ItemType.CarneCrua: return "Carne ainda não preparada. Pode ser cozida usando madeira ou transformada em uma receita.";
            case ItemType.Ferro: return "Minério metálico resistente, utilizado na fabricação de ferramentas e equipamentos.";
            case ItemType.Cobre: return "Metal condutor que pode ser aproveitado em componentes e receitas de fabricação.";
            case ItemType.Zinco: return "Minério metálico encontrado na região. Pode ser combinado com outros recursos.";
            case ItemType.Gelo: return "Gelo coletado no ambiente. Mantenha-o protegido do calor para evitar que derreta.";
            case ItemType.Areia: return "Areia bruta. Pode ser processada para obter materiais usados na fabricação.";
            case ItemType.Sucata: return "Peças recuperadas de objetos descartados. Uma fonte versátil de materiais.";
            case ItemType.Meteorito: return "Fragmento raro de origem desconhecida. Seu uso pode ser importante em descobertas futuras.";
            case ItemType.Componentes: return "Peças mecânicas reaproveitáveis, necessárias para construir dispositivos.";
            case ItemType.Couro: return "Material resistente obtido de animais. Pode servir para fabricar ou reforçar equipamentos.";
            case ItemType.Localizador: return "Dispositivo de localização portátil, útil para orientar a exploração.";
            case ItemType.Vidro: return "Material transparente utilizado em recipientes e equipamentos.";
            case ItemType.Frasco: return "Recipiente pequeno para armazenar líquidos ou substâncias preparadas.";
            case ItemType.RecipienteMetalico: return "Recipiente robusto de metal, adequado para guardar ou processar materiais.";
            case ItemType.Fio: return "Fio resistente utilizado para unir componentes e fabricar equipamentos.";
            case ItemType.Tecido: return "Tecido aproveitável na confecção de itens e equipamentos.";
            case ItemType.Corda: return "Corda resistente, feita para amarrar objetos ou compor receitas.";
            case ItemType.Latão: return "Liga metálica útil para componentes e mecanismos.";
            case ItemType.FiosDeCobre: return "Fios condutores necessários para montar dispositivos eletrônicos.";
            case ItemType.Engrenagem: return "Peça mecânica que transmite movimento em mecanismos e equipamentos.";
            case ItemType.AguaPotavel: return "Água própria para consumo, também utilizada no preparo de alimentos.";
            case ItemType.SopaDeCogumelo: return "Refeição preparada com cogumelos e água potável. Recupera as energias durante a jornada.";
            case ItemType.CarneCozida: return "Carne preparada no fogo. Mais adequada para consumo do que a carne crua.";
            case ItemType.SopaDeCogumeloComCarne: return "Refeição reforçada feita com cogumelos e carne. Ideal para uma pausa durante a exploração.";
            case ItemType.SubistanciaLuminosa: return "Substância preparada a partir de materiais luminescentes. Pode ter propriedades especiais.";
            case ItemType.SubistanciaCalmante: return "Preparado calmante feito com fungo Luna-Sonos e um frasco.";
            case ItemType.GarrafaTermica: return "Recipiente isolado que ajuda a manter bebidas na temperatura adequada.";
            case ItemType.CilindroDeOxigenio: return "Cilindro portátil para armazenar oxigênio e auxiliar em ambientes de ar limitado.";
            case ItemType.PeDeCabra: return "Ferramenta robusta para alavancar e abrir objetos presos.";
            case ItemType.Comunicador: return "Dispositivo de comunicação montado com componentes, fios de cobre e uma bateria.";
            case ItemType.MedicalKit: return "Kit médico para restaurar a sanidade do personagem ao ser coletado.";
            case ItemType.WarmDrink: return "Bebida quente que ajuda a recuperar o calor corporal ao ser coletada.";
            case ItemType.OxygenTank: return "Tanque de oxigênio que restaura o fôlego ao ser coletado.";
            case ItemType.Key: return playerInventory.GetKeyIDs().Count == 0 ? "Chaves encontradas durante a exploração." : "Chaves encontradas: " + string.Join(", ", playerInventory.GetKeyIDs()) + ".";
            default: return $"{GetItemName(item)} carregado pelo personagem. Edite este texto na lista Descrições editáveis do Inspector.";
        }
    }

}
