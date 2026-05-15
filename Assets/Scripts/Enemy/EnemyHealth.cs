using UnityEngine;
using UnityEngine.UI;

public class EnemyHealth : MonoBehaviour
{
    [Header("Health")]
    [SerializeField] private int maxHealth = 3;
    [SerializeField] private Image hpBarFillImage;

    [Header("Loot")]
    [SerializeField] private EnemyLoot enemyLoot;

    [Header("Quest")]
    [SerializeField] private EnemyQuestTag enemyQuestTag;

    [Header("Death SFX")]
    [SerializeField] private AudioClip deathSFX;
    [SerializeField][Range(0f, 1f)] private float deathSFXVolume = 1f;

    private int currentHealth;
    private bool isDead;

    private void Awake()
    {
        if (enemyLoot == null)
        {
            enemyLoot = GetComponent<EnemyLoot>();
        }

        if (enemyQuestTag == null)
        {
            enemyQuestTag = GetComponent<EnemyQuestTag>();

            if (enemyQuestTag == null)
            {
                enemyQuestTag = GetComponentInParent<EnemyQuestTag>();
            }

            if (enemyQuestTag == null)
            {
                enemyQuestTag = GetComponentInChildren<EnemyQuestTag>();
            }
        }
    }

    private void Start()
    {
        currentHealth = maxHealth;
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

        PlayDeathSFX();

        if (enemyQuestTag != null && QuestManager.Instance != null)
        {
            QuestManager.Instance.ReportEnemyKilled(enemyQuestTag.EnemyQuestType);
        }

        if (enemyLoot != null)
        {
            enemyLoot.DropLoot();
        }

        Debug.Log(gameObject.name + " died.");
        Destroy(gameObject);
    }

    private void PlayDeathSFX()
    {
        if (deathSFX == null)
            return;

        GameObject tempAudioObject = new GameObject("EnemyDeathSFX");
        tempAudioObject.transform.position = transform.position;

        AudioSource tempAudioSource = tempAudioObject.AddComponent<AudioSource>();
        tempAudioSource.clip = deathSFX;
        tempAudioSource.volume = deathSFXVolume;
        tempAudioSource.playOnAwake = false;
        tempAudioSource.loop = false;
        tempAudioSource.spatialBlend = 0f;

        tempAudioSource.Play();

        Destroy(tempAudioObject, deathSFX.length + 0.1f);
    }
}