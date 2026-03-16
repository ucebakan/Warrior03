using UnityEngine;
using UnityEngine.UI;

public class PlayerHpBarUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Image fillImage;

    public void UpdateBar(int currentHealth, int maxHealth)
    {
        if (fillImage == null) return;
        if (maxHealth <= 0)
        {
            fillImage.fillAmount = 0f;
            return;
        }

        fillImage.fillAmount = (float)currentHealth / maxHealth;
    }
}
