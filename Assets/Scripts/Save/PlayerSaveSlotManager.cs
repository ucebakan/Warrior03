using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerSaveSlotManager : MonoBehaviour
{
    public static PlayerSaveSlotManager Instance { get; private set; }

    [Header("Slot Settings")]
    [SerializeField, Min(1)] private int slotCount = 3;
    [SerializeField, Min(1)] private int defaultSlotIndex = 1;
    [SerializeField] private string slotKeyPrefixFormat = "Save{0}";

    [Header("Startup Behaviour")]
    [SerializeField] private bool askForSaveSlotEveryLaunch = true;
    [SerializeField] private bool autoSelectLastUsedSlotIfNotAsking = true;
    [SerializeField] private bool pauseGameUntilSlotSelected = true;
    [SerializeField] private bool restorePreviousTimeScaleAfterSelection = true;

    [Header("New Game Behaviour")]
    [SerializeField] private bool clearSlotBeforeStartingNewGame = true;
    [SerializeField] private bool markSlotHasDataWhenStartingNewGame = true;
    [SerializeField] private bool movePlayerToSpawnOnNewGame = true;

    [Header("Dead Player Save Behaviour")]
    [SerializeField] private bool saveRespawnStateIfPlayerIsDead = true;

    [Header("Auto Save")]
    [SerializeField] private bool saveOnApplicationQuit = true;
    [SerializeField] private bool saveOnApplicationPause = true;
    [SerializeField] private bool saveOnApplicationFocusLost = false;
    [SerializeField] private bool saveBeforeChangingSlot = true;

    [Header("Player Position Save")]
    [SerializeField] private bool savePlayerPosition = true;
    [SerializeField] private bool loadPlayerPosition = true;
    [SerializeField] private bool savePlayerRotationY = true;
    [SerializeField] private bool onlyLoadPositionInSameScene = true;

    [Header("Player References")]
    [SerializeField] private Transform playerTransform;
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private Transform newGameSpawnPoint;
    [SerializeField] private string newGameSpawnPointName = "Player_Spawn_Point";

    [Header("Persistence")]
    [SerializeField] private bool rememberLastSelectedSlot = true;
    [SerializeField] private string lastSelectedSlotKey = "LastSelectedSaveSlot";
    [SerializeField] private bool dontDestroyOnLoad = false;

    [Header("Debug Keyboard Selection")]
    [SerializeField] private bool enableDebugKeyboardSelection = true;
    [SerializeField] private KeyCode selectSlot1Key = KeyCode.Alpha1;
    [SerializeField] private KeyCode selectSlot2Key = KeyCode.Alpha2;
    [SerializeField] private KeyCode selectSlot3Key = KeyCode.Alpha3;

    [Header("Debug")]
    [SerializeField] private bool logActions = true;

    private int selectedSlotIndex = -1;
    private float previousTimeScale = 1f;
    private bool waitingForSlotSelection;
    private bool isApplicationQuitting;

    public int SlotCount => slotCount;
    public int SelectedSlotIndex => selectedSlotIndex;
    public bool HasSelectedSlot => selectedSlotIndex >= 1 && selectedSlotIndex <= slotCount;
    public bool IsWaitingForSlotSelection => waitingForSlotSelection;

    public string SelectedSlotPrefix
    {
        get
        {
            if (!HasSelectedSlot)
            {
                return string.Format(slotKeyPrefixFormat, defaultSlotIndex);
            }

            return string.Format(slotKeyPrefixFormat, selectedSlotIndex);
        }
    }

    public event Action<int> OnSaveSlotSelected;
    public event Action<int> OnNewGameStartedInSlot;
    public event Action<int> OnSaveSlotContinued;
    public event Action<int> OnSaveSlotCleared;
    public event Action OnWaitingForSlotSelectionStarted;
    public event Action OnWaitingForSlotSelectionEnded;
    public event Action<int> OnGameSaved;

    private const string HasDataKey = "HasData";

    private const string PlayerHasPositionKey = "Player_HasPosition";
    private const string PlayerSceneNameKey = "Player_SceneName";
    private const string PlayerPositionXKey = "Player_Position_X";
    private const string PlayerPositionYKey = "Player_Position_Y";
    private const string PlayerPositionZKey = "Player_Position_Z";
    private const string PlayerRotationYKey = "Player_Rotation_Y";

    private const string PlayerHealthHasDataKey = "PlayerHealth_HasHealthData";
    private const string PlayerHealthCurrentHealthKey = "PlayerHealth_CurrentHealth";

    private const string PendingReloadActiveKey = "SaveSlot_PendingReload_Active";
    private const string PendingReloadSlotKey = "SaveSlot_PendingReload_Slot";
    private const string PendingReloadNewGameKey = "SaveSlot_PendingReload_NewGame";

    private void Awake()
    {
        SetupSingleton();
        NormalizeValues();
        ResolveSceneReferences();
        InitializeSlotSelection();
    }

    private void Start()
    {
        ResolveSceneReferences();

        if (HasSelectedSlot && loadPlayerPosition)
        {
            ApplySavedPlayerPositionIfAvailable();
        }
    }

    private void Update()
    {
        if (!enableDebugKeyboardSelection)
            return;

        if (!waitingForSlotSelection && HasSelectedSlot)
            return;

        if (Input.GetKeyDown(selectSlot1Key))
        {
            ContinueOrStartSlot(1);
        }

        if (Input.GetKeyDown(selectSlot2Key))
        {
            ContinueOrStartSlot(2);
        }

        if (Input.GetKeyDown(selectSlot3Key))
        {
            ContinueOrStartSlot(3);
        }
    }

    private void OnApplicationQuit()
    {
        isApplicationQuitting = true;

        if (saveOnApplicationQuit)
        {
            SaveCurrentGameState();
        }
    }

    private void OnApplicationPause(bool pauseStatus)
    {
        if (!saveOnApplicationPause)
            return;

        if (pauseStatus)
        {
            SaveCurrentGameState();
        }
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        if (!saveOnApplicationFocusLost)
            return;

        if (!hasFocus)
        {
            SaveCurrentGameState();
        }
    }

    private void OnDisable()
    {
        if (isApplicationQuitting && saveOnApplicationQuit)
        {
            SaveCurrentGameState();
        }
    }

    private void SetupSingleton()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (dontDestroyOnLoad)
        {
            DontDestroyOnLoad(gameObject);
        }
    }

    private void NormalizeValues()
    {
        slotCount = Mathf.Max(1, slotCount);
        defaultSlotIndex = Mathf.Clamp(defaultSlotIndex, 1, slotCount);

        if (string.IsNullOrWhiteSpace(slotKeyPrefixFormat))
        {
            slotKeyPrefixFormat = "Save{0}";
        }

        if (string.IsNullOrWhiteSpace(lastSelectedSlotKey))
        {
            lastSelectedSlotKey = "LastSelectedSaveSlot";
        }

        if (string.IsNullOrWhiteSpace(playerTag))
        {
            playerTag = "Player";
        }

        if (string.IsNullOrWhiteSpace(newGameSpawnPointName))
        {
            newGameSpawnPointName = "Player_Spawn_Point";
        }
    }

    private void ResolveSceneReferences()
    {
        if (playerTransform == null)
        {
            GameObject playerObject = GameObject.FindGameObjectWithTag(playerTag);

            if (playerObject != null)
            {
                playerTransform = playerObject.transform;
            }
        }

        if (newGameSpawnPoint == null && !string.IsNullOrWhiteSpace(newGameSpawnPointName))
        {
            GameObject spawnObject = GameObject.Find(newGameSpawnPointName);

            if (spawnObject != null)
            {
                newGameSpawnPoint = spawnObject.transform;
            }
        }
    }

    private void InitializeSlotSelection()
    {
        selectedSlotIndex = -1;

        if (TryApplyPendingReloadSlotRequest())
        {
            return;
        }

        if (askForSaveSlotEveryLaunch)
        {
            StartWaitingForSlotSelection();
            return;
        }

        if (autoSelectLastUsedSlotIfNotAsking && rememberLastSelectedSlot)
        {
            int lastSlot = PlayerPrefs.GetInt(lastSelectedSlotKey, defaultSlotIndex);
            ContinueOrStartSlot(lastSlot);
            return;
        }

        ContinueOrStartSlot(defaultSlotIndex);
    }

    private bool TryApplyPendingReloadSlotRequest()
    {
        int hasPendingRequest = PlayerPrefs.GetInt(PendingReloadActiveKey, 0);

        if (hasPendingRequest != 1)
            return false;

        int pendingSlot = PlayerPrefs.GetInt(PendingReloadSlotKey, defaultSlotIndex);
        bool pendingNewGame = PlayerPrefs.GetInt(PendingReloadNewGameKey, 0) == 1;

        ClearPendingReloadSlotRequest();

        if (pendingNewGame)
        {
            StartNewGameInSlot(pendingSlot);
        }
        else
        {
            ContinueSlot(pendingSlot);
        }

        if (logActions)
        {
            Debug.Log(
                $"Applied pending save slot reload request. Slot: {pendingSlot}, New Game: {pendingNewGame}",
                this
            );
        }

        return true;
    }

    public void PreparePendingReloadSlotRequest(int slotIndex, bool startNewGame)
    {
        int clampedSlotIndex = Mathf.Clamp(slotIndex, 1, slotCount);

        PlayerPrefs.SetInt(PendingReloadActiveKey, 1);
        PlayerPrefs.SetInt(PendingReloadSlotKey, clampedSlotIndex);
        PlayerPrefs.SetInt(PendingReloadNewGameKey, startNewGame ? 1 : 0);
        PlayerPrefs.Save();

        if (logActions)
        {
            Debug.Log(
                $"Prepared pending save slot reload request. Slot: {clampedSlotIndex}, New Game: {startNewGame}",
                this
            );
        }
    }

    public void ClearPendingReloadSlotRequest()
    {
        PlayerPrefs.DeleteKey(PendingReloadActiveKey);
        PlayerPrefs.DeleteKey(PendingReloadSlotKey);
        PlayerPrefs.DeleteKey(PendingReloadNewGameKey);
        PlayerPrefs.Save();
    }

    private void StartWaitingForSlotSelection()
    {
        waitingForSlotSelection = true;
        previousTimeScale = Time.timeScale;

        if (pauseGameUntilSlotSelected)
        {
            Time.timeScale = 0f;
        }

        OnWaitingForSlotSelectionStarted?.Invoke();

        if (logActions)
        {
            Debug.Log($"Waiting for save slot selection. Slot Count: {slotCount}", this);
        }
    }

    public void ContinueOrStartSlot(int slotIndex)
    {
        int clampedSlotIndex = Mathf.Clamp(slotIndex, 1, slotCount);

        if (HasSlotData(clampedSlotIndex))
        {
            ContinueSlot(clampedSlotIndex);
        }
        else
        {
            StartNewGameInSlot(clampedSlotIndex);
        }
    }

    public void ContinueSlot(int slotIndex)
    {
        int clampedSlotIndex = Mathf.Clamp(slotIndex, 1, slotCount);

        if (!HasSlotData(clampedSlotIndex))
        {
            StartNewGameInSlot(clampedSlotIndex);
            return;
        }

        if (saveBeforeChangingSlot && HasSelectedSlot && selectedSlotIndex != clampedSlotIndex)
        {
            SaveCurrentGameState();
        }

        SelectSlotInternal(clampedSlotIndex);

        if (loadPlayerPosition)
        {
            ApplySavedPlayerPositionIfAvailable();
        }

        OnSaveSlotContinued?.Invoke(clampedSlotIndex);

        if (logActions)
        {
            Debug.Log($"Continued Save {clampedSlotIndex}.", this);
        }
    }

    public void StartNewGameInSlot(int slotIndex)
    {
        int clampedSlotIndex = Mathf.Clamp(slotIndex, 1, slotCount);

        if (saveBeforeChangingSlot && HasSelectedSlot && selectedSlotIndex != clampedSlotIndex)
        {
            SaveCurrentGameState();
        }

        if (clearSlotBeforeStartingNewGame)
        {
            ClearSlotData(clampedSlotIndex);
        }

        SelectSlotInternal(clampedSlotIndex);

        if (movePlayerToSpawnOnNewGame)
        {
            MovePlayerToNewGameSpawnPoint();
        }

        PlayerHealth playerHealth = FindObjectOfType<PlayerHealth>();

        if (playerHealth != null)
        {
            playerHealth.RestoreFullHealth();
            playerHealth.SaveHealthStateAsFullHealth();
        }

        if (markSlotHasDataWhenStartingNewGame)
        {
            MarkSelectedSlotHasData();
        }

        SaveCurrentGameState();

        OnNewGameStartedInSlot?.Invoke(clampedSlotIndex);

        if (logActions)
        {
            Debug.Log($"Started New Game in Save {clampedSlotIndex}.", this);
        }
    }

    public void SelectSlot(int slotIndex)
    {
        ContinueOrStartSlot(slotIndex);
    }

    private void SelectSlotInternal(int slotIndex)
    {
        NormalizeValues();

        int clampedSlotIndex = Mathf.Clamp(slotIndex, 1, slotCount);
        selectedSlotIndex = clampedSlotIndex;

        if (rememberLastSelectedSlot)
        {
            PlayerPrefs.SetInt(lastSelectedSlotKey, selectedSlotIndex);
            PlayerPrefs.Save();
        }

        if (waitingForSlotSelection)
        {
            waitingForSlotSelection = false;

            if (pauseGameUntilSlotSelected && restorePreviousTimeScaleAfterSelection)
            {
                Time.timeScale = previousTimeScale;
            }

            OnWaitingForSlotSelectionEnded?.Invoke();
        }

        OnSaveSlotSelected?.Invoke(selectedSlotIndex);

        if (logActions)
        {
            Debug.Log($"Save Slot Selected: Save {selectedSlotIndex} | Prefix: {SelectedSlotPrefix}", this);
        }
    }

    public void SaveCurrentGameState()
    {
        if (!HasSelectedSlot)
            return;

        PlayerHealth playerHealth = FindObjectOfType<PlayerHealth>();
        bool playerIsDead = saveRespawnStateIfPlayerIsDead && playerHealth != null && playerHealth.IsDead;

        if (playerIsDead)
        {
            SaveRespawnPositionForDeadPlayer();

            if (playerHealth != null)
            {
                playerHealth.SaveHealthStateAsFullHealth();
            }

            if (logActions)
            {
                Debug.Log("Player was dead during save. Respawn position and full health were saved.", this);
            }
        }
        else
        {
            SavePlayerPosition();

            if (playerHealth != null)
            {
                playerHealth.SaveHealthState();
            }
        }

        PlayerStats playerStats = FindObjectOfType<PlayerStats>();

        if (playerStats != null)
        {
            playerStats.SaveUpgrades();
        }

        PlayerWallet playerWallet = FindObjectOfType<PlayerWallet>();

        if (playerWallet != null)
        {
            playerWallet.SaveCoins();
        }

        MarkSelectedSlotHasData();

        PlayerPrefs.Save();
        OnGameSaved?.Invoke(selectedSlotIndex);

        if (logActions)
        {
            Debug.Log($"Game saved for Save {selectedSlotIndex}.", this);
        }
    }

    private void SaveRespawnPositionForDeadPlayer()
    {
        if (!savePlayerPosition)
            return;

        if (!HasSelectedSlot)
            return;

        ResolveSceneReferences();

        if (newGameSpawnPoint != null)
        {
            SavePositionValue(newGameSpawnPoint.position, newGameSpawnPoint.rotation.eulerAngles.y);
            return;
        }

        SavePlayerPosition();
    }

    public void SavePlayerPosition()
    {
        if (!savePlayerPosition)
            return;

        if (!HasSelectedSlot)
            return;

        ResolveSceneReferences();

        if (playerTransform == null)
        {
            if (logActions)
            {
                Debug.LogWarning("PlayerSaveSlotManager: Player Transform bulunamadý. Position kaydedilmedi.", this);
            }

            return;
        }

        SavePositionValue(playerTransform.position, playerTransform.eulerAngles.y);
    }

    private void SavePositionValue(Vector3 position, float rotationY)
    {
        string sceneName = SceneManager.GetActiveScene().name;

        PlayerPrefs.SetInt(GetSelectedSlotKey(PlayerHasPositionKey), 1);
        PlayerPrefs.SetString(GetSelectedSlotKey(PlayerSceneNameKey), sceneName);
        PlayerPrefs.SetFloat(GetSelectedSlotKey(PlayerPositionXKey), position.x);
        PlayerPrefs.SetFloat(GetSelectedSlotKey(PlayerPositionYKey), position.y);
        PlayerPrefs.SetFloat(GetSelectedSlotKey(PlayerPositionZKey), position.z);
        PlayerPrefs.SetFloat(GetSelectedSlotKey(PlayerRotationYKey), rotationY);

        if (logActions)
        {
            Debug.Log($"Player position saved for Save {selectedSlotIndex}: {position}", this);
        }
    }

    public bool ApplySavedPlayerPositionIfAvailable()
    {
        if (!loadPlayerPosition)
            return false;

        if (!HasSelectedSlot)
            return false;

        ResolveSceneReferences();

        if (playerTransform == null)
        {
            if (logActions)
            {
                Debug.LogWarning("PlayerSaveSlotManager: Player Transform bulunamadý. Saved position uygulanmadý.", this);
            }

            return false;
        }

        bool hasSavedPosition = PlayerPrefs.GetInt(GetSelectedSlotKey(PlayerHasPositionKey), 0) == 1;

        if (!hasSavedPosition)
        {
            if (logActions)
            {
                Debug.Log($"Save {selectedSlotIndex} için kayýtlý player position yok.", this);
            }

            return false;
        }

        string savedSceneName = PlayerPrefs.GetString(GetSelectedSlotKey(PlayerSceneNameKey), "");
        string currentSceneName = SceneManager.GetActiveScene().name;

        if (onlyLoadPositionInSameScene && !string.IsNullOrWhiteSpace(savedSceneName) && savedSceneName != currentSceneName)
        {
            if (logActions)
            {
                Debug.LogWarning(
                    $"Saved position farklý sahneye ait. Saved Scene: {savedSceneName}, Current Scene: {currentSceneName}. Position uygulanmadý.",
                    this
                );
            }

            return false;
        }

        float x = PlayerPrefs.GetFloat(GetSelectedSlotKey(PlayerPositionXKey), playerTransform.position.x);
        float y = PlayerPrefs.GetFloat(GetSelectedSlotKey(PlayerPositionYKey), playerTransform.position.y);
        float z = PlayerPrefs.GetFloat(GetSelectedSlotKey(PlayerPositionZKey), playerTransform.position.z);
        float rotationY = PlayerPrefs.GetFloat(GetSelectedSlotKey(PlayerRotationYKey), playerTransform.eulerAngles.y);

        Vector3 savedPosition = new Vector3(x, y, z);
        Quaternion savedRotation = Quaternion.Euler(0f, rotationY, 0f);

        MovePlayerSafely(savedPosition, savePlayerRotationY ? savedRotation : playerTransform.rotation);

        if (logActions)
        {
            Debug.Log($"Player position loaded for Save {selectedSlotIndex}: {savedPosition}", this);
        }

        return true;
    }

    public void MovePlayerToNewGameSpawnPoint()
    {
        ResolveSceneReferences();

        if (playerTransform == null)
        {
            if (logActions)
            {
                Debug.LogWarning("PlayerSaveSlotManager: Player Transform bulunamadý. New Game spawn uygulanmadý.", this);
            }

            return;
        }

        if (newGameSpawnPoint == null)
        {
            if (logActions)
            {
                Debug.LogWarning("PlayerSaveSlotManager: New Game Spawn Point bulunamadý. Player mevcut konumda kalacak.", this);
            }

            return;
        }

        MovePlayerSafely(newGameSpawnPoint.position, newGameSpawnPoint.rotation);

        if (logActions)
        {
            Debug.Log($"Player moved to New Game spawn point: {newGameSpawnPoint.position}", this);
        }
    }

    private void MovePlayerSafely(Vector3 position, Quaternion rotation)
    {
        if (playerTransform == null)
            return;

        CharacterController characterController = playerTransform.GetComponent<CharacterController>();
        Rigidbody rb = playerTransform.GetComponent<Rigidbody>();

        bool characterControllerWasEnabled = false;

        if (characterController != null)
        {
            characterControllerWasEnabled = characterController.enabled;
            characterController.enabled = false;
        }

        if (rb != null)
        {
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        playerTransform.SetPositionAndRotation(position, rotation);

        if (characterController != null)
        {
            characterController.enabled = characterControllerWasEnabled;
        }
    }

    public void SelectSlot1()
    {
        ContinueOrStartSlot(1);
    }

    public void SelectSlot2()
    {
        ContinueOrStartSlot(2);
    }

    public void SelectSlot3()
    {
        ContinueOrStartSlot(3);
    }

    public void StartNewGameInSlot1()
    {
        StartNewGameInSlot(1);
    }

    public void StartNewGameInSlot2()
    {
        StartNewGameInSlot(2);
    }

    public void StartNewGameInSlot3()
    {
        StartNewGameInSlot(3);
    }

    public void ContinueSlot1()
    {
        ContinueSlot(1);
    }

    public void ContinueSlot2()
    {
        ContinueSlot(2);
    }

    public void ContinueSlot3()
    {
        ContinueSlot(3);
    }

    public string GetSlotPrefix(int slotIndex)
    {
        NormalizeValues();

        int clampedSlotIndex = Mathf.Clamp(slotIndex, 1, slotCount);
        return string.Format(slotKeyPrefixFormat, clampedSlotIndex);
    }

    public string GetSelectedSlotKey(string baseKey)
    {
        return GetSlotKey(selectedSlotIndex, baseKey);
    }

    public string GetSlotKey(int slotIndex, string baseKey)
    {
        NormalizeValues();

        if (string.IsNullOrWhiteSpace(baseKey))
        {
            baseKey = "UnknownKey";
        }

        int clampedSlotIndex = Mathf.Clamp(slotIndex, 1, slotCount);
        string prefix = GetSlotPrefix(clampedSlotIndex);

        return $"{prefix}_{baseKey}";
    }

    public bool HasSlotData(int slotIndex)
    {
        string markerKey = GetSlotKey(slotIndex, HasDataKey);
        return PlayerPrefs.GetInt(markerKey, 0) == 1;
    }

    public bool IsSlotEmpty(int slotIndex)
    {
        return !HasSlotData(slotIndex);
    }

    public void MarkSelectedSlotHasData()
    {
        if (!HasSelectedSlot)
            return;

        string markerKey = GetSelectedSlotKey(HasDataKey);
        PlayerPrefs.SetInt(markerKey, 1);
        PlayerPrefs.Save();
    }

    public void MarkSlotHasData(int slotIndex)
    {
        string markerKey = GetSlotKey(slotIndex, HasDataKey);
        PlayerPrefs.SetInt(markerKey, 1);
        PlayerPrefs.Save();
    }

    public void ClearSlotData(int slotIndex)
    {
        int clampedSlotIndex = Mathf.Clamp(slotIndex, 1, slotCount);
        string prefix = GetSlotPrefix(clampedSlotIndex);

        PlayerPrefs.DeleteKey($"{prefix}_{HasDataKey}");

        PlayerPrefs.DeleteKey($"{prefix}_PlayerStats_AttackDamageLevel");
        PlayerPrefs.DeleteKey($"{prefix}_PlayerStats_MaxHealthLevel");
        PlayerPrefs.DeleteKey($"{prefix}_PlayerStats_ArmorLevel");

        PlayerPrefs.DeleteKey($"{prefix}_PlayerWallet_Coins");

        PlayerPrefs.DeleteKey($"{prefix}_Quest_CurrentQuestIndex");
        PlayerPrefs.DeleteKey($"{prefix}_Quest_CurrentKillCount");
        PlayerPrefs.DeleteKey($"{prefix}_Quest_CurrentQuestCompleted");
        PlayerPrefs.DeleteKey($"{prefix}_Quest_AllQuestsCompleted");

        PlayerPrefs.DeleteKey($"{prefix}_{PlayerHasPositionKey}");
        PlayerPrefs.DeleteKey($"{prefix}_{PlayerSceneNameKey}");
        PlayerPrefs.DeleteKey($"{prefix}_{PlayerPositionXKey}");
        PlayerPrefs.DeleteKey($"{prefix}_{PlayerPositionYKey}");
        PlayerPrefs.DeleteKey($"{prefix}_{PlayerPositionZKey}");
        PlayerPrefs.DeleteKey($"{prefix}_{PlayerRotationYKey}");

        PlayerPrefs.DeleteKey($"{prefix}_{PlayerHealthHasDataKey}");
        PlayerPrefs.DeleteKey($"{prefix}_{PlayerHealthCurrentHealthKey}");

        PlayerPrefs.Save();

        OnSaveSlotCleared?.Invoke(clampedSlotIndex);

        if (logActions)
        {
            Debug.Log($"Save Slot {clampedSlotIndex} data cleared.", this);
        }
    }

    public void ClearSelectedSlotData()
    {
        if (!HasSelectedSlot)
            return;

        ClearSlotData(selectedSlotIndex);
    }

    public void ClearAllSlotData()
    {
        for (int i = 1; i <= slotCount; i++)
        {
            ClearSlotData(i);
        }

        if (rememberLastSelectedSlot)
        {
            PlayerPrefs.DeleteKey(lastSelectedSlotKey);
        }

        ClearPendingReloadSlotRequest();

        PlayerPrefs.Save();

        if (logActions)
        {
            Debug.Log("All save slot data cleared.", this);
        }
    }

    public string GetSlotDisplayName(int slotIndex)
    {
        int clampedSlotIndex = Mathf.Clamp(slotIndex, 1, slotCount);
        return $"Save {clampedSlotIndex}";
    }

    public string GetSlotStatusText(int slotIndex)
    {
        return HasSlotData(slotIndex) ? "Saved Game" : "Empty Slot";
    }

    public string GetSlotMainActionText(int slotIndex)
    {
        return HasSlotData(slotIndex) ? "CONTINUE" : "NEW GAME";
    }

    [ContextMenu("Debug Save Current Game State")]
    private void DebugSaveCurrentGameState()
    {
        SaveCurrentGameState();
    }

    [ContextMenu("Debug Load Player Position")]
    private void DebugLoadPlayerPosition()
    {
        ApplySavedPlayerPositionIfAvailable();
    }

    [ContextMenu("Debug Select Save 1")]
    private void DebugSelectSave1()
    {
        SelectSlot1();
    }

    [ContextMenu("Debug Select Save 2")]
    private void DebugSelectSave2()
    {
        SelectSlot2();
    }

    [ContextMenu("Debug Select Save 3")]
    private void DebugSelectSave3()
    {
        SelectSlot3();
    }

    [ContextMenu("Debug New Game Save 1")]
    private void DebugNewGameSave1()
    {
        StartNewGameInSlot1();
    }

    [ContextMenu("Debug New Game Save 2")]
    private void DebugNewGameSave2()
    {
        StartNewGameInSlot2();
    }

    [ContextMenu("Debug New Game Save 3")]
    private void DebugNewGameSave3()
    {
        StartNewGameInSlot3();
    }

    [ContextMenu("Debug Clear Selected Slot")]
    private void DebugClearSelectedSlot()
    {
        ClearSelectedSlotData();
    }

    [ContextMenu("Debug Clear All Save Slots")]
    private void DebugClearAllSaveSlots()
    {
        ClearAllSlotData();
    }

    [ContextMenu("Debug Print Current Save Slot")]
    private void DebugPrintCurrentSaveSlot()
    {
        Debug.Log(
            $"Current Save Slot | " +
            $"Has Selected Slot: {HasSelectedSlot} | " +
            $"Selected Slot: {selectedSlotIndex} | " +
            $"Prefix: {SelectedSlotPrefix}",
            this
        );
    }
}