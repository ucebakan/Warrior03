using System;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class EnemyHPBarAuto : MonoBehaviour
{
    public enum FrameStyle
    {
        Silver,
        Gold
    }

    [Header("Canvas")]
    public Canvas overlayCanvas;

    [Header("Sprites")]
    public Sprite backgroundSprite;
    public Sprite fillSprite;
    public Sprite silverFrameSprite;
    public Sprite goldFrameSprite;

    [Header("Style")]
    public FrameStyle frameStyle = FrameStyle.Silver;
    public Color backgroundColor = Color.white;
    public Color fillColor = Color.white;
    public Color frameColor = Color.white;

    [Header("Locked Dynamic Placement")]
    public bool useCalibratedRendererPlacement = true;

    [Tooltip("Enemy spawn olduktan sonra kaç baþarýlý frame ölçüm alýnacaðý.")]
    public int calibrationFrames = 10;

    [Tooltip("Açýk kalýrsa bar ilk ölçüm bitene kadar gizlenir. Zýplamayý engeller.")]
    public bool hideUntilCalibrated = true;

    [Tooltip("Açýk kalýrsa bar yeri bir kere hesaplanýr, sonra sabit offset ile takip eder.")]
    public bool lockAfterCalibration = true;

    [Range(0f, 1.2f)]
    [Tooltip("0 = renderer altý, 0.5 = ortasý, 1 = en üstü. Çok yukarýdaysa düþür.")]
    public float verticalAnchorRatio = 0.72f;

    [Tooltip("Kalibre edilmiþ noktaya ekstra ekran piksel Y ayarý.")]
    public float verticalPixelAdjustment = 4f;

    [Header("Fallback Placement")]
    public Vector3 fallbackWorldOffset = Vector3.zero;
    public Vector2 fallbackScreenOffset = new Vector2(0f, 28f);

    [Header("Dynamic Width")]
    public bool dynamicSizeFromRenderer = true;
    public float rendererWidthToBarWidth = 0.65f;
    public float minBarWidth = 45f;
    public float maxBarWidth = 95f;

    [Header("Bar Shape")]
    public float baseBarHeight = 9f;
    public float minBarHeight = 8f;
    public float maxBarHeight = 13f;
    public float referenceWidthForHeight = 65f;

    [Range(0.1f, 1f)]
    public float fillWidthPercent = 0.84f;

    [Range(0.1f, 1f)]
    public float fillHeightPercent = 0.52f;

    [Header("Renderer Filtering")]
    public bool ignoreParticleRenderers = true;

    [Header("Health Auto Read")]
    public bool autoReadEnemyHealth = true;

    [Header("Visibility")]
    public bool hideWhenDead = true;
    public bool hideWhenFull = false;
    public bool hideIfBehindCamera = true;

    [Header("Enemy Name Label")]
    public bool showEnemyName = true;

    [Tooltip("Boþ býrakýlýrsa EnemyNameTag okunur. O da yoksa GameObject adý kullanýlýr.")]
    public string displayNameOverride = "";

    public bool readEnemyNameTag = true;
    public string enemyNameTagComponentName = "EnemyNameTag";
    public bool useObjectNameIfEmpty = true;
    public bool cleanRuntimeObjectName = true;

    [Header("Enemy Name Label Visual")]
    public Font nameFont;

    [Tooltip("Referans font size. Dynamic açýksa bu deðer normal enemy için baz alýnýr.")]
    public int nameFontSize = 13;

    public bool nameUseBestFit = true;
    public Color nameColor = new Color(1f, 0.95f, 0.82f, 1f);
    public Color goldFrameNameColor = new Color(1f, 0.82f, 0.42f, 1f);
    public bool useGoldNameColorForGoldFrame = true;

    [Header("Enemy Name Dynamic Size")]
    public bool dynamicNameSizeFromBarWidth = true;

    [Tooltip("Font size, bar geniþliðiyle orantýlý hesaplanýr. 0.20 deðeri: 65px bar = yaklaþýk 13 font.")]
    public float nameFontSizeFromBarWidthMultiplier = 0.20f;

    public int minDynamicNameFontSize = 11;
    public int maxDynamicNameFontSize = 22;

    [Tooltip("Gold frame / boss için fonta ekstra büyütme ekler.")]
    public bool boostGoldFrameNameSize = true;

    public int goldFrameNameFontBonus = 2;

    [Header("Enemy Name Label Layout")]
    public Vector2 nameLabelSize = new Vector2(140f, 22f);

    [Tooltip("HP bar üstüne isim için temel Y mesafesi.")]
    public float nameLabelYOffset = 18f;

    [Tooltip("Dinamik label geniþliði: barWidth * bu deðer. Boss isimleri için büyük tutulabilir.")]
    public float nameLabelWidthToBarWidth = 2.2f;

    [Tooltip("Dinamik label yüksekliði: fontSize * bu deðer.")]
    public float nameLabelHeightToFontSize = 1.65f;

    [Tooltip("Açýk olursa boss/gold isim biraz daha yukarý alýnýr.")]
    public bool addExtraYOffsetForGoldFrame = true;

    public float goldFrameExtraNameYOffset = 4f;

    private const string RootObjectName = "Generated_Enemy_HPBar_Root";

    private RectTransform rootRect;
    private Image backgroundImage;
    private Image fillImage;
    private Image frameImage;
    private Text nameLabel;
    private RectTransform nameLabelRect;

    private Camera targetCamera;
    private Renderer[] cachedRenderers;

    private MonoBehaviour enemyHealthSource;
    private MemberInfo currentHealthMember;
    private MemberInfo maxHealthMember;
    private bool healthWarningShown;

    private float lastHealth01 = 1f;
    private bool lastPositionValid = true;

    private bool isCalibrated;
    private int successfulCalibrationSamples;
    private Vector2 calibrationOffsetSum;
    private float calibrationWidthSum;
    private Vector2 lockedScreenOffset;
    private float lockedBarWidth;

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

    private readonly string[] enemyDisplayNameMemberNames =
    {
        "DisplayName",
        "displayName",
        "EnemyName",
        "enemyName",
        "Name",
        "name"
    };

    private Font RuntimeNameFont
    {
        get
        {
            if (nameFont != null)
                return nameFont;

            Font legacyFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            if (legacyFont != null)
                return legacyFont;

            return Font.CreateDynamicFontFromOSFont("Arial", 14);
        }
    }

    private void Awake()
    {
        CacheCamera();
        CacheRenderers();
        CleanupOldWorldSpaceBar();
        ResetCalibration();
        RebuildNow();
    }

    private void OnEnable()
    {
        CacheCamera();
        CacheRenderers();
        CleanupOldWorldSpaceBar();
        ResetCalibration();
        RebuildNow();
    }

    private void LateUpdate()
    {
        EnsureUIExists();

        if (autoReadEnemyHealth)
        {
            UpdateHealthFromEnemyHealth();
        }

        UpdatePositionAndSize();
        UpdateVisibility();
    }

    private void OnDestroy()
    {
        if (rootRect != null)
        {
            Destroy(rootRect.gameObject);
        }
    }

    public void RebuildNow()
    {
        CacheCamera();
        CacheRenderers();
        CacheEnemyHealthSource();
        EnsureUIExists();
        ApplyVisuals();
        SetHealthNormalized(lastHealth01);
        UpdatePositionAndSize();
        UpdateVisibility();
    }

    public void ResetCalibration()
    {
        isCalibrated = false;
        successfulCalibrationSamples = 0;
        calibrationOffsetSum = Vector2.zero;
        calibrationWidthSum = 0f;
        lockedScreenOffset = fallbackScreenOffset;
        lockedBarWidth = referenceWidthForHeight;
    }

    public void SetHealth(float currentHealth, float maxHealth)
    {
        if (maxHealth <= 0f)
        {
            SetHealthNormalized(0f);
            return;
        }

        SetHealthNormalized(currentHealth / maxHealth);
    }

    public void SetHealthNormalized(float normalizedHealth)
    {
        lastHealth01 = Mathf.Clamp01(normalizedHealth);

        if (fillImage != null)
        {
            fillImage.fillAmount = lastHealth01;
        }
    }

    private void CleanupOldWorldSpaceBar()
    {
        Transform oldCanvas = transform.Find("Generated_Enemy_HPBar_Canvas");

        if (oldCanvas != null)
        {
            Destroy(oldCanvas.gameObject);
        }
    }

    private void CacheCamera()
    {
        if (targetCamera != null)
        {
            return;
        }

        targetCamera = Camera.main;

        if (targetCamera == null)
        {
            targetCamera = FindObjectOfType<Camera>();
        }
    }

    private void CacheRenderers()
    {
        cachedRenderers = GetComponentsInChildren<Renderer>(true);
    }

    private void EnsureUIExists()
    {
        if (overlayCanvas == null)
        {
            return;
        }

        bool nameLabelReady = !showEnemyName || nameLabel != null;

        if (rootRect != null && backgroundImage != null && fillImage != null && frameImage != null && nameLabelReady)
        {
            return;
        }

        string uniqueName = RootObjectName + "_" + GetInstanceID();
        Transform existing = overlayCanvas.transform.Find(uniqueName);

        if (existing == null)
        {
            GameObject root = new GameObject(uniqueName);
            root.transform.SetParent(overlayCanvas.transform, false);

            rootRect = root.AddComponent<RectTransform>();

            backgroundImage = CreateImage("Background", rootRect);
            fillImage = CreateImage("Fill", rootRect);
            frameImage = CreateImage("Frame", rootRect);

            if (showEnemyName)
            {
                nameLabel = CreateText("NameLabel", rootRect);
                nameLabelRect = nameLabel.rectTransform;
            }
        }
        else
        {
            rootRect = existing.GetComponent<RectTransform>();
            backgroundImage = GetOrCreateImage("Background", rootRect);
            fillImage = GetOrCreateImage("Fill", rootRect);
            frameImage = GetOrCreateImage("Frame", rootRect);

            if (showEnemyName)
            {
                nameLabel = GetOrCreateText("NameLabel", rootRect);
                nameLabelRect = nameLabel.rectTransform;
            }
        }

        backgroundImage.transform.SetSiblingIndex(0);
        fillImage.transform.SetSiblingIndex(1);
        frameImage.transform.SetSiblingIndex(2);

        if (nameLabel != null)
        {
            nameLabel.transform.SetSiblingIndex(3);
        }

        ApplyVisuals();
    }

    private Image CreateImage(string objectName, RectTransform parent)
    {
        GameObject go = new GameObject(objectName);
        go.transform.SetParent(parent, false);

        RectTransform rect = go.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;

        Image image = go.AddComponent<Image>();
        image.raycastTarget = false;

        return image;
    }

    private Image GetOrCreateImage(string objectName, RectTransform parent)
    {
        Transform child = parent.Find(objectName);

        if (child != null)
        {
            Image image = child.GetComponent<Image>();

            if (image == null)
            {
                image = child.gameObject.AddComponent<Image>();
            }

            image.raycastTarget = false;
            return image;
        }

        return CreateImage(objectName, parent);
    }

    private Text CreateText(string objectName, RectTransform parent)
    {
        GameObject go = new GameObject(objectName);
        go.transform.SetParent(parent, false);

        RectTransform rect = go.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = new Vector2(0f, nameLabelYOffset);
        rect.sizeDelta = nameLabelSize;

        Text text = go.AddComponent<Text>();
        text.raycastTarget = false;

        return text;
    }

    private Text GetOrCreateText(string objectName, RectTransform parent)
    {
        Transform child = parent.Find(objectName);

        if (child != null)
        {
            Text text = child.GetComponent<Text>();

            if (text == null)
            {
                text = child.gameObject.AddComponent<Text>();
            }

            text.raycastTarget = false;
            return text;
        }

        return CreateText(objectName, parent);
    }

    private void ApplyVisuals()
    {
        if (backgroundImage != null)
        {
            backgroundImage.sprite = backgroundSprite;
            backgroundImage.type = Image.Type.Simple;
            backgroundImage.color = backgroundColor;
            backgroundImage.raycastTarget = false;
        }

        if (fillImage != null)
        {
            fillImage.sprite = fillSprite;
            fillImage.type = Image.Type.Filled;
            fillImage.fillMethod = Image.FillMethod.Horizontal;
            fillImage.fillOrigin = (int)Image.OriginHorizontal.Left;
            fillImage.color = fillColor;
            fillImage.raycastTarget = false;
        }

        if (frameImage != null)
        {
            frameImage.sprite = frameStyle == FrameStyle.Gold ? goldFrameSprite : silverFrameSprite;
            frameImage.type = Image.Type.Simple;
            frameImage.color = frameColor;
            frameImage.raycastTarget = false;
        }

        ApplyNameLabelVisuals(CalculateDynamicNameFontSize(referenceWidthForHeight));
    }

    private void ApplyNameLabelVisuals(int calculatedFontSize)
    {
        if (nameLabel == null)
        {
            return;
        }

        string displayName = GetEnemyDisplayName();
        bool shouldShowName = showEnemyName && !string.IsNullOrWhiteSpace(displayName);

        nameLabel.gameObject.SetActive(shouldShowName);

        if (!shouldShowName)
        {
            return;
        }

        int finalFontSize = Mathf.Clamp(calculatedFontSize, minDynamicNameFontSize, maxDynamicNameFontSize);

        nameLabel.text = displayName;
        nameLabel.font = RuntimeNameFont;
        nameLabel.fontSize = finalFontSize;
        nameLabel.alignment = TextAnchor.MiddleCenter;
        nameLabel.color = useGoldNameColorForGoldFrame && frameStyle == FrameStyle.Gold ? goldFrameNameColor : nameColor;
        nameLabel.resizeTextForBestFit = nameUseBestFit;
        nameLabel.resizeTextMinSize = Mathf.Max(8, finalFontSize - 6);
        nameLabel.resizeTextMaxSize = finalFontSize;
        nameLabel.horizontalOverflow = HorizontalWrapMode.Overflow;
        nameLabel.verticalOverflow = VerticalWrapMode.Truncate;
        nameLabel.raycastTarget = false;
    }

    private int CalculateDynamicNameFontSize(float barWidth)
    {
        if (!dynamicNameSizeFromBarWidth)
        {
            int fixedSize = nameFontSize;

            if (boostGoldFrameNameSize && frameStyle == FrameStyle.Gold)
                fixedSize += goldFrameNameFontBonus;

            return Mathf.Clamp(fixedSize, minDynamicNameFontSize, maxDynamicNameFontSize);
        }

        int calculatedSize = Mathf.RoundToInt(barWidth * nameFontSizeFromBarWidthMultiplier);

        if (boostGoldFrameNameSize && frameStyle == FrameStyle.Gold)
            calculatedSize += goldFrameNameFontBonus;

        calculatedSize = Mathf.Clamp(calculatedSize, minDynamicNameFontSize, maxDynamicNameFontSize);

        return calculatedSize;
    }

    private void UpdatePositionAndSize()
    {
        if (rootRect == null || overlayCanvas == null)
        {
            return;
        }

        if (targetCamera == null)
        {
            CacheCamera();

            if (targetCamera == null)
            {
                return;
            }
        }

        if (!ProjectWorldToCanvasLocal(transform.position + fallbackWorldOffset, out Vector2 rootLocalPoint))
        {
            lastPositionValid = false;
            return;
        }

        Vector2 finalOffset = fallbackScreenOffset;
        float finalBarWidth = referenceWidthForHeight;

        if (useCalibratedRendererPlacement)
        {
            if (!isCalibrated || !lockAfterCalibration)
            {
                TryCollectCalibrationSample(rootLocalPoint);
            }

            if (isCalibrated)
            {
                finalOffset = lockedScreenOffset;
                finalBarWidth = lockedBarWidth;
            }
            else
            {
                finalOffset = fallbackScreenOffset;
                finalBarWidth = referenceWidthForHeight;
            }
        }

        rootRect.anchoredPosition = rootLocalPoint + finalOffset;
        ApplyBarSize(finalBarWidth);

        lastPositionValid = true;
    }

    private void TryCollectCalibrationSample(Vector2 rootLocalPoint)
    {
        if (!TryGetRendererCanvasBounds(out Vector2 min, out Vector2 max))
        {
            return;
        }

        float rendererWidth = Mathf.Max(1f, max.x - min.x);
        float rendererHeight = Mathf.Max(1f, max.y - min.y);

        float anchorX = (min.x + max.x) * 0.5f;
        float anchorY = min.y + rendererHeight * verticalAnchorRatio + verticalPixelAdjustment;

        Vector2 calculatedOffset = new Vector2(anchorX, anchorY) - rootLocalPoint;

        float calculatedWidth = dynamicSizeFromRenderer
            ? Mathf.Clamp(rendererWidth * rendererWidthToBarWidth, minBarWidth, maxBarWidth)
            : Mathf.Clamp(referenceWidthForHeight, minBarWidth, maxBarWidth);

        calibrationOffsetSum += calculatedOffset;
        calibrationWidthSum += calculatedWidth;
        successfulCalibrationSamples++;

        if (successfulCalibrationSamples >= Mathf.Max(1, calibrationFrames))
        {
            lockedScreenOffset = calibrationOffsetSum / successfulCalibrationSamples;
            lockedBarWidth = calibrationWidthSum / successfulCalibrationSamples;
            isCalibrated = true;
        }
    }

    private void ApplyBarSize(float barWidth)
    {
        if (rootRect == null || backgroundImage == null || fillImage == null || frameImage == null)
        {
            return;
        }

        float widthRatio = barWidth / Mathf.Max(1f, referenceWidthForHeight);
        float barHeight = Mathf.Clamp(baseBarHeight * widthRatio, minBarHeight, maxBarHeight);

        Vector2 barSize = new Vector2(barWidth, barHeight);
        Vector2 fillSize = new Vector2(barWidth * fillWidthPercent, barHeight * fillHeightPercent);

        rootRect.sizeDelta = barSize;

        SetRect(backgroundImage.rectTransform, barSize);
        SetRect(fillImage.rectTransform, fillSize);
        SetRect(frameImage.rectTransform, barSize);

        int dynamicFontSize = CalculateDynamicNameFontSize(barWidth);
        ApplyNameLabelVisuals(dynamicFontSize);
        ApplyNameLabelSize(barWidth, barHeight, dynamicFontSize);
    }

    private void ApplyNameLabelSize(float barWidth, float barHeight, int dynamicFontSize)
    {
        if (nameLabelRect == null)
        {
            return;
        }

        float labelWidth = Mathf.Max(nameLabelSize.x, barWidth * nameLabelWidthToBarWidth);
        float labelHeight = Mathf.Max(nameLabelSize.y, dynamicFontSize * nameLabelHeightToFontSize);

        float finalYOffset = nameLabelYOffset + barHeight * 0.5f;

        if (addExtraYOffsetForGoldFrame && frameStyle == FrameStyle.Gold)
            finalYOffset += goldFrameExtraNameYOffset;

        nameLabelRect.anchorMin = new Vector2(0.5f, 0.5f);
        nameLabelRect.anchorMax = new Vector2(0.5f, 0.5f);
        nameLabelRect.pivot = new Vector2(0.5f, 0.5f);
        nameLabelRect.anchoredPosition = new Vector2(0f, finalYOffset);
        nameLabelRect.sizeDelta = new Vector2(labelWidth, labelHeight);
    }

    private void SetRect(RectTransform rect, Vector2 size)
    {
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = size;
    }

    private bool TryGetRendererCanvasBounds(out Vector2 min, out Vector2 max)
    {
        min = new Vector2(float.MaxValue, float.MaxValue);
        max = new Vector2(float.MinValue, float.MinValue);

        if (cachedRenderers == null || cachedRenderers.Length == 0)
        {
            CacheRenderers();
        }

        bool foundAnyPoint = false;

        foreach (Renderer renderer in cachedRenderers)
        {
            if (renderer == null)
            {
                continue;
            }

            if (!renderer.enabled)
            {
                continue;
            }

            if (ignoreParticleRenderers && IsIgnoredRenderer(renderer))
            {
                continue;
            }

            Bounds bounds = renderer.bounds;
            Vector3 center = bounds.center;
            Vector3 extents = bounds.extents;

            Vector3[] corners =
            {
                center + new Vector3(-extents.x, -extents.y, -extents.z),
                center + new Vector3(-extents.x, -extents.y,  extents.z),
                center + new Vector3(-extents.x,  extents.y, -extents.z),
                center + new Vector3(-extents.x,  extents.y,  extents.z),
                center + new Vector3( extents.x, -extents.y, -extents.z),
                center + new Vector3( extents.x, -extents.y,  extents.z),
                center + new Vector3( extents.x,  extents.y, -extents.z),
                center + new Vector3( extents.x,  extents.y,  extents.z)
            };

            foreach (Vector3 corner in corners)
            {
                if (!ProjectWorldToCanvasLocal(corner, out Vector2 localPoint))
                {
                    continue;
                }

                foundAnyPoint = true;

                min.x = Mathf.Min(min.x, localPoint.x);
                min.y = Mathf.Min(min.y, localPoint.y);

                max.x = Mathf.Max(max.x, localPoint.x);
                max.y = Mathf.Max(max.y, localPoint.y);
            }
        }

        return foundAnyPoint;
    }

    private bool ProjectWorldToCanvasLocal(Vector3 worldPoint, out Vector2 localPoint)
    {
        localPoint = Vector2.zero;

        if (targetCamera == null || overlayCanvas == null)
        {
            return false;
        }

        Vector3 screenPoint = targetCamera.WorldToScreenPoint(worldPoint);

        if (hideIfBehindCamera && screenPoint.z <= 0f)
        {
            return false;
        }

        RectTransform canvasRect = overlayCanvas.transform as RectTransform;

        Camera uiCamera = null;

        if (overlayCanvas.renderMode != RenderMode.ScreenSpaceOverlay)
        {
            uiCamera = overlayCanvas.worldCamera != null ? overlayCanvas.worldCamera : targetCamera;
        }

        return RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect,
            new Vector2(screenPoint.x, screenPoint.y),
            uiCamera,
            out localPoint
        );
    }

    private bool IsIgnoredRenderer(Renderer renderer)
    {
        return renderer is ParticleSystemRenderer
            || renderer is TrailRenderer
            || renderer is LineRenderer;
    }

    private void CacheEnemyHealthSource()
    {
        enemyHealthSource = null;
        currentHealthMember = null;
        maxHealthMember = null;

        MonoBehaviour[] components = GetComponents<MonoBehaviour>();

        foreach (MonoBehaviour component in components)
        {
            if (component == null || component == this)
            {
                continue;
            }

            if (component.GetType().Name == "EnemyHealth")
            {
                enemyHealthSource = component;
                break;
            }
        }

        if (enemyHealthSource == null)
        {
            return;
        }

        Type type = enemyHealthSource.GetType();

        currentHealthMember = FindNumericMember(type, currentHealthNames);
        maxHealthMember = FindNumericMember(type, maxHealthNames);
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

    private void UpdateHealthFromEnemyHealth()
    {
        if (enemyHealthSource == null)
        {
            CacheEnemyHealthSource();
        }

        if (enemyHealthSource == null)
        {
            return;
        }

        if (currentHealthMember == null || maxHealthMember == null)
        {
            if (!healthWarningShown)
            {
                Debug.LogWarning(
                    "[EnemyHPBarAuto] EnemyHealth içinde currentHealth / maxHealth benzeri alanlar bulunamadý. HP bar görseli oluþur ama can azalmasý otomatik okunamayabilir.",
                    this
                );

                healthWarningShown = true;
            }

            return;
        }

        float currentHealth = ReadNumericMember(enemyHealthSource, currentHealthMember);
        float maxHealth = ReadNumericMember(enemyHealthSource, maxHealthMember);

        SetHealth(currentHealth, maxHealth);
    }

    private void UpdateVisibility()
    {
        if (rootRect == null)
        {
            return;
        }

        bool shouldShow = true;

        if (!lastPositionValid)
        {
            shouldShow = false;
        }

        if (hideUntilCalibrated && useCalibratedRendererPlacement && !isCalibrated)
        {
            shouldShow = false;
        }

        if (hideWhenDead && lastHealth01 <= 0.001f)
        {
            shouldShow = false;
        }

        if (hideWhenFull && lastHealth01 >= 0.999f)
        {
            shouldShow = false;
        }

        if (rootRect.gameObject.activeSelf != shouldShow)
        {
            rootRect.gameObject.SetActive(shouldShow);
        }
    }

    private string GetEnemyDisplayName()
    {
        if (!string.IsNullOrWhiteSpace(displayNameOverride))
        {
            return displayNameOverride.Trim();
        }

        if (readEnemyNameTag && TryReadEnemyNameTag(out string taggedName))
        {
            return taggedName;
        }

        if (useObjectNameIfEmpty)
        {
            return CleanObjectName(gameObject.name);
        }

        return "";
    }

    private bool TryReadEnemyNameTag(out string displayName)
    {
        displayName = "";

        MonoBehaviour[] components = GetComponents<MonoBehaviour>();

        foreach (MonoBehaviour component in components)
        {
            if (component == null || component == this)
            {
                continue;
            }

            Type type = component.GetType();

            if (type.Name != enemyNameTagComponentName)
            {
                continue;
            }

            string value = ReadStringMember(component, type, enemyDisplayNameMemberNames);

            if (!string.IsNullOrWhiteSpace(value))
            {
                displayName = value.Trim();
                return true;
            }
        }

        return false;
    }

    private string ReadStringMember(MonoBehaviour source, Type type, string[] names)
    {
        BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.IgnoreCase;

        foreach (string memberName in names)
        {
            FieldInfo field = type.GetField(memberName, flags);

            if (field != null && field.FieldType == typeof(string))
            {
                object value = field.GetValue(source);
                return value as string;
            }

            PropertyInfo property = type.GetProperty(memberName, flags);

            if (property != null && property.CanRead && property.PropertyType == typeof(string))
            {
                object value = property.GetValue(source);
                return value as string;
            }
        }

        return "";
    }

    private string CleanObjectName(string rawName)
    {
        if (string.IsNullOrWhiteSpace(rawName))
        {
            return "Enemy";
        }

        string cleaned = rawName;

        if (cleanRuntimeObjectName)
        {
            cleaned = cleaned.Replace("(Clone)", "");
            cleaned = cleaned.Replace("_", " ");
            cleaned = cleaned.Replace("-", " ");
            cleaned = cleaned.Trim();

            while (cleaned.Contains("  "))
            {
                cleaned = cleaned.Replace("  ", " ");
            }
        }

        return string.IsNullOrWhiteSpace(cleaned) ? "Enemy" : cleaned;
    }
}