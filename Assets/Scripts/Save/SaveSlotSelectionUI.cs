using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class SaveSlotSelectionUI : MonoBehaviour
{
    private enum PendingSlotAction
    {
        None,
        Continue,
        NewGame
    }

    [Header("References")]
    [SerializeField] private Canvas targetCanvas;
    [SerializeField] private PlayerSaveSlotManager saveSlotManager;

    [Header("Build")]
    [SerializeField] private bool buildOnStart = true;
    [SerializeField] private bool showOnStartIfWaitingForSlot = true;

    [Header("Pause Menu Behaviour")]
    [SerializeField] private bool useApplyCancelWhenOpenedFromPauseMenu = true;
    [SerializeField] private bool reloadSceneAfterApplyFromPauseMenu = true;
    [SerializeField] private bool saveCurrentGameBeforeApplyFromPauseMenu = true;
    [SerializeField] private float reloadDelay = 0.15f;

    [Header("Sorting")]
    [SerializeField] private bool useDedicatedSortingCanvas = true;
    [SerializeField] private int sortingOrder = 9000;

    [Header("Layout")]
    [SerializeField] private Vector2 panelSize = new Vector2(900f, 590f);
    [SerializeField] private Vector2 panelAnchoredPosition = Vector2.zero;

    [SerializeField] private Vector2 slotCardSize = new Vector2(250f, 280f);
    [SerializeField] private float slotCardSpacing = 285f;
    [SerializeField] private float slotCardY = -5f;

    [Header("Apply / Cancel Layout")]
    [SerializeField] private Vector2 selectedInfoPosition = new Vector2(0f, -218f);
    [SerializeField] private Vector2 selectedInfoSize = new Vector2(720f, 34f);
    [SerializeField] private Vector2 applyButtonPosition = new Vector2(-115f, -265f);
    [SerializeField] private Vector2 cancelButtonPosition = new Vector2(115f, -265f);
    [SerializeField] private Vector2 applyCancelButtonSize = new Vector2(190f, 48f);

    [Header("Apply / Cancel Visibility")]
    [SerializeField] private bool showSelectedInfoBeforeSelection = true;
    [SerializeField] private bool showApplyButtonOnlyAfterSelection = true;
    [SerializeField] private bool keepCancelButtonVisible = true;

    [Header("Texts")]
    [SerializeField] private string titleText = "Choose Save Slot";
    [SerializeField] private string subtitleText = "Select a save slot to continue or start a new game.";
    [SerializeField] private string emptySlotText = "Empty Slot";
    [SerializeField] private string savedGameText = "Saved Game";
    [SerializeField] private string continueButtonText = "CONTINUE";
    [SerializeField] private string newGameButtonText = "NEW GAME";
    [SerializeField] private string deleteButtonText = "DELETE";
    [SerializeField] private string applyButtonText = "APPLY";
    [SerializeField] private string cancelButtonText = "CANCEL";
    [SerializeField] private string noPendingSelectionText = "Select a save slot.";
    [SerializeField] private string selectedContinueTextFormat = "Selected: Save {0} - Continue";
    [SerializeField] private string selectedNewGameTextFormat = "Selected: Save {0} - New Game";
    [SerializeField] private string alreadyCurrentSlotTextFormat = "Save {0} is already active.";

    [Header("Confirmation")]
    [SerializeField] private bool requireConfirmationBeforeOverwrite = true;
    [SerializeField] private string confirmTitleText = "Start New Game?";
    [SerializeField] private string confirmMessageText = "This will erase the selected save slot.";
    [SerializeField] private string confirmYesText = "YES";
    [SerializeField] private string confirmNoText = "NO";

    [Header("Loading Overlay")]
    [SerializeField] private string loadingTitleText = "Loading Save";
    [SerializeField] private string loadingMessageText = "Please wait...";
    [SerializeField] private Vector2 loadingTitlePosition = new Vector2(0f, 35f);
    [SerializeField] private Vector2 loadingMessagePosition = new Vector2(0f, -20f);

    [Header("Sprites")]
    [SerializeField] private Sprite panelBackgroundSprite;
    [SerializeField] private Sprite slotCardBackgroundSprite;
    [SerializeField] private Sprite mainButtonSprite;
    [SerializeField] private Sprite secondaryButtonSprite;
    [SerializeField] private Sprite deleteButtonSprite;

    [Header("Colors - Hex")]
    [SerializeField] private string overlayColorHex = "#000000CC";
    [SerializeField] private string panelColorHex = "#1F1A14F8";
    [SerializeField] private string slotCardColorHex = "#2C241BF2";
    [SerializeField] private string selectedSlotCardColorHex = "#5A3C20F2";
    [SerializeField] private string titleColorHex = "#F5D27A";
    [SerializeField] private string normalTextColorHex = "#FFFFFF";
    [SerializeField] private string mutedTextColorHex = "#C8BBA0";
    [SerializeField] private string mainButtonColorHex = "#7A4E24";
    [SerializeField] private string secondaryButtonColorHex = "#4F3826";
    [SerializeField] private string deleteButtonColorHex = "#7A2424";
    [SerializeField] private string emptySlotColorHex = "#C8BBA0";
    [SerializeField] private string savedSlotColorHex = "#18C964";
    [SerializeField] private string confirmPanelColorHex = "#1F1A14FF";
    [SerializeField] private string loadingOverlayColorHex = "#000000F5";
    [SerializeField] private string loadingTextColorHex = "#F5D27A";

    [Header("Live Refresh")]
    [SerializeField] private bool refreshEveryFrameWhileOpen = false;

    [Header("Debug")]
    [SerializeField] private bool logActions = true;

    private GameObject generatedLayerObject;
    private RectTransform generatedLayerRect;

    private GameObject rootObject;
    private GameObject confirmRootObject;
    private GameObject loadingRootObject;

    private TMP_Text selectedInfoText;
    private Button applyButton;
    private Button cancelButton;
    private TMP_Text applyButtonLabel;
    private TMP_Text cancelButtonLabel;
    private TMP_Text loadingTitleLabel;
    private TMP_Text loadingMessageLabel;

    private readonly List<RuntimeSlotCard> runtimeSlotCards = new List<RuntimeSlotCard>();

    private int pendingNewGameSlot = -1;
    private int pendingSelectedSlot = -1;
    private PendingSlotAction pendingSlotAction = PendingSlotAction.None;

    private bool isOpen;
    private bool openedFromPauseMenu;
    private bool isReloadingScene;

    private class RuntimeSlotCard
    {
        public int SlotIndex;
        public GameObject RootObject;
        public Image CardImage;
        public TMP_Text SlotNameText;
        public TMP_Text StatusText;
        public Button MainButton;
        public TMP_Text MainButtonText;
        public Button NewGameButton;
        public TMP_Text NewGameButtonText;
        public Button DeleteButton;
        public TMP_Text DeleteButtonText;
    }

    private void Awake()
    {
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

        if (showOnStartIfWaitingForSlot && saveSlotManager != null && saveSlotManager.IsWaitingForSlotSelection)
        {
            Open();
        }
        else
        {
            CloseInstant();
        }

        RefreshUI();
    }

    private void OnDisable()
    {
        UnsubscribeEvents();
    }

    private void Update()
    {
        if (isOpen && refreshEveryFrameWhileOpen)
        {
            RefreshUI();
        }
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

        if (saveSlotManager == null)
        {
            saveSlotManager = PlayerSaveSlotManager.Instance;
        }

        if (saveSlotManager == null)
        {
            saveSlotManager = FindObjectOfType<PlayerSaveSlotManager>();
        }
    }

    private void SubscribeEvents()
    {
        if (saveSlotManager == null)
            return;

        saveSlotManager.OnWaitingForSlotSelectionStarted -= HandleWaitingStarted;
        saveSlotManager.OnWaitingForSlotSelectionEnded -= HandleWaitingEnded;
        saveSlotManager.OnSaveSlotSelected -= HandleSlotSelected;
        saveSlotManager.OnSaveSlotCleared -= HandleSlotCleared;

        saveSlotManager.OnWaitingForSlotSelectionStarted += HandleWaitingStarted;
        saveSlotManager.OnWaitingForSlotSelectionEnded += HandleWaitingEnded;
        saveSlotManager.OnSaveSlotSelected += HandleSlotSelected;
        saveSlotManager.OnSaveSlotCleared += HandleSlotCleared;
    }

    private void UnsubscribeEvents()
    {
        if (saveSlotManager == null)
            return;

        saveSlotManager.OnWaitingForSlotSelectionStarted -= HandleWaitingStarted;
        saveSlotManager.OnWaitingForSlotSelectionEnded -= HandleWaitingEnded;
        saveSlotManager.OnSaveSlotSelected -= HandleSlotSelected;
        saveSlotManager.OnSaveSlotCleared -= HandleSlotCleared;
    }

    private void HandleWaitingStarted()
    {
        openedFromPauseMenu = false;
        Open();
    }

    private void HandleWaitingEnded()
    {
        if (!openedFromPauseMenu)
        {
            Close();
        }
    }

    private void HandleSlotSelected(int slotIndex)
    {
        RefreshUI();

        if (!openedFromPauseMenu)
        {
            Close();
        }
    }

    private void HandleSlotCleared(int slotIndex)
    {
        if (pendingSelectedSlot == slotIndex)
        {
            ClearPendingSelection();
        }

        RefreshUI();
    }

    [ContextMenu("Build Save Slot UI")]
    public void BuildUI()
    {
        ResolveReferences();

        if (targetCanvas == null)
        {
            Debug.LogWarning("SaveSlotSelectionUI could not find Canvas.", this);
            return;
        }

        DestroyGeneratedUI();
        BuildGeneratedLayer();
        BuildRootPanel();
        BuildConfirmPanel();
        BuildLoadingOverlay();

        RefreshUI();
        CloseInstant();

        if (logActions)
        {
            Debug.Log("Save Slot Selection UI built.", this);
        }
    }

    private void BuildGeneratedLayer()
    {
        generatedLayerObject = CreateUIObject("SaveSlot_Generated_Layer", targetCanvas.transform);
        generatedLayerRect = generatedLayerObject.GetComponent<RectTransform>();

        generatedLayerRect.anchorMin = Vector2.zero;
        generatedLayerRect.anchorMax = Vector2.one;
        generatedLayerRect.offsetMin = Vector2.zero;
        generatedLayerRect.offsetMax = Vector2.zero;

        if (useDedicatedSortingCanvas)
        {
            Canvas canvas = generatedLayerObject.AddComponent<Canvas>();
            canvas.overrideSorting = true;
            canvas.sortingOrder = sortingOrder;

            GraphicRaycaster raycaster = generatedLayerObject.AddComponent<GraphicRaycaster>();
            raycaster.ignoreReversedGraphics = true;
        }

        generatedLayerObject.transform.SetAsLastSibling();
    }

    private void BuildRootPanel()
    {
        rootObject = CreateUIObject("SaveSlot_Root", generatedLayerRect);

        RectTransform rootRect = rootObject.GetComponent<RectTransform>();
        rootRect.anchorMin = Vector2.zero;
        rootRect.anchorMax = Vector2.one;
        rootRect.offsetMin = Vector2.zero;
        rootRect.offsetMax = Vector2.zero;

        Image overlayImage = rootObject.AddComponent<Image>();
        overlayImage.color = GetColor(overlayColorHex);
        overlayImage.raycastTarget = true;

        GameObject panelObject = CreateUIObject("SaveSlot_Panel", rootObject.transform);

        RectTransform panelRect = panelObject.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.sizeDelta = panelSize;
        panelRect.anchoredPosition = panelAnchoredPosition;

        Image panelImage = panelObject.AddComponent<Image>();
        panelImage.sprite = panelBackgroundSprite;
        panelImage.color = panelBackgroundSprite != null ? Color.white : GetColor(panelColorHex);
        panelImage.raycastTarget = true;

        TMP_Text title = CreateText(
            "Title",
            panelObject.transform,
            titleText,
            40f,
            GetColor(titleColorHex),
            TextAlignmentOptions.Center
        );

        RectTransform titleRect = title.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0.5f, 1f);
        titleRect.anchorMax = new Vector2(0.5f, 1f);
        titleRect.pivot = new Vector2(0.5f, 0.5f);
        titleRect.sizeDelta = new Vector2(700f, 60f);
        titleRect.anchoredPosition = new Vector2(0f, -48f);

        TMP_Text subtitle = CreateText(
            "Subtitle",
            panelObject.transform,
            subtitleText,
            18f,
            GetColor(mutedTextColorHex),
            TextAlignmentOptions.Center
        );

        RectTransform subtitleRect = subtitle.GetComponent<RectTransform>();
        subtitleRect.anchorMin = new Vector2(0.5f, 1f);
        subtitleRect.anchorMax = new Vector2(0.5f, 1f);
        subtitleRect.pivot = new Vector2(0.5f, 0.5f);
        subtitleRect.sizeDelta = new Vector2(760f, 42f);
        subtitleRect.anchoredPosition = new Vector2(0f, -92f);

        BuildSlotCards(panelObject.transform);
        BuildApplyCancelControls(panelObject.transform);
    }

    private void BuildSlotCards(Transform parent)
    {
        runtimeSlotCards.Clear();

        int slotCount = saveSlotManager != null ? saveSlotManager.SlotCount : 3;

        for (int i = 1; i <= slotCount; i++)
        {
            RuntimeSlotCard card = BuildSlotCard(parent, i, slotCount);
            runtimeSlotCards.Add(card);
        }
    }

    private RuntimeSlotCard BuildSlotCard(Transform parent, int slotIndex, int totalSlots)
    {
        RuntimeSlotCard card = new RuntimeSlotCard();
        card.SlotIndex = slotIndex;

        card.RootObject = CreateUIObject("SaveSlot_Card_" + slotIndex, parent);

        RectTransform cardRect = card.RootObject.GetComponent<RectTransform>();
        cardRect.anchorMin = new Vector2(0.5f, 0.5f);
        cardRect.anchorMax = new Vector2(0.5f, 0.5f);
        cardRect.pivot = new Vector2(0.5f, 0.5f);
        cardRect.sizeDelta = slotCardSize;

        float centerOffset = (totalSlots - 1) * slotCardSpacing * 0.5f;
        float x = ((slotIndex - 1) * slotCardSpacing) - centerOffset;
        cardRect.anchoredPosition = new Vector2(x, slotCardY);

        card.CardImage = card.RootObject.AddComponent<Image>();
        card.CardImage.sprite = slotCardBackgroundSprite;
        card.CardImage.color = slotCardBackgroundSprite != null ? Color.white : GetColor(slotCardColorHex);
        card.CardImage.raycastTarget = true;

        card.SlotNameText = CreateText(
            "SlotName",
            card.RootObject.transform,
            "Save " + slotIndex,
            28f,
            GetColor(titleColorHex),
            TextAlignmentOptions.Center
        );

        RectTransform slotNameRect = card.SlotNameText.GetComponent<RectTransform>();
        slotNameRect.anchorMin = new Vector2(0.5f, 1f);
        slotNameRect.anchorMax = new Vector2(0.5f, 1f);
        slotNameRect.pivot = new Vector2(0.5f, 0.5f);
        slotNameRect.sizeDelta = new Vector2(220f, 42f);
        slotNameRect.anchoredPosition = new Vector2(0f, -38f);

        card.StatusText = CreateText(
            "Status",
            card.RootObject.transform,
            emptySlotText,
            20f,
            GetColor(emptySlotColorHex),
            TextAlignmentOptions.Center
        );

        RectTransform statusRect = card.StatusText.GetComponent<RectTransform>();
        statusRect.anchorMin = new Vector2(0.5f, 1f);
        statusRect.anchorMax = new Vector2(0.5f, 1f);
        statusRect.pivot = new Vector2(0.5f, 0.5f);
        statusRect.sizeDelta = new Vector2(220f, 36f);
        statusRect.anchoredPosition = new Vector2(0f, -82f);

        card.MainButton = CreateTopAnchoredButton(
            "MainButton",
            card.RootObject.transform,
            new Vector2(190f, 52f),
            new Vector2(0f, -145f),
            mainButtonSprite,
            mainButtonColorHex,
            out card.MainButtonText
        );

        int capturedSlotIndex = slotIndex;
        card.MainButton.onClick.AddListener(() => HandleMainButtonClicked(capturedSlotIndex));

        card.NewGameButton = CreateTopAnchoredButton(
            "NewGameButton",
            card.RootObject.transform,
            new Vector2(190f, 44f),
            new Vector2(0f, -198f),
            secondaryButtonSprite,
            secondaryButtonColorHex,
            out card.NewGameButtonText
        );

        card.NewGameButton.onClick.AddListener(() => HandleNewGameButtonClicked(capturedSlotIndex));

        card.DeleteButton = CreateTopAnchoredButton(
            "DeleteButton",
            card.RootObject.transform,
            new Vector2(190f, 38f),
            new Vector2(0f, -242f),
            deleteButtonSprite,
            deleteButtonColorHex,
            out card.DeleteButtonText
        );

        card.DeleteButton.onClick.AddListener(() => HandleDeleteButtonClicked(capturedSlotIndex));

        return card;
    }

    private void BuildApplyCancelControls(Transform parent)
    {
        selectedInfoText = CreateText(
            "Selected_Info_Text",
            parent,
            noPendingSelectionText,
            18f,
            GetColor(mutedTextColorHex),
            TextAlignmentOptions.Center
        );

        RectTransform selectedInfoRect = selectedInfoText.GetComponent<RectTransform>();
        selectedInfoRect.anchorMin = new Vector2(0.5f, 0.5f);
        selectedInfoRect.anchorMax = new Vector2(0.5f, 0.5f);
        selectedInfoRect.pivot = new Vector2(0.5f, 0.5f);
        selectedInfoRect.sizeDelta = selectedInfoSize;
        selectedInfoRect.anchoredPosition = selectedInfoPosition;

        applyButton = CreateCenterAnchoredButton(
            "Apply_Button",
            parent,
            applyCancelButtonSize,
            applyButtonPosition,
            mainButtonSprite,
            mainButtonColorHex,
            out applyButtonLabel
        );

        applyButtonLabel.text = applyButtonText;
        applyButton.onClick.AddListener(ApplyPendingSelectionFromPauseMenu);

        cancelButton = CreateCenterAnchoredButton(
            "Cancel_Button",
            parent,
            applyCancelButtonSize,
            cancelButtonPosition,
            secondaryButtonSprite,
            secondaryButtonColorHex,
            out cancelButtonLabel
        );

        cancelButtonLabel.text = cancelButtonText;
        cancelButton.onClick.AddListener(CancelFromPauseMenu);
    }

    private void BuildConfirmPanel()
    {
        confirmRootObject = CreateUIObject("SaveSlot_Confirm_Root", generatedLayerRect);

        RectTransform confirmRootRect = confirmRootObject.GetComponent<RectTransform>();
        confirmRootRect.anchorMin = Vector2.zero;
        confirmRootRect.anchorMax = Vector2.one;
        confirmRootRect.offsetMin = Vector2.zero;
        confirmRootRect.offsetMax = Vector2.zero;

        Image overlayImage = confirmRootObject.AddComponent<Image>();
        overlayImage.color = GetColor("#000000CC");
        overlayImage.raycastTarget = true;

        GameObject confirmPanelObject = CreateUIObject("Confirm_Panel", confirmRootObject.transform);

        RectTransform confirmPanelRect = confirmPanelObject.GetComponent<RectTransform>();
        confirmPanelRect.anchorMin = new Vector2(0.5f, 0.5f);
        confirmPanelRect.anchorMax = new Vector2(0.5f, 0.5f);
        confirmPanelRect.pivot = new Vector2(0.5f, 0.5f);
        confirmPanelRect.sizeDelta = new Vector2(520f, 260f);
        confirmPanelRect.anchoredPosition = Vector2.zero;

        Image confirmPanelImage = confirmPanelObject.AddComponent<Image>();
        confirmPanelImage.color = GetColor(confirmPanelColorHex);
        confirmPanelImage.raycastTarget = true;

        TMP_Text title = CreateText(
            "Confirm_Title",
            confirmPanelObject.transform,
            confirmTitleText,
            30f,
            GetColor(titleColorHex),
            TextAlignmentOptions.Center
        );

        RectTransform titleRect = title.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0.5f, 1f);
        titleRect.anchorMax = new Vector2(0.5f, 1f);
        titleRect.pivot = new Vector2(0.5f, 0.5f);
        titleRect.sizeDelta = new Vector2(460f, 44f);
        titleRect.anchoredPosition = new Vector2(0f, -42f);

        TMP_Text message = CreateText(
            "Confirm_Message",
            confirmPanelObject.transform,
            confirmMessageText,
            20f,
            GetColor(normalTextColorHex),
            TextAlignmentOptions.Center
        );

        RectTransform messageRect = message.GetComponent<RectTransform>();
        messageRect.anchorMin = new Vector2(0.5f, 0.5f);
        messageRect.anchorMax = new Vector2(0.5f, 0.5f);
        messageRect.pivot = new Vector2(0.5f, 0.5f);
        messageRect.sizeDelta = new Vector2(440f, 70f);
        messageRect.anchoredPosition = new Vector2(0f, 18f);

        Button yesButton = CreateCenterAnchoredButton(
            "Confirm_Yes_Button",
            confirmPanelObject.transform,
            new Vector2(150f, 52f),
            new Vector2(-95f, -80f),
            mainButtonSprite,
            deleteButtonColorHex,
            out TMP_Text yesText
        );

        yesText.text = confirmYesText;
        yesButton.onClick.AddListener(ConfirmStartNewGame);

        Button noButton = CreateCenterAnchoredButton(
            "Confirm_No_Button",
            confirmPanelObject.transform,
            new Vector2(150f, 52f),
            new Vector2(95f, -80f),
            secondaryButtonSprite,
            secondaryButtonColorHex,
            out TMP_Text noText
        );

        noText.text = confirmNoText;
        noButton.onClick.AddListener(CancelConfirmation);

        confirmRootObject.SetActive(false);
    }

    private void BuildLoadingOverlay()
    {
        loadingRootObject = CreateUIObject("SaveSlot_Loading_Overlay", generatedLayerRect);

        RectTransform loadingRect = loadingRootObject.GetComponent<RectTransform>();
        loadingRect.anchorMin = Vector2.zero;
        loadingRect.anchorMax = Vector2.one;
        loadingRect.offsetMin = Vector2.zero;
        loadingRect.offsetMax = Vector2.zero;

        Image loadingImage = loadingRootObject.AddComponent<Image>();
        loadingImage.color = GetColor(loadingOverlayColorHex);
        loadingImage.raycastTarget = true;

        loadingTitleLabel = CreateText(
            "Loading_Title",
            loadingRootObject.transform,
            loadingTitleText,
            38f,
            GetColor(loadingTextColorHex),
            TextAlignmentOptions.Center
        );

        RectTransform loadingTitleRect = loadingTitleLabel.GetComponent<RectTransform>();
        loadingTitleRect.anchorMin = new Vector2(0.5f, 0.5f);
        loadingTitleRect.anchorMax = new Vector2(0.5f, 0.5f);
        loadingTitleRect.pivot = new Vector2(0.5f, 0.5f);
        loadingTitleRect.sizeDelta = new Vector2(700f, 64f);
        loadingTitleRect.anchoredPosition = loadingTitlePosition;

        loadingMessageLabel = CreateText(
            "Loading_Message",
            loadingRootObject.transform,
            loadingMessageText,
            22f,
            GetColor(normalTextColorHex),
            TextAlignmentOptions.Center
        );

        RectTransform loadingMessageRect = loadingMessageLabel.GetComponent<RectTransform>();
        loadingMessageRect.anchorMin = new Vector2(0.5f, 0.5f);
        loadingMessageRect.anchorMax = new Vector2(0.5f, 0.5f);
        loadingMessageRect.pivot = new Vector2(0.5f, 0.5f);
        loadingMessageRect.sizeDelta = new Vector2(700f, 44f);
        loadingMessageRect.anchoredPosition = loadingMessagePosition;

        loadingRootObject.SetActive(false);
    }

    private Button CreateTopAnchoredButton(
        string objectName,
        Transform parent,
        Vector2 size,
        Vector2 anchoredPosition,
        Sprite sprite,
        string colorHex,
        out TMP_Text buttonText
    )
    {
        GameObject buttonObject = CreateUIObject(objectName, parent);

        RectTransform buttonRect = buttonObject.GetComponent<RectTransform>();
        buttonRect.anchorMin = new Vector2(0.5f, 1f);
        buttonRect.anchorMax = new Vector2(0.5f, 1f);
        buttonRect.pivot = new Vector2(0.5f, 0.5f);
        buttonRect.sizeDelta = size;
        buttonRect.anchoredPosition = anchoredPosition;

        return FinalizeButton(buttonObject, sprite, colorHex, out buttonText);
    }

    private Button CreateCenterAnchoredButton(
        string objectName,
        Transform parent,
        Vector2 size,
        Vector2 anchoredPosition,
        Sprite sprite,
        string colorHex,
        out TMP_Text buttonText
    )
    {
        GameObject buttonObject = CreateUIObject(objectName, parent);

        RectTransform buttonRect = buttonObject.GetComponent<RectTransform>();
        buttonRect.anchorMin = new Vector2(0.5f, 0.5f);
        buttonRect.anchorMax = new Vector2(0.5f, 0.5f);
        buttonRect.pivot = new Vector2(0.5f, 0.5f);
        buttonRect.sizeDelta = size;
        buttonRect.anchoredPosition = anchoredPosition;

        return FinalizeButton(buttonObject, sprite, colorHex, out buttonText);
    }

    private Button FinalizeButton(GameObject buttonObject, Sprite sprite, string colorHex, out TMP_Text buttonText)
    {
        Image buttonImage = buttonObject.AddComponent<Image>();
        buttonImage.sprite = sprite;
        buttonImage.color = sprite != null ? Color.white : GetColor(colorHex);
        buttonImage.raycastTarget = true;

        Button button = buttonObject.AddComponent<Button>();
        button.targetGraphic = buttonImage;

        buttonText = CreateText(
            "Text",
            buttonObject.transform,
            "BUTTON",
            18f,
            GetColor(normalTextColorHex),
            TextAlignmentOptions.Center
        );

        RectTransform textRect = buttonText.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        return button;
    }

    private void HandleMainButtonClicked(int slotIndex)
    {
        if (saveSlotManager == null)
            return;

        bool hasData = saveSlotManager.HasSlotData(slotIndex);

        if (openedFromPauseMenu && useApplyCancelWhenOpenedFromPauseMenu)
        {
            SetPendingSelection(slotIndex, hasData ? PendingSlotAction.Continue : PendingSlotAction.NewGame);
            return;
        }

        if (hasData)
        {
            saveSlotManager.ContinueSlot(slotIndex);
        }
        else
        {
            saveSlotManager.StartNewGameInSlot(slotIndex);
        }
    }

    private void HandleNewGameButtonClicked(int slotIndex)
    {
        if (saveSlotManager == null)
            return;

        if (openedFromPauseMenu && useApplyCancelWhenOpenedFromPauseMenu)
        {
            SetPendingSelection(slotIndex, PendingSlotAction.NewGame);
            return;
        }

        if (saveSlotManager.HasSlotData(slotIndex) && requireConfirmationBeforeOverwrite)
        {
            pendingNewGameSlot = slotIndex;
            OpenConfirmation();
            return;
        }

        saveSlotManager.StartNewGameInSlot(slotIndex);
    }

    private void HandleDeleteButtonClicked(int slotIndex)
    {
        if (saveSlotManager == null)
            return;

        saveSlotManager.ClearSlotData(slotIndex);
        RefreshUI();
    }

    private void SetPendingSelection(int slotIndex, PendingSlotAction action)
    {
        pendingSelectedSlot = slotIndex;
        pendingSlotAction = action;

        RefreshUI();
    }

    private void ClearPendingSelection()
    {
        pendingSelectedSlot = -1;
        pendingSlotAction = PendingSlotAction.None;
    }

    private void ApplyPendingSelectionFromPauseMenu()
    {
        if (!openedFromPauseMenu)
            return;

        if (saveSlotManager == null)
            return;

        if (pendingSelectedSlot < 1 || pendingSlotAction == PendingSlotAction.None)
        {
            UpdateSelectedInfoText(noPendingSelectionText);
            return;
        }

        if (
            pendingSlotAction == PendingSlotAction.Continue &&
            saveSlotManager.HasSelectedSlot &&
            saveSlotManager.SelectedSlotIndex == pendingSelectedSlot
        )
        {
            UpdateSelectedInfoText(string.Format(alreadyCurrentSlotTextFormat, pendingSelectedSlot));
            return;
        }

        if (
            pendingSlotAction == PendingSlotAction.NewGame &&
            saveSlotManager.HasSlotData(pendingSelectedSlot) &&
            requireConfirmationBeforeOverwrite
        )
        {
            pendingNewGameSlot = pendingSelectedSlot;
            OpenConfirmation();
            return;
        }

        ApplyPendingSelectionAndReload();
    }

    private void ApplyPendingSelectionAndReload()
    {
        if (saveSlotManager == null)
            return;

        if (pendingSelectedSlot < 1 || pendingSlotAction == PendingSlotAction.None)
            return;

        bool startNewGame = pendingSlotAction == PendingSlotAction.NewGame;

        if (saveCurrentGameBeforeApplyFromPauseMenu && saveSlotManager.HasSelectedSlot)
        {
            saveSlotManager.SaveCurrentGameState();
        }

        if (reloadSceneAfterApplyFromPauseMenu)
        {
            saveSlotManager.PreparePendingReloadSlotRequest(pendingSelectedSlot, startNewGame);
            ShowLoadingOverlay(pendingSelectedSlot, startNewGame);
            StartCoroutine(ReloadActiveSceneRoutine());
            return;
        }

        if (startNewGame)
        {
            saveSlotManager.StartNewGameInSlot(pendingSelectedSlot);
        }
        else
        {
            saveSlotManager.ContinueSlot(pendingSelectedSlot);
        }

        Close();
    }

    private void CancelFromPauseMenu()
    {
        ClearPendingSelection();
        Close();
    }

    private void OpenConfirmation()
    {
        if (confirmRootObject != null)
        {
            confirmRootObject.SetActive(true);
            confirmRootObject.transform.SetAsLastSibling();
        }
    }

    private void CancelConfirmation()
    {
        pendingNewGameSlot = -1;

        if (confirmRootObject != null)
        {
            confirmRootObject.SetActive(false);
        }
    }

    private void ConfirmStartNewGame()
    {
        if (pendingNewGameSlot < 1)
        {
            CancelConfirmation();
            return;
        }

        if (openedFromPauseMenu && useApplyCancelWhenOpenedFromPauseMenu)
        {
            pendingSelectedSlot = pendingNewGameSlot;
            pendingSlotAction = PendingSlotAction.NewGame;
            pendingNewGameSlot = -1;

            if (confirmRootObject != null)
            {
                confirmRootObject.SetActive(false);
            }

            ApplyPendingSelectionAndReload();
            return;
        }

        int slotToStart = pendingNewGameSlot;
        pendingNewGameSlot = -1;

        if (confirmRootObject != null)
        {
            confirmRootObject.SetActive(false);
        }

        if (saveSlotManager != null)
        {
            saveSlotManager.StartNewGameInSlot(slotToStart);
        }
    }

    public void Open()
    {
        openedFromPauseMenu = false;
        ClearPendingSelection();
        OpenInternal();
    }

    public void OpenFromPauseMenu()
    {
        openedFromPauseMenu = true;
        ClearPendingSelection();
        OpenInternal();
    }

    private void OpenInternal()
    {
        if (rootObject == null)
        {
            BuildUI();
        }

        if (rootObject == null)
            return;

        isOpen = true;
        rootObject.SetActive(true);

        if (loadingRootObject != null)
        {
            loadingRootObject.SetActive(false);
        }

        if (generatedLayerObject != null)
        {
            generatedLayerObject.transform.SetAsLastSibling();
        }

        RefreshUI();

        if (logActions)
        {
            Debug.Log(openedFromPauseMenu
                ? "Save Slot Selection UI opened from Pause Menu."
                : "Save Slot Selection UI opened.");
        }
    }

    public void Close()
    {
        isOpen = false;
        openedFromPauseMenu = false;
        ClearPendingSelection();

        if (rootObject != null)
        {
            rootObject.SetActive(false);
        }

        if (confirmRootObject != null)
        {
            confirmRootObject.SetActive(false);
        }

        if (loadingRootObject != null)
        {
            loadingRootObject.SetActive(false);
        }

        if (logActions)
        {
            Debug.Log("Save Slot Selection UI closed.", this);
        }
    }

    private void CloseInstant()
    {
        isOpen = false;
        openedFromPauseMenu = false;
        ClearPendingSelection();

        if (rootObject != null)
        {
            rootObject.SetActive(false);
        }

        if (confirmRootObject != null)
        {
            confirmRootObject.SetActive(false);
        }

        if (loadingRootObject != null)
        {
            loadingRootObject.SetActive(false);
        }
    }

    private void ShowLoadingOverlay(int slotIndex, bool startNewGame)
    {
        if (loadingRootObject == null)
            return;

        if (loadingTitleLabel != null)
        {
            loadingTitleLabel.text = startNewGame
                ? $"Starting Save {slotIndex}"
                : $"Loading Save {slotIndex}";
        }

        if (loadingMessageLabel != null)
        {
            loadingMessageLabel.text = loadingMessageText;
        }

        loadingRootObject.SetActive(true);
        loadingRootObject.transform.SetAsLastSibling();

        if (rootObject != null)
        {
            rootObject.SetActive(false);
        }

        if (confirmRootObject != null)
        {
            confirmRootObject.SetActive(false);
        }
    }

    private IEnumerator ReloadActiveSceneRoutine()
    {
        if (isReloadingScene)
            yield break;

        isReloadingScene = true;

        Time.timeScale = 1f;
        AudioListener.pause = false;

        if (logActions)
        {
            Debug.Log("Save Slot apply requested from menu. Reloading active scene safely.", this);
        }

        yield return new WaitForSecondsRealtime(Mathf.Max(0f, reloadDelay));

        Scene activeScene = SceneManager.GetActiveScene();
        SceneManager.LoadScene(activeScene.name);
    }

    public void RefreshUI()
    {
        ResolveReferences();

        for (int i = 0; i < runtimeSlotCards.Count; i++)
        {
            RefreshSlotCard(runtimeSlotCards[i]);
        }

        RefreshApplyCancelVisibility();
        RefreshSelectedInfoText();
    }

    private void RefreshSlotCard(RuntimeSlotCard card)
    {
        if (card == null || saveSlotManager == null)
            return;

        bool hasData = saveSlotManager.HasSlotData(card.SlotIndex);
        bool isPendingSelected = openedFromPauseMenu && pendingSelectedSlot == card.SlotIndex;

        card.SlotNameText.text = saveSlotManager.GetSlotDisplayName(card.SlotIndex);
        card.StatusText.text = hasData ? savedGameText : emptySlotText;
        card.StatusText.color = hasData ? GetColor(savedSlotColorHex) : GetColor(emptySlotColorHex);

        card.MainButtonText.text = hasData ? continueButtonText : newGameButtonText;

        card.NewGameButton.gameObject.SetActive(hasData);
        card.NewGameButtonText.text = newGameButtonText;

        card.DeleteButton.gameObject.SetActive(hasData);
        card.DeleteButtonText.text = deleteButtonText;

        if (card.CardImage != null)
        {
            if (slotCardBackgroundSprite != null)
            {
                card.CardImage.color = isPendingSelected ? GetColor(selectedSlotCardColorHex) : Color.white;
            }
            else
            {
                card.CardImage.color = isPendingSelected ? GetColor(selectedSlotCardColorHex) : GetColor(slotCardColorHex);
            }
        }
    }

    private void RefreshApplyCancelVisibility()
    {
        bool menuMode = openedFromPauseMenu && useApplyCancelWhenOpenedFromPauseMenu;
        bool hasPendingSelection = pendingSelectedSlot >= 1 && pendingSlotAction != PendingSlotAction.None;

        if (selectedInfoText != null)
        {
            selectedInfoText.gameObject.SetActive(menuMode && (showSelectedInfoBeforeSelection || hasPendingSelection));
        }

        if (applyButton != null)
        {
            bool showApply = menuMode && (!showApplyButtonOnlyAfterSelection || hasPendingSelection);
            applyButton.gameObject.SetActive(showApply);
            applyButton.interactable = hasPendingSelection;
        }

        if (cancelButton != null)
        {
            cancelButton.gameObject.SetActive(menuMode && keepCancelButtonVisible);
        }
    }

    private void RefreshSelectedInfoText()
    {
        if (selectedInfoText == null)
            return;

        if (!openedFromPauseMenu || !useApplyCancelWhenOpenedFromPauseMenu)
        {
            selectedInfoText.text = "";
            return;
        }

        if (pendingSelectedSlot < 1 || pendingSlotAction == PendingSlotAction.None)
        {
            selectedInfoText.text = noPendingSelectionText;
            selectedInfoText.color = GetColor(mutedTextColorHex);
            return;
        }

        if (pendingSlotAction == PendingSlotAction.Continue)
        {
            selectedInfoText.text = string.Format(selectedContinueTextFormat, pendingSelectedSlot);
        }
        else
        {
            selectedInfoText.text = string.Format(selectedNewGameTextFormat, pendingSelectedSlot);
        }

        selectedInfoText.color = GetColor(titleColorHex);
    }

    private void UpdateSelectedInfoText(string message)
    {
        if (selectedInfoText == null)
            return;

        selectedInfoText.text = message;
        selectedInfoText.color = GetColor(mutedTextColorHex);
    }

    private void DestroyGeneratedUI()
    {
        runtimeSlotCards.Clear();

        if (generatedLayerObject != null)
        {
            Destroy(generatedLayerObject);
            generatedLayerObject = null;
            generatedLayerRect = null;
        }

        rootObject = null;
        confirmRootObject = null;
        loadingRootObject = null;
        selectedInfoText = null;
        applyButton = null;
        cancelButton = null;
        applyButtonLabel = null;
        cancelButtonLabel = null;
        loadingTitleLabel = null;
        loadingMessageLabel = null;
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

    [ContextMenu("Open Save Slot UI")]
    private void DebugOpen()
    {
        Open();
    }

    [ContextMenu("Open Save Slot UI From Pause Menu")]
    private void DebugOpenFromPauseMenu()
    {
        OpenFromPauseMenu();
    }

    [ContextMenu("Close Save Slot UI")]
    private void DebugClose()
    {
        Close();
    }

    [ContextMenu("Refresh Save Slot UI")]
    private void DebugRefresh()
    {
        RefreshUI();
    }

    [ContextMenu("Rebuild Save Slot UI")]
    private void DebugRebuild()
    {
        BuildUI();
    }
}