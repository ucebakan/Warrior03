using UnityEngine;
using UnityEngine.UI;

public class EnemyHealth : MonoBehaviour
{
    [SerializeField] private int maxHealth = 3;
    [SerializeField] private Image hpBarFillImage;

    private int currentHealth;
    private bool isDead;
    private EnemyLoot enemyLoot;

    private void Start()
    {
        currentHealth = maxHealth;
        enemyLoot = GetComponent<EnemyLoot>();
        UpdateHPBar();
    }

    public void TakeDamage(int damageAmount)
    {
        if (isDead) return;

        currentHealth -= damageAmount;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);

        Debug.Log(gameObject.name + " took damage. Current Health: " + currentHealth);

        UpdateHPBar();

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    private void UpdateHPBar()
    {
        if (hpBarFillImage == null) return;

        hpBarFillImage.fillAmount = (float)currentHealth / maxHealth;
    }

    private void Die()
    {
        if (isDead) return;

        isDead = true;

        if (enemyLoot != null)
        {
            enemyLoot.DropLoot();
        }

        Debug.Log(gameObject.name + " died.");
        Destroy(gameObject);
    }
}