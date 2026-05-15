using System;
using UnityEngine;

public class PlayerStatUpgradeController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerStats playerStats;
    [SerializeField] private PlayerWallet playerWallet;

    [Header("Debug Test")]
    [SerializeField] private bool enableKeyboardTest = true;
    [SerializeField] private KeyCode buyAttackDamageKey = KeyCode.Alpha1;
    [SerializeField] private KeyCode buyMaxHealthKey = KeyCode.Alpha2;
    [SerializeField] private KeyCode buyArmorKey = KeyCode.Alpha3;
    [SerializeField] private KeyCode printInfoKey = KeyCode.P;

    [Header("Debug Log")]
    [SerializeField] private bool logUpgradeResults = true;

    public event Action<PlayerStatUpgradeResult> OnUpgradeAttemptFinished;

    private void Awake()
    {
        ResolveReferences();
    }

    private void Update()
    {
        if (!enableKeyboardTest)
            return;

        if (Input.GetKeyDown(buyAttackDamageKey))
        {
            BuyAttackDamageUpgrade();
        }

        if (Input.GetKeyDown(buyMaxHealthKey))
        {
            BuyMaxHealthUpgrade();
        }

        if (Input.GetKeyDown(buyArmorKey))
        {
            BuyArmorUpgrade();
        }

        if (Input.GetKeyDown(printInfoKey))
        {
            PrintUpgradeInfo();
        }
    }

    private void ResolveReferences()
    {
        if (playerStats == null)
        {
            playerStats = GetComponent<PlayerStats>();
        }

        if (playerStats == null)
        {
            playerStats = GetComponentInParent<PlayerStats>();
        }

        if (playerStats == null)
        {
            playerStats = FindObjectOfType<PlayerStats>();
        }

        if (playerWallet == null)
        {
            playerWallet = GetComponent<PlayerWallet>();
        }

        if (playerWallet == null)
        {
            playerWallet = GetComponentInParent<PlayerWallet>();
        }

        if (playerWallet == null)
        {
            playerWallet = FindObjectOfType<PlayerWallet>();
        }
    }

    public void BuyAttackDamageUpgrade()
    {
        TryBuyUpgrade(PlayerStatType.AttackDamage, out _);
    }

    public void BuyMaxHealthUpgrade()
    {
        TryBuyUpgrade(PlayerStatType.MaxHealth, out _);
    }

    public void BuyArmorUpgrade()
    {
        TryBuyUpgrade(PlayerStatType.Armor, out _);
    }

    public bool TryBuyAttackDamageUpgrade(out PlayerStatUpgradeResult result)
    {
        return TryBuyUpgrade(PlayerStatType.AttackDamage, out result);
    }

    public bool TryBuyMaxHealthUpgrade(out PlayerStatUpgradeResult result)
    {
        return TryBuyUpgrade(PlayerStatType.MaxHealth, out result);
    }

    public bool TryBuyArmorUpgrade(out PlayerStatUpgradeResult result)
    {
        return TryBuyUpgrade(PlayerStatType.Armor, out result);
    }

    public bool TryBuyUpgrade(PlayerStatType statType, out PlayerStatUpgradeResult result)
    {
        ResolveReferences();

        if (playerStats == null)
        {
            result = new PlayerStatUpgradeResult(
                false,
                statType,
                0,
                0,
                0,
                0,
                0,
                0,
                "PlayerStats reference is missing."
            );

            FinishUpgradeAttempt(result);
            return false;
        }

        if (playerWallet == null)
        {
            int oldLevelWithoutWallet = playerStats.GetUpgradeLevel(statType);
            int oldValueWithoutWallet = playerStats.GetCurrentStatValue(statType);

            result = new PlayerStatUpgradeResult(
                false,
                statType,
                playerStats.GetUpgradeCost(statType),
                0,
                oldLevelWithoutWallet,
                oldLevelWithoutWallet,
                oldValueWithoutWallet,
                oldValueWithoutWallet,
                "PlayerWallet reference is missing."
            );

            FinishUpgradeAttempt(result);
            return false;
        }

        int cost = playerStats.GetUpgradeCost(statType);
        int oldLevel = playerStats.GetUpgradeLevel(statType);
        int oldValue = playerStats.GetCurrentStatValue(statType);
        int currentCoins = playerWallet.GetCoins();

        if (playerStats.IsUpgradeMaxed(statType))
        {
            result = new PlayerStatUpgradeResult(
                false,
                statType,
                cost,
                currentCoins,
                oldLevel,
                oldLevel,
                oldValue,
                oldValue,
                $"{GetDisplayName(statType)} upgrade is already maxed."
            );

            FinishUpgradeAttempt(result);
            return false;
        }

        if (cost <= 0)
        {
            result = new PlayerStatUpgradeResult(
                false,
                statType,
                cost,
                currentCoins,
                oldLevel,
                oldLevel,
                oldValue,
                oldValue,
                $"{GetDisplayName(statType)} upgrade cost is invalid."
            );

            FinishUpgradeAttempt(result);
            return false;
        }

        if (!playerWallet.CanSpendCoins(cost))
        {
            result = new PlayerStatUpgradeResult(
                false,
                statType,
                cost,
                currentCoins,
                oldLevel,
                oldLevel,
                oldValue,
                oldValue,
                $"Not enough coins for {GetDisplayName(statType)} upgrade. Required: {cost}, Current: {currentCoins}."
            );

            FinishUpgradeAttempt(result);
            return false;
        }

        bool spentCoins = playerWallet.SpendCoins(cost);

        if (!spentCoins)
        {
            result = new PlayerStatUpgradeResult(
                false,
                statType,
                cost,
                playerWallet.GetCoins(),
                oldLevel,
                oldLevel,
                oldValue,
                oldValue,
                $"Could not spend coins for {GetDisplayName(statType)} upgrade."
            );

            FinishUpgradeAttempt(result);
            return false;
        }

        bool upgraded = playerStats.ApplyUpgrade(statType);

        if (!upgraded)
        {
            playerWallet.AddCoins(cost);

            result = new PlayerStatUpgradeResult(
                false,
                statType,
                cost,
                playerWallet.GetCoins(),
                oldLevel,
                oldLevel,
                oldValue,
                oldValue,
                $"{GetDisplayName(statType)} upgrade failed. Coins were refunded."
            );

            FinishUpgradeAttempt(result);
            return false;
        }

        int newLevel = playerStats.GetUpgradeLevel(statType);
        int newValue = playerStats.GetCurrentStatValue(statType);
        int remainingCoins = playerWallet.GetCoins();

        result = new PlayerStatUpgradeResult(
            true,
            statType,
            cost,
            remainingCoins,
            oldLevel,
            newLevel,
            oldValue,
            newValue,
            $"{GetDisplayName(statType)} upgraded successfully. Level {oldLevel} -> {newLevel}, Value {oldValue} -> {newValue}."
        );

        FinishUpgradeAttempt(result);
        return true;
    }

    public int GetUpgradeCost(PlayerStatType statType)
    {
        if (playerStats == null)
        {
            ResolveReferences();
        }

        return playerStats == null ? -1 : playerStats.GetUpgradeCost(statType);
    }

    public bool CanBuyUpgrade(PlayerStatType statType)
    {
        if (playerStats == null || playerWallet == null)
        {
            ResolveReferences();
        }

        if (playerStats == null || playerWallet == null)
            return false;

        if (playerStats.IsUpgradeMaxed(statType))
            return false;

        int cost = playerStats.GetUpgradeCost(statType);

        if (cost <= 0)
            return false;

        return playerWallet.CanSpendCoins(cost);
    }

    private void FinishUpgradeAttempt(PlayerStatUpgradeResult result)
    {
        if (logUpgradeResults)
        {
            Debug.Log(
                $"Upgrade Result | " +
                $"Success: {result.Success} | " +
                $"Stat: {result.StatType} | " +
                $"Cost: {result.Cost} | " +
                $"Remaining Coins: {result.RemainingCoins} | " +
                $"Level: {result.OldLevel} -> {result.NewLevel} | " +
                $"Value: {result.OldValue} -> {result.NewValue} | " +
                $"Message: {result.Message}",
                this
            );
        }

        OnUpgradeAttemptFinished?.Invoke(result);
    }

    private string GetDisplayName(PlayerStatType statType)
    {
        switch (statType)
        {
            case PlayerStatType.AttackDamage:
                return "Attack Damage";

            case PlayerStatType.MaxHealth:
                return "Max Health";

            case PlayerStatType.Armor:
                return "Armor";

            default:
                return statType.ToString();
        }
    }

    [ContextMenu("Buy Attack Damage Upgrade")]
    private void DebugBuyAttackDamageUpgrade()
    {
        BuyAttackDamageUpgrade();
    }

    [ContextMenu("Buy Max Health Upgrade")]
    private void DebugBuyMaxHealthUpgrade()
    {
        BuyMaxHealthUpgrade();
    }

    [ContextMenu("Buy Armor Upgrade")]
    private void DebugBuyArmorUpgrade()
    {
        BuyArmorUpgrade();
    }

    [ContextMenu("Print Upgrade Info")]
    private void PrintUpgradeInfo()
    {
        ResolveReferences();

        if (playerStats == null || playerWallet == null)
        {
            Debug.LogWarning("Cannot print upgrade info. PlayerStats or PlayerWallet is missing.", this);
            return;
        }

        Debug.Log(
            $"Upgrade Info | " +
            $"Coins: {playerWallet.GetCoins()} | " +
            $"Attack Damage: {playerStats.AttackDamage} | Level: {playerStats.AttackDamageUpgradeLevel}/{playerStats.MaxAttackDamageUpgradeLevel} | Cost: {playerStats.GetAttackDamageUpgradeCost()} | " +
            $"Max Health: {playerStats.MaxHealth} | Level: {playerStats.MaxHealthUpgradeLevel}/{playerStats.MaxHealthUpgradeLevelLimit} | Cost: {playerStats.GetMaxHealthUpgradeCost()} | " +
            $"Armor: {playerStats.Armor} | Level: {playerStats.ArmorUpgradeLevel}/{playerStats.MaxArmorUpgradeLevel} | Cost: {playerStats.GetArmorUpgradeCost()}",
            this
        );
    }
}