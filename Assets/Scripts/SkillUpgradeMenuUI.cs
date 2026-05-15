using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public enum SkillButtonAnchorPreset
{
    TopLeft,
    TopRight,
    BottomLeft,
    BottomRight,
    Custom
}

[Serializable]
public class SkillUpgradeMenuEntry
{
    [Header("State")]
    public bool enabled = true;

    [Header("Stat")]
    public PlayerStatType statType = PlayerStatType.AttackDamage;
    public string title = "Attack Damage";

    [Header("Icon / Sprites")]
    public Sprite iconSprite;
    public Sprite rowBackgroundSprite;
    public Sprite buyButtonSprite;

    [Header("Layout Override")]
    public bool useCustomRowPosition = false;
    public Vector2 customRowAnchoredPosition = Vector2.zero;
    public Vector2 customRowSize = Vector2.zero;

    [Header("Text")]
    public string buyLabel = "BUY";
    public string noCoinLabel = "NO COIN";
    public string maxLabel = "MAX";
}

public class SkillUpgradeMenuUI : MonoBehaviour
{
    [Header("References - Can Be Empty")]
    [SerializeField] private Canvas targetCanvas;
    [SerializeField] private RectTransform skillButtonParent;
    [SerializeField] private RectTransform skillPanelParent;

    [SerializeField] private PlayerStats playerStats;
    [SerializeField] private PlayerWallet playerWallet;
    [SerializeField] private PlayerStatUpgradeController upgradeController;

    [Header("Sorting / Layer Order")]
    [SerializeField] private bool useDedicatedSortingCanvas = true;
    [SerializeField] private int sortingOrder = 6000;
    [SerializeField] private bool bringToFrontWhenOpened = true;

    [Header("Behaviour")]
    [SerializeField] private bool buildOnStart = true;
    [SerializeField] private bool pauseGameWhenOpen = true;
    [SerializeField] private bool restorePreviousTimeScaleOnClose = true;
    [SerializeField] private bool closeWithEscapeKey = true;

    [Header("Live Inspector Refresh")]
    [SerializeField] private bool liveRefreshInPlayMode = true;
    [SerializeField] private bool rebuildWhenInspectorChanges = true;
    [SerializeField] private bool refreshEveryFrameWhileOpen = false;

    [Header("Skill Button")]
    [SerializeField] private bool showSkillButton = true;
    [SerializeField] private SkillButtonAnchorPreset skillButtonAnchorPreset = SkillButtonAnchorPreset.TopRight;
    [SerializeField] private Vector2 customSkillButtonAnchor = new Vector2(1f, 1f);
    [SerializeField] private Vector2 skillButtonPivot = new Vector2(0.5f, 0.5f);
    [SerializeField] private Vector2 skillButtonSize = new Vector2(72f, 72f);
    [SerializeField] private Vector2 skillButtonAnchoredPosition = new Vector2(-90f, -250f);
    [SerializeField] private string skillButtonLabel = "SKILL";
    [SerializeField] private float skillButtonLabelFontSize = 16f;
    [SerializeField] private Sprite skillButtonSprite;

    [Header("Panel Layout")]
    [SerializeField] private Vector2 panelSize = new Vector2(860f, 560f);
    [SerializeField] private Vector2 panelAnchoredPosition = Vector2.zero;
    [SerializeField] private string panelTitle = "Skill Upgrades";

    [Header("Panel Sprites")]
    [SerializeField] private Sprite closeButtonSprite;
    [SerializeField] private Sprite panelBackgroundSprite;
    [SerializeField] private Sprite defaultRowBackgroundSprite;
    [SerializeField] private Sprite defaultBuyButtonSprite;

    [Header("Rows - Add / Remove / Reorder From Inspector")]
    [SerializeField] private List<SkillUpgradeMenuEntry> skillEntries = new List<SkillUpgradeMenuEntry>();

    [Header("Default Row Layout")]
    [SerializeField] private Vector2 rowSize = new Vector2(760f, 118f);
    [SerializeField] private float firstRowY = 90f;
    [SerializeField] private float rowSpacing = 132f;

    [Header("Colors - Hex")]
    [SerializeField] private string overlayColorHex = "#00000099";
    [SerializeField] private string panelColorHex = "#1F1A14F2";
    [SerializeField] private string rowColorHex = "#2C241BE6";
    [SerializeField] private string titleColorHex = "#F5D27A";
    [SerializeField] private string normalTextColorHex = "#FFFFFF";
    [SerializeField] private string mutedTextColorHex = "#C8BBA0";
    [SerializeField] private string affordableColorHex = "#18C964";
    [SerializeField] private string notAffordableColorHex = "#E53935";
    [SerializeField] private string maxedColorHex = "#F5A524";
    [SerializeField] private string buttonColorHex = "#7A4E24";
    [SerializeField] private string buttonDisabledColorHex = "#4A4035";
    [SerializeField] private string iconPlaceholderColorHex = "#7A4E24";

    [Header("Debug")]
    [SerializeField] private bool logActions = true;

    private GameObject generatedLayerObject;
    private RectTransform generatedLayerRect;

    private GameObject skillButtonObject;
    private GameObject panelRootObject;
    private RectTransform panelCardRect;

    private TMP_Text coinText;
    private TMP_Text feedbackText;

    private readonly List<RuntimeSkillRow> runtimeRows = new List<RuntimeSkillRow>();

    private bool isOpen;
    private bool rebuildRequested;
    private float previousTimeScale = 1f;

    private class RuntimeSkillRow
    {
        public SkillUpgradeMenuEntry Entry;
        public GameObject RootObject;
        public Image RowImage;
        public Image IconImage;
        public TMP_Text TitleText;
        public TMP_Text LevelText;
        public TMP_Text ValueText;
        public TMP_Text CostText;
        public Button BuyButton;
        public Image BuyButtonImage;
        public TMP_Text BuyButtonText;
    }

    private void Reset()
    {
        AddDefaultEntriesIfEmpty();

        skillButtonAnchorPreset = SkillButtonAnchorPreset.TopRight;
        skillButtonAnchoredPosition = new Vector2(-90f, -250f);
    }

    private void Awake()
    {
        AddDefaultEntriesIfEmpty();
        ResolveReferences();
    }

    private void OnEnable()
    {
        ResolveReferences();
        SubscribeEvents();
    }

    private void Start()
    {
        ResolveReferences();

        if (buildOnStart)
        {
            BuildUI();
        }

        RefreshUI();
        CloseSkillMenuInstant();
    }

    private void OnDisable()
    {
        UnsubscribeEvents();
    }

    private void OnValidate()
    {
        AddDefaultEntriesIfEmpty();
        NormalizeValues();

        if (Application.isPlaying && liveRefreshInPlayMode && rebuildWhenInspectorChanges)
        {
            rebuildRequested = true;
        }
    }

    private void Update()
    {
        if (rebuildRequested)
        {
            rebuildRequested = false;
            bool wasOpen = isOpen;

            BuildUI();

            if (wasOpen)
            {
                isOpen = true;
                OpenSkillMenuInstant();
            }
            else
            {
                CloseSkillMenuInstant();
            }
        }

        if (isOpen && closeWithEscapeKey && Input.GetKeyDown(KeyCode.Escape))
        {
            CloseSkillMenu();
        }

        if (isOpen && refreshEveryFrameWhileOpen)
        {
            RefreshUI();
        }
    }

    private void AddDefaultEntriesIfEmpty()
    {
        if (skillEntries == null)
        {
            skillEntries = new List<SkillUpgradeMenuEntry>();
        }

        if (skillEntries.Count > 0)
            return;

        skillEntries.Add(new SkillUpgradeMenuEntry
        {
            enabled = true,
            statType = PlayerStatType.AttackDamage,
            title = "Attack Damage",
            buyLabel = "BUY",
            noCoinLabel = "NO COIN",
            maxLabel = "MAX"
        });

        skillEntries.Add(new SkillUpgradeMenuEntry
        {
            enabled = true,
            statType = PlayerStatType.MaxHealth,
            title = "Max Health",
            buyLabel = "BUY",
            noCoinLabel = "NO COIN",
            maxLabel = "MAX"
        });

        skillEntries.Add(new SkillUpgradeMenuEntry
        {
            enabled = true,
            statType = PlayerStatType.Armor,
            title = "Armor",
            buyLabel = "BUY",
            noCoinLabel = "NO COIN",
            maxLabel = "MAX"
        });
    }

    private void NormalizeValues()
    {
        sortingOrder = Mathf.Max(0, sortingOrder);

        skillButtonSize.x = Mathf.Max(1f, skillButtonSize.x);
        skillButtonSize.y = Mathf.Max(1f, skillButtonSize.y);

        skillButtonPivot.x = Mathf.Clamp01(skillButtonPivot.x);
        skillButtonPivot.y = Mathf.Clamp01(skillButtonPivot.y);

        customSkillButtonAnchor.x = Mathf.Clamp01(customSkillButtonAnchor.x);
        customSkillButtonAnchor.y = Mathf.Clamp01(customSkillButtonAnchor.y);

        skillButtonLabelFontSize = Mathf.Max(1f, skillButtonLabelFontSize);

        panelSize.x = Mathf.Max(1f, panelSize.x);
        panelSize.y = Mathf.Max(1f, panelSize.y);

        rowSize.x = Mathf.Max(1f, rowSize.x);
        rowSize.y = Mathf.Max(1f, rowSize.y);

        rowSpacing = Mathf.Max(1f, rowSpacing);
    }

    private void ResolveReferences()
    {
        if (targetCanvas == null)
        {
            targetCanvas = GetComponentInParent<Canvas>();
        }

        if (targetCanvas == null)
        {
            targetCanvas = FindObjectOfType<Canvas>();
        }

        if (playerStats == null)
        {
            playerStats = FindObjectOfType<PlayerStats>();
        }

        if (playerWallet == null)
        {
            playerWallet = FindObjectOfType<PlayerWallet>();
        }

        if (upgradeController == null)
        {
            upgradeController = FindObjectOfType<PlayerStatUpgradeController>();
        }

        if (upgradeController == null && playerStats != null)
        {
            upgradeController = playerStats.GetComponent<PlayerStatUpgradeController>();
        }

        if (upgradeController == null && playerStats != null)
        {
            upgradeController = playerStats.gameObject.AddComponent<PlayerStatUpgradeController>();
            Debug.LogWarning(
                "PlayerStatUpgradeController was missing. SkillUpgradeMenuUI added it automatically to the PlayerStats object.",
                this
            );
        }

        if (skillButtonParent == null && targetCanvas != null)
        {
            skillButtonParent = targetCanvas.transform as RectTransform;
        }

        if (skillPanelParent == null && targetCanvas != null)
        {
            skillPanelParent = targetCanvas.transform as RectTransform;
        }
    }

    private void SubscribeEvents()
    {
        if (playerStats != null)
        {
            playerStats.OnStatsChanged -= RefreshUI;
            playerStats.OnStatsChanged += RefreshUI;
        }

        if (playerWallet != null)
        {
            playerWallet.OnCoinsChanged -= HandleCoinsChanged;
            playerWallet.OnCoinsChanged += HandleCoinsChanged;
        }

        if (upgradeController != null)
        {
            upgradeController.OnUpgradeAttemptFinished -= HandleUpgradeAttemptFinished;
            upgradeController.OnUpgradeAttemptFinished += HandleUpgradeAttemptFinished;
        }
    }

    private void UnsubscribeEvents()
    {
        if (playerStats != null)
        {
            playerStats.OnStatsChanged -= RefreshUI;
        }

        if (playerWallet != null)
        {
            playerWallet.OnCoinsChanged -= HandleCoinsChanged;
        }

        if (upgradeController != null)
        {
            upgradeController.OnUpgradeAttemptFinished -= HandleUpgradeAttemptFinished;
        }
    }

    private void HandleCoinsChanged(int newCoinAmount)
    {
        RefreshUI();
    }

    private void HandleUpgradeAttemptFinished(PlayerStatUpgradeResult result)
    {
        if (feedbackText != null)
        {
            feedbackText.text = result.Message;
            feedbackText.color = result.Success ? GetColor(affordableColorHex) : GetColor(notAffordableColorHex);
        }

        RefreshUI();
    }

    [ContextMenu("Build / Rebuild Skill Upgrade UI")]
    public void BuildUI()
    {
        ResolveReferences();

        if (targetCanvas == null)
        {
            Debug.LogWarning("SkillUpgradeMenuUI could not find a Canvas.", this);
            return;
        }

        DestroyGeneratedUI();
        BuildGeneratedLayer();

        if (showSkillButton)
        {
            BuildSkillButton();
        }

        BuildPanel();
        RefreshUI();
        CloseSkillMenuInstant();

        if (logActions)
        {
            Debug.Log("Skill Upgrade UI rebuilt.", this);
        }
    }

    private void BuildGeneratedLayer()
    {
        Transform parent = targetCanvas.transform;

        generatedLayerObject = CreateUIObject("Skill_Upgrade_Generated_Layer", parent);
        generatedLayerRect = generatedLayerObject.GetComponent<RectTransform>();

        generatedLayerRect.anchorMin = Vector2.zero;
        generatedLayerRect.anchorMax = Vector2.one;
        generatedLayerRect.offsetMin = Vector2.zero;
        generatedLayerRect.offsetMax = Vector2.zero;
        generatedLayerRect.pivot = new Vector2(0.5f, 0.5f);

        if (useDedicatedSortingCanvas)
        {
            Canvas generatedCanvas = generatedLayerObject.AddComponent<Canvas>();
            generatedCanvas.overrideSorting = true;
            generatedCanvas.sortingOrder = sortingOrder;

            GraphicRaycaster raycaster = generatedLayerObject.AddComponent<GraphicRaycaster>();
            raycaster.ignoreReversedGraphics = true;
        }

        generatedLayerObject.transform.SetAsLastSibling();
    }

    private void DestroyGeneratedUI()
    {
        runtimeRows.Clear();

        if (skillButtonObject != null)
        {
            Destroy(skillButtonObject);
            skillButtonObject = null;
        }

        if (panelRootObject != null)
        {
            Destroy(panelRootObject);
            panelRootObject = null;
        }

        if (generatedLayerObject != null)
        {
            Destroy(generatedLayerObject);
            generatedLayerObject = null;
            generatedLayerRect = null;
        }
    }

    private RectTransform GetSafeButtonParent()
    {
        if (useDedicatedSortingCanvas && generatedLayerRect != null)
        {
            return generatedLayerRect;
        }

        if (skillButtonParent != null)
        {
            return skillButtonParent;
        }

        if (targetCanvas != null)
        {
            return targetCanvas.transform as RectTransform;
        }

        return transform as RectTransform;
    }

    private RectTransform GetSafePanelParent()
    {
        if (useDedicatedSortingCanvas && generatedLayerRect != null)
        {
            return generatedLayerRect;
        }

        if (skillPanelParent != null)
        {
            return skillPanelParent;
        }

        if (targetCanvas != null)
        {
            return targetCanvas.transform as RectTransform;
        }

        return transform as RectTransform;
    }

    private Vector2 GetSkillButtonAnchor()
    {
        switch (skillButtonAnchorPreset)
        {
            case SkillButtonAnchorPreset.TopLeft:
                return new Vector2(0f, 1f);

            case SkillButtonAnchorPreset.TopRight:
                return new Vector2(1f, 1f);

            case SkillButtonAnchorPreset.BottomLeft:
                return new Vector2(0f, 0f);

            case SkillButtonAnchorPreset.BottomRight:
                return new Vector2(1f, 0f);

            case SkillButtonAnchorPreset.Custom:
                return customSkillButtonAnchor;

            default:
                return new Vector2(1f, 1f);
        }
    }

    private void BuildSkillButton()
    {
        RectTransform parent = GetSafeButtonParent();

        if (parent == null)
            return;

        skillButtonObject = CreateUIObject("Skill_Upgrade_Button", parent);

        RectTransform buttonRect = skillButtonObject.GetComponent<RectTransform>();

        Vector2 buttonAnchor = GetSkillButtonAnchor();

        buttonRect.anchorMin = buttonAnchor;
        buttonRect.anchorMax = buttonAnchor;
        buttonRect.pivot = skillButtonPivot;
        buttonRect.sizeDelta = skillButtonSize;
        buttonRect.anchoredPosition = skillButtonAnchoredPosition;

        Image buttonImage = skillButtonObject.AddComponent<Image>();
        buttonImage.sprite = skillButtonSprite;
        buttonImage.color = skillButtonSprite != null ? Color.white : GetColor(buttonColorHex);
        buttonImage.raycastTarget = true;

        Button button = skillButtonObject.AddComponent<Button>();
        button.targetGraphic = buttonImage;
        button.onClick.AddListener(OpenSkillMenu);

        TMP_Text label = CreateText(
            "Skill_Button_Label",
            skillButtonObject.transform,
            skillButtonLabel,
            skillButtonLabelFontSize,
            GetColor(normalTextColorHex),
            TextAlignmentOptions.Center
        );

        RectTransform labelRect = label.GetComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;

        skillButtonObject.transform.SetAsLastSibling();
    }

    private void BuildPanel()
    {
        RectTransform parent = GetSafePanelParent();

        if (parent == null)
            return;

        panelRootObject = CreateUIObject("Skill_Upgrade_Panel_Root", parent);

        RectTransform rootRect = panelRootObject.GetComponent<RectTransform>();
        rootRect.anchorMin = Vector2.zero;
        rootRect.anchorMax = Vector2.one;
        rootRect.pivot = new Vector2(0.5f, 0.5f);
        rootRect.offsetMin = Vector2.zero;
        rootRect.offsetMax = Vector2.zero;

        Image overlayImage = panelRootObject.AddComponent<Image>();
        overlayImage.color = GetColor(overlayColorHex);
        overlayImage.raycastTarget = true;

        GameObject cardObject = CreateUIObject("Skill_Upgrade_Panel", panelRootObject.transform);
        panelCardRect = cardObject.GetComponent<RectTransform>();
        panelCardRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelCardRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelCardRect.pivot = new Vector2(0.5f, 0.5f);
        panelCardRect.sizeDelta = panelSize;
        panelCardRect.anchoredPosition = panelAnchoredPosition;

        Image cardImage = cardObject.AddComponent<Image>();
        cardImage.sprite = panelBackgroundSprite;
        cardImage.color = panelBackgroundSprite != null ? Color.white : GetColor(panelColorHex);
        cardImage.raycastTarget = true;

        BuildPanelTitle(cardObject.transform);
        BuildCloseButton(cardObject.transform);
        BuildCoinText(cardObject.transform);
        BuildFeedbackText(cardObject.transform);
        BuildRows(cardObject.transform);

        panelRootObject.transform.SetAsLastSibling();
    }

    private void BuildPanelTitle(Transform parent)
    {
        TMP_Text title = CreateText(
            "Skill_Panel_Title",
            parent,
            panelTitle,
            38f,
            GetColor(titleColorHex),
            TextAlignmentOptions.Center
        );

        RectTransform titleRect = title.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0.5f, 1f);
        titleRect.anchorMax = new Vector2(0.5f, 1f);
        titleRect.pivot = new Vector2(0.5f, 0.5f);
        titleRect.sizeDelta = new Vector2(600f, 58f);
        titleRect.anchoredPosition = new Vector2(0f, -44f);
    }

    private void BuildCloseButton(Transform parent)
    {
        GameObject closeObject = CreateUIObject("Skill_Close_Button", parent);

        RectTransform closeRect = closeObject.GetComponent<RectTransform>();
        closeRect.anchorMin = new Vector2(1f, 1f);
        closeRect.anchorMax = new Vector2(1f, 1f);
        closeRect.pivot = new Vector2(0.5f, 0.5f);
        closeRect.sizeDelta = new Vector2(58f, 58f);
        closeRect.anchoredPosition = new Vector2(-44f, -44f);

        Image closeImage = closeObject.AddComponent<Image>();
        closeImage.sprite = closeButtonSprite;
        closeImage.color = closeButtonSprite != null ? Color.white : GetColor(buttonColorHex);
        closeImage.raycastTarget = true;

        Button closeButton = closeObject.AddComponent<Button>();
        closeButton.targetGraphic = closeImage;
        closeButton.onClick.AddListener(CloseSkillMenu);

        TMP_Text closeText = CreateText(
            "Close_Text",
            closeObject.transform,
            "X",
            28f,
            GetColor(normalTextColorHex),
            TextAlignmentOptions.Center
        );

        RectTransform closeTextRect = closeText.GetComponent<RectTransform>();
        closeTextRect.anchorMin = Vector2.zero;
        closeTextRect.anchorMax = Vector2.one;
        closeTextRect.offsetMin = Vector2.zero;
        closeTextRect.offsetMax = Vector2.zero;
    }

    private void BuildCoinText(Transform parent)
    {
        coinText = CreateText(
            "Skill_Coin_Text",
            parent,
            "Coins: 0",
            24f,
            GetColor(normalTextColorHex),
            TextAlignmentOptions.Center
        );

        RectTransform coinRect = coinText.GetComponent<RectTransform>();
        coinRect.anchorMin = new Vector2(0.5f, 1f);
        coinRect.anchorMax = new Vector2(0.5f, 1f);
        coinRect.pivot = new Vector2(0.5f, 0.5f);
        coinRect.sizeDelta = new Vector2(360f, 40f);
        coinRect.anchoredPosition = new Vector2(0f, -88f);
    }

    private void BuildFeedbackText(Transform parent)
    {
        feedbackText = CreateText(
            "Skill_Feedback_Text",
            parent,
            "",
            18f,
            GetColor(mutedTextColorHex),
            TextAlignmentOptions.Center
        );

        RectTransform feedbackRect = feedbackText.GetComponent<RectTransform>();
        feedbackRect.anchorMin = new Vector2(0.5f, 0f);
        feedbackRect.anchorMax = new Vector2(0.5f, 0f);
        feedbackRect.pivot = new Vector2(0.5f, 0.5f);
        feedbackRect.sizeDelta = new Vector2(760f, 44f);
        feedbackRect.anchoredPosition = new Vector2(0f, 30f);
    }

    private void BuildRows(Transform parent)
    {
        runtimeRows.Clear();

        int visibleIndex = 0;

        for (int i = 0; i < skillEntries.Count; i++)
        {
            SkillUpgradeMenuEntry entry = skillEntries[i];

            if (entry == null || !entry.enabled)
                continue;

            RuntimeSkillRow row = BuildSkillRow(parent, entry, visibleIndex);
            runtimeRows.Add(row);
            visibleIndex++;
        }
    }

    private RuntimeSkillRow BuildSkillRow(Transform parent, SkillUpgradeMenuEntry entry, int visibleIndex)
    {
        RuntimeSkillRow row = new RuntimeSkillRow();
        row.Entry = entry;

        row.RootObject = CreateUIObject(entry.title + "_Row", parent);

        RectTransform rowRect = row.RootObject.GetComponent<RectTransform>();
        rowRect.anchorMin = new Vector2(0.5f, 0.5f);
        rowRect.anchorMax = new Vector2(0.5f, 0.5f);
        rowRect.pivot = new Vector2(0.5f, 0.5f);

        Vector2 finalRowSize = entry.customRowSize.x > 0f && entry.customRowSize.y > 0f
            ? entry.customRowSize
            : rowSize;

        Vector2 finalRowPosition = entry.useCustomRowPosition
            ? entry.customRowAnchoredPosition
            : new Vector2(0f, firstRowY - (rowSpacing * visibleIndex));

        rowRect.sizeDelta = finalRowSize;
        rowRect.anchoredPosition = finalRowPosition;

        row.RowImage = row.RootObject.AddComponent<Image>();
        row.RowImage.sprite = entry.rowBackgroundSprite != null ? entry.rowBackgroundSprite : defaultRowBackgroundSprite;
        row.RowImage.color = row.RowImage.sprite != null ? Color.white : GetColor(rowColorHex);
        row.RowImage.raycastTarget = true;

        GameObject iconObject = CreateUIObject("Icon", row.RootObject.transform);
        RectTransform iconRect = iconObject.GetComponent<RectTransform>();
        iconRect.anchorMin = new Vector2(0f, 0.5f);
        iconRect.anchorMax = new Vector2(0f, 0.5f);
        iconRect.pivot = new Vector2(0.5f, 0.5f);
        iconRect.sizeDelta = new Vector2(76f, 76f);
        iconRect.anchoredPosition = new Vector2(58f, 0f);

        row.IconImage = iconObject.AddComponent<Image>();
        row.IconImage.sprite = entry.iconSprite;
        row.IconImage.color = entry.iconSprite != null ? Color.white : GetColor(iconPlaceholderColorHex);
        row.IconImage.raycastTarget = false;

        row.TitleText = CreateText(
            "Title",
            row.RootObject.transform,
            entry.title,
            24f,
            GetColor(titleColorHex),
            TextAlignmentOptions.Left
        );

        RectTransform titleRect = row.TitleText.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0f, 1f);
        titleRect.anchorMax = new Vector2(0f, 1f);
        titleRect.pivot = new Vector2(0f, 0.5f);
        titleRect.sizeDelta = new Vector2(280f, 34f);
        titleRect.anchoredPosition = new Vector2(112f, -28f);

        row.LevelText = CreateText(
            "Level",
            row.RootObject.transform,
            "Level: 0/0",
            18f,
            GetColor(mutedTextColorHex),
            TextAlignmentOptions.Left
        );

        RectTransform levelRect = row.LevelText.GetComponent<RectTransform>();
        levelRect.anchorMin = new Vector2(0f, 1f);
        levelRect.anchorMax = new Vector2(0f, 1f);
        levelRect.pivot = new Vector2(0f, 0.5f);
        levelRect.sizeDelta = new Vector2(260f, 28f);
        levelRect.anchoredPosition = new Vector2(112f, -61f);

        row.ValueText = CreateText(
            "Value",
            row.RootObject.transform,
            "Value: 0 → 0",
            18f,
            GetColor(normalTextColorHex),
            TextAlignmentOptions.Left
        );

        RectTransform valueRect = row.ValueText.GetComponent<RectTransform>();
        valueRect.anchorMin = new Vector2(0f, 1f);
        valueRect.anchorMax = new Vector2(0f, 1f);
        valueRect.pivot = new Vector2(0f, 0.5f);
        valueRect.sizeDelta = new Vector2(300f, 28f);
        valueRect.anchoredPosition = new Vector2(112f, -91f);

        row.CostText = CreateText(
            "Cost",
            row.RootObject.transform,
            "Cost: 0",
            20f,
            GetColor(normalTextColorHex),
            TextAlignmentOptions.Center
        );

        RectTransform costRect = row.CostText.GetComponent<RectTransform>();
        costRect.anchorMin = new Vector2(1f, 0.5f);
        costRect.anchorMax = new Vector2(1f, 0.5f);
        costRect.pivot = new Vector2(0.5f, 0.5f);
        costRect.sizeDelta = new Vector2(160f, 34f);
        costRect.anchoredPosition = new Vector2(-190f, 18f);

        GameObject buyButtonObject = CreateUIObject("Buy_Button", row.RootObject.transform);

        RectTransform buyRect = buyButtonObject.GetComponent<RectTransform>();
        buyRect.anchorMin = new Vector2(1f, 0.5f);
        buyRect.anchorMax = new Vector2(1f, 0.5f);
        buyRect.pivot = new Vector2(0.5f, 0.5f);
        buyRect.sizeDelta = new Vector2(130f, 46f);
        buyRect.anchoredPosition = new Vector2(-76f, -22f);

        row.BuyButtonImage = buyButtonObject.AddComponent<Image>();
        row.BuyButtonImage.sprite = entry.buyButtonSprite != null ? entry.buyButtonSprite : defaultBuyButtonSprite;
        row.BuyButtonImage.color = row.BuyButtonImage.sprite != null ? Color.white : GetColor(buttonColorHex);
        row.BuyButtonImage.raycastTarget = true;

        row.BuyButton = buyButtonObject.AddComponent<Button>();
        row.BuyButton.targetGraphic = row.BuyButtonImage;
        row.BuyButton.onClick.AddListener(() => BuyUpgrade(entry.statType));

        row.BuyButtonText = CreateText(
            "Buy_Text",
            buyButtonObject.transform,
            entry.buyLabel,
            20f,
            GetColor(normalTextColorHex),
            TextAlignmentOptions.Center
        );

        RectTransform buyTextRect = row.BuyButtonText.GetComponent<RectTransform>();
        buyTextRect.anchorMin = Vector2.zero;
        buyTextRect.anchorMax = Vector2.one;
        buyTextRect.offsetMin = Vector2.zero;
        buyTextRect.offsetMax = Vector2.zero;

        return row;
    }

    public void OpenSkillMenu()
    {
        if (panelRootObject == null)
        {
            BuildUI();
        }

        if (panelRootObject == null)
            return;

        if (isOpen)
            return;

        isOpen = true;
        previousTimeScale = Time.timeScale;

        if (pauseGameWhenOpen)
        {
            Time.timeScale = 0f;
        }

        OpenSkillMenuInstant();

        if (logActions)
        {
            Debug.Log("Skill Upgrade Menu opened.", this);
        }
    }

    private void OpenSkillMenuInstant()
    {
        if (panelRootObject != null)
        {
            panelRootObject.SetActive(true);

            if (bringToFrontWhenOpened)
            {
                if (generatedLayerObject != null)
                {
                    generatedLayerObject.transform.SetAsLastSibling();
                }

                panelRootObject.transform.SetAsLastSibling();
            }
        }

        RefreshUI();
    }

    public void CloseSkillMenu()
    {
        if (!isOpen)
            return;

        isOpen = false;

        if (panelRootObject != null)
        {
            panelRootObject.SetActive(false);
        }

        if (pauseGameWhenOpen && restorePreviousTimeScaleOnClose)
        {
            Time.timeScale = previousTimeScale;
        }

        if (logActions)
        {
            Debug.Log("Skill Upgrade Menu closed.", this);
        }
    }

    private void CloseSkillMenuInstant()
    {
        isOpen = false;

        if (panelRootObject != null)
        {
            panelRootObject.SetActive(false);
        }
    }

    private void BuyUpgrade(PlayerStatType statType)
    {
        ResolveReferences();

        if (upgradeController == null)
        {
            if (feedbackText != null)
            {
                feedbackText.text = "Upgrade controller is missing.";
                feedbackText.color = GetColor(notAffordableColorHex);
            }

            Debug.LogWarning("SkillUpgradeMenuUI could not find PlayerStatUpgradeController.", this);
            return;
        }

        bool success = upgradeController.TryBuyUpgrade(statType, out PlayerStatUpgradeResult result);

        if (feedbackText != null)
        {
            feedbackText.text = result.Message;
            feedbackText.color = success ? GetColor(affordableColorHex) : GetColor(notAffordableColorHex);
        }

        RefreshUI();
    }

    public void RefreshUI()
    {
        ResolveReferences();

        if (coinText != null)
        {
            int coins = playerWallet != null ? playerWallet.GetCoins() : 0;
            coinText.text = "Coins: " + coins;
        }

        for (int i = 0; i < runtimeRows.Count; i++)
        {
            RefreshRow(runtimeRows[i]);
        }
    }

    private void RefreshRow(RuntimeSkillRow row)
    {
        if (row == null || row.Entry == null)
            return;

        if (playerStats == null || playerWallet == null)
        {
            SetRowMissingReference(row);
            return;
        }

        SkillUpgradeMenuEntry entry = row.Entry;

        int currentValue = playerStats.GetCurrentStatValue(entry.statType);
        int nextValue = playerStats.GetStatValueAfterNextUpgrade(entry.statType);
        int currentLevel = playerStats.GetUpgradeLevel(entry.statType);
        int levelLimit = playerStats.GetUpgradeLevelLimit(entry.statType);
        int cost = playerStats.GetUpgradeCost(entry.statType);
        int coins = playerWallet.GetCoins();

        bool isMaxed = playerStats.IsUpgradeMaxed(entry.statType);
        bool affordable = cost > 0 && coins >= cost;
        bool canBuy = upgradeController != null && upgradeController.CanBuyUpgrade(entry.statType);

        if (row.IconImage != null)
        {
            row.IconImage.sprite = entry.iconSprite;
            row.IconImage.color = entry.iconSprite != null ? Color.white : GetColor(iconPlaceholderColorHex);
        }

        if (row.RowImage != null)
        {
            row.RowImage.sprite = entry.rowBackgroundSprite != null ? entry.rowBackgroundSprite : defaultRowBackgroundSprite;
            row.RowImage.color = row.RowImage.sprite != null ? Color.white : GetColor(rowColorHex);
        }

        if (row.BuyButtonImage != null)
        {
            row.BuyButtonImage.sprite = entry.buyButtonSprite != null ? entry.buyButtonSprite : defaultBuyButtonSprite;
        }

        row.TitleText.text = entry.title;
        row.LevelText.text = "Level: " + currentLevel + "/" + levelLimit;

        if (isMaxed)
        {
            row.ValueText.text = "Value: " + currentValue;
            row.CostText.text = entry.maxLabel;
            row.CostText.color = GetColor(maxedColorHex);

            row.BuyButton.interactable = false;
            row.BuyButtonText.text = entry.maxLabel;

            SetButtonVisual(row, false);
            return;
        }

        row.ValueText.text = "Value: " + currentValue + " → " + nextValue;
        row.CostText.text = "Cost: " + cost;
        row.CostText.color = affordable ? GetColor(affordableColorHex) : GetColor(notAffordableColorHex);

        row.BuyButton.interactable = canBuy;
        row.BuyButtonText.text = affordable ? entry.buyLabel : entry.noCoinLabel;

        SetButtonVisual(row, canBuy);
    }

    private void SetRowMissingReference(RuntimeSkillRow row)
    {
        row.LevelText.text = "Level: -";
        row.ValueText.text = "Value: -";
        row.CostText.text = "Missing";
        row.CostText.color = GetColor(notAffordableColorHex);

        row.BuyButton.interactable = false;
        row.BuyButtonText.text = "LOCKED";

        SetButtonVisual(row, false);
    }

    private void SetButtonVisual(RuntimeSkillRow row, bool enabled)
    {
        if (row == null || row.BuyButtonImage == null)
            return;

        bool hasSprite = row.BuyButtonImage.sprite != null;

        if (hasSprite)
        {
            row.BuyButtonImage.color = enabled ? Color.white : GetColor(buttonDisabledColorHex);
        }
        else
        {
            row.BuyButtonImage.color = enabled ? GetColor(buttonColorHex) : GetColor(buttonDisabledColorHex);
        }
    }

    private GameObject CreateUIObject(string objectName, Transform parent)
    {
        GameObject obj = new GameObject(objectName, typeof(RectTransform));
        obj.transform.SetParent(parent, false);
        return obj;
    }

    private TMP_Text CreateText(
        string objectName,
        Transform parent,
        string text,
        float fontSize,
        Color color,
        TextAlignmentOptions alignment
    )
    {
        GameObject textObject = CreateUIObject(objectName, parent);
        TextMeshProUGUI tmp = textObject.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.color = color;
        tmp.alignment = alignment;
        tmp.raycastTarget = false;
        tmp.enableWordWrapping = false;
        return tmp;
    }

    private Color GetColor(string hex)
    {
        if (ColorUtility.TryParseHtmlString(hex, out Color color))
        {
            return color;
        }

        return Color.white;
    }

    [ContextMenu("Open Skill Menu")]
    private void DebugOpenSkillMenu()
    {
        OpenSkillMenu();
    }

    [ContextMenu("Close Skill Menu")]
    private void DebugCloseSkillMenu()
    {
        CloseSkillMenu();
    }

    [ContextMenu("Refresh Skill Menu")]
    private void DebugRefreshSkillMenu()
    {
        RefreshUI();
    }

    [ContextMenu("Rebuild Skill Menu")]
    private void DebugRebuildSkillMenu()
    {
        BuildUI();
    }
}