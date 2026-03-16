using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class WalletItemEntry
{
    public string itemId;
    public int amount;
}

public class PlayerWallet : MonoBehaviour
{
    [Header("Currency")]
    [SerializeField] private int coins = 0;

    [Header("Stored Items")]
    [SerializeField] private List<WalletItemEntry> items = new List<WalletItemEntry>();

    public void AddCoins(int amount)
    {
        if (amount <= 0) return;

        coins += amount;
        Debug.Log("Coins: " + coins);
    }

    public bool SpendCoins(int amount)
    {
        if (amount <= 0) return false;
        if (coins < amount) return false;

        coins -= amount;
        Debug.Log("Coins: " + coins);
        return true;
    }

    public int GetCoins()
    {
        return coins;
    }

    public void AddItem(string itemId, int amount = 1)
    {
        if (string.IsNullOrWhiteSpace(itemId)) return;
        if (amount <= 0) return;

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
        if (string.IsNullOrWhiteSpace(itemId)) return false;
        if (amount <= 0) return false;

        WalletItemEntry entry = FindEntry(itemId);

        if (entry == null) return false;
        if (entry.amount < amount) return false;

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
}