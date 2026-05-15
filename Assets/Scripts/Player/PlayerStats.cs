using System;
using UnityEngine;

public enum PlayerStatType
{
    AttackDamage,
    MaxHealth,
    Armor
}

[Serializable]
public struct PlayerStatUpgradeResult
{
    public bool Success;
    public PlayerStatType StatType;
    public int Cost;
    public int RemainingCoins;
    public int OldLevel;
    public int NewLevel;
    public int OldValue;
    public int NewValue;
    public string Message;

    public PlayerStatUpgradeResult(
        bool success,
        PlayerStatType statType,
        int cost,
        int remainingCoins,
        int oldLevel,
        int newLevel,
        int oldValue,
        int newValue,
        string message
    )
    {
        Success = success;
        StatType = statType;
        Cost = cost;
        RemainingCoins = remainingCoins;
        OldLevel = oldLevel;
        NewLevel = newLevel;
        OldValue = oldValue;
        NewValue = newValue;
        Message = message;
    }
}

public class PlayerStats : MonoBehaviour
{
    [Header("Save Settings")]
    [SerializeField] private bool saveUpgrades = true;
    [SerializeField] private bool useSaveSlotManager = true;
    [SerializeField] private string saveKeyPrefix = "PlayerStats";

    [Header("Base Stats")]
    [SerializeField, Min(0)] private int baseAttackDamage = 10;
    [SerializeField, Min(1)] private int baseMaxHealth = 100;
    [SerializeField, Min(0)] private int baseArmor = 0;

    [Header("Attack Damage Upgrade")]
    [SerializeField, Min(0)] private int attackDamageUpgradeLevel = 0;
    [SerializeField, Min(1)] private int attackDamagePerUpgrade = 2;
    [SerializeField, Min(1)] private int attackDamageBaseCost = 25;
    [SerializeField, Min(1f)] private float attackDamageCostMultiplier = 1.35f;
    [SerializeField, Min(1)] private int maxAttackDamageUpgradeLevel = 50;

    [Header("Max Health Upgrade")]
    [SerializeField, Min(0)] private int maxHealthUpgradeLevel = 0;
    [SerializeField, Min(1)] private int maxHealthPerUpgrade = 10;
    [SerializeField, Min(1)] private int maxHealthBaseCost = 30;
    [SerializeField, Min(1f)] private float maxHealthCostMultiplier = 1.35f;
    [SerializeField, Min(1)] private int maxHealthUpgradeLevelLimit = 50;

    [Header("Armor Upgrade")]
    [SerializeField, Min(0)] private int armorUpgradeLevel = 0;
    [SerializeField, Min(1)] private int armorPerUpgrade = 2;
    [SerializeField, Min(1)] private int armorBaseCost = 35;
    [SerializeField, Min(1f)] private float armorCostMultiplier = 1.35f;
    [SerializeField, Min(1)] private int maxArmorUpgradeLevel = 50;

    [Header("Debug")]
    [SerializeField] private bool logSaveLoad = true;

    private PlayerSaveSlotManager saveSlotManager;
    private bool hasLoadedForCurrentSlot;

    public event Action OnStatsChanged;

    public int BaseAttackDamage => baseAttackDamage;
    public int BaseMaxHealth => baseMaxHealth;
    public int BaseArmor => baseArmor;

    public int AttackDamage => Mathf.Max(0, baseAttackDamage + (attackDamageUpgradeLevel * attackDamagePerUpgrade));
    public int MaxHealth => Mathf.Max(1, baseMaxHealth + (maxHealthUpgradeLevel * maxHealthPerUpgrade));
    public int Armor => Mathf.Max(0, baseArmor + (armorUpgradeLevel * armorPerUpgrade));

    public int AttackDamageUpgradeLevel => attackDamageUpgradeLevel;
    public int MaxHealthUpgradeLevel => maxHealthUpgradeLevel;
    public int ArmorUpgradeLevel => armorUpgradeLevel;

    public int MaxAttackDamageUpgradeLevel => maxAttackDamageUpgradeLevel;
    public int MaxHealthUpgradeLevelLimit => maxHealthUpgradeLevelLimit;
    public int MaxArmorUpgradeLevel => maxArmorUpgradeLevel;

    public bool IsAttackDamageUpgradeMaxed => attackDamageUpgradeLevel >= maxAttackDamageUpgradeLevel;
    public bool IsMaxHealthUpgradeMaxed => maxHealthUpgradeLevel >= maxHealthUpgradeLevelLimit;
    public bool IsArmorUpgradeMaxed => armorUpgradeLevel >= maxArmorUpgradeLevel;

    private string AttackDamageLevelBaseKey => $"{saveKeyPrefix}_AttackDamageLevel";
    private string MaxHealthLevelBaseKey => $"{saveKeyPrefix}_MaxHealthLevel";
    private string ArmorLevelBaseKey => $"{saveKeyPrefix}_ArmorLevel";

    private string AttackDamageLevelKey => GetSaveKey(AttackDamageLevelBaseKey);
    private string MaxHealthLevelKey => GetSaveKey(MaxHealthLevelBaseKey);
    private string ArmorLevelKey => GetSaveKey(ArmorLevelBaseKey);

    private void Awake()
    {
        NormalizeValues();
        ResolveSaveSlotManager();
    }

    private void OnEnable()
    {
        ResolveSaveSlotManager();
        SubscribeToSaveSlotManager();

        TryLoadForCurrentSaveSlot();
    }

    private void Start()
    {
        TryLoadForCurrentSaveSlot();
        NotifyStatsChanged();
    }

    private void OnDisable()
    {
        UnsubscribeFromSaveSlotManager();
    }

    private void OnValidate()
    {
        NormalizeValues();

        if (Application.isPlaying)
        {
            NotifyStatsChanged();
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
        LoadUpgrades();
        hasLoadedForCurrentSlot = true;
        NotifyStatsChanged();

        if (logSaveLoad)
        {
            Debug.Log($"PlayerStats loaded for Save {slotIndex}.", this);
        }
    }

    private void TryLoadForCurrentSaveSlot()
    {
        if (!saveUpgrades)
            return;

        if (hasLoadedForCurrentSlot)
            return;

        if (!useSaveSlotManager)
        {
            LoadUpgrades();
            hasLoadedForCurrentSlot = true;
            return;
        }

        ResolveSaveSlotManager();

        if (saveSlotManager == null)
        {
            LoadUpgrades();
            hasLoadedForCurrentSlot = true;

            if (logSaveLoad)
            {
                Debug.LogWarning(
                    "PlayerStats could not find PlayerSaveSlotManager. Falling back to non-slot save keys.",
                    this
                );
            }

            return;
        }

        if (!saveSlotManager.HasSelectedSlot)
        {
            ResetRuntimeUpgradeLevels();

            if (logSaveLoad)
            {
                Debug.Log("PlayerStats is waiting for save slot selection.", this);
            }

            return;
        }

        LoadUpgrades();
        hasLoadedForCurrentSlot = true;
    }

    private void ResetRuntimeUpgradeLevels()
    {
        attackDamageUpgradeLevel = 0;
        maxHealthUpgradeLevel = 0;
        armorUpgradeLevel = 0;

        NormalizeValues();
        NotifyStatsChanged();
    }

    private void NormalizeValues()
    {
        baseAttackDamage = Mathf.Max(0, baseAttackDamage);
        baseMaxHealth = Mathf.Max(1, baseMaxHealth);
        baseArmor = Mathf.Max(0, baseArmor);

        attackDamageUpgradeLevel = Mathf.Max(0, attackDamageUpgradeLevel);
        maxHealthUpgradeLevel = Mathf.Max(0, maxHealthUpgradeLevel);
        armorUpgradeLevel = Mathf.Max(0, armorUpgradeLevel);

        attackDamagePerUpgrade = Mathf.Max(1, attackDamagePerUpgrade);
        maxHealthPerUpgrade = Mathf.Max(1, maxHealthPerUpgrade);
        armorPerUpgrade = Mathf.Max(1, armorPerUpgrade);

        attackDamageBaseCost = Mathf.Max(1, attackDamageBaseCost);
        maxHealthBaseCost = Mathf.Max(1, maxHealthBaseCost);
        armorBaseCost = Mathf.Max(1, armorBaseCost);

        attackDamageCostMultiplier = Mathf.Max(1f, attackDamageCostMultiplier);
        maxHealthCostMultiplier = Mathf.Max(1f, maxHealthCostMultiplier);
        armorCostMultiplier = Mathf.Max(1f, armorCostMultiplier);

        maxAttackDamageUpgradeLevel = Mathf.Max(1, maxAttackDamageUpgradeLevel);
        maxHealthUpgradeLevelLimit = Mathf.Max(1, maxHealthUpgradeLevelLimit);
        maxArmorUpgradeLevel = Mathf.Max(1, maxArmorUpgradeLevel);

        attackDamageUpgradeLevel = Mathf.Min(attackDamageUpgradeLevel, maxAttackDamageUpgradeLevel);
        maxHealthUpgradeLevel = Mathf.Min(maxHealthUpgradeLevel, maxHealthUpgradeLevelLimit);
        armorUpgradeLevel = Mathf.Min(armorUpgradeLevel, maxArmorUpgradeLevel);
    }

    private string GetSaveKey(string baseKey)
    {
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

    public int GetUpgradeCost(PlayerStatType statType)
    {
        switch (statType)
        {
            case PlayerStatType.AttackDamage:
                return GetAttackDamageUpgradeCost();

            case PlayerStatType.MaxHealth:
                return GetMaxHealthUpgradeCost();

            case PlayerStatType.Armor:
                return GetArmorUpgradeCost();

            default:
                return -1;
        }
    }

    public int GetAttackDamageUpgradeCost()
    {
        if (IsAttackDamageUpgradeMaxed)
            return -1;

        return CalculateUpgradeCost(attackDamageBaseCost, attackDamageCostMultiplier, attackDamageUpgradeLevel);
    }

    public int GetMaxHealthUpgradeCost()
    {
        if (IsMaxHealthUpgradeMaxed)
            return -1;

        return CalculateUpgradeCost(maxHealthBaseCost, maxHealthCostMultiplier, maxHealthUpgradeLevel);
    }

    public int GetArmorUpgradeCost()
    {
        if (IsArmorUpgradeMaxed)
            return -1;

        return CalculateUpgradeCost(armorBaseCost, armorCostMultiplier, armorUpgradeLevel);
    }

    private int CalculateUpgradeCost(int baseCost, float multiplier, int currentLevel)
    {
        float rawCost = baseCost * Mathf.Pow(multiplier, currentLevel);
        int roundedCost = Mathf.RoundToInt(rawCost);

        return Mathf.Max(1, roundedCost);
    }

    public bool IsUpgradeMaxed(PlayerStatType statType)
    {
        switch (statType)
        {
            case PlayerStatType.AttackDamage:
                return IsAttackDamageUpgradeMaxed;

            case PlayerStatType.MaxHealth:
                return IsMaxHealthUpgradeMaxed;

            case PlayerStatType.Armor:
                return IsArmorUpgradeMaxed;

            default:
                return true;
        }
    }

    public int GetUpgradeLevel(PlayerStatType statType)
    {
        switch (statType)
        {
            case PlayerStatType.AttackDamage:
                return attackDamageUpgradeLevel;

            case PlayerStatType.MaxHealth:
                return maxHealthUpgradeLevel;

            case PlayerStatType.Armor:
                return armorUpgradeLevel;

            default:
                return 0;
        }
    }

    public int GetUpgradeLevelLimit(PlayerStatType statType)
    {
        switch (statType)
        {
            case PlayerStatType.AttackDamage:
                return maxAttackDamageUpgradeLevel;

            case PlayerStatType.MaxHealth:
                return maxHealthUpgradeLevelLimit;

            case PlayerStatType.Armor:
                return maxArmorUpgradeLevel;

            default:
                return 0;
        }
    }

    public int GetCurrentStatValue(PlayerStatType statType)
    {
        switch (statType)
        {
            case PlayerStatType.AttackDamage:
                return AttackDamage;

            case PlayerStatType.MaxHealth:
                return MaxHealth;

            case PlayerStatType.Armor:
                return Armor;

            default:
                return 0;
        }
    }

    public int GetStatValueAfterNextUpgrade(PlayerStatType statType)
    {
        if (IsUpgradeMaxed(statType))
        {
            return GetCurrentStatValue(statType);
        }

        switch (statType)
        {
            case PlayerStatType.AttackDamage:
                return Mathf.Max(0, baseAttackDamage + ((attackDamageUpgradeLevel + 1) * attackDamagePerUpgrade));

            case PlayerStatType.MaxHealth:
                return Mathf.Max(1, baseMaxHealth + ((maxHealthUpgradeLevel + 1) * maxHealthPerUpgrade));

            case PlayerStatType.Armor:
                return Mathf.Max(0, baseArmor + ((armorUpgradeLevel + 1) * armorPerUpgrade));

            default:
                return GetCurrentStatValue(statType);
        }
    }

    public bool CanAffordUpgrade(PlayerStatType statType, int currentCoins)
    {
        int cost = GetUpgradeCost(statType);

        if (cost < 0)
            return false;

        return currentCoins >= cost;
    }

    public bool ApplyUpgrade(PlayerStatType statType)
    {
        if (useSaveSlotManager)
        {
            ResolveSaveSlotManager();

            if (saveSlotManager != null && !saveSlotManager.HasSelectedSlot)
            {
                Debug.LogWarning("Cannot apply stat upgrade before selecting a save slot.", this);
                return false;
            }
        }

        switch (statType)
        {
            case PlayerStatType.AttackDamage:
                return ApplyAttackDamageUpgrade();

            case PlayerStatType.MaxHealth:
                return ApplyMaxHealthUpgrade();

            case PlayerStatType.Armor:
                return ApplyArmorUpgrade();

            default:
                return false;
        }
    }

    public bool ApplyAttackDamageUpgrade()
    {
        if (IsAttackDamageUpgradeMaxed)
            return false;

        attackDamageUpgradeLevel++;
        SaveUpgradesIfNeeded();
        NotifyStatsChanged();

        return true;
    }

    public bool ApplyMaxHealthUpgrade()
    {
        if (IsMaxHealthUpgradeMaxed)
            return false;

        maxHealthUpgradeLevel++;
        SaveUpgradesIfNeeded();
        NotifyStatsChanged();

        return true;
    }

    public bool ApplyArmorUpgrade()
    {
        if (IsArmorUpgradeMaxed)
            return false;

        armorUpgradeLevel++;
        SaveUpgradesIfNeeded();
        NotifyStatsChanged();

        return true;
    }

    public int CalculateDamageAfterArmor(int incomingDamage)
    {
        int safeIncomingDamage = Mathf.Max(0, incomingDamage);

        if (safeIncomingDamage <= 0)
            return 0;

        float armorMultiplier = 100f / (100f + Armor);
        int finalDamage = Mathf.RoundToInt(safeIncomingDamage * armorMultiplier);

        return Mathf.Max(1, finalDamage);
    }

    public void ResetUpgrades()
    {
        attackDamageUpgradeLevel = 0;
        maxHealthUpgradeLevel = 0;
        armorUpgradeLevel = 0;

        SaveUpgradesIfNeeded();
        NotifyStatsChanged();
    }

    private void SaveUpgradesIfNeeded()
    {
        if (!saveUpgrades)
            return;

        SaveUpgrades();
    }

    public void SaveUpgrades()
    {
        if (useSaveSlotManager)
        {
            ResolveSaveSlotManager();

            if (saveSlotManager != null && !saveSlotManager.HasSelectedSlot)
            {
                Debug.LogWarning("PlayerStats cannot save because no save slot is selected.", this);
                return;
            }
        }

        PlayerPrefs.SetInt(AttackDamageLevelKey, attackDamageUpgradeLevel);
        PlayerPrefs.SetInt(MaxHealthLevelKey, maxHealthUpgradeLevel);
        PlayerPrefs.SetInt(ArmorLevelKey, armorUpgradeLevel);

        if (useSaveSlotManager && saveSlotManager != null && saveSlotManager.HasSelectedSlot)
        {
            saveSlotManager.MarkSelectedSlotHasData();
        }

        PlayerPrefs.Save();

        if (logSaveLoad)
        {
            Debug.Log(
                $"PlayerStats saved. " +
                $"Attack Level: {attackDamageUpgradeLevel}, " +
                $"Health Level: {maxHealthUpgradeLevel}, " +
                $"Armor Level: {armorUpgradeLevel}",
                this
            );
        }
    }

    public void LoadUpgrades()
    {
        attackDamageUpgradeLevel = PlayerPrefs.GetInt(AttackDamageLevelKey, 0);
        maxHealthUpgradeLevel = PlayerPrefs.GetInt(MaxHealthLevelKey, 0);
        armorUpgradeLevel = PlayerPrefs.GetInt(ArmorLevelKey, 0);

        NormalizeValues();
        NotifyStatsChanged();

        if (logSaveLoad)
        {
            Debug.Log(
                $"PlayerStats loaded. " +
                $"Attack Level: {attackDamageUpgradeLevel}, " +
                $"Health Level: {maxHealthUpgradeLevel}, " +
                $"Armor Level: {armorUpgradeLevel}",
                this
            );
        }
    }

    public void ForceReloadFromCurrentSlot()
    {
        hasLoadedForCurrentSlot = false;
        TryLoadForCurrentSaveSlot();
    }

    private void NotifyStatsChanged()
    {
        OnStatsChanged?.Invoke();
    }

    [ContextMenu("Debug Save Upgrades")]
    private void DebugSaveUpgrades()
    {
        SaveUpgrades();
    }

    [ContextMenu("Debug Load Upgrades")]
    private void DebugLoadUpgrades()
    {
        LoadUpgrades();
    }

    [ContextMenu("Debug Print Current Stats")]
    private void DebugPrintCurrentStats()
    {
        Debug.Log(
            $"Player Stats | " +
            $"Attack Damage: {AttackDamage} | " +
            $"Max Health: {MaxHealth} | " +
            $"Armor: {Armor} | " +
            $"Attack Level: {attackDamageUpgradeLevel}/{maxAttackDamageUpgradeLevel} | " +
            $"Health Level: {maxHealthUpgradeLevel}/{maxHealthUpgradeLevelLimit} | " +
            $"Armor Level: {armorUpgradeLevel}/{maxArmorUpgradeLevel} | " +
            $"Attack Cost: {GetAttackDamageUpgradeCost()} | " +
            $"Health Cost: {GetMaxHealthUpgradeCost()} | " +
            $"Armor Cost: {GetArmorUpgradeCost()} | " +
            $"Attack Key: {AttackDamageLevelKey} | " +
            $"Health Key: {MaxHealthLevelKey} | " +
            $"Armor Key: {ArmorLevelKey}",
            this
        );
    }

    [ContextMenu("Reset Player Stat Upgrades")]
    private void DebugResetUpgrades()
    {
        ResetUpgrades();
        Debug.Log("Player stat upgrades reset.", this);
    }
}