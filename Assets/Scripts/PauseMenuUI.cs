using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class PauseMenuUI : MonoBehaviour
{
    public enum MenuButtonAction
    {
        None,
        ResumeGame,
        OpenSettings,
        OpenPauseMenu,
        GoToMainMenu,
        QuitGame,
        ToggleSound,
        ApplySettings,
        OpenSaveSlots
    }

    private enum AnchorPreset
    {
        Center,
        TopRight
    }

    [Serializable]
    public class MenuButtonConfig
    {
        public bool isEnabled = true;
        public string buttonName = "Button";
        public Sprite buttonSprite;
        public Sprite alternateSprite;
        public bool useSoundStateSprites = false;
        public MenuButtonAction action = MenuButtonAction.None;

        [Header("Optional Label")]
        public string label = "";
        public bool forceShowLabel = false;

        [Header("Optional Custom Layout")]
        public bool useCustomPosition = false;
        public Vector2 customPosition = Vector2.zero;
        public bool useCustomSize = false;
        public Vector2 customSize = new Vector2(92f, 92f);

        public MenuButtonConfig()
        {
        }

        public MenuButtonConfig(string name, MenuButtonAction targetAction)
        {
            buttonName = name;
            label = name;
            action = targetAction;
        }
    }

    private class SoundButtonBinding
    {
        public Image image;
        public MenuButtonConfig config;

        public SoundButtonBinding(Image image, MenuButtonConfig config)
        {
            this.image = image;
            this.config = config;
        }
    }

    [Header("Canvas")]
    [SerializeField] private Canvas targetCanvas;

    [Header("Scene")]
    [Tooltip("Ana menü sahnen varsa adını yaz. Örnek: MainMenu. Boş kalırsa Home butonu sadece uyarı verir.")]
    [SerializeField] private string mainMenuSceneName = "";

    [Header("Save Slots")]
    [SerializeField] private SaveSlotSelectionUI saveSlotSelectionUI;
    [SerializeField] private PlayerSaveSlotManager saveSlotManager;
    [SerializeField] private bool saveCurrentGameBeforeOpeningSaveSlots = true;

    [Header("Top Right Menu Button")]
    [SerializeField] private Sprite menuButtonSprite;
    [SerializeField] private Vector2 menuButtonSize = new Vector2(76f, 76f);
    [SerializeField] private Vector2 menuButtonPosition = new Vector2(-90f, -60f);

    [Header("Pause Panel Buttons")]
    [SerializeField]
    private List<MenuButtonConfig> pausePanelButtons = new List<MenuButtonConfig>
    {
        new MenuButtonConfig("Resume", MenuButtonAction.ResumeGame),
        new MenuButtonConfig("Settings", MenuButtonAction.OpenSettings),
        new MenuButtonConfig("Save", MenuButtonAction.OpenSaveSlots),
        new MenuButtonConfig("Home", MenuButtonAction.GoToMainMenu)
    };

    [Header("Settings Panel Buttons")]
    [SerializeField]
    private List<MenuButtonConfig> settingsPanelButtons = new List<MenuButtonConfig>
    {
        new MenuButtonConfig("Sound", MenuButtonAction.ToggleSound)
        {
            useSoundStateSprites = true
        },
        new MenuButtonConfig("Apply", MenuButtonAction.ApplySettings),
        new MenuButtonConfig("Back", MenuButtonAction.OpenPauseMenu)
    };

    [Header("Panel Layout")]
    [SerializeField] private Vector2 panelSize = new Vector2(560f, 340f);
    [SerializeField] private Vector2 defaultIconButtonSize = new Vector2(92f, 92f);

    [Header("Pause Panel Layout")]
    [SerializeField] private Vector2 pausePanelButtonStartPosition = new Vector2(0f, 5f);
    [SerializeField] private int pauseButtonsPerRow = 4;
    [SerializeField] private float pauseColumnSpacing = 120f;
    [SerializeField] private float pauseRowSpacing = 118f;
    [SerializeField] private Vector2 pauseTitlePosition = new Vector2(0f, 105f);

    [Header("Settings Panel Layout")]
    [SerializeField] private Vector2 settingsPanelButtonStartPosition = new Vector2(0f, 5f);
    [SerializeField] private int settingsButtonsPerRow = 4;
    [SerializeField] private float settingsColumnSpacing = 120f;
    [SerializeField] private float settingsRowSpacing = 118f;
    [SerializeField] private Vector2 settingsTitlePosition = new Vector2(0f, 105f);

    [Header("Colors - Hex")]
    [SerializeField] private string dimBackgroundColorHex = "#00000099";
    [SerializeField] private string panelColorHex = "#1E1A16E6";
    [SerializeField] private string fallbackButtonColorHex = "#6B4A2ECC";
    [SerializeField] private string titleColorHex = "#FFD36A";
    [SerializeField] private string labelColorHex = "#FFF3D0";

    [Header("Text")]
    [SerializeField] private Font customFont;
    [SerializeField] private bool showPanelTitles = true;
    [SerializeField] private bool showButtonLabels = false;
    [SerializeField] private int titleFontSize = 38;
    [SerializeField] private int labelFontSize = 20;

    [Header("Behaviour")]
    [SerializeField] private bool hideMenuButtonWhenPaused = true;
    [SerializeField] private bool pauseAudioWithGame = false;

    [Header("Live Play Mode Editing")]
    [Tooltip("Açıkken Play Mode sırasında Inspector'daki değişiklikleri otomatik algılar ve pause/settings UI'ını yeniden kurar.")]
    [SerializeField] private bool autoRefreshInPlayMode = true;

    [Tooltip("Inspector değişikliklerini kaç saniyede bir kontrol edeceğini belirler. Düşük değer daha hızlı tepki verir.")]
    [SerializeField] private float liveRefreshCheckInterval = 0.15f;

    private GameObject menuButtonRoot;
    private GameObject overlayRoot;
    private RectTransform pausePanel;
    private RectTransform settingsPanel;

    private readonly List<SoundButtonBinding> soundButtonBindings = new List<SoundButtonBinding>();

    private bool isPaused;
    private bool soundEnabled = true;
    private float previousTimeScale = 1f;

    private Color dimBackgroundColor;
    private Color panelColor;
    private Color fallbackButtonColor;
    private Color titleColor;
    private Color labelColor;

    private int lastInspectorHash;
    private bool rebuildQueued;
    private float nextLiveRefreshCheckTime;

    private const string SoundPrefsKey = "PauseMenu_SoundEnabled";

    private Font RuntimeFont
    {
        get
        {
            if (customFont != null)
                return customFont;

            Font legacyFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            if (legacyFont != null)
                return legacyFont;

            return Font.CreateDynamicFontFromOSFont("Arial", 14);
        }
    }

    private void Awake()
    {
        ParseColors();
        EnsureCanvas();
        EnsureEventSystem();
        ResolveSaveReferences();

        LoadSoundState();
        ApplySoundStateToAudio();

        BuildRuntimeUI();
        SetOverlayVisible(false);

        lastInspectorHash = CalculateInspectorHash();
    }

    private void Update()
    {
        if (!Application.isPlaying)
            return;

        if (rebuildQueued)
        {
            rebuildQueued = false;
            RefreshRuntimeUIFromInspector();
            return;
        }

        if (!autoRefreshInPlayMode)
            return;

        if (Time.unscaledTime < nextLiveRefreshCheckTime)
            return;

        nextLiveRefreshCheckTime = Time.unscaledTime + Mathf.Max(0.05f, liveRefreshCheckInterval);

        int currentHash = CalculateInspectorHash();

        if (currentHash == lastInspectorHash)
            return;

        lastInspectorHash = currentHash;
        RefreshRuntimeUIFromInspector();
    }

    private void OnValidate()
    {
        pauseButtonsPerRow = Mathf.Max(1, pauseButtonsPerRow);
        settingsButtonsPerRow = Mathf.Max(1, settingsButtonsPerRow);

        pauseColumnSpacing = Mathf.Max(1f, pauseColumnSpacing);
        pauseRowSpacing = Mathf.Max(1f, pauseRowSpacing);

        settingsColumnSpacing = Mathf.Max(1f, settingsColumnSpacing);
        settingsRowSpacing = Mathf.Max(1f, settingsRowSpacing);

        liveRefreshCheckInterval = Mathf.Max(0.05f, liveRefreshCheckInterval);

        if (Application.isPlaying && autoRefreshInPlayMode)
            rebuildQueued = true;
    }

    private void OnDisable()
    {
        if (isPaused)
        {
            Time.timeScale = previousTimeScale <= 0f ? 1f : previousTimeScale;

            if (pauseAudioWithGame)
                AudioListener.pause = false;
        }
    }

    [ContextMenu("Refresh UI Now")]
    public void RefreshRuntimeUIFromInspector()
    {
        if (!Application.isPlaying)
            return;

        bool wasOverlayVisible = overlayRoot != null && overlayRoot.activeSelf;
        bool wasSettingsOpen = settingsPanel != null && settingsPanel.gameObject.activeSelf;

        ClearRuntimeUI();

        ParseColors();
        ResolveSaveReferences();
        BuildRuntimeUI();

        SetOverlayVisible(wasOverlayVisible);

        if (wasSettingsOpen)
            OpenSettingsPanel();
        else
            OpenPausePanel();

        if (menuButtonRoot != null)
            menuButtonRoot.SetActive(!isPaused || !hideMenuButtonWhenPaused);

        lastInspectorHash = CalculateInspectorHash();
    }

    private void ResolveSaveReferences()
    {
        if (saveSlotSelectionUI == null)
        {
            saveSlotSelectionUI = FindObjectOfType<SaveSlotSelectionUI>();
        }

        if (saveSlotManager == null)
        {
            saveSlotManager = PlayerSaveSlotManager.Instance;
        }

        if (saveSlotManager == null)
        {
            saveSlotManager = FindObjectOfType<PlayerSaveSlotManager>();
        }
    }

    private void ParseColors()
    {
        dimBackgroundColor = ParseHex(dimBackgroundColorHex, new Color(0f, 0f, 0f, 0.6f));
        panelColor = ParseHex(panelColorHex, new Color(0.12f, 0.10f, 0.08f, 0.9f));
        fallbackButtonColor = ParseHex(fallbackButtonColorHex, new Color(0.42f, 0.29f, 0.18f, 0.8f));
        titleColor = ParseHex(titleColorHex, new Color(1f, 0.82f, 0.42f, 1f));
        labelColor = ParseHex(labelColorHex, new Color(1f, 0.95f, 0.82f, 1f));
    }

    private Color ParseHex(string hex, Color fallback)
    {
        if (ColorUtility.TryParseHtmlString(hex, out Color parsedColor))
            return parsedColor;

        return fallback;
    }

    private void EnsureCanvas()
    {
        if (targetCanvas != null)
            return;

        targetCanvas = GetComponentInParent<Canvas>();

        if (targetCanvas != null)
            return;

        Canvas foundCanvas = FindObjectOfType<Canvas>();

        if (foundCanvas != null)
        {
            targetCanvas = foundCanvas;
            return;
        }

        GameObject canvasObject = new GameObject("MenuCanvas_Auto");
        targetCanvas = canvasObject.AddComponent<Canvas>();
        targetCanvas.renderMode = RenderMode.ScreenSpaceOverlay;

        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        canvasObject.AddComponent<GraphicRaycaster>();
    }

    private void EnsureEventSystem()
    {
        EventSystem existingEventSystem = FindObjectOfType<EventSystem>();

        if (existingEventSystem != null)
            return;

        GameObject eventSystemObject = new GameObject("EventSystem");
        eventSystemObject.AddComponent<EventSystem>();
        eventSystemObject.AddComponent<StandaloneInputModule>();
    }

    private void LoadSoundState()
    {
        soundEnabled = PlayerPrefs.GetInt(SoundPrefsKey, 1) == 1;
    }

    private void SaveSoundState()
    {
        PlayerPrefs.SetInt(SoundPrefsKey, soundEnabled ? 1 : 0);
        PlayerPrefs.Save();
    }

    private void ApplySoundStateToAudio()
    {
        AudioListener.volume = soundEnabled ? 1f : 0f;
        UpdateSoundButtonVisuals();
    }

    private void BuildRuntimeUI()
    {
        soundButtonBindings.Clear();

        BuildTopRightMenuButton();
        BuildOverlayAndPanels();
    }

    private void ClearRuntimeUI()
    {
        soundButtonBindings.Clear();

        if (menuButtonRoot != null)
        {
            Destroy(menuButtonRoot);
            menuButtonRoot = null;
        }

        if (overlayRoot != null)
        {
            Destroy(overlayRoot);
            overlayRoot = null;
        }

        pausePanel = null;
        settingsPanel = null;
    }

    private void BuildTopRightMenuButton()
    {
        menuButtonRoot = CreateIconButtonObject(
            "Menu_Button",
            targetCanvas.transform,
            menuButtonSprite,
            menuButtonSize,
            menuButtonPosition,
            AnchorPreset.TopRight
        );

        Button button = menuButtonRoot.GetComponent<Button>();
        button.onClick.AddListener(PauseGame);
    }

    private void BuildOverlayAndPanels()
    {
        overlayRoot = new GameObject("Pause_Menu_Overlay", typeof(RectTransform), typeof(Image));
        overlayRoot.transform.SetParent(targetCanvas.transform, false);

        RectTransform overlayRect = overlayRoot.GetComponent<RectTransform>();
        overlayRect.anchorMin = Vector2.zero;
        overlayRect.anchorMax = Vector2.one;
        overlayRect.offsetMin = Vector2.zero;
        overlayRect.offsetMax = Vector2.zero;

        Image overlayImage = overlayRoot.GetComponent<Image>();
        overlayImage.color = dimBackgroundColor;

        pausePanel = CreatePanel("Pause_Panel");
        settingsPanel = CreatePanel("Settings_Panel");

        BuildPausePanel();
        BuildSettingsPanel();

        settingsPanel.gameObject.SetActive(false);
    }

    private RectTransform CreatePanel(string objectName)
    {
        GameObject panelObject = new GameObject(objectName, typeof(RectTransform), typeof(Image));
        panelObject.transform.SetParent(overlayRoot.transform, false);

        RectTransform rect = panelObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = panelSize;
        rect.anchoredPosition = Vector2.zero;

        Image image = panelObject.GetComponent<Image>();
        image.color = panelColor;

        return rect;
    }

    private void BuildPausePanel()
    {
        if (showPanelTitles)
        {
            CreateText(
                "Pause_Title",
                pausePanel,
                "PAUSE",
                titleFontSize,
                titleColor,
                TextAnchor.MiddleCenter,
                pauseTitlePosition,
                new Vector2(panelSize.x - 80f, 60f)
            );
        }

        CreateButtonList(
            pausePanelButtons,
            pausePanel,
            pausePanelButtonStartPosition,
            pauseButtonsPerRow,
            pauseColumnSpacing,
            pauseRowSpacing
        );
    }

    private void BuildSettingsPanel()
    {
        if (showPanelTitles)
        {
            CreateText(
                "Settings_Title",
                settingsPanel,
                "SETTINGS",
                titleFontSize,
                titleColor,
                TextAnchor.MiddleCenter,
                settingsTitlePosition,
                new Vector2(panelSize.x - 80f, 60f)
            );
        }

        CreateButtonList(
            settingsPanelButtons,
            settingsPanel,
            settingsPanelButtonStartPosition,
            settingsButtonsPerRow,
            settingsColumnSpacing,
            settingsRowSpacing
        );
    }

    private void CreateButtonList(
        List<MenuButtonConfig> buttonConfigs,
        Transform parent,
        Vector2 startPosition,
        int buttonsPerRow,
        float columnSpacing,
        float rowSpacing
    )
    {
        List<MenuButtonConfig> activeButtons = new List<MenuButtonConfig>();

        for (int i = 0; i < buttonConfigs.Count; i++)
        {
            if (buttonConfigs[i] != null && buttonConfigs[i].isEnabled)
                activeButtons.Add(buttonConfigs[i]);
        }

        for (int i = 0; i < activeButtons.Count; i++)
        {
            MenuButtonConfig config = activeButtons[i];

            Vector2 position = config.useCustomPosition
                ? config.customPosition
                : CalculateGridPosition(i, activeButtons.Count, buttonsPerRow, startPosition, columnSpacing, rowSpacing);

            Vector2 size = config.useCustomSize ? config.customSize : defaultIconButtonSize;
            Sprite sprite = GetButtonSprite(config);

            GameObject buttonObject = CreateIconButtonObject(
                GetSafeObjectName(config.buttonName, i),
                parent,
                sprite,
                size,
                position,
                AnchorPreset.Center
            );

            Button button = buttonObject.GetComponent<Button>();
            button.onClick.AddListener(() => ExecuteButtonAction(config.action));

            if (config.action == MenuButtonAction.ToggleSound || config.useSoundStateSprites)
            {
                Image buttonImage = buttonObject.GetComponent<Image>();

                if (buttonImage != null)
                    soundButtonBindings.Add(new SoundButtonBinding(buttonImage, config));
            }

            bool shouldShowLabel = showButtonLabels || config.forceShowLabel;

            if (shouldShowLabel && !string.IsNullOrWhiteSpace(config.label))
            {
                CreateButtonLabel(
                    GetSafeObjectName(config.buttonName, i) + "_Label",
                    parent,
                    config.label,
                    new Vector2(position.x, position.y - (size.y * 0.62f))
                );
            }
        }
    }

    private Vector2 CalculateGridPosition(
        int index,
        int totalCount,
        int buttonsPerRow,
        Vector2 startPosition,
        float columnSpacing,
        float rowSpacing
    )
    {
        int row = index / buttonsPerRow;
        int column = index % buttonsPerRow;

        int firstIndexInRow = row * buttonsPerRow;
        int remainingCount = totalCount - firstIndexInRow;
        int countInThisRow = Mathf.Min(buttonsPerRow, remainingCount);

        float rowWidth = (countInThisRow - 1) * columnSpacing;
        float startX = startPosition.x - (rowWidth * 0.5f);

        float x = startX + (column * columnSpacing);
        float y = startPosition.y - (row * rowSpacing);

        return new Vector2(x, y);
    }

    private Sprite GetButtonSprite(MenuButtonConfig config)
    {
        if (config == null)
            return null;

        if (config.useSoundStateSprites || config.action == MenuButtonAction.ToggleSound)
        {
            if (soundEnabled)
                return config.buttonSprite;

            return config.alternateSprite != null ? config.alternateSprite : config.buttonSprite;
        }

        return config.buttonSprite;
    }

    private string GetSafeObjectName(string baseName, int index)
    {
        if (string.IsNullOrWhiteSpace(baseName))
            return "Menu_Button_" + index;

        return baseName.Replace(" ", "_") + "_Button_" + index;
    }

    private GameObject CreateIconButtonObject(
        string objectName,
        Transform parent,
        Sprite sprite,
        Vector2 size,
        Vector2 anchoredPosition,
        AnchorPreset anchorPreset
    )
    {
        GameObject buttonObject = new GameObject(objectName, typeof(RectTransform), typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(parent, false);

        RectTransform rect = buttonObject.GetComponent<RectTransform>();
        ApplyAnchor(rect, anchorPreset);
        rect.sizeDelta = size;
        rect.anchoredPosition = anchoredPosition;

        Image image = buttonObject.GetComponent<Image>();
        image.sprite = sprite;
        image.color = sprite != null ? Color.white : fallbackButtonColor;
        image.preserveAspect = true;

        Button button = buttonObject.GetComponent<Button>();
        button.transition = Selectable.Transition.ColorTint;
        button.colors = CreateIconButtonColors();

        return buttonObject;
    }

    private void ApplyAnchor(RectTransform rect, AnchorPreset anchorPreset)
    {
        if (anchorPreset == AnchorPreset.TopRight)
        {
            rect.anchorMin = new Vector2(1f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            return;
        }

        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
    }

    private void CreateButtonLabel(string objectName, Transform parent, string label, Vector2 position)
    {
        CreateText(
            objectName,
            parent,
            label,
            labelFontSize,
            labelColor,
            TextAnchor.MiddleCenter,
            position,
            new Vector2(150f, 34f)
        );
    }

    private Text CreateText(
        string objectName,
        Transform parent,
        string text,
        int fontSize,
        Color color,
        TextAnchor alignment,
        Vector2 anchoredPosition,
        Vector2 size
    )
    {
        GameObject textObject = new GameObject(objectName, typeof(RectTransform), typeof(Text));
        textObject.transform.SetParent(parent, false);

        RectTransform rect = textObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;
        rect.anchoredPosition = anchoredPosition;

        Text uiText = textObject.GetComponent<Text>();
        uiText.text = text;
        uiText.font = RuntimeFont;
        uiText.fontSize = fontSize;
        uiText.color = color;
        uiText.alignment = alignment;
        uiText.raycastTarget = false;
        uiText.resizeTextForBestFit = true;
        uiText.resizeTextMinSize = Mathf.Max(10, fontSize - 10);
        uiText.resizeTextMaxSize = fontSize;

        return uiText;
    }

    private ColorBlock CreateIconButtonColors()
    {
        ColorBlock colors = ColorBlock.defaultColorBlock;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1f, 1f, 1f, 0.85f);
        colors.pressedColor = new Color(0.8f, 0.8f, 0.8f, 1f);
        colors.selectedColor = Color.white;
        colors.disabledColor = new Color(0.4f, 0.4f, 0.6f);
        colors.colorMultiplier = 1f;
        colors.fadeDuration = 0.08f;

        return colors;
    }

    private void ExecuteButtonAction(MenuButtonAction action)
    {
        switch (action)
        {
            case MenuButtonAction.ResumeGame:
                ResumeGame();
                break;

            case MenuButtonAction.OpenSettings:
                OpenSettingsPanel();
                break;

            case MenuButtonAction.OpenPauseMenu:
                OpenPausePanel();
                break;

            case MenuButtonAction.GoToMainMenu:
                GoToMainMenu();
                break;

            case MenuButtonAction.QuitGame:
                QuitGame();
                break;

            case MenuButtonAction.ToggleSound:
                ToggleSound();
                break;

            case MenuButtonAction.ApplySettings:
                ApplySettings();
                break;

            case MenuButtonAction.OpenSaveSlots:
                OpenSaveSlotsFromPauseMenu();
                break;

            case MenuButtonAction.None:
            default:
                Debug.Log("PauseMenuUI: Bu buton için action seçilmemiş.");
                break;
        }
    }

    public void PauseGame()
    {
        if (isPaused)
            return;

        previousTimeScale = Time.timeScale;
        Time.timeScale = 0f;

        if (pauseAudioWithGame)
            AudioListener.pause = true;

        isPaused = true;

        OpenPausePanel();
        SetOverlayVisible(true);

        if (menuButtonRoot != null && hideMenuButtonWhenPaused)
            menuButtonRoot.SetActive(false);
    }

    public void ResumeGame()
    {
        if (!isPaused)
            return;

        Time.timeScale = previousTimeScale <= 0f ? 1f : previousTimeScale;

        if (pauseAudioWithGame)
            AudioListener.pause = false;

        isPaused = false;

        SetOverlayVisible(false);

        if (menuButtonRoot != null)
            menuButtonRoot.SetActive(true);
    }

    public void OpenPausePanel()
    {
        if (pausePanel != null)
            pausePanel.gameObject.SetActive(true);

        if (settingsPanel != null)
            settingsPanel.gameObject.SetActive(false);
    }

    public void OpenSettingsPanel()
    {
        if (pausePanel != null)
            pausePanel.gameObject.SetActive(false);

        if (settingsPanel != null)
            settingsPanel.gameObject.SetActive(true);

        UpdateSoundButtonVisuals();
    }

    public void OpenSaveSlotsFromPauseMenu()
    {
        ResolveSaveReferences();

        if (saveCurrentGameBeforeOpeningSaveSlots && saveSlotManager != null && saveSlotManager.HasSelectedSlot)
        {
            saveSlotManager.SaveCurrentGameState();
        }

        if (saveSlotSelectionUI == null)
        {
            Debug.LogWarning("PauseMenuUI: SaveSlotSelectionUI bulunamadı. SaveSystem objesinde SaveSlotSelectionUI olduğundan emin ol.");
            return;
        }

        saveSlotSelectionUI.OpenFromPauseMenu();
    }

    public void ToggleSound()
    {
        soundEnabled = !soundEnabled;
        ApplySoundStateToAudio();
    }

    public void ApplySettings()
    {
        SaveSoundState();
        ApplySoundStateToAudio();
        OpenPausePanel();
    }

    private void UpdateSoundButtonVisuals()
    {
        for (int i = 0; i < soundButtonBindings.Count; i++)
        {
            SoundButtonBinding binding = soundButtonBindings[i];

            if (binding == null || binding.image == null || binding.config == null)
                continue;

            binding.image.sprite = GetButtonSprite(binding.config);
            binding.image.color = binding.image.sprite != null ? Color.white : fallbackButtonColor;
        }
    }

    public void GoToMainMenu()
    {
        if (string.IsNullOrWhiteSpace(mainMenuSceneName))
        {
            Debug.LogWarning("PauseMenuUI: Main Menu Scene Name boş. Inspector'dan ana menü sahne adını yazmalısın.");
            return;
        }

        Time.timeScale = 1f;

        if (pauseAudioWithGame)
            AudioListener.pause = false;

        SceneManager.LoadScene(mainMenuSceneName);
    }

    public void QuitGame()
    {
        Time.timeScale = 1f;

        if (pauseAudioWithGame)
            AudioListener.pause = false;

#if UNITY_EDITOR
        EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void SetOverlayVisible(bool visible)
    {
        if (overlayRoot != null)
            overlayRoot.SetActive(visible);
    }

    private int CalculateInspectorHash()
    {
        unchecked
        {
            int hash = 17;

            AddHash(ref hash, targetCanvas != null ? targetCanvas.GetInstanceID() : 0);
            AddHash(ref hash, mainMenuSceneName);

            AddHash(ref hash, saveSlotSelectionUI != null ? saveSlotSelectionUI.GetInstanceID() : 0);
            AddHash(ref hash, saveSlotManager != null ? saveSlotManager.GetInstanceID() : 0);
            AddHash(ref hash, saveCurrentGameBeforeOpeningSaveSlots);

            AddHash(ref hash, menuButtonSprite != null ? menuButtonSprite.GetInstanceID() : 0);
            AddHash(ref hash, menuButtonSize);
            AddHash(ref hash, menuButtonPosition);

            AddButtonListHash(ref hash, pausePanelButtons);
            AddButtonListHash(ref hash, settingsPanelButtons);

            AddHash(ref hash, panelSize);
            AddHash(ref hash, defaultIconButtonSize);

            AddHash(ref hash, pausePanelButtonStartPosition);
            AddHash(ref hash, pauseButtonsPerRow);
            AddHash(ref hash, pauseColumnSpacing);
            AddHash(ref hash, pauseRowSpacing);
            AddHash(ref hash, pauseTitlePosition);

            AddHash(ref hash, settingsPanelButtonStartPosition);
            AddHash(ref hash, settingsButtonsPerRow);
            AddHash(ref hash, settingsColumnSpacing);
            AddHash(ref hash, settingsRowSpacing);
            AddHash(ref hash, settingsTitlePosition);

            AddHash(ref hash, dimBackgroundColorHex);
            AddHash(ref hash, panelColorHex);
            AddHash(ref hash, fallbackButtonColorHex);
            AddHash(ref hash, titleColorHex);
            AddHash(ref hash, labelColorHex);

            AddHash(ref hash, customFont != null ? customFont.GetInstanceID() : 0);
            AddHash(ref hash, showPanelTitles);
            AddHash(ref hash, showButtonLabels);
            AddHash(ref hash, titleFontSize);
            AddHash(ref hash, labelFontSize);

            AddHash(ref hash, hideMenuButtonWhenPaused);
            AddHash(ref hash, pauseAudioWithGame);

            return hash;
        }
    }

    private void AddButtonListHash(ref int hash, List<MenuButtonConfig> list)
    {
        if (list == null)
        {
            AddHash(ref hash, 0);
            return;
        }

        AddHash(ref hash, list.Count);

        for (int i = 0; i < list.Count; i++)
        {
            MenuButtonConfig config = list[i];

            if (config == null)
            {
                AddHash(ref hash, 0);
                continue;
            }

            AddHash(ref hash, config.isEnabled);
            AddHash(ref hash, config.buttonName);
            AddHash(ref hash, config.buttonSprite != null ? config.buttonSprite.GetInstanceID() : 0);
            AddHash(ref hash, config.alternateSprite != null ? config.alternateSprite.GetInstanceID() : 0);
            AddHash(ref hash, config.useSoundStateSprites);
            AddHash(ref hash, (int)config.action);
            AddHash(ref hash, config.label);
            AddHash(ref hash, config.forceShowLabel);
            AddHash(ref hash, config.useCustomPosition);
            AddHash(ref hash, config.customPosition);
            AddHash(ref hash, config.useCustomSize);
            AddHash(ref hash, config.customSize);
        }
    }

    private void AddHash(ref int hash, object value)
    {
        hash = hash * 31 + (value != null ? value.GetHashCode() : 0);
    }
}