using System;
using System.Collections;
using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerStats playerStats;

    [Header("Health Upgrade Behaviour")]
    [SerializeField] private bool addNewMaxHealthToCurrentHealth = true;

    [Header("Health Save Settings")]
    [SerializeField] private bool saveHealth = true;
    [SerializeField] private bool useSaveSlotManager = true;
    [SerializeField] private string saveKeyPrefix = "PlayerHealth";
    [SerializeField] private bool loadHealthAfterSlotSelected = true;
    [SerializeField] private bool saveOnApplicationQuit = true;
    [SerializeField] private bool saveOnApplicationPause = true;
    [SerializeField] private bool saveOnEveryHealthChange = false;

    [Header("Runtime Values")]
    [SerializeField] private int maxHealth;
    [SerializeField] private int currentHealth;

    [Header("Debug")]
    [SerializeField] private bool enableDebugDamageKey = true;
    [SerializeField] private KeyCode debugDamageKey = KeyCode.H;
    [SerializeField] private int debugDamageAmount = 1;
    [SerializeField] private bool logSaveLoad = true;

    private PlayerHpBarUI hpBarUI;
    private PlayerSaveSlotManager saveSlotManager;

    private Coroutine invulnerabilityRoutine;
    private Coroutine delayedLoadRoutine;

    private bool isDead;
    private bool isInvulnerable;
    private bool hasLoadedHealthForCurrentSlot;
    private bool isApplicationQuitting;

    public int MaxHealth => maxHealth;
    public int CurrentHealth => currentHealth;
    public float HealthPercent => maxHealth <= 0 ? 0f : (float)currentHealth / maxHealth;
    public bool IsDead => isDead;
    public bool IsInvulnerable => isInvulnerable;

    public event Action OnDied;
    public event Action<int, int> OnHealthChanged;

    private string HasHealthDataBaseKey => $"{saveKeyPrefix}_HasHealthData";
    private string CurrentHealthBaseKey => $"{saveKeyPrefix}_CurrentHealth";

    private string HasHealthDataKey => GetSaveKey(HasHealthDataBaseKey);
    private string CurrentHealthKey => GetSaveKey(CurrentHealthBaseKey);

    private void Awake()
    {
        ResolveReferences();

        maxHealth = GetCurrentStatsMaxHealth();
        currentHealth = maxHealth;
        isDead = false;

        UpdateHpBar();
    }

    private void OnEnable()
    {
        ResolveReferences();
        SubscribeEvents();
        TryLoadHealthForCurrentSlotDelayed();
    }

    private void Start()
    {
        RefreshHealthFromStats(false);
        TryLoadHealthForCurrentSlotDelayed();
        UpdateHpBar();
    }

    private void OnDisable()
    {
        UnsubscribeEvents();

        if (isApplicationQuitting && saveOnApplicationQuit)
        {
            SaveHealthState();
        }
    }

    private void OnApplicationQuit()
    {
        isApplicationQuitting = true;

        if (saveOnApplicationQuit)
        {
            SaveHealthState();
        }
    }

    private void OnApplicationPause(bool pauseStatus)
    {
        if (!saveOnApplicationPause)
            return;

        if (pauseStatus)
        {
            SaveHealthState();
        }
    }

    private void Update()
    {
        if (enableDebugDamageKey && Input.GetKeyDown(debugDamageKey))
        {
            TakeDamage(debugDamageAmount);
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

        if (hpBarUI == null)
        {
            hpBarUI = FindObjectOfType<PlayerHpBarUI>();
        }

        if (useSaveSlotManager && saveSlotManager == null)
        {
            saveSlotManager = PlayerSaveSlotManager.Instance;
        }

        if (useSaveSlotManager && saveSlotManager == null)
        {
            saveSlotManager = FindObjectOfType<PlayerSaveSlotManager>();
        }
    }

    private void SubscribeEvents()
    {
        if (playerStats != null)
        {
            playerStats.OnStatsChanged -= HandleStatsChanged;
            playerStats.OnStatsChanged += HandleStatsChanged;
        }

        if (saveSlotManager != null)
        {
            saveSlotManager.OnSaveSlotSelected -= HandleSaveSlotSelected;
            saveSlotManager.OnNewGameStartedInSlot -= HandleNewGameStartedInSlot;
            saveSlotManager.OnGameSaved -= HandleGameSaved;

            saveSlotManager.OnSaveSlotSelected += HandleSaveSlotSelected;
            saveSlotManager.OnNewGameStartedInSlot += HandleNewGameStartedInSlot;
            saveSlotManager.OnGameSaved += HandleGameSaved;
        }
    }

    private void UnsubscribeEvents()
    {
        if (playerStats != null)
        {
            playerStats.OnStatsChanged -= HandleStatsChanged;
        }

        if (saveSlotManager != null)
        {
            saveSlotManager.OnSaveSlotSelected -= HandleSaveSlotSelected;
            saveSlotManager.OnNewGameStartedInSlot -= HandleNewGameStartedInSlot;
            saveSlotManager.OnGameSaved -= HandleGameSaved;
        }
    }

    private void HandleSaveSlotSelected(int slotIndex)
    {
        if (!loadHealthAfterSlotSelected)
            return;

        hasLoadedHealthForCurrentSlot = false;
        TryLoadHealthForCurrentSlotDelayed();

        if (logSaveLoad)
        {
            Debug.Log($"PlayerHealth waiting to load health for Save {slotIndex}.", this);
        }
    }

    private void HandleNewGameStartedInSlot(int slotIndex)
    {
        RestoreFullHealth();
        SaveHealthStateAsFullHealth();

        if (logSaveLoad)
        {
            Debug.Log($"PlayerHealth reset to full health for New Game in Save {slotIndex}.", this);
        }
    }

    private void HandleGameSaved(int slotIndex)
    {
        SaveHealthState();
    }

    private int GetCurrentStatsMaxHealth()
    {
        if (playerStats == null)
        {
            Debug.LogWarning("PlayerHealth could not find PlayerStats. Using fallback Max Health value of 1.", this);
            return 1;
        }

        return Mathf.Max(1, playerStats.MaxHealth);
    }

    private void HandleStatsChanged()
    {
        int oldMaxHealth = maxHealth;
        int newMaxHealth = GetCurrentStatsMaxHealth();

        if (newMaxHealth == oldMaxHealth)
        {
            UpdateHpBar();
            return;
        }

        maxHealth = newMaxHealth;

        if (isDead)
        {
            currentHealth = 0;
        }
        else if (newMaxHealth > oldMaxHealth)
        {
            int gainedMaxHealth = newMaxHealth - oldMaxHealth;

            if (addNewMaxHealthToCurrentHealth)
            {
                currentHealth += gainedMaxHealth;
            }

            currentHealth = Mathf.Clamp(currentHealth, 1, maxHealth);
        }
        else
        {
            currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);
            isDead = currentHealth <= 0;
        }

        UpdateHpBar();

        if (saveOnEveryHealthChange)
        {
            SaveHealthState();
        }
    }

    public void TakeDamage(int damageAmount)
    {
        if (damageAmount <= 0)
            return;

        if (isDead)
            return;

        if (isInvulnerable)
            return;

        int finalDamage = CalculateFinalIncomingDamage(damageAmount);

        if (finalDamage <= 0)
            return;

        currentHealth -= finalDamage;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);

        UpdateHpBar();

        if (saveOnEveryHealthChange)
        {
            SaveHealthState();
        }

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    private int CalculateFinalIncomingDamage(int rawDamage)
    {
        if (playerStats == null)
        {
            ResolveReferences();
        }

        if (playerStats == null)
        {
            return Mathf.Max(1, rawDamage);
        }

        return playerStats.CalculateDamageAfterArmor(rawDamage);
    }

    public void Heal(int healAmount)
    {
        if (healAmount <= 0)
            return;

        if (isDead)
            return;

        currentHealth += healAmount;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);

        UpdateHpBar();

        if (saveOnEveryHealthChange)
        {
            SaveHealthState();
        }
    }

    public void RestoreFullHealth()
    {
        maxHealth = GetCurrentStatsMaxHealth();
        isDead = false;
        currentHealth = maxHealth;
        UpdateHpBar();

        if (saveOnEveryHealthChange)
        {
            SaveHealthState();
        }
    }

    public void RefreshHealthFromStats(bool refillHealth)
    {
        int newMaxHealth = GetCurrentStatsMaxHealth();
        maxHealth = newMaxHealth;

        if (refillHealth)
        {
            currentHealth = maxHealth;
            isDead = false;
        }
        else
        {
            currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);
            isDead = currentHealth <= 0;
        }

        UpdateHpBar();
    }

    public void SetMaxHealth(int newMaxHealth, bool refillHealth = true)
    {
        if (playerStats != null)
        {
            Debug.LogWarning(
                "PlayerHealth.SetMaxHealth was called, but PlayerStats is now the source of Max Health. Refreshing health from PlayerStats instead.",
                this
            );

            RefreshHealthFromStats(refillHealth);
            return;
        }

        maxHealth = Mathf.Max(1, newMaxHealth);

        if (refillHealth)
        {
            currentHealth = maxHealth;
            isDead = false;
        }
        else
        {
            currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);
            isDead = currentHealth <= 0;
        }

        UpdateHpBar();
    }

    private void TryLoadHealthForCurrentSlotDelayed()
    {
        if (!saveHealth)
            return;

        if (hasLoadedHealthForCurrentSlot)
            return;

        if (delayedLoadRoutine != null)
        {
            StopCoroutine(delayedLoadRoutine);
            delayedLoadRoutine = null;
        }

        delayedLoadRoutine = StartCoroutine(DelayedLoadHealthRoutine());
    }

    private IEnumerator DelayedLoadHealthRoutine()
    {
        yield return null;

        LoadHealthState();
        delayedLoadRoutine = null;
    }

    public void SaveHealthState()
    {
        if (!saveHealth)
            return;

        if (isDead)
        {
            SaveHealthStateAsFullHealth();

            if (logSaveLoad)
            {
                Debug.Log("PlayerHealth was dead during save. Full health was saved for next load.", this);
            }

            return;
        }

        SaveHealthValue(currentHealth);
    }

    public void SaveHealthStateAsFullHealth()
    {
        if (!saveHealth)
            return;

        maxHealth = GetCurrentStatsMaxHealth();
        SaveHealthValue(maxHealth);
    }

    private void SaveHealthValue(int healthValue)
    {
        if (useSaveSlotManager)
        {
            ResolveReferences();

            if (saveSlotManager != null && !saveSlotManager.HasSelectedSlot)
            {
                if (logSaveLoad)
                {
                    Debug.Log("PlayerHealth cannot save because no save slot is selected.", this);
                }

                return;
            }
        }

        maxHealth = GetCurrentStatsMaxHealth();
        int healthToSave = Mathf.Clamp(healthValue, 0, maxHealth);

        PlayerPrefs.SetInt(HasHealthDataKey, 1);
        PlayerPrefs.SetInt(CurrentHealthKey, healthToSave);

        if (useSaveSlotManager && saveSlotManager != null && saveSlotManager.HasSelectedSlot)
        {
            saveSlotManager.MarkSelectedSlotHasData();
        }

        PlayerPrefs.Save();

        if (logSaveLoad)
        {
            Debug.Log($"PlayerHealth saved. Current Health: {healthToSave} | Key: {CurrentHealthKey}", this);
        }
    }

    public bool LoadHealthState()
    {
        if (!saveHealth)
            return false;

        if (useSaveSlotManager)
        {
            ResolveReferences();

            if (saveSlotManager != null && !saveSlotManager.HasSelectedSlot)
            {
                if (logSaveLoad)
                {
                    Debug.Log("PlayerHealth is waiting for save slot selection.", this);
                }

                return false;
            }
        }

        maxHealth = GetCurrentStatsMaxHealth();

        bool hasHealthData = PlayerPrefs.GetInt(HasHealthDataKey, 0) == 1;

        if (!hasHealthData)
        {
            currentHealth = maxHealth;
            isDead = false;
            hasLoadedHealthForCurrentSlot = true;
            UpdateHpBar();

            if (logSaveLoad)
            {
                Debug.Log("PlayerHealth found no saved health. Starting with full health.", this);
            }

            return false;
        }

        int loadedHealth = PlayerPrefs.GetInt(CurrentHealthKey, maxHealth);

        currentHealth = Mathf.Clamp(loadedHealth, 1, maxHealth);
        isDead = false;

        hasLoadedHealthForCurrentSlot = true;
        UpdateHpBar();

        if (logSaveLoad)
        {
            Debug.Log($"PlayerHealth loaded. Current Health: {currentHealth}/{maxHealth} | Key: {CurrentHealthKey}", this);
        }

        return true;
    }

    private string GetSaveKey(string baseKey)
    {
        if (string.IsNullOrWhiteSpace(baseKey))
        {
            baseKey = "PlayerHealth_UnknownKey";
        }

        if (!useSaveSlotManager)
        {
            return baseKey;
        }

        ResolveReferences();

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

    public void SetTemporaryInvulnerability(float duration)
    {
        if (invulnerabilityRoutine != null)
        {
            StopCoroutine(invulnerabilityRoutine);
            invulnerabilityRoutine = null;
        }

        if (duration <= 0f)
        {
            isInvulnerable = false;
            return;
        }

        invulnerabilityRoutine = StartCoroutine(InvulnerabilityRoutine(duration));
    }

    private IEnumerator InvulnerabilityRoutine(float duration)
    {
        isInvulnerable = true;

        yield return new WaitForSeconds(duration);

        isInvulnerable = false;
        invulnerabilityRoutine = null;
    }

    private void UpdateHpBar()
    {
        if (hpBarUI == null)
        {
            hpBarUI = FindObjectOfType<PlayerHpBarUI>();
        }

        if (hpBarUI != null)
        {
            hpBarUI.UpdateBar(currentHealth, maxHealth);
        }

        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    private void Die()
    {
        if (isDead)
            return;

        isDead = true;
        currentHealth = 0;

        UpdateHpBar();

        Debug.Log("Player died.");
        OnDied?.Invoke();
    }

    [ContextMenu("Debug Save Health")]
    private void DebugSaveHealth()
    {
        SaveHealthState();
    }

    [ContextMenu("Debug Save Full Health")]
    private void DebugSaveFullHealth()
    {
        SaveHealthStateAsFullHealth();
    }

    [ContextMenu("Debug Load Health")]
    private void DebugLoadHealth()
    {
        LoadHealthState();
    }

    [ContextMenu("Debug Restore Full Health")]
    private void DebugRestoreFullHealth()
    {
        RestoreFullHealth();
    }
}