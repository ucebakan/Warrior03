using System;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

[System.Serializable]
public class WalletItemEntry
{
    public string itemId;
    public int amount;
}

public class PlayerWallet : MonoBehaviour
{
    [Header("Save Settings")]
    [SerializeField] private bool saveCoins = true;
    [SerializeField] private bool useSaveSlotManager = true;
    [SerializeField] private string coinSaveKey = "PlayerWallet_Coins";

    [Header("Currency")]
    [SerializeField, Min(0)] private int coins = 0;

    [Header("Stored Items")]
    [SerializeField] private List<WalletItemEntry> items = new List<WalletItemEntry>();

    [Header("UI")]
    [SerializeField] private TMP_Text coinText;
    [SerializeField] private string coinTextPrefix = "Coins: ";

    [Header("Debug")]
    [SerializeField] private bool logSaveLoad = true;

    private PlayerSaveSlotManager saveSlotManager;
    private bool hasLoadedForCurrentSlot;

    public int Coins => coins;

    public event Action<int> OnCoinsChanged;

    private string CurrentCoinSaveKey => GetSaveKey(coinSaveKey);

    private void Awake()
    {
        coins = Mathf.Max(0, coins);
        ResolveSaveSlotManager();
    }

    private void OnEnable()
    {
        ResolveSaveSlotManager();
        SubscribeToSaveSlotManager();

        TryLoadForCurrentSaveSlot();
        RefreshCoinUI();
    }

    private void Start()
    {
        TryLoadForCurrentSaveSlot();
        RefreshCoinUI();
    }

    private void OnDisable()
    {
        UnsubscribeFromSaveSlotManager();
    }

    private void OnValidate()
    {
        coins = Mathf.Max(0, coins);

        if (Application.isPlaying)
        {
            RefreshCoinUI();
        }
    }

    private void ResolveSaveSlotManager()
    {
        if (!useSaveSlotManager)
            return;

        if (saveSlotManager == null)
        {
            saveSlotManager = PlayerSaveSlotManager.Instance;
        }

        if (saveSlotManager == null)
        {
            saveSlotManager = FindObjectOfType<PlayerSaveSlotManager>();
        }
    }

    private void SubscribeToSaveSlotManager()
    {
        if (!useSaveSlotManager)
            return;

        if (saveSlotManager == null)
            return;

        saveSlotManager.OnSaveSlotSelected -= HandleSaveSlotSelected;
        saveSlotManager.OnSaveSlotSelected += HandleSaveSlotSelected;
    }

    private void UnsubscribeFromSaveSlotManager()
    {
        if (saveSlotManager == null)
            return;

        saveSlotManager.OnSaveSlotSelected -= HandleSaveSlotSelected;
    }

    private void HandleSaveSlotSelected(int slotIndex)
    {
        hasLoadedForCurrentSlot = false;
        LoadCoins();
        hasLoadedForCurrentSlot = true;
        RefreshCoinUI();

        if (logSaveLoad)
        {
            Debug.Log($"PlayerWallet loaded for Save {slotIndex}. Coins: {coins}", this);
        }
    }

    private void TryLoadForCurrentSaveSlot()
    {
        if (!saveCoins)
            return;

        if (hasLoadedForCurrentSlot)
            return;

        if (!useSaveSlotManager)
        {
            LoadCoins();
            hasLoadedForCurrentSlot = true;
            return;
        }

        ResolveSaveSlotManager();

        if (saveSlotManager == null)
        {
            LoadCoins();
            hasLoadedForCurrentSlot = true;

            if (logSaveLoad)
            {
                Debug.LogWarning(
                    "PlayerWallet could not find PlayerSaveSlotManager. Falling back to non-slot coin save key.",
                    this
                );
            }

            return;
        }

        if (!saveSlotManager.HasSelectedSlot)
        {
            coins = 0;
            RefreshCoinUI();

            if (logSaveLoad)
            {
                Debug.Log("PlayerWallet is waiting for save slot selection.", this);
            }

            return;
        }

        LoadCoins();
        hasLoadedForCurrentSlot = true;
    }

    private string GetSaveKey(string baseKey)
    {
        if (string.IsNullOrWhiteSpace(baseKey))
        {
            baseKey = "PlayerWallet_Coins";
        }

        if (!useSaveSlotManager)
        {
            return baseKey;
        }

        ResolveSaveSlotManager();

        if (saveSlotManager == null)
        {
            return baseKey;
        }

        if (!saveSlotManager.HasSelectedSlot)
        {
            return baseKey;
        }

        return saveSlotManager.GetSelectedSlotKey(baseKey);
    }

    public void AddCoins(int amount)
    {
        if (amount <= 0)
            return;

        if (!CanModifyWallet())
            return;

        coins += amount;
        coins = Mathf.Max(0, coins);

        SaveCoinsIfNeeded();
        RefreshCoinUI();

        Debug.Log("Coins: " + coins);
    }

    public bool SpendCoins(int amount)
    {
        if (amount <= 0)
            return false;

        if (!CanModifyWallet())
            return false;

        if (coins < amount)
            return false;

        coins -= amount;
        coins = Mathf.Max(0, coins);

        SaveCoinsIfNeeded();
        RefreshCoinUI();

        Debug.Log("Coins: " + coins);
        return true;
    }

    public bool CanSpendCoins(int amount)
    {
        if (amount <= 0)
            return false;

        return coins >= amount;
    }

    public void SetCoins(int newCoinAmount)
    {
        if (!CanModifyWallet())
            return;

        coins = Mathf.Max(0, newCoinAmount);

        SaveCoinsIfNeeded();
        RefreshCoinUI();

        Debug.Log("Coins set to: " + coins);
    }

    public int GetCoins()
    {
        return coins;
    }

    private bool CanModifyWallet()
    {
        if (!useSaveSlotManager)
            return true;

        ResolveSaveSlotManager();

        if (saveSlotManager == null)
            return true;

        if (!saveSlotManager.HasSelectedSlot)
        {
            Debug.LogWarning("PlayerWallet cannot be modified before selecting a save slot.", this);
            return false;
        }

        return true;
    }

    public void AddItem(string itemId, int amount = 1)
    {
        if (string.IsNullOrWhiteSpace(itemId))
            return;

        if (amount <= 0)
            return;

        WalletItemEntry entry = FindEntry(itemId);

        if (entry == null)
        {
            entry = new WalletItemEntry
            {
                itemId = itemId,
                amount = 0
            };

            items.Add(entry);
        }

        entry.amount += amount;
        Debug.Log(itemId + " added. Amount: " + entry.amount);
    }

    public bool RemoveItem(string itemId, int amount = 1)
    {
        if (string.IsNullOrWhiteSpace(itemId))
            return false;

        if (amount <= 0)
            return false;

        WalletItemEntry entry = FindEntry(itemId);

        if (entry == null)
            return false;

        if (entry.amount < amount)
            return false;

        entry.amount -= amount;

        if (entry.amount <= 0)
        {
            items.Remove(entry);
        }

        Debug.Log(itemId + " removed. Remaining: " + GetItemAmount(itemId));
        return true;
    }

    public int GetItemAmount(string itemId)
    {
        WalletItemEntry entry = FindEntry(itemId);
        return entry != null ? entry.amount : 0;
    }

    public bool HasItem(string itemId, int amount = 1)
    {
        return GetItemAmount(itemId) >= amount;
    }

    private WalletItemEntry FindEntry(string itemId)
    {
        for (int i = 0; i < items.Count; i++)
        {
            if (items[i].itemId == itemId)
            {
                return items[i];
            }
        }

        return null;
    }

    public void RefreshCoinUI()
    {
        if (coinText != null)
        {
            coinText.text = coinTextPrefix + coins;
        }

        OnCoinsChanged?.Invoke(coins);
    }

    private void SaveCoinsIfNeeded()
    {
        if (!saveCoins)
            return;

        SaveCoins();
    }

    public void SaveCoins()
    {
        if (useSaveSlotManager)
        {
            ResolveSaveSlotManager();

            if (saveSlotManager != null && !saveSlotManager.HasSelectedSlot)
            {
                Debug.LogWarning("PlayerWallet cannot save because no save slot is selected.", this);
                return;
            }
        }

        PlayerPrefs.SetInt(CurrentCoinSaveKey, coins);

        if (useSaveSlotManager && saveSlotManager != null && saveSlotManager.HasSelectedSlot)
        {
            saveSlotManager.MarkSelectedSlotHasData();
        }

        PlayerPrefs.Save();

        if (logSaveLoad)
        {
            Debug.Log($"PlayerWallet saved. Coins: {coins} | Key: {CurrentCoinSaveKey}", this);
        }
    }

    public void LoadCoins()
    {
        coins = PlayerPrefs.GetInt(CurrentCoinSaveKey, 0);
        coins = Mathf.Max(0, coins);

        RefreshCoinUI();

        if (logSaveLoad)
        {
            Debug.Log($"PlayerWallet loaded. Coins: {coins} | Key: {CurrentCoinSaveKey}", this);
        }
    }

    public void ForceReloadFromCurrentSlot()
    {
        hasLoadedForCurrentSlot = false;
        TryLoadForCurrentSaveSlot();
        RefreshCoinUI();
    }

    [ContextMenu("Debug Add 100 Coins")]
    private void DebugAdd100Coins()
    {
        AddCoins(100);
    }

    [ContextMenu("Debug Add 1000 Coins")]
    private void DebugAdd1000Coins()
    {
        AddCoins(1000);
    }

    [ContextMenu("Debug Reset Coins To 0")]
    private void DebugResetCoinsToZero()
    {
        SetCoins(0);
    }

    [ContextMenu("Debug Save Coins")]
    private void DebugSaveCoins()
    {
        SaveCoins();
    }

    [ContextMenu("Debug Load Coins")]
    private void DebugLoadCoins()
    {
        LoadCoins();
    }

    [ContextMenu("Debug Print Wallet")]
    private void DebugPrintWallet()
    {
        Debug.Log(
            $"PlayerWallet | Coins: {coins} | " +
            $"Use Save Slot Manager: {useSaveSlotManager} | " +
            $"Current Key: {CurrentCoinSaveKey}",
            this
        );
    }
}