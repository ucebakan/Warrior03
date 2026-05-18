using System;
using UnityEngine;
using UnityEngine.UI;

public class PlayerXP : MonoBehaviour
{
    [Header("Save Settings")]
    [SerializeField] private bool saveXP = true;
    [SerializeField] private bool useSaveSlotManager = true;
    [SerializeField] private string saveKeyPrefix = "PlayerXP";
    [SerializeField] private bool saveOnEveryXPChange = true;
    [SerializeField] private bool saveOnApplicationQuit = true;
    [SerializeField] private bool saveOnApplicationPause = true;
    [SerializeField] private bool resetXPOnNewGame = true;
    [SerializeField] private bool loadXPAfterSlotSelected = true;

    [Header("Level Settings")]
    [SerializeField, Min(1)] private int startLevel = 1;
    [SerializeField, Min(1)] private int maxLevel = 50;
    [SerializeField, Min(1)] private int baseXPToNextLevel = 100;
    [SerializeField, Min(1f)] private float xpRequirementMultiplier = 1.25f;

    [Header("Runtime XP")]
    [SerializeField, Min(1)] private int currentLevel = 1;
    [SerializeField, Min(0)] private int currentXP = 0;

    [Header("Generated XP UI")]
    [SerializeField] private bool createXPUI = true;
    [SerializeField] private RectTransform hudParent;
    [SerializeField] private string generatedPanelName = "Generated_PlayerXPPanel";
    [SerializeField] private bool showPanelBackground = false;

    [Header("Live Play Mode Editing")]
    [SerializeField] private bool liveRefreshInPlayMode = true;
    [SerializeField, Min(0.02f)] private float liveRefreshInterval = 0.05f;
    [SerializeField] private bool rebuildMissingUIElementsAutomatically = true;

    [Header("XP UI Icon")]
    [SerializeField] private bool showXPIcon = false;
    [SerializeField] private Sprite xpIconSprite;
    [SerializeField] private Vector2 iconPosition = new Vector2(-180f, 0f);
    [SerializeField] private Vector2 iconSize = new Vector2(44f, 44f);

    [Header("XP Bar Sprites")]
    [SerializeField] private bool useSpriteBarVisuals = true;
    [SerializeField] private Sprite xpBarBackgroundSprite;
    [SerializeField] private Sprite xpBarFillSprite;
    [SerializeField] private Sprite xpBarFrameSprite;

    [Header("XP UI Layout")]
    [SerializeField] private Vector2 panelAnchoredPosition = new Vector2(0f, -82f);
    [SerializeField] private Vector2 panelSize = new Vector2(440f, 70f);

    [SerializeField] private Vector2 levelTextPosition = new Vector2(-105f, 10f);
    [SerializeField] private Vector2 levelTextSize = new Vector2(100f, 30f);

    [SerializeField] private Vector2 xpTextPosition = new Vector2(90f, 10f);
    [SerializeField] private Vector2 xpTextSize = new Vector2(230f, 30f);

    [SerializeField] private Vector2 barPosition = new Vector2(45f, -18f);
    [SerializeField] private Vector2 barSize = new Vector2(320f, 24f);

    [Header("XP UI Text")]
    [SerializeField] private Font uiFont;
    [SerializeField] private int levelFontSize = 18;
    [SerializeField] private int xpFontSize = 15;
    [SerializeField] private string levelPrefix = "Lv. ";
    [SerializeField] private string xpPrefix = "XP: ";
    [SerializeField] private string maxLevelText = "MAX";

    [Header("XP UI Colors - Hex")]
    [SerializeField] private string panelColorHex = "#00000000";
    [SerializeField] private string levelTextColorHex = "#F5D27A";
    [SerializeField] private string xpTextColorHex = "#FFFFFF";
    [SerializeField] private string barBackgroundColorHex = "#FFFFFF";
    [SerializeField] private string barFillColorHex = "#FFFFFF";
    [SerializeField] private string barFrameColorHex = "#FFFFFF";
    [SerializeField] private string fallbackIconColorHex = "#C76ED6";
    [SerializeField] private string fallbackBarFillColorHex = "#C76ED6";

    [Header("Debug")]
    [SerializeField] private bool enableDebugXPKey = true;
    [SerializeField] private KeyCode debugAddXPKey = KeyCode.X;
    [SerializeField, Min(1)] private int debugXPAmount = 25;
    [SerializeField] private bool logActions = true;

    private PlayerSaveSlotManager saveSlotManager;

    private RectTransform panelRect;
    private Image panelImage;

    private Image iconImage;
    private RectTransform iconRect;

    private Text levelText;
    private Text xpText;

    private RectTransform barBackgroundRect;
    private Image barBackgroundImage;

    private RectTransform barFillRect;
    private Image barFillImage;

    private RectTransform barFrameRect;
    private Image barFrameImage;

    private Font cachedDefaultFont;
    private bool missingHUDWarningShown;
    private bool hasLoadedForCurrentSlot;
    private bool isApplicationQuitting;
    private float nextLiveRefreshTime;

    public int CurrentLevel => currentLevel;
    public int CurrentXP => currentXP;
    public int MaxLevel => maxLevel;
    public bool IsMaxLevel => currentLevel >= maxLevel;
    public int XPToNextLevel => IsMaxLevel ? 0 : CalculateXPToNextLevel(currentLevel);
    public float XPPercent => IsMaxLevel ? 1f : Mathf.Clamp01((float)currentXP / Mathf.Max(1, XPToNextLevel));

    public event Action<int> OnLevelChanged;
    public event Action<int, int> OnXPChanged;
    public event Action<int> OnLevelUp;

    private string LevelBaseKey => $"{saveKeyPrefix}_Level";
    private string CurrentXPBaseKey => $"{saveKeyPrefix}_CurrentXP";

    private string LevelKey => GetSaveKey(LevelBaseKey);
    private string CurrentXPKey => GetSaveKey(CurrentXPBaseKey);

    private void Awake()
    {
        NormalizeValues();
        ResolveReferences();
    }

    private void OnEnable()
    {
        ResolveReferences();
        SubscribeToSaveSlotManager();
        TryLoadForCurrentSaveSlot();
    }

    private void Start()
    {
        ResolveReferences();

        if (createXPUI)
        {
            EnsureXPUI();
        }

        TryLoadForCurrentSaveSlot();
        RefreshUI();
        ApplyUILayout();
    }

    private void Update()
    {
        if (enableDebugXPKey && Input.GetKeyDown(debugAddXPKey))
        {
            AddExperience(debugXPAmount);
        }

        if (!Application.isPlaying)
            return;

        if (!liveRefreshInPlayMode)
            return;

        if (Time.unscaledTime < nextLiveRefreshTime)
            return;

        nextLiveRefreshTime = Time.unscaledTime + Mathf.Max(0.02f, liveRefreshInterval);

        LiveRefreshUI();
    }

    private void OnDisable()
    {
        UnsubscribeFromSaveSlotManager();

        if (isApplicationQuitting && saveOnApplicationQuit)
        {
            SaveXPProgress();
        }
    }

    private void OnApplicationQuit()
    {
        isApplicationQuitting = true;

        if (saveOnApplicationQuit)
        {
            SaveXPProgress();
        }
    }

    private void OnApplicationPause(bool pauseStatus)
    {
        if (!saveOnApplicationPause)
            return;

        if (pauseStatus)
        {
            SaveXPProgress();
        }
    }

    private void OnValidate()
    {
        NormalizeValues();

        if (Application.isPlaying)
        {
            LiveRefreshUI();
        }
    }

    private void LiveRefreshUI()
    {
        if (!createXPUI)
            return;

        if (panelRect == null || rebuildMissingUIElementsAutomatically)
        {
            EnsureXPUI();
        }

        ApplyUILayout();
        RefreshUI();
    }

    private void NormalizeValues()
    {
        startLevel = Mathf.Max(1, startLevel);
        maxLevel = Mathf.Max(startLevel, maxLevel);
        baseXPToNextLevel = Mathf.Max(1, baseXPToNextLevel);
        xpRequirementMultiplier = Mathf.Max(1f, xpRequirementMultiplier);

        currentLevel = Mathf.Clamp(currentLevel, startLevel, maxLevel);
        currentXP = Mathf.Max(0, currentXP);

        if (IsMaxLevel)
        {
            currentXP = 0;
        }
        else
        {
            currentXP = Mathf.Clamp(currentXP, 0, XPToNextLevel - 1);
        }

        levelFontSize = Mathf.Max(8, levelFontSize);
        xpFontSize = Mathf.Max(8, xpFontSize);
    }

    private void ResolveReferences()
    {
        if (useSaveSlotManager && saveSlotManager == null)
        {
            saveSlotManager = PlayerSaveSlotManager.Instance;
        }

        if (useSaveSlotManager && saveSlotManager == null)
        {
            saveSlotManager = FindObjectOfType<PlayerSaveSlotManager>();
        }

        if (hudParent == null)
        {
            GameObject foundHUD = GameObject.Find("PlayerHUD");

            if (foundHUD != null)
            {
                hudParent = foundHUD.GetComponent<RectTransform>();
            }
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
        saveSlotManager.OnSaveSlotCleared -= HandleSaveSlotCleared;

        saveSlotManager.OnSaveSlotSelected += HandleSaveSlotSelected;
        saveSlotManager.OnNewGameStartedInSlot += HandleNewGameStartedInSlot;
        saveSlotManager.OnGameSaved += HandleGameSaved;
        saveSlotManager.OnSaveSlotCleared += HandleSaveSlotCleared;
    }

    private void UnsubscribeFromSaveSlotManager()
    {
        if (saveSlotManager == null)
            return;

        saveSlotManager.OnSaveSlotSelected -= HandleSaveSlotSelected;
        saveSlotManager.OnNewGameStartedInSlot -= HandleNewGameStartedInSlot;
        saveSlotManager.OnGameSaved -= HandleGameSaved;
        saveSlotManager.OnSaveSlotCleared -= HandleSaveSlotCleared;
    }

    private void HandleSaveSlotSelected(int slotIndex)
    {
        if (!loadXPAfterSlotSelected)
            return;

        hasLoadedForCurrentSlot = false;
        TryLoadForCurrentSaveSlot();

        if (logActions)
        {
            Debug.Log($"PlayerXP loaded for Save {slotIndex}. Level: {currentLevel}, XP: {currentXP}", this);
        }
    }

    private void HandleNewGameStartedInSlot(int slotIndex)
    {
        if (!resetXPOnNewGame)
            return;

        ResetXPProgress();
        SaveXPProgress();

        if (logActions)
        {
            Debug.Log($"PlayerXP reset for New Game in Save {slotIndex}.", this);
        }
    }

    private void HandleGameSaved(int slotIndex)
    {
        SaveXPProgress();
    }

    private void HandleSaveSlotCleared(int slotIndex)
    {
        if (saveSlotManager == null)
            return;

        PlayerPrefs.DeleteKey(saveSlotManager.GetSlotKey(slotIndex, LevelBaseKey));
        PlayerPrefs.DeleteKey(saveSlotManager.GetSlotKey(slotIndex, CurrentXPBaseKey));
        PlayerPrefs.Save();

        if (logActions)
        {
            Debug.Log($"PlayerXP cleared for Save {slotIndex}.", this);
        }
    }

    public void AddExperience(int amount)
    {
        if (amount <= 0)
            return;

        if (!CanModifyXP())
            return;

        if (IsMaxLevel)
        {
            RefreshUI();
            return;
        }

        currentXP += amount;
        bool leveledUp = false;

        while (!IsMaxLevel && currentXP >= XPToNextLevel)
        {
            currentXP -= XPToNextLevel;
            currentLevel++;
            currentLevel = Mathf.Clamp(currentLevel, startLevel, maxLevel);
            leveledUp = true;

            if (IsMaxLevel)
            {
                currentXP = 0;
            }

            HandleLevelUp(currentLevel);

            if (IsMaxLevel)
                break;
        }

        NormalizeValues();
        RefreshUI();

        if (saveOnEveryXPChange)
        {
            SaveXPProgress();
        }

        OnXPChanged?.Invoke(currentXP, XPToNextLevel);

        if (leveledUp)
        {
            OnLevelChanged?.Invoke(currentLevel);
        }

        if (logActions)
        {
            Debug.Log($"XP Added: +{amount} | Level: {currentLevel} | XP: {currentXP}/{XPToNextLevel}", this);
        }
    }

    private void HandleLevelUp(int newLevel)
    {
        OnLevelUp?.Invoke(newLevel);

        if (logActions)
        {
            Debug.Log($"LEVEL UP! New Level: {newLevel}", this);
        }
    }

    public int CalculateXPToNextLevel(int level)
    {
        int safeLevel = Mathf.Max(startLevel, level);
        int levelOffset = safeLevel - startLevel;

        float rawXP = baseXPToNextLevel * Mathf.Pow(xpRequirementMultiplier, levelOffset);
        int roundedXP = Mathf.RoundToInt(rawXP);

        return Mathf.Max(1, roundedXP);
    }

    private bool CanModifyXP()
    {
        if (!useSaveSlotManager)
            return true;

        ResolveReferences();

        if (saveSlotManager == null)
            return true;

        if (!saveSlotManager.HasSelectedSlot)
        {
            if (logActions)
            {
                Debug.LogWarning("PlayerXP cannot be modified before selecting a save slot.", this);
            }

            return false;
        }

        return true;
    }

    private bool TryLoadForCurrentSaveSlot()
    {
        if (!saveXP)
            return false;

        if (hasLoadedForCurrentSlot)
            return true;

        if (!useSaveSlotManager)
        {
            LoadXPProgress();
            hasLoadedForCurrentSlot = true;
            return true;
        }

        ResolveReferences();

        if (saveSlotManager == null)
        {
            LoadXPProgress();
            hasLoadedForCurrentSlot = true;

            if (logActions)
            {
                Debug.LogWarning("PlayerXP could not find PlayerSaveSlotManager. Falling back to non-slot XP save keys.", this);
            }

            return true;
        }

        if (!saveSlotManager.HasSelectedSlot)
        {
            currentLevel = startLevel;
            currentXP = 0;
            RefreshUI();

            if (logActions)
            {
                Debug.Log("PlayerXP is waiting for save slot selection.", this);
            }

            return false;
        }

        LoadXPProgress();
        hasLoadedForCurrentSlot = true;
        return true;
    }

    public void SaveXPProgress()
    {
        if (!saveXP)
            return;

        if (useSaveSlotManager)
        {
            ResolveReferences();

            if (saveSlotManager != null && !saveSlotManager.HasSelectedSlot)
            {
                if (logActions)
                {
                    Debug.Log("PlayerXP cannot save because no save slot is selected.", this);
                }

                return;
            }
        }

        NormalizeValues();

        PlayerPrefs.SetInt(LevelKey, currentLevel);
        PlayerPrefs.SetInt(CurrentXPKey, currentXP);

        if (useSaveSlotManager && saveSlotManager != null && saveSlotManager.HasSelectedSlot)
        {
            saveSlotManager.MarkSelectedSlotHasData();
        }

        PlayerPrefs.Save();

        if (logActions)
        {
            Debug.Log($"PlayerXP saved. Level: {currentLevel}, XP: {currentXP} | Keys: {LevelKey}, {CurrentXPKey}", this);
        }
    }

    public void LoadXPProgress()
    {
        currentLevel = PlayerPrefs.GetInt(LevelKey, startLevel);
        currentXP = PlayerPrefs.GetInt(CurrentXPKey, 0);

        NormalizeValues();
        RefreshUI();

        if (logActions)
        {
            Debug.Log($"PlayerXP loaded. Level: {currentLevel}, XP: {currentXP} | Keys: {LevelKey}, {CurrentXPKey}", this);
        }
    }

    public void ResetXPProgress()
    {
        currentLevel = startLevel;
        currentXP = 0;

        NormalizeValues();
        RefreshUI();

        if (saveXP)
        {
            SaveXPProgress();
        }
    }

    private string GetSaveKey(string baseKey)
    {
        if (string.IsNullOrWhiteSpace(baseKey))
        {
            baseKey = "PlayerXP_UnknownKey";
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

    private void EnsureXPUI()
    {
        if (!createXPUI)
            return;

        EnsureHUDParent();

        if (hudParent == null)
            return;

        Transform existingPanel = hudParent.Find(generatedPanelName);

        if (existingPanel == null)
        {
            GameObject panelObject = new GameObject(generatedPanelName);
            panelObject.transform.SetParent(hudParent, false);

            panelRect = panelObject.AddComponent<RectTransform>();
            panelImage = panelObject.AddComponent<Image>();

            iconImage = CreateUIImage("XPIcon", panelRect);
            iconRect = iconImage.rectTransform;

            levelText = CreateUIText("LevelText", panelRect);
            xpText = CreateUIText("XPText", panelRect);

            barBackgroundImage = CreateUIImage("XPBar_Background", panelRect);
            barBackgroundRect = barBackgroundImage.rectTransform;

            barFillImage = CreateUIImage("XPBar_Fill", panelRect);
            barFillRect = barFillImage.rectTransform;

            barFrameImage = CreateUIImage("XPBar_Frame", panelRect);
            barFrameRect = barFrameImage.rectTransform;
        }
        else
        {
            panelRect = existingPanel.GetComponent<RectTransform>();

            if (panelRect == null)
            {
                panelRect = existingPanel.gameObject.AddComponent<RectTransform>();
            }

            panelImage = existingPanel.GetComponent<Image>();

            if (panelImage == null)
            {
                panelImage = existingPanel.gameObject.AddComponent<Image>();
            }

            iconImage = GetOrCreateImage("XPIcon", panelRect);
            iconRect = iconImage.rectTransform;

            levelText = GetOrCreateText("LevelText", panelRect);
            xpText = GetOrCreateText("XPText", panelRect);

            barBackgroundImage = GetOrCreateImage("XPBar_Background", panelRect);
            barBackgroundRect = barBackgroundImage.rectTransform;

            barFillImage = GetOrCreateImage("XPBar_Fill", panelRect);
            barFillRect = barFillImage.rectTransform;

            barFrameImage = GetOrCreateImage("XPBar_Frame", panelRect);
            barFrameRect = barFrameImage.rectTransform;
        }

        ApplyUILayout();
        RefreshUI();
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
            Debug.LogWarning("[PlayerXP] PlayerHUD bulunamadý. Hud Parent alanýna PlayerHUD objesini sürükle.", this);
            missingHUDWarningShown = true;
        }
    }

    private Image CreateUIImage(string objectName, RectTransform parent)
    {
        GameObject imageObject = new GameObject(objectName);
        imageObject.transform.SetParent(parent, false);
        imageObject.AddComponent<RectTransform>();

        Image image = imageObject.AddComponent<Image>();
        image.raycastTarget = false;
        image.preserveAspect = false;

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
            image.preserveAspect = false;
            return image;
        }

        return CreateUIImage(objectName, parent);
    }

    private Text CreateUIText(string objectName, RectTransform parent)
    {
        GameObject textObject = new GameObject(objectName);
        textObject.transform.SetParent(parent, false);
        textObject.AddComponent<RectTransform>();

        Text text = textObject.AddComponent<Text>();
        text.raycastTarget = false;
        text.supportRichText = true;
        text.horizontalOverflow = HorizontalWrapMode.Overflow;
        text.verticalOverflow = VerticalWrapMode.Truncate;
        text.resizeTextForBestFit = true;

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
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            text.resizeTextForBestFit = true;

            return text;
        }

        return CreateUIText(objectName, parent);
    }

    private void ApplyUILayout()
    {
        if (!createXPUI)
            return;

        if (panelRect == null)
            return;

        panelRect.anchorMin = new Vector2(0.5f, 1f);
        panelRect.anchorMax = new Vector2(0.5f, 1f);
        panelRect.pivot = new Vector2(0.5f, 1f);
        panelRect.anchoredPosition = panelAnchoredPosition;
        panelRect.sizeDelta = panelSize;

        if (panelImage != null)
        {
            panelImage.enabled = showPanelBackground;
            panelImage.color = GetColor(panelColorHex);
            panelImage.raycastTarget = false;
        }

        ApplyIconLayout();
        ApplyBarLayout();
        ApplyTextLayout();
        ApplySiblingOrder();
    }

    private void ApplyIconLayout()
    {
        if (iconRect != null)
        {
            iconRect.anchorMin = new Vector2(0.5f, 0.5f);
            iconRect.anchorMax = new Vector2(0.5f, 0.5f);
            iconRect.pivot = new Vector2(0.5f, 0.5f);
            iconRect.anchoredPosition = iconPosition;
            iconRect.sizeDelta = iconSize;
        }

        if (iconImage != null)
        {
            iconImage.gameObject.SetActive(showXPIcon);
            iconImage.sprite = xpIconSprite;
            iconImage.color = xpIconSprite != null ? Color.white : GetColor(fallbackIconColorHex);
            iconImage.preserveAspect = true;
        }
    }

    private void ApplyBarLayout()
    {
        SetupBarImage(
            barBackgroundRect,
            barBackgroundImage,
            xpBarBackgroundSprite,
            GetColor(barBackgroundColorHex),
            false
        );

        SetupBarImage(
            barFillRect,
            barFillImage,
            xpBarFillSprite,
            useSpriteBarVisuals && xpBarFillSprite != null ? GetColor(barFillColorHex) : GetColor(fallbackBarFillColorHex),
            true
        );

        SetupBarImage(
            barFrameRect,
            barFrameImage,
            xpBarFrameSprite,
            GetColor(barFrameColorHex),
            false
        );

        if (barFillImage != null)
        {
            if (useSpriteBarVisuals && xpBarFillSprite != null)
            {
                barFillImage.type = Image.Type.Filled;
                barFillImage.fillMethod = Image.FillMethod.Horizontal;
                barFillImage.fillOrigin = (int)Image.OriginHorizontal.Left;
                barFillImage.fillAmount = XPPercent;
            }
            else
            {
                barFillImage.type = Image.Type.Simple;
                barFillImage.fillAmount = 1f;

                if (barFillRect != null)
                {
                    barFillRect.anchorMin = new Vector2(0.5f, 0.5f);
                    barFillRect.anchorMax = new Vector2(0.5f, 0.5f);
                    barFillRect.pivot = new Vector2(0f, 0.5f);
                    barFillRect.anchoredPosition = new Vector2(
                        barPosition.x - (barSize.x * 0.5f),
                        barPosition.y
                    );
                    barFillRect.sizeDelta = new Vector2(barSize.x * XPPercent, barSize.y);
                }
            }
        }
    }

    private void SetupBarImage(RectTransform rect, Image image, Sprite sprite, Color color, bool isFill)
    {
        if (rect != null)
        {
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = barPosition;
            rect.sizeDelta = barSize;
        }

        if (image != null)
        {
            image.sprite = useSpriteBarVisuals ? sprite : null;
            image.color = color;
            image.raycastTarget = false;
            image.preserveAspect = false;

            if (!isFill)
            {
                image.type = Image.Type.Simple;
                image.fillAmount = 1f;
            }
        }
    }

    private void ApplyTextLayout()
    {
        SetupText(
            levelText,
            levelTextPosition,
            levelTextSize,
            levelFontSize,
            FontStyle.Bold,
            GetColor(levelTextColorHex),
            TextAnchor.MiddleCenter
        );

        SetupText(
            xpText,
            xpTextPosition,
            xpTextSize,
            xpFontSize,
            FontStyle.Bold,
            GetColor(xpTextColorHex),
            TextAnchor.MiddleCenter
        );
    }

    private void ApplySiblingOrder()
    {
        if (barBackgroundImage != null)
            barBackgroundImage.transform.SetSiblingIndex(0);

        if (barFillImage != null)
            barFillImage.transform.SetSiblingIndex(1);

        if (barFrameImage != null)
            barFrameImage.transform.SetSiblingIndex(2);

        if (iconImage != null)
            iconImage.transform.SetSiblingIndex(3);

        if (levelText != null)
            levelText.transform.SetSiblingIndex(4);

        if (xpText != null)
            xpText.transform.SetSiblingIndex(5);
    }

    private void SetupText(
        Text targetText,
        Vector2 position,
        Vector2 size,
        int fontSize,
        FontStyle fontStyle,
        Color color,
        TextAnchor alignment
    )
    {
        if (targetText == null)
            return;

        RectTransform rect = targetText.GetComponent<RectTransform>();

        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;

        targetText.font = uiFont != null ? uiFont : GetDefaultFont();
        targetText.fontSize = fontSize;
        targetText.fontStyle = fontStyle;
        targetText.color = color;
        targetText.alignment = alignment;
        targetText.resizeTextForBestFit = true;
        targetText.resizeTextMinSize = Mathf.Max(10, fontSize - 8);
        targetText.resizeTextMaxSize = fontSize;
    }

    private void RefreshUI()
    {
        if (createXPUI && panelRect == null)
        {
            EnsureXPUI();
        }

        if (levelText != null)
        {
            levelText.text = levelPrefix + currentLevel;
        }

        if (xpText != null)
        {
            if (IsMaxLevel)
            {
                xpText.text = maxLevelText;
            }
            else
            {
                xpText.text = xpPrefix + currentXP + " / " + XPToNextLevel;
            }
        }

        if (barFillImage != null)
        {
            if (useSpriteBarVisuals && xpBarFillSprite != null)
            {
                barFillImage.type = Image.Type.Filled;
                barFillImage.fillMethod = Image.FillMethod.Horizontal;
                barFillImage.fillOrigin = (int)Image.OriginHorizontal.Left;
                barFillImage.fillAmount = XPPercent;
            }
        }
    }

    private Font GetDefaultFont()
    {
        if (cachedDefaultFont != null)
            return cachedDefaultFont;

        cachedDefaultFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        if (cachedDefaultFont == null)
        {
            cachedDefaultFont = Font.CreateDynamicFontFromOSFont("Arial", 14);
        }

        return cachedDefaultFont;
    }

    private Color GetColor(string hex)
    {
        if (ColorUtility.TryParseHtmlString(hex, out Color color))
        {
            return color;
        }

        return Color.white;
    }

    [ContextMenu("Refresh XP UI Now")]
    private void DebugRefreshXPUI()
    {
        LiveRefreshUI();
    }

    [ContextMenu("Rebuild Generated XP UI")]
    private void DebugRebuildGeneratedXPUI()
    {
        RebuildGeneratedXPUI();
    }

    public void RebuildGeneratedXPUI()
    {
        if (!createXPUI)
            return;

        EnsureHUDParent();

        if (hudParent == null)
            return;

        Transform existingPanel = hudParent.Find(generatedPanelName);

        if (existingPanel != null)
        {
            if (Application.isPlaying)
            {
                Destroy(existingPanel.gameObject);
            }
            else
            {
                DestroyImmediate(existingPanel.gameObject);
            }
        }

        panelRect = null;
        panelImage = null;
        iconImage = null;
        iconRect = null;
        levelText = null;
        xpText = null;
        barBackgroundRect = null;
        barBackgroundImage = null;
        barFillRect = null;
        barFillImage = null;
        barFrameRect = null;
        barFrameImage = null;

        EnsureXPUI();
        RefreshUI();
        ApplyUILayout();
    }

    [ContextMenu("Debug Add XP")]
    private void DebugAddXP()
    {
        AddExperience(debugXPAmount);
    }

    [ContextMenu("Debug Add 100 XP")]
    private void DebugAdd100XP()
    {
        AddExperience(100);
    }

    [ContextMenu("Debug Save XP")]
    private void DebugSaveXP()
    {
        SaveXPProgress();
    }

    [ContextMenu("Debug Load XP")]
    private void DebugLoadXP()
    {
        LoadXPProgress();
    }

    [ContextMenu("Debug Reset XP")]
    private void DebugResetXP()
    {
        ResetXPProgress();
    }

    [ContextMenu("Debug Print XP")]
    private void DebugPrintXP()
    {
        Debug.Log(
            $"PlayerXP | Level: {currentLevel}/{maxLevel} | XP: {currentXP}/{XPToNextLevel} | Percent: {XPPercent}",
            this
        );
    }
}