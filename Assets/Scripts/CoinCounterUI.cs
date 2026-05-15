using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CoinCounterUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private RectTransform hudParent;
    [SerializeField] private PlayerWallet playerWallet;

    [Header("Sprites")]
    [SerializeField] private Sprite backgroundSprite;
    [SerializeField] private Sprite coinIconSprite;

    [Header("Object Names")]
    [SerializeField] private string panelObjectName = "Generated_CoinCounterPanel";
    [SerializeField] private string backgroundObjectName = "Background";
    [SerializeField] private string iconObjectName = "CoinIcon";
    [SerializeField] private string textObjectName = "CoinText";

    [Header("Panel Layout")]
    [SerializeField] private Vector2 anchoredPosition = new Vector2(-45f, -42f);
    [SerializeField] private Vector2 panelSize = new Vector2(190f, 64f);

    [Header("Icon Layout")]
    [SerializeField] private Vector2 iconAnchoredPosition = new Vector2(-55f, 0f);
    [SerializeField] private Vector2 iconSize = new Vector2(48f, 48f);
    [SerializeField] private bool preserveIconAspect = true;

    [Header("Text Layout")]
    [SerializeField] private Vector2 textAnchoredPosition = new Vector2(28f, 0f);
    [SerializeField] private Vector2 textSize = new Vector2(110f, 44f);
    [SerializeField] private int fontSize = 32;
    [SerializeField] private Color textColor = new Color(1f, 0.8470588f, 0.4196079f, 1f); // #FFD86B
    [SerializeField] private FontStyles fontStyle = FontStyles.Bold;
    [SerializeField] private string numberPrefix = "";
    [SerializeField] private string numberSuffix = "";

    [Header("Text Shadow")]
    [SerializeField] private bool useTextShadow = true;
    [SerializeField] private Color shadowColor = new Color(0f, 0f, 0f, 0.75f);
    [SerializeField] private Vector2 shadowDistance = new Vector2(2f, -2f);

    [Header("Runtime")]
    [SerializeField] private bool autoFindHUDParent = true;
    [SerializeField] private bool autoFindPlayerWallet = true;

    private RectTransform panelRect;
    private Image backgroundImage;
    private Image iconImage;
    private TextMeshProUGUI coinText;

    private int lastShownCoins = int.MinValue;
    private bool missingHUDWarningShown;
    private bool missingWalletWarningShown;

    private void Start()
    {
        EnsureUI();
        RefreshCoinText(forceRefresh: true);
    }

    private void Update()
    {
        EnsureUI();
        RefreshCoinText(forceRefresh: false);
    }

    private void EnsureUI()
    {
        EnsureReferences();

        if (hudParent == null)
        {
            if (!missingHUDWarningShown)
            {
                Debug.LogWarning("[CoinCounterUI] PlayerHUD bulunamadý. Hud Parent alanýna PlayerHUD objesini sürükle.", this);
                missingHUDWarningShown = true;
            }

            return;
        }

        Transform existingPanel = hudParent.Find(panelObjectName);

        if (existingPanel == null)
        {
            GameObject panelObject = new GameObject(panelObjectName);
            panelObject.transform.SetParent(hudParent, false);

            panelRect = panelObject.AddComponent<RectTransform>();

            backgroundImage = CreateImage(backgroundObjectName, panelRect);
            iconImage = CreateImage(iconObjectName, panelRect);
            coinText = CreateText(textObjectName, panelRect);
        }
        else
        {
            panelRect = existingPanel.GetComponent<RectTransform>();

            if (panelRect == null)
            {
                panelRect = existingPanel.gameObject.AddComponent<RectTransform>();
            }

            backgroundImage = GetOrCreateImage(backgroundObjectName, panelRect);
            iconImage = GetOrCreateImage(iconObjectName, panelRect);
            coinText = GetOrCreateText(textObjectName, panelRect);
        }

        ApplyLayout();
        ApplyVisuals();
    }

    private void EnsureReferences()
    {
        if (hudParent == null && autoFindHUDParent)
        {
            GameObject foundHUD = GameObject.Find("PlayerHUD");

            if (foundHUD != null)
            {
                hudParent = foundHUD.GetComponent<RectTransform>();
            }
        }

        if (playerWallet == null && autoFindPlayerWallet)
        {
            playerWallet = FindObjectOfType<PlayerWallet>();
        }
    }

    private Image CreateImage(string objectName, RectTransform parent)
    {
        GameObject imageObject = new GameObject(objectName);
        imageObject.transform.SetParent(parent, false);

        RectTransform rect = imageObject.AddComponent<RectTransform>();
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

    private TextMeshProUGUI CreateText(string objectName, RectTransform parent)
    {
        GameObject textObject = new GameObject(objectName);
        textObject.transform.SetParent(parent, false);

        textObject.AddComponent<RectTransform>();

        TextMeshProUGUI text = textObject.AddComponent<TextMeshProUGUI>();
        text.raycastTarget = false;
        text.alignment = TextAlignmentOptions.Center;
        text.enableAutoSizing = false;

        if (useTextShadow)
        {
            Shadow shadow = textObject.AddComponent<Shadow>();
            shadow.effectColor = shadowColor;
            shadow.effectDistance = shadowDistance;
        }

        return text;
    }

    private TextMeshProUGUI GetOrCreateText(string objectName, RectTransform parent)
    {
        Transform existing = parent.Find(objectName);

        if (existing != null)
        {
            TextMeshProUGUI text = existing.GetComponent<TextMeshProUGUI>();

            if (text == null)
            {
                text = existing.gameObject.AddComponent<TextMeshProUGUI>();
            }

            text.raycastTarget = false;

            Shadow shadow = existing.GetComponent<Shadow>();

            if (useTextShadow)
            {
                if (shadow == null)
                {
                    shadow = existing.gameObject.AddComponent<Shadow>();
                }

                shadow.effectColor = shadowColor;
                shadow.effectDistance = shadowDistance;
            }
            else
            {
                if (shadow != null)
                {
                    Destroy(shadow);
                }
            }

            return text;
        }

        return CreateText(objectName, parent);
    }

    private void ApplyLayout()
    {
        if (panelRect == null)
        {
            return;
        }

        panelRect.anchorMin = new Vector2(1f, 1f);
        panelRect.anchorMax = new Vector2(1f, 1f);
        panelRect.pivot = new Vector2(1f, 1f);
        panelRect.anchoredPosition = anchoredPosition;
        panelRect.sizeDelta = panelSize;

        if (backgroundImage != null)
        {
            RectTransform backgroundRect = backgroundImage.rectTransform;

            backgroundRect.anchorMin = new Vector2(0.5f, 0.5f);
            backgroundRect.anchorMax = new Vector2(0.5f, 0.5f);
            backgroundRect.pivot = new Vector2(0.5f, 0.5f);
            backgroundRect.anchoredPosition = Vector2.zero;
            backgroundRect.sizeDelta = panelSize;
        }

        if (iconImage != null)
        {
            RectTransform iconRect = iconImage.rectTransform;

            iconRect.anchorMin = new Vector2(0.5f, 0.5f);
            iconRect.anchorMax = new Vector2(0.5f, 0.5f);
            iconRect.pivot = new Vector2(0.5f, 0.5f);
            iconRect.anchoredPosition = iconAnchoredPosition;
            iconRect.sizeDelta = iconSize;
        }

        if (coinText != null)
        {
            RectTransform textRect = coinText.rectTransform;

            textRect.anchorMin = new Vector2(0.5f, 0.5f);
            textRect.anchorMax = new Vector2(0.5f, 0.5f);
            textRect.pivot = new Vector2(0.5f, 0.5f);
            textRect.anchoredPosition = textAnchoredPosition;
            textRect.sizeDelta = textSize;
        }
    }

    private void ApplyVisuals()
    {
        if (backgroundImage != null)
        {
            backgroundImage.sprite = backgroundSprite;
            backgroundImage.type = Image.Type.Simple;
            backgroundImage.color = Color.white;
            backgroundImage.raycastTarget = false;
        }

        if (iconImage != null)
        {
            iconImage.sprite = coinIconSprite;
            iconImage.type = Image.Type.Simple;
            iconImage.color = Color.white;
            iconImage.preserveAspect = preserveIconAspect;
            iconImage.raycastTarget = false;
        }

        if (coinText != null)
        {
            coinText.color = textColor;
            coinText.fontSize = fontSize;
            coinText.fontStyle = fontStyle;
            coinText.alignment = TextAlignmentOptions.Center;
            coinText.raycastTarget = false;
        }

        if (backgroundImage != null)
        {
            backgroundImage.transform.SetSiblingIndex(0);
        }

        if (iconImage != null)
        {
            iconImage.transform.SetSiblingIndex(1);
        }

        if (coinText != null)
        {
            coinText.transform.SetSiblingIndex(2);
        }
    }

    private void RefreshCoinText(bool forceRefresh)
    {
        if (coinText == null)
        {
            return;
        }

        if (playerWallet == null)
        {
            EnsureReferences();

            if (playerWallet == null)
            {
                if (!missingWalletWarningShown)
                {
                    Debug.LogWarning("[CoinCounterUI] PlayerWallet bulunamadý. Player Wallet alanýna PlayerWallet olan player objesini sürükle.", this);
                    missingWalletWarningShown = true;
                }

                coinText.text = numberPrefix + "0" + numberSuffix;
                return;
            }
        }

        int coins = playerWallet.GetCoins();

        if (!forceRefresh && coins == lastShownCoins)
        {
            return;
        }

        lastShownCoins = coins;
        coinText.text = numberPrefix + coins + numberSuffix;
    }
}