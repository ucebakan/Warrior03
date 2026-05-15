using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public enum EnemyQuestType
{
    None,
    SmallLog,
    MediumLog,
    BossLog
}

[System.Serializable]
public class KillQuestDefinition
{
    public string questTitle = "New Quest";
    public EnemyQuestType targetEnemyType = EnemyQuestType.SmallLog;
    public int requiredKillCount = 5;
    public int completionCoinReward = 50;
}

public class QuestManager : MonoBehaviour
{
    public static QuestManager Instance { get; private set; }

    [Header("Quest Chain")]
    [SerializeField] private List<KillQuestDefinition> questChain = new List<KillQuestDefinition>();

    [Header("Quest Flow")]
    [SerializeField] private float nextQuestDelay = 1.8f;
    [SerializeField] private string allQuestsCompletedTitle = "Demo Completed";
    [SerializeField] private string questCompletedLabel = "Quest Completed";

    [Header("Quest Save Settings")]
    [SerializeField] private bool saveQuestProgress = true;
    [SerializeField] private bool useSaveSlotManager = true;
    [SerializeField] private string saveKeyPrefix = "Quest";
    [SerializeField] private bool saveOnEveryProgressChange = true;
    [SerializeField] private bool loadQuestAfterSlotSelected = true;
    [SerializeField] private bool resetQuestOnNewGame = true;
    [SerializeField] private bool saveOnApplicationQuit = true;
    [SerializeField] private bool saveOnApplicationPause = true;
    [SerializeField] private bool logSaveLoad = true;

    [Header("Quest Complete SFX")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip questCompleteSFX;
    [SerializeField, Range(0f, 1f)] private float questCompleteSFXVolume = 1f;

    [Header("Runtime UI Tuning")]
    [SerializeField] private bool liveRefreshUILayout = true;

    [Header("Quest Tracker UI")]
    [SerializeField] private bool createQuestTrackerUI = true;
    [SerializeField] private RectTransform hudParent;
    [SerializeField] private Sprite questPanelSprite;
    [SerializeField] private string trackerPanelObjectName = "Generated_QuestTrackerPanel";

    [Header("Quest Tracker Panel Layout")]
    [SerializeField] private Vector2 trackerAnchoredPosition = new Vector2(35f, -75f);
    [SerializeField] private Vector2 trackerPanelSize = new Vector2(460f, 300f);

    [Header("Quest Tracker Text Content")]
    [SerializeField] private string trackerHeaderText = "Current Quest";
    [SerializeField] private string progressPrefix = "Progress: ";
    [SerializeField] private string rewardPrefix = "Reward: ";
    [SerializeField] private string rewardSuffix = " Coins";

    [Header("Quest Tracker Text Style")]
    [SerializeField] private Font trackerTextFont;
    [SerializeField] private Color trackerTitleColor = new Color(1f, 0.9098039f, 0.6392157f, 1f);
    [SerializeField] private Color trackerBodyColor = Color.white;
    [SerializeField] private int trackerTitleFontSize = 25;
    [SerializeField] private int trackerQuestNameFontSize = 20;
    [SerializeField] private int trackerBodyFontSize = 19;
    [SerializeField] private FontStyle trackerTitleFontStyle = FontStyle.Bold;
    [SerializeField] private FontStyle trackerBodyFontStyle = FontStyle.Bold;

    [Header("Quest Tracker Auto Fit")]
    [SerializeField] private bool useBestFit = true;
    [SerializeField] private int titleMinFontSize = 16;
    [SerializeField] private int titleMaxFontSize = 25;
    [SerializeField] private int questNameMinFontSize = 13;
    [SerializeField] private int questNameMaxFontSize = 20;
    [SerializeField] private int bodyMinFontSize = 13;
    [SerializeField] private int bodyMaxFontSize = 19;

    [Header("Quest Tracker Text Layout")]
    [SerializeField] private Vector2 titlePosition = new Vector2(55f, 72f);
    [SerializeField] private Vector2 questNamePosition = new Vector2(55f, 28f);
    [SerializeField] private Vector2 progressPosition = new Vector2(55f, -38f);
    [SerializeField] private Vector2 rewardPosition = new Vector2(55f, -70f);

    [SerializeField] private Vector2 titleSize = new Vector2(330f, 38f);
    [SerializeField] private Vector2 questNameSize = new Vector2(330f, 72f);
    [SerializeField] private Vector2 bodySize = new Vector2(330f, 34f);

    [Header("Quest Tracker Text Alignment")]
    [SerializeField] private TextAnchor titleAlignment = TextAnchor.MiddleCenter;
    [SerializeField] private TextAnchor questNameAlignment = TextAnchor.MiddleCenter;
    [SerializeField] private TextAnchor bodyAlignment = TextAnchor.MiddleCenter;

    [Header("Quest Complete Popup UI")]
    [SerializeField] private bool createQuestCompletePopupUI = true;
    [SerializeField] private Sprite popupBackgroundSprite;
    [SerializeField] private Sprite popupRewardIconSprite;
    [SerializeField] private string popupPanelObjectName = "Generated_QuestCompleteRewardPopup";

    [Header("Quest Complete Popup Layout")]
    [SerializeField] private Vector2 popupAnchoredPosition = new Vector2(0f, -120f);
    [SerializeField] private Vector2 popupPanelSize = new Vector2(560f, 210f);
    [SerializeField] private Vector2 popupIconPosition = new Vector2(-185f, -58f);
    [SerializeField] private Vector2 popupIconSize = new Vector2(82f, 82f);

    [Header("Quest Complete Popup Visibility")]
    [SerializeField] private bool showPopupQuestName = false;

    [Header("Quest Complete Popup Text")]
    [SerializeField] private string popupTitleText = "Quest Complete!";
    [SerializeField] private string popupRewardPrefix = "+";
    [SerializeField] private string popupRewardSuffix = " Coins";

    [SerializeField] private Color popupTitleColor = new Color(1f, 0.9098039f, 0.6392157f, 1f);
    [SerializeField] private Color popupQuestColor = Color.white;
    [SerializeField] private Color popupRewardColor = new Color(1f, 0.8470588f, 0.4196079f, 1f);

    [SerializeField] private int popupTitleFontSize = 30;
    [SerializeField] private int popupQuestFontSize = 20;
    [SerializeField] private int popupRewardFontSize = 25;

    [Header("Quest Complete Popup Text Layout")]
    [SerializeField] private Vector2 popupTitlePosition = new Vector2(55f, 72f);
    [SerializeField] private Vector2 popupQuestPosition = new Vector2(55f, 28f);
    [SerializeField] private Vector2 popupRewardPosition = new Vector2(80f, -38f);

    [SerializeField] private Vector2 popupTitleSize = new Vector2(420f, 42f);
    [SerializeField] private Vector2 popupQuestSize = new Vector2(420f, 36f);
    [SerializeField] private Vector2 popupRewardSize = new Vector2(340f, 42f);

    [Header("Popup Layout When Quest Name Hidden")]
    [SerializeField] private Vector2 popupRewardPositionNoQuest = new Vector2(80f, 0f);
    [SerializeField] private Vector2 popupRewardSizeNoQuest = new Vector2(340f, 46f);

    [Header("Quest Complete Popup Animation")]
    [SerializeField] private float popupDuration = 1.6f;
    [SerializeField] private float popupScaleInDuration = 0.12f;
    [SerializeField] private float popupFadeOutDuration = 0.18f;
    [SerializeField] private Vector3 popupHiddenScale = new Vector3(0.85f, 0.85f, 1f);
    [SerializeField] private Vector3 popupVisibleScale = Vector3.one;

    private int currentQuestIndex = 0;
    private int currentKillCount = 0;
    private bool isCurrentQuestCompleted = false;
    private bool isAllQuestsCompleted = false;

    private PlayerWallet playerWallet;
    private PlayerSaveSlotManager saveSlotManager;

    private Coroutine nextQuestCoroutine;
    private Coroutine popupCoroutine;

    private RectTransform trackerPanelRect;
    private Image trackerPanelImage;
    private Text trackerTitleLabel;
    private Text trackerQuestNameLabel;
    private Text trackerProgressLabel;
    private Text trackerRewardLabel;

    private RectTransform popupPanelRect;
    private Image popupBackgroundImage;
    private Image popupRewardIconImage;
    private Text popupTitleLabel;
    private Text popupQuestLabel;
    private Text popupRewardLabel;
    private CanvasGroup popupCanvasGroup;

    private Font cachedDefaultFont;
    private bool missingHUDWarningShown;
    private bool hasLoadedForCurrentSlot;
    private bool isApplicationQuitting;

    public int CurrentQuestIndex => currentQuestIndex;
    public int CurrentKillCount => currentKillCount;
    public bool IsCurrentQuestCompleted => isCurrentQuestCompleted;
    public bool IsAllQuestsCompleted => isAllQuestsCompleted;

    private string CurrentQuestIndexBaseKey => $"{saveKeyPrefix}_CurrentQuestIndex";
    private string CurrentKillCountBaseKey => $"{saveKeyPrefix}_CurrentKillCount";
    private string CurrentQuestCompletedBaseKey => $"{saveKeyPrefix}_CurrentQuestCompleted";
    private string AllQuestsCompletedBaseKey => $"{saveKeyPrefix}_AllQuestsCompleted";

    private string CurrentQuestIndexKey => GetSaveKey(CurrentQuestIndexBaseKey);
    private string CurrentKillCountKey => GetSaveKey(CurrentKillCountBaseKey);
    private string CurrentQuestCompletedKey => GetSaveKey(CurrentQuestCompletedBaseKey);
    private string AllQuestsCompletedKey => GetSaveKey(AllQuestsCompletedBaseKey);

    public KillQuestDefinition CurrentQuest
    {
        get
        {
            if (questChain == null || questChain.Count == 0)
                return null;

            if (currentQuestIndex < 0 || currentQuestIndex >= questChain.Count)
                return null;

            return questChain[currentQuestIndex];
        }
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        ResolveSaveSlotManager();

        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }

        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        audioSource.playOnAwake = false;
        audioSource.loop = false;
        audioSource.spatialBlend = 0f;
    }

    private void OnEnable()
    {
        ResolveSaveSlotManager();
        SubscribeToSaveSlotManager();

        TryLoadForCurrentSaveSlot();
    }

    private void Start()
    {
        playerWallet = FindObjectOfType<PlayerWallet>();

        if (createQuestTrackerUI)
        {
            EnsureQuestTrackerUI();
        }

        if (createQuestCompletePopupUI)
        {
            EnsureQuestCompletePopupUI();
            HideQuestCompletePopupImmediate();
        }

        bool loaded = TryLoadForCurrentSaveSlot();

        if (!loaded)
        {
            InitializeQuestChain();
        }
        else
        {
            RefreshUI();
            ContinueCompletedQuestAdvanceIfNeeded();
        }
    }

    private void Update()
    {
        if (!liveRefreshUILayout)
            return;

        if (createQuestTrackerUI && trackerPanelRect != null)
        {
            ApplyQuestTrackerPanelLayout();
            ApplyQuestTrackerTextLayout(updateTextContent: false);
        }

        if (createQuestCompletePopupUI && popupPanelRect != null)
        {
            ApplyQuestCompletePopupLayout(updateTextContent: false);
        }
    }

    private void OnDisable()
    {
        UnsubscribeFromSaveSlotManager();

        if (isApplicationQuitting && saveOnApplicationQuit)
        {
            SaveQuestProgress();
        }
    }

    private void OnApplicationQuit()
    {
        isApplicationQuitting = true;

        if (saveOnApplicationQuit)
        {
            SaveQuestProgress();
        }
    }

    private void OnApplicationPause(bool pauseStatus)
    {
        if (!saveOnApplicationPause)
            return;

        if (pauseStatus)
        {
            SaveQuestProgress();
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
        saveSlotManager.OnNewGameStartedInSlot -= HandleNewGameStartedInSlot;
        saveSlotManager.OnGameSaved -= HandleGameSaved;

        saveSlotManager.OnSaveSlotSelected += HandleSaveSlotSelected;
        saveSlotManager.OnNewGameStartedInSlot += HandleNewGameStartedInSlot;
        saveSlotManager.OnGameSaved += HandleGameSaved;
    }

    private void UnsubscribeFromSaveSlotManager()
    {
        if (saveSlotManager == null)
            return;

        saveSlotManager.OnSaveSlotSelected -= HandleSaveSlotSelected;
        saveSlotManager.OnNewGameStartedInSlot -= HandleNewGameStartedInSlot;
        saveSlotManager.OnGameSaved -= HandleGameSaved;
    }

    private void HandleSaveSlotSelected(int slotIndex)
    {
        if (!loadQuestAfterSlotSelected)
            return;

        hasLoadedForCurrentSlot = false;

        bool loaded = TryLoadForCurrentSaveSlot();

        if (!loaded)
        {
            InitializeQuestChain();
        }

        if (logSaveLoad)
        {
            Debug.Log($"QuestManager loaded for Save {slotIndex}.", this);
        }
    }

    private void HandleNewGameStartedInSlot(int slotIndex)
    {
        if (!resetQuestOnNewGame)
            return;

        ResetQuestChain();

        if (logSaveLoad)
        {
            Debug.Log($"QuestManager reset for New Game in Save {slotIndex}.", this);
        }
    }

    private void HandleGameSaved(int slotIndex)
    {
        SaveQuestProgress();
    }

    private void InitializeQuestChain()
    {
        if (questChain == null || questChain.Count == 0)
        {
            Debug.LogWarning("QuestManager: Quest Chain boþ.");
            isAllQuestsCompleted = true;
            RefreshUI();
            return;
        }

        currentQuestIndex = Mathf.Clamp(currentQuestIndex, 0, questChain.Count - 1);
        currentKillCount = 0;
        isCurrentQuestCompleted = false;
        isAllQuestsCompleted = false;

        RefreshUI();
    }

    public void ReportEnemyKilled(EnemyQuestType killedEnemyType)
    {
        if (isAllQuestsCompleted)
            return;

        if (isCurrentQuestCompleted)
            return;

        KillQuestDefinition activeQuest = CurrentQuest;

        if (activeQuest == null)
            return;

        if (killedEnemyType != activeQuest.targetEnemyType)
            return;

        currentKillCount++;
        currentKillCount = Mathf.Clamp(currentKillCount, 0, activeQuest.requiredKillCount);

        Debug.Log($"Quest Progress: {activeQuest.questTitle} -> {currentKillCount}/{activeQuest.requiredKillCount}");

        if (currentKillCount >= activeQuest.requiredKillCount)
        {
            CompleteCurrentQuest();
        }
        else
        {
            RefreshUI();
            SaveQuestProgressIfNeeded();
        }
    }

    public void ResetQuestChain()
    {
        if (nextQuestCoroutine != null)
        {
            StopCoroutine(nextQuestCoroutine);
            nextQuestCoroutine = null;
        }

        if (popupCoroutine != null)
        {
            StopCoroutine(popupCoroutine);
            popupCoroutine = null;
        }

        currentQuestIndex = 0;
        currentKillCount = 0;
        isCurrentQuestCompleted = false;
        isAllQuestsCompleted = false;

        HideQuestCompletePopupImmediate();
        RefreshUI();
        SaveQuestProgressIfNeeded();
    }

    private void CompleteCurrentQuest()
    {
        if (isCurrentQuestCompleted)
            return;

        KillQuestDefinition activeQuest = CurrentQuest;

        if (activeQuest == null)
            return;

        isCurrentQuestCompleted = true;
        currentKillCount = activeQuest.requiredKillCount;

        PlayQuestCompleteSFX();
        GiveQuestReward(activeQuest.completionCoinReward);
        ShowQuestCompletePopup(activeQuest.questTitle, activeQuest.completionCoinReward);

        Debug.Log($"Quest Complete: {activeQuest.questTitle}");

        RefreshUI();
        SaveQuestProgressIfNeeded();

        if (nextQuestCoroutine != null)
        {
            StopCoroutine(nextQuestCoroutine);
        }

        nextQuestCoroutine = StartCoroutine(AdvanceToNextQuestRoutine());
    }

    private IEnumerator AdvanceToNextQuestRoutine()
    {
        yield return new WaitForSeconds(nextQuestDelay);

        currentQuestIndex++;

        if (currentQuestIndex >= questChain.Count)
        {
            isAllQuestsCompleted = true;
            isCurrentQuestCompleted = true;
            currentKillCount = 0;
        }
        else
        {
            currentKillCount = 0;
            isCurrentQuestCompleted = false;
        }

        RefreshUI();
        SaveQuestProgressIfNeeded();

        nextQuestCoroutine = null;
    }

    private void ContinueCompletedQuestAdvanceIfNeeded()
    {
        if (isAllQuestsCompleted)
            return;

        if (!isCurrentQuestCompleted)
            return;

        if (CurrentQuest == null)
            return;

        if (nextQuestCoroutine != null)
        {
            StopCoroutine(nextQuestCoroutine);
        }

        nextQuestCoroutine = StartCoroutine(AdvanceToNextQuestRoutine());
    }

    private void GiveQuestReward(int rewardAmount)
    {
        if (rewardAmount <= 0)
            return;

        if (playerWallet == null)
        {
            playerWallet = FindObjectOfType<PlayerWallet>();
        }

        if (playerWallet != null)
        {
            playerWallet.AddCoins(rewardAmount);
        }
    }

    private void PlayQuestCompleteSFX()
    {
        if (audioSource == null)
            return;

        if (questCompleteSFX == null)
            return;

        audioSource.PlayOneShot(questCompleteSFX, questCompleteSFXVolume);
    }

    private void SaveQuestProgressIfNeeded()
    {
        if (!saveOnEveryProgressChange)
            return;

        SaveQuestProgress();
    }

    public void SaveQuestProgress()
    {
        if (!saveQuestProgress)
            return;

        if (useSaveSlotManager)
        {
            ResolveSaveSlotManager();

            if (saveSlotManager != null && !saveSlotManager.HasSelectedSlot)
            {
                if (logSaveLoad)
                {
                    Debug.Log("QuestManager cannot save because no save slot is selected.", this);
                }

                return;
            }
        }

        PlayerPrefs.SetInt(CurrentQuestIndexKey, currentQuestIndex);
        PlayerPrefs.SetInt(CurrentKillCountKey, currentKillCount);
        PlayerPrefs.SetInt(CurrentQuestCompletedKey, isCurrentQuestCompleted ? 1 : 0);
        PlayerPrefs.SetInt(AllQuestsCompletedKey, isAllQuestsCompleted ? 1 : 0);

        if (useSaveSlotManager && saveSlotManager != null && saveSlotManager.HasSelectedSlot)
        {
            saveSlotManager.MarkSelectedSlotHasData();
        }

        PlayerPrefs.Save();

        if (logSaveLoad)
        {
            Debug.Log(
                $"Quest progress saved. " +
                $"Quest Index: {currentQuestIndex}, " +
                $"Kill Count: {currentKillCount}, " +
                $"Current Completed: {isCurrentQuestCompleted}, " +
                $"All Completed: {isAllQuestsCompleted}",
                this
            );
        }
    }

    public bool LoadQuestProgress()
    {
        if (!saveQuestProgress)
            return false;

        if (questChain == null || questChain.Count == 0)
        {
            isAllQuestsCompleted = true;
            RefreshUI();
            return true;
        }

        currentQuestIndex = PlayerPrefs.GetInt(CurrentQuestIndexKey, 0);
        currentKillCount = PlayerPrefs.GetInt(CurrentKillCountKey, 0);
        isCurrentQuestCompleted = PlayerPrefs.GetInt(CurrentQuestCompletedKey, 0) == 1;
        isAllQuestsCompleted = PlayerPrefs.GetInt(AllQuestsCompletedKey, 0) == 1;

        ValidateLoadedQuestState();

        hasLoadedForCurrentSlot = true;
        RefreshUI();

        if (logSaveLoad)
        {
            Debug.Log(
                $"Quest progress loaded. " +
                $"Quest Index: {currentQuestIndex}, " +
                $"Kill Count: {currentKillCount}, " +
                $"Current Completed: {isCurrentQuestCompleted}, " +
                $"All Completed: {isAllQuestsCompleted}",
                this
            );
        }

        return true;
    }

    private bool TryLoadForCurrentSaveSlot()
    {
        if (!saveQuestProgress)
            return false;

        if (hasLoadedForCurrentSlot)
            return true;

        if (!useSaveSlotManager)
        {
            return LoadQuestProgress();
        }

        ResolveSaveSlotManager();

        if (saveSlotManager == null)
        {
            if (logSaveLoad)
            {
                Debug.LogWarning("QuestManager could not find PlayerSaveSlotManager. Falling back to non-slot quest save keys.", this);
            }

            return LoadQuestProgress();
        }

        if (!saveSlotManager.HasSelectedSlot)
        {
            if (logSaveLoad)
            {
                Debug.Log("QuestManager is waiting for save slot selection.", this);
            }

            return false;
        }

        return LoadQuestProgress();
    }

    private void ValidateLoadedQuestState()
    {
        if (questChain == null || questChain.Count == 0)
        {
            currentQuestIndex = 0;
            currentKillCount = 0;
            isCurrentQuestCompleted = true;
            isAllQuestsCompleted = true;
            return;
        }

        if (isAllQuestsCompleted)
        {
            currentQuestIndex = Mathf.Clamp(currentQuestIndex, 0, questChain.Count);
            currentKillCount = 0;
            isCurrentQuestCompleted = true;
            return;
        }

        currentQuestIndex = Mathf.Clamp(currentQuestIndex, 0, questChain.Count - 1);

        KillQuestDefinition activeQuest = CurrentQuest;

        if (activeQuest == null)
        {
            currentKillCount = 0;
            isCurrentQuestCompleted = false;
            return;
        }

        currentKillCount = Mathf.Clamp(currentKillCount, 0, activeQuest.requiredKillCount);

        if (isCurrentQuestCompleted)
        {
            currentKillCount = activeQuest.requiredKillCount;
        }
    }

    private string GetSaveKey(string baseKey)
    {
        if (string.IsNullOrWhiteSpace(baseKey))
        {
            baseKey = "Quest_UnknownKey";
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

    private void RefreshUI()
    {
        if (!createQuestTrackerUI)
            return;

        EnsureQuestTrackerUI();

        if (isAllQuestsCompleted)
        {
            UpdateQuestTrackerText(
                trackerHeaderText,
                allQuestsCompletedTitle,
                "",
                questCompletedLabel
            );

            return;
        }

        KillQuestDefinition activeQuest = CurrentQuest;

        if (activeQuest == null)
        {
            UpdateQuestTrackerText("", "", "", "");
            return;
        }

        string progressText = progressPrefix + currentKillCount + " / " + activeQuest.requiredKillCount;
        string rewardText = rewardPrefix + activeQuest.completionCoinReward + rewardSuffix;

        if (isCurrentQuestCompleted)
        {
            progressText = progressPrefix + activeQuest.requiredKillCount + " / " + activeQuest.requiredKillCount;
            rewardText = questCompletedLabel;
        }

        UpdateQuestTrackerText(
            trackerHeaderText,
            activeQuest.questTitle,
            progressText,
            rewardText
        );
    }

    private void EnsureQuestTrackerUI()
    {
        if (!createQuestTrackerUI)
            return;

        EnsureHUDParent();

        if (hudParent == null)
            return;

        Transform existingPanel = hudParent.Find(trackerPanelObjectName);

        if (existingPanel == null)
        {
            GameObject panelObject = new GameObject(trackerPanelObjectName);
            panelObject.transform.SetParent(hudParent, false);

            trackerPanelRect = panelObject.AddComponent<RectTransform>();
            trackerPanelImage = panelObject.AddComponent<Image>();

            trackerTitleLabel = CreateText("TitleText", trackerPanelRect);
            trackerQuestNameLabel = CreateText("QuestNameText", trackerPanelRect);
            trackerProgressLabel = CreateText("ProgressText", trackerPanelRect);
            trackerRewardLabel = CreateText("RewardText", trackerPanelRect);
        }
        else
        {
            trackerPanelRect = existingPanel.GetComponent<RectTransform>();

            if (trackerPanelRect == null)
            {
                trackerPanelRect = existingPanel.gameObject.AddComponent<RectTransform>();
            }

            trackerPanelImage = existingPanel.GetComponent<Image>();

            if (trackerPanelImage == null)
            {
                trackerPanelImage = existingPanel.gameObject.AddComponent<Image>();
            }

            trackerTitleLabel = GetOrCreateText("TitleText", trackerPanelRect);
            trackerQuestNameLabel = GetOrCreateText("QuestNameText", trackerPanelRect);
            trackerProgressLabel = GetOrCreateText("ProgressText", trackerPanelRect);
            trackerRewardLabel = GetOrCreateText("RewardText", trackerPanelRect);
        }

        ApplyQuestTrackerPanelLayout();
        ApplyQuestTrackerTextLayout(updateTextContent: true);
    }

    private void ApplyQuestTrackerPanelLayout()
    {
        if (trackerPanelRect == null || trackerPanelImage == null)
            return;

        trackerPanelRect.anchorMin = new Vector2(0f, 1f);
        trackerPanelRect.anchorMax = new Vector2(0f, 1f);
        trackerPanelRect.pivot = new Vector2(0f, 1f);
        trackerPanelRect.anchoredPosition = trackerAnchoredPosition;
        trackerPanelRect.sizeDelta = trackerPanelSize;

        trackerPanelImage.sprite = questPanelSprite;
        trackerPanelImage.type = Image.Type.Simple;
        trackerPanelImage.color = Color.white;
        trackerPanelImage.raycastTarget = false;
    }

    private void ApplyQuestTrackerTextLayout(bool updateTextContent)
    {
        SetupText(
            trackerTitleLabel,
            trackerHeaderText,
            titlePosition,
            titleSize,
            trackerTitleFontSize,
            titleMinFontSize,
            titleMaxFontSize,
            trackerTitleFontStyle,
            trackerTitleColor,
            titleAlignment,
            updateTextContent
        );

        SetupText(
            trackerQuestNameLabel,
            "",
            questNamePosition,
            questNameSize,
            trackerQuestNameFontSize,
            questNameMinFontSize,
            questNameMaxFontSize,
            trackerBodyFontStyle,
            trackerBodyColor,
            questNameAlignment,
            updateTextContent
        );

        SetupText(
            trackerProgressLabel,
            "",
            progressPosition,
            bodySize,
            trackerBodyFontSize,
            bodyMinFontSize,
            bodyMaxFontSize,
            trackerBodyFontStyle,
            trackerBodyColor,
            bodyAlignment,
            updateTextContent
        );

        SetupText(
            trackerRewardLabel,
            "",
            rewardPosition,
            bodySize,
            trackerBodyFontSize,
            bodyMinFontSize,
            bodyMaxFontSize,
            trackerBodyFontStyle,
            trackerBodyColor,
            bodyAlignment,
            updateTextContent
        );
    }

    private void UpdateQuestTrackerText(string header, string questName, string progress, string reward)
    {
        if (!createQuestTrackerUI)
            return;

        if (trackerTitleLabel != null)
        {
            trackerTitleLabel.text = header;
        }

        if (trackerQuestNameLabel != null)
        {
            trackerQuestNameLabel.text = questName;
        }

        if (trackerProgressLabel != null)
        {
            trackerProgressLabel.text = progress;
        }

        if (trackerRewardLabel != null)
        {
            trackerRewardLabel.text = reward;
        }
    }

    private void EnsureQuestCompletePopupUI()
    {
        if (!createQuestCompletePopupUI)
            return;

        EnsureHUDParent();

        if (hudParent == null)
            return;

        Transform existingPopup = hudParent.Find(popupPanelObjectName);

        if (existingPopup == null)
        {
            GameObject popupObject = new GameObject(popupPanelObjectName);
            popupObject.transform.SetParent(hudParent, false);

            popupPanelRect = popupObject.AddComponent<RectTransform>();
            popupCanvasGroup = popupObject.AddComponent<CanvasGroup>();
            popupBackgroundImage = popupObject.AddComponent<Image>();

            popupRewardIconImage = CreateImage("RewardIcon", popupPanelRect);
            popupTitleLabel = CreateText("PopupTitleText", popupPanelRect);
            popupQuestLabel = CreateText("PopupQuestText", popupPanelRect);
            popupRewardLabel = CreateText("PopupRewardText", popupPanelRect);
        }
        else
        {
            popupPanelRect = existingPopup.GetComponent<RectTransform>();

            if (popupPanelRect == null)
            {
                popupPanelRect = existingPopup.gameObject.AddComponent<RectTransform>();
            }

            popupCanvasGroup = existingPopup.GetComponent<CanvasGroup>();

            if (popupCanvasGroup == null)
            {
                popupCanvasGroup = existingPopup.gameObject.AddComponent<CanvasGroup>();
            }

            popupBackgroundImage = existingPopup.GetComponent<Image>();

            if (popupBackgroundImage == null)
            {
                popupBackgroundImage = existingPopup.gameObject.AddComponent<Image>();
            }

            popupRewardIconImage = GetOrCreateImage("RewardIcon", popupPanelRect);
            popupTitleLabel = GetOrCreateText("PopupTitleText", popupPanelRect);
            popupQuestLabel = GetOrCreateText("PopupQuestText", popupPanelRect);
            popupRewardLabel = GetOrCreateText("PopupRewardText", popupPanelRect);
        }

        ApplyQuestCompletePopupLayout(updateTextContent: true);
    }

    private void ApplyQuestCompletePopupLayout(bool updateTextContent)
    {
        if (popupPanelRect == null || popupBackgroundImage == null)
            return;

        popupPanelRect.anchorMin = new Vector2(0.5f, 1f);
        popupPanelRect.anchorMax = new Vector2(0.5f, 1f);
        popupPanelRect.pivot = new Vector2(0.5f, 1f);
        popupPanelRect.anchoredPosition = popupAnchoredPosition;
        popupPanelRect.sizeDelta = popupPanelSize;

        popupBackgroundImage.sprite = popupBackgroundSprite;
        popupBackgroundImage.type = Image.Type.Simple;
        popupBackgroundImage.color = Color.white;
        popupBackgroundImage.raycastTarget = false;

        if (popupCanvasGroup != null)
        {
            popupCanvasGroup.blocksRaycasts = false;
            popupCanvasGroup.interactable = false;
        }

        if (popupRewardIconImage != null)
        {
            RectTransform iconRect = popupRewardIconImage.rectTransform;

            iconRect.anchorMin = new Vector2(0.5f, 0.5f);
            iconRect.anchorMax = new Vector2(0.5f, 0.5f);
            iconRect.pivot = new Vector2(0.5f, 0.5f);
            iconRect.anchoredPosition = popupIconPosition;
            iconRect.sizeDelta = popupIconSize;

            popupRewardIconImage.sprite = popupRewardIconSprite;
            popupRewardIconImage.type = Image.Type.Simple;
            popupRewardIconImage.color = Color.white;
            popupRewardIconImage.preserveAspect = true;
            popupRewardIconImage.raycastTarget = false;
        }

        SetupText(
            popupTitleLabel,
            popupTitleText,
            popupTitlePosition,
            popupTitleSize,
            popupTitleFontSize,
            Mathf.Max(14, popupTitleFontSize - 8),
            popupTitleFontSize,
            FontStyle.Bold,
            popupTitleColor,
            TextAnchor.MiddleCenter,
            updateTextContent
        );

        SetupText(
            popupQuestLabel,
            "",
            popupQuestPosition,
            popupQuestSize,
            popupQuestFontSize,
            Mathf.Max(12, popupQuestFontSize - 6),
            popupQuestFontSize,
            FontStyle.Bold,
            popupQuestColor,
            TextAnchor.MiddleCenter,
            updateTextContent
        );

        Vector2 activeRewardPosition = showPopupQuestName ? popupRewardPosition : popupRewardPositionNoQuest;
        Vector2 activeRewardSize = showPopupQuestName ? popupRewardSize : popupRewardSizeNoQuest;

        SetupText(
            popupRewardLabel,
            "",
            activeRewardPosition,
            activeRewardSize,
            popupRewardFontSize,
            Mathf.Max(14, popupRewardFontSize - 6),
            popupRewardFontSize,
            FontStyle.Bold,
            popupRewardColor,
            TextAnchor.MiddleCenter,
            updateTextContent
        );

        if (popupQuestLabel != null)
        {
            popupQuestLabel.gameObject.SetActive(showPopupQuestName);
        }

        popupBackgroundImage.transform.SetSiblingIndex(0);

        if (popupRewardIconImage != null)
        {
            popupRewardIconImage.transform.SetSiblingIndex(1);
        }

        if (popupTitleLabel != null)
        {
            popupTitleLabel.transform.SetSiblingIndex(2);
        }

        if (popupQuestLabel != null)
        {
            popupQuestLabel.transform.SetSiblingIndex(3);
        }

        if (popupRewardLabel != null)
        {
            popupRewardLabel.transform.SetSiblingIndex(4);
        }
    }

    private void ShowQuestCompletePopup(string completedQuestTitle, int rewardAmount)
    {
        if (!createQuestCompletePopupUI)
            return;

        EnsureQuestCompletePopupUI();

        if (popupPanelRect == null || popupCanvasGroup == null)
            return;

        ApplyQuestCompletePopupLayout(updateTextContent: false);

        if (popupTitleLabel != null)
        {
            popupTitleLabel.text = popupTitleText;
        }

        if (popupQuestLabel != null)
        {
            popupQuestLabel.text = completedQuestTitle;
        }

        if (popupRewardLabel != null)
        {
            popupRewardLabel.text = popupRewardPrefix + rewardAmount + popupRewardSuffix;
        }

        if (popupCoroutine != null)
        {
            StopCoroutine(popupCoroutine);
        }

        popupCoroutine = StartCoroutine(QuestCompletePopupRoutine());
    }

    private IEnumerator QuestCompletePopupRoutine()
    {
        popupPanelRect.gameObject.SetActive(true);
        popupCanvasGroup.alpha = 0f;
        popupPanelRect.localScale = popupHiddenScale;

        float timer = 0f;

        while (timer < popupScaleInDuration)
        {
            timer += Time.deltaTime;

            float t = Mathf.Clamp01(timer / Mathf.Max(0.001f, popupScaleInDuration));
            popupCanvasGroup.alpha = t;
            popupPanelRect.localScale = Vector3.Lerp(popupHiddenScale, popupVisibleScale, t);

            yield return null;
        }

        popupCanvasGroup.alpha = 1f;
        popupPanelRect.localScale = popupVisibleScale;

        float visibleTime = Mathf.Max(0f, popupDuration - popupScaleInDuration - popupFadeOutDuration);
        yield return new WaitForSeconds(visibleTime);

        timer = 0f;

        while (timer < popupFadeOutDuration)
        {
            timer += Time.deltaTime;

            float t = Mathf.Clamp01(timer / Mathf.Max(0.001f, popupFadeOutDuration));
            popupCanvasGroup.alpha = 1f - t;

            yield return null;
        }

        HideQuestCompletePopupImmediate();
        popupCoroutine = null;
    }

    private void HideQuestCompletePopupImmediate()
    {
        if (popupPanelRect == null)
            return;

        popupPanelRect.gameObject.SetActive(false);

        if (popupCanvasGroup != null)
        {
            popupCanvasGroup.alpha = 0f;
            popupCanvasGroup.blocksRaycasts = false;
            popupCanvasGroup.interactable = false;
        }

        popupPanelRect.localScale = popupHiddenScale;
    }

    private void EnsureHUDParent()
    {
        if (hudParent == null)
        {
            GameObject foundHUD = GameObject.Find("PlayerHUD");

            if (foundHUD != null)
            {
                hudParent = foundHUD.GetComponent<RectTransform>();
            }
        }

        if (hudParent == null && !missingHUDWarningShown)
        {
            Debug.LogWarning("[QuestManager] PlayerHUD bulunamadý. Hud Parent alanýna PlayerHUD objesini sürükle.", this);
            missingHUDWarningShown = true;
        }
    }

    private Image CreateImage(string objectName, RectTransform parent)
    {
        GameObject imageObject = new GameObject(objectName);
        imageObject.transform.SetParent(parent, false);

        imageObject.AddComponent<RectTransform>();

        Image image = imageObject.AddComponent<Image>();
        image.raycastTarget = false;

        return image;
    }

    private Image GetOrCreateImage(string objectName, RectTransform parent)
    {
        Transform existing = parent.Find(objectName);

        if (existing != null)
        {
            Image image = existing.GetComponent<Image>();

            if (image == null)
            {
                image = existing.gameObject.AddComponent<Image>();
            }

            image.raycastTarget = false;
            return image;
        }

        return CreateImage(objectName, parent);
    }

    private Text CreateText(string objectName, RectTransform parent)
    {
        GameObject textObject = new GameObject(objectName);
        textObject.transform.SetParent(parent, false);

        textObject.AddComponent<RectTransform>();

        Text text = textObject.AddComponent<Text>();
        text.raycastTarget = false;
        text.supportRichText = true;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Truncate;
        text.resizeTextForBestFit = useBestFit;

        return text;
    }

    private Text GetOrCreateText(string objectName, RectTransform parent)
    {
        Transform existing = parent.Find(objectName);

        if (existing != null)
        {
            Text text = existing.GetComponent<Text>();

            if (text == null)
            {
                text = existing.gameObject.AddComponent<Text>();
            }

            text.raycastTarget = false;
            text.supportRichText = true;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            text.resizeTextForBestFit = useBestFit;

            return text;
        }

        return CreateText(objectName, parent);
    }

    private void SetupText(
        Text label,
        string content,
        Vector2 position,
        Vector2 size,
        int fontSize,
        int minFontSize,
        int maxFontSize,
        FontStyle fontStyle,
        Color color,
        TextAnchor alignment,
        bool updateTextContent
    )
    {
        if (label == null)
            return;

        RectTransform rect = label.GetComponent<RectTransform>();

        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;

        if (updateTextContent)
        {
            label.text = content;
        }

        label.font = trackerTextFont != null ? trackerTextFont : GetDefaultFont();
        label.fontSize = fontSize;
        label.fontStyle = fontStyle;
        label.color = color;
        label.alignment = alignment;

        label.horizontalOverflow = HorizontalWrapMode.Wrap;
        label.verticalOverflow = VerticalWrapMode.Truncate;

        label.resizeTextForBestFit = useBestFit;
        label.resizeTextMinSize = minFontSize;
        label.resizeTextMaxSize = maxFontSize;
    }

    private Font GetDefaultFont()
    {
        if (cachedDefaultFont != null)
            return cachedDefaultFont;

        cachedDefaultFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        if (cachedDefaultFont == null)
        {
            Debug.LogWarning("[QuestManager] LegacyRuntime.ttf bulunamadý. Tracker Text Font alanýna manuel font atayabilirsin.", this);
        }

        return cachedDefaultFont;
    }

    [ContextMenu("Debug Save Quest Progress")]
    private void DebugSaveQuestProgress()
    {
        SaveQuestProgress();
    }

    [ContextMenu("Debug Load Quest Progress")]
    private void DebugLoadQuestProgress()
    {
        LoadQuestProgress();
    }

    [ContextMenu("Debug Reset Quest Chain")]
    private void DebugResetQuestChain()
    {
        ResetQuestChain();
    }
}