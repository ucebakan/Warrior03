using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    [Header("Health Settings")]
    [SerializeField] private int maxHealth = 10;
    [SerializeField] private int currentHealth = 10;

    [Header("Debug")]
    [SerializeField] private bool enableDebugDamageKey = true;
    [SerializeField] private KeyCode debugDamageKey = KeyCode.H;
    [SerializeField] private int debugDamageAmount = 1;

    private PlayerHpBarUI hpBarUI;

    public int MaxHealth => maxHealth;
    public int CurrentHealth => currentHealth;
    public float HealthPercent => maxHealth <= 0 ? 0f : (float)currentHealth / maxHealth;

    private void Awake()
    {
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);

        hpBarUI = FindObjectOfType<PlayerHpBarUI>();
        UpdateHpBar();
    }

    private void Start()
    {
        UpdateHpBar();
    }

    private void Update()
    {
        if (enableDebugDamageKey && Input.GetKeyDown(debugDamageKey))
        {
            TakeDamage(debugDamageAmount);
        }
    }

    public void TakeDamage(int damageAmount)
    {
        if (damageAmount <= 0) return;
        if (currentHealth <= 0) return;

        currentHealth -= damageAmount;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);

        UpdateHpBar();

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    public void Heal(int healAmount)
    {
        if (healAmount <= 0) return;
        if (currentHealth <= 0) return;

        currentHealth += healAmount;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);

        UpdateHpBar();
    }

    public void SetMaxHealth(int newMaxHealth, bool refillHealth = true)
    {
        maxHealth = Mathf.Max(1, newMaxHealth);

        if (refillHealth)
            currentHealth = maxHealth;
        else
            currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);

        UpdateHpBar();
    }

    private void UpdateHpBar()
    {
        if (hpBarUI == null)
        {
            hpBarUI = FindObjectOfType<PlayerHpBarUI>();
        }

        if (hpBarUI != null)
        {
            hpBarUI.UpdateBar(currentHealth, maxHealth);
        }
    }

    private void Die()
    {
        Debug.Log("Player died.");
    }
}
