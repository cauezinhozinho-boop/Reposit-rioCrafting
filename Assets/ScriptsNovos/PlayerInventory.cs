using UnityEngine;
using System;
using System.Collections.Generic;

public class PlayerInventory : MonoBehaviour
{
    [Serializable]
    public sealed class ItemStackLimit
    {
        public ItemType item;
        [Min(1)] public int maximumQuantity = 99;
    }

    public const int EquipmentSlotCount = 5;

    [Header("Capacidade dos slots da mão")]
    [Min(1)] public int defaultEquipmentStackLimit = 99;
    [Tooltip("Limite por item; os tipos sem entrada usam o limite padrão.")]
    public List<ItemStackLimit> equipmentStackLimits = new List<ItemStackLimit>();

    public event Action OnInventoryChanged;

    private static readonly HashSet<int> keyIDs = new HashSet<int>();
    private static readonly Dictionary<ItemType, int> itemCounts = new Dictionary<ItemType, int>();
    private static readonly ItemType?[] equippedItems = new ItemType?[EquipmentSlotCount];
    private static readonly int[] equippedQuantities = new int[EquipmentSlotCount];
    private static readonly List<int>[] equippedKeyIDs = CreateEquippedKeySlots();

    public int BatteryCount => GetItemCount(ItemType.Battery);
    public int WoodCount => GetItemCount(ItemType.Wood);

    private PlayerSanity playerSanity;
    private PlayerWarmth playerWarmth;
    private PlayerOxygen playerOxygen;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetRuntimeCache()
    {
        ResetAll();
    }

    private void Awake()
    {
        playerSanity = GetComponent<PlayerSanity>();
        playerWarmth = GetComponent<PlayerWarmth>();
        playerOxygen = GetComponent<PlayerOxygen>();
    }

    private static List<int>[] CreateEquippedKeySlots()
    {
        List<int>[] slots = new List<int>[EquipmentSlotCount];
        for (int i = 0; i < slots.Length; i++) slots[i] = new List<int>();
        return slots;
    }

    public void AddItem(ItemType type, int id, float amount)
    {
        switch (type)
        {
            case ItemType.Key:
                if (keyIDs.Add(id))
                    Debug.Log($"[INVENTARIO] Chave ID {id} adicionada.");
                break;
            case ItemType.MedicalKit:
                if (playerSanity != null)
                    playerSanity.currentSanity = Mathf.Min(playerSanity.currentSanity + amount, playerSanity.maxSanity);
                break;
            case ItemType.WarmDrink:
                if (playerWarmth != null)
                    playerWarmth.currentWarmth = Mathf.Min(playerWarmth.currentWarmth + amount, playerWarmth.maxWarmth);
                break;
            case ItemType.OxygenTank:
                if (playerOxygen != null)
                    playerOxygen.currentOxygen = Mathf.Min(playerOxygen.currentOxygen + amount, playerOxygen.maxOxygen);
                break;
            default:
                int count = type == ItemType.Battery ? 1 : Mathf.Max(1, Mathf.FloorToInt(amount));
                AddCount(type, count);
                break;
        }

        OnInventoryChanged?.Invoke();
    }

    public static void ResetAll()
    {
        itemCounts.Clear();
        keyIDs.Clear();
        Array.Clear(equippedItems, 0, equippedItems.Length);
        Array.Clear(equippedQuantities, 0, equippedQuantities.Length);
        for (int i = 0; i < equippedKeyIDs.Length; i++) equippedKeyIDs[i].Clear();
    }

    public int GetItemCount(ItemType type)
    {
        return itemCounts.TryGetValue(type, out int count) ? count : 0;
    }

    public Dictionary<ItemType, int> GetItemsSnapshot()
    {
        return new Dictionary<ItemType, int>(itemCounts);
    }

    public ItemType? GetEquippedItem(int slotIndex)
    {
        return slotIndex >= 0 && slotIndex < equippedItems.Length ? equippedItems[slotIndex] : null;
    }

    public int GetEquippedQuantity(int slotIndex)
    {
        return slotIndex >= 0 && slotIndex < equippedQuantities.Length ? equippedQuantities[slotIndex] : 0;
    }

    public int GetEquipmentStackLimit(ItemType type)
    {
        if (equipmentStackLimits != null)
            foreach (ItemStackLimit limit in equipmentStackLimits)
                if (limit != null && limit.item == type)
                    return Mathf.Max(1, limit.maximumQuantity);
        return Mathf.Max(1, defaultEquipmentStackLimit);
    }

    /// <summary>Moves a stack from the backpack up to its hand-slot capacity, or returns the full stack when cleared.</summary>
    public bool TrySetEquippedItem(int slotIndex, ItemType? item)
    {
        if (slotIndex < 0 || slotIndex >= equippedItems.Length) return false;
        ItemType? currentItem = equippedItems[slotIndex];
        if (!item.HasValue) return ReturnEquippedStack(slotIndex);

        ItemType requestedItem = item.Value;
        int room = GetEquipmentStackLimit(requestedItem) -
            (currentItem == requestedItem ? equippedQuantities[slotIndex] : 0);
        if (room <= 0) return false;

        int available = requestedItem == ItemType.Key ? keyIDs.Count : GetItemCount(requestedItem);
        int transferCount = Mathf.Min(available, room);
        if (transferCount <= 0) return false;

        if (currentItem.HasValue && currentItem != requestedItem)
            ReturnEquippedStackToBackpack(slotIndex);

        if (requestedItem == ItemType.Key)
            TransferKeysToEquipment(slotIndex, transferCount);
        else
            TransferItemsToEquipment(requestedItem, transferCount);

        equippedItems[slotIndex] = requestedItem;
        equippedQuantities[slotIndex] += transferCount;
        OnInventoryChanged?.Invoke();
        return true;
    }

    private bool ReturnEquippedStack(int slotIndex)
    {
        if (!equippedItems[slotIndex].HasValue) return true;
        ReturnEquippedStackToBackpack(slotIndex);
        OnInventoryChanged?.Invoke();
        return true;
    }

    private void ReturnEquippedStackToBackpack(int slotIndex)
    {
        ItemType type = equippedItems[slotIndex].Value;
        if (type == ItemType.Key)
        {
            foreach (int keyID in equippedKeyIDs[slotIndex]) keyIDs.Add(keyID);
            equippedKeyIDs[slotIndex].Clear();
        }
        else
            AddCount(type, equippedQuantities[slotIndex]);

        equippedItems[slotIndex] = null;
        equippedQuantities[slotIndex] = 0;
    }

    private void TransferKeysToEquipment(int slotIndex, int count)
    {
        List<int> keysToMove = new List<int>(keyIDs);
        keysToMove.Sort();
        for (int i = 0; i < count; i++)
        {
            int keyID = keysToMove[i];
            keyIDs.Remove(keyID);
            equippedKeyIDs[slotIndex].Add(keyID);
        }
    }

    private void TransferItemsToEquipment(ItemType type, int count)
    {
        int remaining = GetItemCount(type) - count;
        if (remaining <= 0) itemCounts.Remove(type);
        else itemCounts[type] = remaining;
    }

    public bool TrySwapEquippedItems(int firstSlot, int secondSlot)
    {
        if (firstSlot < 0 || firstSlot >= equippedItems.Length || secondSlot < 0 || secondSlot >= equippedItems.Length)
            return false;
        if (firstSlot == secondSlot) return true;
        ItemType? first = equippedItems[firstSlot];
        equippedItems[firstSlot] = equippedItems[secondSlot];
        equippedItems[secondSlot] = first;
        int firstQuantity = equippedQuantities[firstSlot];
        equippedQuantities[firstSlot] = equippedQuantities[secondSlot];
        equippedQuantities[secondSlot] = firstQuantity;
        List<int> firstKeys = equippedKeyIDs[firstSlot];
        equippedKeyIDs[firstSlot] = equippedKeyIDs[secondSlot];
        equippedKeyIDs[secondSlot] = firstKeys;
        OnInventoryChanged?.Invoke();
        return true;
    }

    public bool HasItems(IReadOnlyDictionary<ItemType, int> required)
    {
        if (required == null) return false;
        foreach (KeyValuePair<ItemType, int> item in required)
        {
            if (item.Value < 0 || GetItemCount(item.Key) < item.Value)
                return false;
        }
        return true;
    }

    public bool TryConsumeItems(IReadOnlyDictionary<ItemType, int> required)
    {
        if (!HasItems(required))
            return false;

        foreach (KeyValuePair<ItemType, int> item in required)
        {
            int remaining = GetItemCount(item.Key) - item.Value;
            if (remaining == 0)
                itemCounts.Remove(item.Key);
            else if (remaining > 0)
                itemCounts[item.Key] = remaining;
        }

        OnInventoryChanged?.Invoke();
        return true;
    }

    public bool TryCraftItems(IReadOnlyDictionary<ItemType, int> required, ItemType output, int outputCount)
    {
        if (outputCount <= 0 || output == ItemType.Key || output == ItemType.MedicalKit ||
            output == ItemType.WarmDrink || output == ItemType.OxygenTank || !HasItems(required))
            return false;

        foreach (KeyValuePair<ItemType, int> item in required)
        {
            int remaining = GetItemCount(item.Key) - item.Value;
            if (remaining == 0) itemCounts.Remove(item.Key);
            else itemCounts[item.Key] = remaining;
        }

        AddCount(output, outputCount);
        OnInventoryChanged?.Invoke();
        return true;
    }

    private void AddCount(ItemType type, int count)
    {
        if (count <= 0)
            return;
        itemCounts[type] = GetItemCount(type) + count;
        Debug.Log($"[INVENTARIO] {type} +{count}. Total: {itemCounts[type]}");
    }

    public bool HasKey(int keyID)
    {
        if (keyIDs.Contains(keyID)) return true;
        for (int i = 0; i < equippedKeyIDs.Length; i++)
            if (equippedKeyIDs[i].Contains(keyID)) return true;
        return false;
    }

    public HashSet<int> GetKeyIDs() => keyIDs;

    public bool UseBattery() => TryConsumeItems(new Dictionary<ItemType, int> { { ItemType.Battery, 1 } });

    public bool UseWood(int amountNeeded)
    {
        return amountNeeded >= 0 && TryConsumeItems(new Dictionary<ItemType, int> { { ItemType.Wood, amountNeeded } });
    }
}
