using System;
using System.Collections;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

public class HPBarAutoInstaller : MonoBehaviour
{
    [Header("Enemy Search")]
    public Transform enemySearchRoot;
    public float refreshInterval = 0.5f;

    [Tooltip("Açýk kalýrsa Enemy root dýþýnda yanlýþlýkla eklenen EnemyHPBarAuto componentlerini temizler. Player kafasýnda bar çýkmasýný engeller.")]
    public bool cleanupEnemyBarsOutsideEnemyRoot = true;

    [Header("Overlay Canvas")]
    public Canvas overlayCanvas;
    public bool createCanvasIfMissing = true;
    public int sortingOrder = 300;

    [Header("Shared Sprites")]
    public Sprite backgroundSprite;
    public Sprite enemyFillSprite;
    public Sprite silverFrameSprite;
    public Sprite goldFrameSprite;

    [Header("Enemy Default Style")]
    public EnemyHPBarAuto.FrameStyle defaultEnemyFrameStyle = EnemyHPBarAuto.FrameStyle.Silver;

    [Header("Enemy Boss / Elite Auto Style")]
    public bool useGoldForBossByName = true;
    public string[] goldNameKeywords = { "Boss", "Elite" };

    [Header("Enemy Locked Dynamic Placement")]
    public bool useCalibratedRendererPlacement = true;
    public int calibrationFrames = 10;
    public bool hideUntilCalibrated = true;
    public bool lockAfterCalibration = true;

    [Range(0f, 1.2f)]
    public float verticalAnchorRatio = 0.72f;

    public float verticalPixelAdjustment = 4f;

    [Header("Enemy Fallback Placement")]
    public Vector3 fallbackWorldOffset = Vector3.zero;
    public Vector2 fallbackScreenOffset = new Vector2(0f, 28f);

    [Header("Enemy Dynamic Width")]
    public bool dynamicSizeFromRenderer = true;
    public float rendererWidthToBarWidth = 0.65f;
    public float minBarWidth = 45f;
    public float maxBarWidth = 95f;

    [Header("Enemy Bar Shape")]
    public float baseBarHeight = 9f;
    public float minBarHeight = 8f;
    public float maxBarHeight = 13f;
    public float referenceWidthForHeight = 65f;

    [Range(0.1f, 1f)]
    public float fillWidthPercent = 0.84f;

    [Range(0.1f, 1f)]
    public float fillHeightPercent = 0.52f;

    [Header("Enemy Renderer Filtering")]
    public bool ignoreParticleRenderers = true;

    [Header("Enemy Runtime Behaviour")]
    public bool updateExistingEnemySettings = true;

    [Tooltip("Normalde kapalý kalmalý. Açarsan mevcut enemy barlarý tekrar kalibre edilir.")]
    public bool forceRecalibrateExistingEnemies = false;

    [Header("Player HP Bar")]
    public bool createPlayerHPBar = true;
    public RectTransform playerHUDParent;
    public MonoBehaviour playerHealthSource;
    public bool autoFindPlayerHealth = true;
    public string playerHealthComponentName = "PlayerHealth";

    [Header("Player HP Bar Visual")]
    public string playerBarObjectName = "Generated_Player_HPBar";
    public EnemyHPBarAuto.FrameStyle playerFrameStyle = EnemyHPBarAuto.FrameStyle.Gold;

    [Tooltip("Normal can rengi. Varsayýlan yeþil: #2ECC71")]
    public Color playerFillColor = new Color(0.1803922f, 0.8f, 0.4431373f, 1f);

    [Header("Player HP Color Thresholds")]
    public bool usePlayerColorThresholds = true;

    [Range(0f, 1f)]
    public float playerWarningHealthPercent = 0.5f;

    [Range(0f, 1f)]
    public float playerCriticalHealthPercent = 0.25f;

    [Tooltip("Can %50 altýna düþünce kullanýlacak renk. Varsayýlan sarý: #F1C40F")]
    public Color playerWarningFillColor = new Color(0.945098f, 0.7686275f, 0.05882353f, 1f);

    [Tooltip("Can %25 altýna düþünce kullanýlacak renk. Varsayýlan kýrmýzý: #E74C3C")]
    public Color playerCriticalFillColor = new Color(0.9058824f, 0.2980392f, 0.2352941f, 1f);

    [Header("Player HP Bar Layout")]
    public Vector2 playerAnchoredPosition = new Vector2(0f, -30f);
    public Vector2 playerBarSize = new Vector2(340f, 50f);
    public Vector2 playerFillSize = new Vector2(306f, 25f);
    public Vector2 playerFillOffset = Vector2.zero;

    private Coroutine refreshCoroutine;

    private RectTransform playerRootRect;
    private Image playerBackgroundImage;
    private Image playerFillImage;
    private Image playerFrameImage;

    private Sprite runtimeSolidWhiteSprite;

    private MemberInfo playerCurrentHealthMember;
    private MemberInfo playerMaxHealthMember;
    private bool playerHealthWarningShown;
    private bool missingEnemyRootWarningShown;
    private bool missingPlayerHUDWarningShown;

    private readonly string[] currentHealthNames =
    {
        "currentHealth",
        "currentHp",
        "currentHP",
        "currenthealth",
        "health",
        "hp",
        "Health",
        "HP"
    };

    private readonly string[] maxHealthNames =
    {
        "maxHealth",
        "maxHp",
        "maxHP",
        "maxhealth",
        "maximumHealth",
        "maximumHp",
        "MaxHealth",
        "MaxHP"
    };

    private void Start()
    {
        EnsureOverlayCanvas();

        if (cleanupEnemyBarsOutsideEnemyRoot)
        {
            CleanupEnemyBarsOutsideEnemyRoot();
        }

        InstallEnemyBars();

        if (createPlayerHPBar)
        {
            EnsurePlayerHPBar();
            CachePlayerHealthSource();
            UpdatePlayerHPBar();
        }

        if (refreshInterval > 0f)
        {
            refreshCoroutine = StartCoroutine(RefreshRoutine());
        }
    }

    private void Update()
    {
        if (!createPlayerHPBar)
        {
            return;
        }

        EnsurePlayerHPBar();
        UpdatePlayerHPBar();
    }

    private void OnDisable()
    {
        if (refreshCoroutine != null)
        {
            StopCoroutine(refreshCoroutine);
            refreshCoroutine = null;
        }
    }

    private IEnumerator RefreshRoutine()
    {
        WaitForSeconds wait = new WaitForSeconds(refreshInterval);

        while (true)
        {
            yield return wait;

            if (cleanupEnemyBarsOutsideEnemyRoot)
            {
                CleanupEnemyBarsOutsideEnemyRoot();
            }

            InstallEnemyBars();
        }
    }

    private void EnsureOverlayCanvas()
    {
        if (overlayCanvas != null)
        {
            return;
        }

        Transform existing = transform.Find("HPBarOverlayCanvas");

        if (existing != null)
        {
            overlayCanvas = existing.GetComponent<Canvas>();

            if (overlayCanvas != null)
            {
                return;
            }
        }

        if (!createCanvasIfMissing)
        {
            return;
        }

        GameObject canvasObject = new GameObject("HPBarOverlayCanvas");
        canvasObject.transform.SetParent(transform, false);

        overlayCanvas = canvasObject.AddComponent<Canvas>();
        overlayCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        overlayCanvas.sortingOrder = sortingOrder;

        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        canvasObject.AddComponent<GraphicRaycaster>();
    }

    public void InstallEnemyBars()
    {
        EnsureOverlayCanvas();

        if (enemySearchRoot == null)
        {
            if (!missingEnemyRootWarningShown)
            {
                Debug.LogWarning("[HPBarAutoInstaller] Enemy Search Root boþ. Enemies objesini buraya sürüklemen gerekiyor.", this);
                missingEnemyRootWarningShown = true;
            }

            return;
        }

        if (!AreEnemySpritesReady())
        {
            return;
        }

        MonoBehaviour[] allBehaviours = enemySearchRoot.GetComponentsInChildren<MonoBehaviour>(true);

        foreach (MonoBehaviour behaviour in allBehaviours)
        {
            if (behaviour == null)
            {
                continue;
            }

            if (behaviour.GetType().Name != "EnemyHealth")
            {
                continue;
            }

            GameObject enemyObject = behaviour.gameObject;

            if (HasComponentInParentsByName(enemyObject.transform, playerHealthComponentName))
            {
                continue;
            }

            EnemyHPBarAuto hpBar = enemyObject.GetComponent<EnemyHPBarAuto>();
            bool createdNow = false;

            if (hpBar == null)
            {
                hpBar = enemyObject.AddComponent<EnemyHPBarAuto>();
                createdNow = true;
            }

            if (createdNow)
            {
                ApplyEnemySettings(enemyObject, hpBar);
                hpBar.ResetCalibration();
                hpBar.RebuildNow();
                continue;
            }

            if (updateExistingEnemySettings)
            {
                ApplyEnemySettings(enemyObject, hpBar);

                if (forceRecalibrateExistingEnemies)
                {
                    hpBar.ResetCalibration();
                    hpBar.RebuildNow();
                }
            }
            else
            {
                AssignMissingEnemyData(enemyObject, hpBar);
            }
        }

        forceRecalibrateExistingEnemies = false;
    }

    private bool AreEnemySpritesReady()
    {
        if (backgroundSprite == null || enemyFillSprite == null || silverFrameSprite == null || goldFrameSprite == null)
        {
            Debug.LogWarning(
                "[HPBarAutoInstaller] Enemy HP bar sprite referanslarý eksik. bgblack, filled, silver ve gold sprite alanlarýný doldur.",
                this
            );

            return false;
        }

        return true;
    }

    private void CleanupEnemyBarsOutsideEnemyRoot()
    {
        if (enemySearchRoot == null)
        {
            return;
        }

        EnemyHPBarAuto[] allEnemyBars = FindObjectsOfType<EnemyHPBarAuto>(true);

        foreach (EnemyHPBarAuto bar in allEnemyBars)
        {
            if (bar == null)
            {
                continue;
            }

            if (!bar.transform.IsChildOf(enemySearchRoot))
            {
                Destroy(bar);
            }
        }
    }

    private void ApplyEnemySettings(GameObject enemyObject, EnemyHPBarAuto hpBar)
    {
        hpBar.overlayCanvas = overlayCanvas;

        hpBar.backgroundSprite = backgroundSprite;
        hpBar.fillSprite = enemyFillSprite;
        hpBar.silverFrameSprite = silverFrameSprite;
        hpBar.goldFrameSprite = goldFrameSprite;

        hpBar.frameStyle = GetEnemyFrameStyle(enemyObject);

        hpBar.useCalibratedRendererPlacement = useCalibratedRendererPlacement;
        hpBar.calibrationFrames = calibrationFrames;
        hpBar.hideUntilCalibrated = hideUntilCalibrated;
        hpBar.lockAfterCalibration = lockAfterCalibration;
        hpBar.verticalAnchorRatio = verticalAnchorRatio;
        hpBar.verticalPixelAdjustment = verticalPixelAdjustment;

        hpBar.fallbackWorldOffset = fallbackWorldOffset;
        hpBar.fallbackScreenOffset = fallbackScreenOffset;

        hpBar.dynamicSizeFromRenderer = dynamicSizeFromRenderer;
        hpBar.rendererWidthToBarWidth = rendererWidthToBarWidth;
        hpBar.minBarWidth = minBarWidth;
        hpBar.maxBarWidth = maxBarWidth;

        hpBar.baseBarHeight = baseBarHeight;
        hpBar.minBarHeight = minBarHeight;
        hpBar.maxBarHeight = maxBarHeight;
        hpBar.referenceWidthForHeight = referenceWidthForHeight;
        hpBar.fillWidthPercent = fillWidthPercent;
        hpBar.fillHeightPercent = fillHeightPercent;

        hpBar.ignoreParticleRenderers = ignoreParticleRenderers;
    }

    private void AssignMissingEnemyData(GameObject enemyObject, EnemyHPBarAuto hpBar)
    {
        if (hpBar.overlayCanvas == null)
        {
            hpBar.overlayCanvas = overlayCanvas;
        }

        if (hpBar.backgroundSprite == null)
        {
            hpBar.backgroundSprite = backgroundSprite;
        }

        if (hpBar.fillSprite == null)
        {
            hpBar.fillSprite = enemyFillSprite;
        }

        if (hpBar.silverFrameSprite == null)
        {
            hpBar.silverFrameSprite = silverFrameSprite;
        }

        if (hpBar.goldFrameSprite == null)
        {
            hpBar.goldFrameSprite = goldFrameSprite;
        }

        hpBar.frameStyle = GetEnemyFrameStyle(enemyObject);
    }

    private EnemyHPBarAuto.FrameStyle GetEnemyFrameStyle(GameObject enemyObject)
    {
        if (!useGoldForBossByName || enemyObject == null)
        {
            return defaultEnemyFrameStyle;
        }

        string enemyName = enemyObject.name.ToLowerInvariant();

        foreach (string keyword in goldNameKeywords)
        {
            if (string.IsNullOrWhiteSpace(keyword))
            {
                continue;
            }

            if (enemyName.Contains(keyword.ToLowerInvariant()))
            {
                return EnemyHPBarAuto.FrameStyle.Gold;
            }
        }

        return defaultEnemyFrameStyle;
    }

    private bool HasComponentInParentsByName(Transform startTransform, string componentName)
    {
        Transform current = startTransform;

        while (current != null)
        {
            MonoBehaviour[] behaviours = current.GetComponents<MonoBehaviour>();

            foreach (MonoBehaviour behaviour in behaviours)
            {
                if (behaviour == null)
                {
                    continue;
                }

                if (behaviour.GetType().Name == componentName)
                {
                    return true;
                }
            }

            current = current.parent;
        }

        return false;
    }

    private void EnsurePlayerHPBar()
    {
        if (playerHUDParent == null)
        {
            GameObject foundHUD = GameObject.Find("PlayerHUD");

            if (foundHUD != null)
            {
                playerHUDParent = foundHUD.GetComponent<RectTransform>();
            }
        }

        if (playerHUDParent == null)
        {
            if (!missingPlayerHUDWarningShown)
            {
                Debug.LogWarning("[HPBarAutoInstaller] Player HUD Parent boþ. PlayerHUD objesini buraya sürükle.", this);
                missingPlayerHUDWarningShown = true;
            }

            return;
        }

        if (backgroundSprite == null || goldFrameSprite == null || silverFrameSprite == null)
        {
            Debug.LogWarning("[HPBarAutoInstaller] Player HP bar için background/gold/silver sprite referanslarý eksik.", this);
            return;
        }

        Transform existingRoot = playerHUDParent.Find(playerBarObjectName);

        if (existingRoot == null)
        {
            GameObject rootObject = new GameObject(playerBarObjectName);
            rootObject.transform.SetParent(playerHUDParent, false);

            playerRootRect = rootObject.AddComponent<RectTransform>();

            playerBackgroundImage = CreateUIImage("Background", playerRootRect);
            playerFillImage = CreateUIImage("Fill", playerRootRect);
            playerFrameImage = CreateUIImage("Frame", playerRootRect);
        }
        else
        {
            playerRootRect = existingRoot.GetComponent<RectTransform>();

            if (playerRootRect == null)
            {
                playerRootRect = existingRoot.gameObject.AddComponent<RectTransform>();
            }

            playerBackgroundImage = GetOrCreateUIImage("Background", playerRootRect);
            playerFillImage = GetOrCreateUIImage("Fill", playerRootRect);
            playerFrameImage = GetOrCreateUIImage("Frame", playerRootRect);
        }

        playerRootRect.anchorMin = new Vector2(0.5f, 1f);
        playerRootRect.anchorMax = new Vector2(0.5f, 1f);
        playerRootRect.pivot = new Vector2(0.5f, 1f);
        playerRootRect.anchoredPosition = playerAnchoredPosition;
        playerRootRect.sizeDelta = playerBarSize;

        playerBackgroundImage.transform.SetSiblingIndex(0);
        playerFillImage.transform.SetSiblingIndex(1);
        playerFrameImage.transform.SetSiblingIndex(2);

        ApplyPlayerVisuals();
    }

    private Image CreateUIImage(string objectName, RectTransform parent)
    {
        GameObject imageObject = new GameObject(objectName);
        imageObject.transform.SetParent(parent, false);

        RectTransform rect = imageObject.AddComponent<RectTransform>();
        Image image = imageObject.AddComponent<Image>();

        image.raycastTarget = false;

        return image;
    }

    private Image GetOrCreateUIImage(string objectName, RectTransform parent)
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

        return CreateUIImage(objectName, parent);
    }

    private void ApplyPlayerVisuals()
    {
        if (playerBackgroundImage != null)
        {
            playerBackgroundImage.sprite = backgroundSprite;
            playerBackgroundImage.type = Image.Type.Simple;
            playerBackgroundImage.color = Color.white;
            playerBackgroundImage.raycastTarget = false;

            SetPlayerChildRect(playerBackgroundImage.rectTransform, playerBarSize, Vector2.zero);
        }

        if (playerFillImage != null)
        {
            playerFillImage.sprite = GetRuntimeSolidWhiteSprite();
            playerFillImage.type = Image.Type.Filled;
            playerFillImage.fillMethod = Image.FillMethod.Horizontal;
            playerFillImage.fillOrigin = (int)Image.OriginHorizontal.Left;
            playerFillImage.raycastTarget = false;

            SetPlayerFillColor(1f);
            SetPlayerChildRect(playerFillImage.rectTransform, playerFillSize, playerFillOffset);
        }

        if (playerFrameImage != null)
        {
            playerFrameImage.sprite = playerFrameStyle == EnemyHPBarAuto.FrameStyle.Gold ? goldFrameSprite : silverFrameSprite;
            playerFrameImage.type = Image.Type.Simple;
            playerFrameImage.color = Color.white;
            playerFrameImage.raycastTarget = false;

            SetPlayerChildRect(playerFrameImage.rectTransform, playerBarSize, Vector2.zero);
        }
    }

    private void SetPlayerChildRect(RectTransform rect, Vector2 size, Vector2 anchoredPosition)
    {
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;
    }

    private Sprite GetRuntimeSolidWhiteSprite()
    {
        if (runtimeSolidWhiteSprite != null)
        {
            return runtimeSolidWhiteSprite;
        }

        Texture2D texture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
        texture.name = "Runtime_SolidWhite_HPFill";
        texture.SetPixel(0, 0, Color.white);
        texture.Apply();

        runtimeSolidWhiteSprite = Sprite.Create(
            texture,
            new Rect(0f, 0f, 1f, 1f),
            new Vector2(0.5f, 0.5f),
            100f
        );

        return runtimeSolidWhiteSprite;
    }

    private void CachePlayerHealthSource()
    {
        playerCurrentHealthMember = null;
        playerMaxHealthMember = null;

        if (playerHealthSource == null && autoFindPlayerHealth)
        {
            MonoBehaviour[] allBehaviours = FindObjectsOfType<MonoBehaviour>(true);

            foreach (MonoBehaviour behaviour in allBehaviours)
            {
                if (behaviour == null)
                {
                    continue;
                }

                if (behaviour.GetType().Name == playerHealthComponentName)
                {
                    playerHealthSource = behaviour;
                    break;
                }
            }
        }

        if (playerHealthSource == null)
        {
            return;
        }

        Type type = playerHealthSource.GetType();

        playerCurrentHealthMember = FindNumericMember(type, currentHealthNames);
        playerMaxHealthMember = FindNumericMember(type, maxHealthNames);
    }

    private void UpdatePlayerHPBar()
    {
        if (playerFillImage == null)
        {
            return;
        }

        if (playerHealthSource == null || playerCurrentHealthMember == null || playerMaxHealthMember == null)
        {
            CachePlayerHealthSource();
        }

        if (playerHealthSource == null)
        {
            SetPlayerFillColor(1f);
            return;
        }

        if (playerCurrentHealthMember == null || playerMaxHealthMember == null)
        {
            if (!playerHealthWarningShown)
            {
                Debug.LogWarning(
                    "[HPBarAutoInstaller] PlayerHealth içinde currentHealth / maxHealth benzeri alanlar bulunamadý. Player HP bar oluþur ama can azalmasý otomatik okunamayabilir.",
                    this
                );

                playerHealthWarningShown = true;
            }

            SetPlayerFillColor(1f);
            return;
        }

        float currentHealth = ReadNumericMember(playerHealthSource, playerCurrentHealthMember);
        float maxHealth = ReadNumericMember(playerHealthSource, playerMaxHealthMember);

        if (maxHealth <= 0f)
        {
            playerFillImage.fillAmount = 0f;
            SetPlayerFillColor(0f);
            return;
        }

        float health01 = Mathf.Clamp01(currentHealth / maxHealth);

        playerFillImage.fillAmount = health01;
        SetPlayerFillColor(health01);
    }

    private void SetPlayerFillColor(float health01)
    {
        if (playerFillImage == null)
        {
            return;
        }

        if (!usePlayerColorThresholds)
        {
            playerFillImage.color = playerFillColor;
            return;
        }

        if (health01 <= playerCriticalHealthPercent)
        {
            playerFillImage.color = playerCriticalFillColor;
            return;
        }

        if (health01 <= playerWarningHealthPercent)
        {
            playerFillImage.color = playerWarningFillColor;
            return;
        }

        playerFillImage.color = playerFillColor;
    }

    private MemberInfo FindNumericMember(Type type, string[] names)
    {
        BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.IgnoreCase;

        foreach (string memberName in names)
        {
            FieldInfo field = type.GetField(memberName, flags);

            if (field != null && IsNumericType(field.FieldType))
            {
                return field;
            }

            PropertyInfo property = type.GetProperty(memberName, flags);

            if (property != null && property.CanRead && IsNumericType(property.PropertyType))
            {
                return property;
            }
        }

        return null;
    }

    private bool IsNumericType(Type type)
    {
        return type == typeof(int)
            || type == typeof(float)
            || type == typeof(double)
            || type == typeof(long)
            || type == typeof(short)
            || type == typeof(uint)
            || type == typeof(decimal);
    }

    private float ReadNumericMember(MonoBehaviour source, MemberInfo member)
    {
        object value = null;

        if (member is FieldInfo field)
        {
            value = field.GetValue(source);
        }
        else if (member is PropertyInfo property)
        {
            value = property.GetValue(source);
        }

        if (value == null)
        {
            return 0f;
        }

        return Convert.ToSingle(value);
    }
}