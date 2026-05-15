using UnityEngine;
using UnityEngine.UI;

public class PlayerDeathPopupUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Canvas targetCanvas;
    [SerializeField] private PlayerRespawnController playerRespawnController;

    [Header("Optional Visuals")]
    [SerializeField] private Sprite panelBackgroundSprite;
    [SerializeField] private Sprite iconSprite;

    [Header("Sorting")]
    [SerializeField] private bool useOverrideSorting = true;
    [SerializeField] private int popupSortingOrder = 1000;

    [Header("Panel Layout")]
    [SerializeField] private Vector2 panelPosition = Vector2.zero;
    [SerializeField] private Vector2 panelSize = new Vector2(520f, 220f);

    [Header("Icon Layout")]
    [SerializeField] private bool showIcon = false;
    [SerializeField] private Vector2 iconPosition = new Vector2(0f, 50f);
    [SerializeField] private Vector2 iconSize = new Vector2(64f, 64f);

    [Header("Title Layout")]
    [SerializeField] private string titleText = "YOU DIED";
    [SerializeField] private Vector2 titlePosition = new Vector2(0f, 25f);
    [SerializeField] private Vector2 titleSize = new Vector2(420f, 60f);
    [SerializeField] private int titleFontSize = 38;

    [Header("Subtitle Layout")]
    [SerializeField] private string subtitleText = "Respawning...";
    [SerializeField] private Vector2 subtitlePosition = new Vector2(0f, -35f);
    [SerializeField] private Vector2 subtitleSize = new Vector2(420f, 44f);
    [SerializeField] private int subtitleFontSize = 22;
    [SerializeField] private bool showCountdown = true;

    [Header("Text")]
    [SerializeField] private Font customFont;
    [SerializeField] private bool useBestFit = true;

    [Header("Colors - Hex")]
    [SerializeField] private string overlayColorHex = "#00000099";
    [SerializeField] private string panelColorHex = "#1E1A16F2";
    [SerializeField] private string titleColorHex = "#FFD36A";
    [SerializeField] private string subtitleColorHex = "#FFF3D0";

    [Header("Live Editing")]
    [SerializeField] private bool liveRefreshInPlayMode = true;

    private GameObject overlayRoot;
    private RectTransform panelRect;
    private RectTransform iconRect;
    private RectTransform titleRect;
    private RectTransform subtitleRect;

    private Image panelImage;
    private Image iconImage;
    private Text titleLabel;
    private Text subtitleLabel;

    private bool isShowing;
    private bool rebuildQueued;
    private float popupEndTime;

    private Color overlayColor;
    private Color panelColor;
    private Color titleColor;
    private Color subtitleColor;

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
        EnsureRespawnController();
        BuildUI();
        SetVisible(false);
    }

    private void OnEnable()
    {
        if (playerRespawnController == null)
            EnsureRespawnController();

        if (playerRespawnController != null)
        {
            playerRespawnController.OnRespawnSequenceStarted += HandleRespawnSequenceStarted;
            playerRespawnController.OnRespawnSequenceFinished += HandleRespawnSequenceFinished;
        }
    }

    private void OnDisable()
    {
        if (playerRespawnController != null)
        {
            playerRespawnController.OnRespawnSequenceStarted -= HandleRespawnSequenceStarted;
            playerRespawnController.OnRespawnSequenceFinished -= HandleRespawnSequenceFinished;
        }
    }

    private void OnValidate()
    {
        panelSize.x = Mathf.Max(10f, panelSize.x);
        panelSize.y = Mathf.Max(10f, panelSize.y);

        iconSize.x = Mathf.Max(1f, iconSize.x);
        iconSize.y = Mathf.Max(1f, iconSize.y);

        titleSize.x = Mathf.Max(10f, titleSize.x);
        titleSize.y = Mathf.Max(10f, titleSize.y);

        subtitleSize.x = Mathf.Max(10f, subtitleSize.x);
        subtitleSize.y = Mathf.Max(10f, subtitleSize.y);

        titleFontSize = Mathf.Max(8, titleFontSize);
        subtitleFontSize = Mathf.Max(8, subtitleFontSize);

        if (Application.isPlaying && liveRefreshInPlayMode)
            rebuildQueued = true;
    }

    private void Update()
    {
        if (rebuildQueued)
        {
            rebuildQueued = false;
            RefreshUIFromInspector();
        }

        if (!isShowing)
            return;

        if (!showCountdown)
            return;

        float remaining = Mathf.Max(0f, popupEndTime - Time.unscaledTime);

        if (subtitleLabel != null)
            subtitleLabel.text = $"Respawning in {remaining:0.0}s";
    }

    [ContextMenu("Refresh UI Now")]
    public void RefreshUIFromInspector()
    {
        bool wasVisible = overlayRoot != null && overlayRoot.activeSelf;

        ClearUI();
        ParseColors();
        BuildUI();

        SetVisible(wasVisible);

        if (isShowing && subtitleLabel != null)
        {
            if (showCountdown)
            {
                float remaining = Mathf.Max(0f, popupEndTime - Time.unscaledTime);
                subtitleLabel.text = $"Respawning in {remaining:0.0}s";
            }
            else
            {
                subtitleLabel.text = subtitleText;
            }
        }
    }

    private void ParseColors()
    {
        overlayColor = ParseHex(overlayColorHex, new Color(0f, 0f, 0f, 0.6f));
        panelColor = ParseHex(panelColorHex, new Color(0.12f, 0.10f, 0.08f, 0.95f));
        titleColor = ParseHex(titleColorHex, new Color(1f, 0.82f, 0.42f, 1f));
        subtitleColor = ParseHex(subtitleColorHex, new Color(1f, 0.95f, 0.82f, 1f));
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

        targetCanvas = FindObjectOfType<Canvas>();
    }

    private void EnsureRespawnController()
    {
        if (playerRespawnController != null)
            return;

        playerRespawnController = FindObjectOfType<PlayerRespawnController>();
    }

    private void BuildUI()
    {
        if (targetCanvas == null)
        {
            Debug.LogWarning("PlayerDeathPopupUI: Target Canvas bulunamadý.");
            return;
        }

        overlayRoot = new GameObject(
            "Player_Death_Popup_Overlay",
            typeof(RectTransform),
            typeof(Canvas),
            typeof(GraphicRaycaster),
            typeof(Image)
        );
        overlayRoot.transform.SetParent(targetCanvas.transform, false);
        overlayRoot.transform.SetAsLastSibling();

        Canvas overlayCanvas = overlayRoot.GetComponent<Canvas>();
        overlayCanvas.renderMode = RenderMode.ScreenSpaceOverlay;

        if (useOverrideSorting)
        {
            overlayCanvas.overrideSorting = true;
            overlayCanvas.sortingOrder = popupSortingOrder;
        }

        RectTransform overlayRect = overlayRoot.GetComponent<RectTransform>();
        overlayRect.anchorMin = Vector2.zero;
        overlayRect.anchorMax = Vector2.one;
        overlayRect.offsetMin = Vector2.zero;
        overlayRect.offsetMax = Vector2.zero;

        Image overlayImage = overlayRoot.GetComponent<Image>();
        overlayImage.color = overlayColor;
        overlayImage.raycastTarget = true;

        GameObject panelObject = new GameObject("Player_Death_Popup_Panel", typeof(RectTransform), typeof(Image));
        panelObject.transform.SetParent(overlayRoot.transform, false);

        panelRect = panelObject.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.sizeDelta = panelSize;
        panelRect.anchoredPosition = panelPosition;

        panelImage = panelObject.GetComponent<Image>();
        panelImage.sprite = panelBackgroundSprite;
        panelImage.type = Image.Type.Simple;
        panelImage.color = panelBackgroundSprite != null ? Color.white : panelColor;
        panelImage.preserveAspect = false;

        if (showIcon && iconSprite != null)
        {
            GameObject iconObject = new GameObject("Icon", typeof(RectTransform), typeof(Image));
            iconObject.transform.SetParent(panelObject.transform, false);

            iconRect = iconObject.GetComponent<RectTransform>();
            iconRect.anchorMin = new Vector2(0.5f, 0.5f);
            iconRect.anchorMax = new Vector2(0.5f, 0.5f);
            iconRect.pivot = new Vector2(0.5f, 0.5f);
            iconRect.sizeDelta = iconSize;
            iconRect.anchoredPosition = iconPosition;

            iconImage = iconObject.GetComponent<Image>();
            iconImage.sprite = iconSprite;
            iconImage.preserveAspect = true;
            iconImage.raycastTarget = false;
        }

        titleLabel = CreateText(
            "Title",
            panelObject.transform,
            titleText,
            titleFontSize,
            titleColor,
            TextAnchor.MiddleCenter,
            titlePosition,
            titleSize,
            out titleRect
        );

        subtitleLabel = CreateText(
            "Subtitle",
            panelObject.transform,
            subtitleText,
            subtitleFontSize,
            subtitleColor,
            TextAnchor.MiddleCenter,
            subtitlePosition,
            subtitleSize,
            out subtitleRect
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
        Vector2 size,
        out RectTransform rect
    )
    {
        GameObject textObject = new GameObject(objectName, typeof(RectTransform), typeof(Text));
        textObject.transform.SetParent(parent, false);

        rect = textObject.GetComponent<RectTransform>();
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
        uiText.resizeTextForBestFit = useBestFit;
        uiText.resizeTextMinSize = Mathf.Max(8, fontSize - 12);
        uiText.resizeTextMaxSize = fontSize;
        uiText.horizontalOverflow = HorizontalWrapMode.Wrap;
        uiText.verticalOverflow = VerticalWrapMode.Truncate;

        return uiText;
    }

    private void ClearUI()
    {
        if (overlayRoot != null)
        {
            Destroy(overlayRoot);
            overlayRoot = null;
        }

        panelRect = null;
        iconRect = null;
        titleRect = null;
        subtitleRect = null;

        panelImage = null;
        iconImage = null;
        titleLabel = null;
        subtitleLabel = null;
    }

    private void HandleRespawnSequenceStarted()
    {
        if (playerRespawnController == null)
            return;

        popupEndTime = Time.unscaledTime + playerRespawnController.RespawnDelay;
        isShowing = true;

        if (subtitleLabel != null)
        {
            subtitleLabel.text = showCountdown
                ? $"Respawning in {playerRespawnController.RespawnDelay:0.0}s"
                : subtitleText;
        }

        if (overlayRoot != null)
            overlayRoot.transform.SetAsLastSibling();

        SetVisible(true);
    }

    private void HandleRespawnSequenceFinished()
    {
        isShowing = false;
        SetVisible(false);
    }

    private void SetVisible(bool visible)
    {
        if (overlayRoot != null)
        {
            overlayRoot.transform.SetAsLastSibling();
            overlayRoot.SetActive(visible);
        }
    }
}