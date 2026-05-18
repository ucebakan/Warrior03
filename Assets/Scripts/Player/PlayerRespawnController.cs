using System;
using System.Collections;
using UnityEngine;

public class PlayerRespawnController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerHealth playerHealth;
    [SerializeField] private Transform respawnPoint;

    [Tooltip("Ölüm sýrasýnda gizlenecek karakter/model objesi. Örn: Warrior_03 child objesi.")]
    [SerializeField] private GameObject visualRootToHide;

    [Tooltip("Ölüm sýrasýnda kapatýlacak scriptler. Örn: MobilePlayerMovement, PlayerMovement, attack scriptleri.")]
    [SerializeField] private Behaviour[] componentsToDisableDuringDeath;

    [Header("Death Visibility")]
    [SerializeField] private bool hideVisualOnDeath = true;
    [SerializeField] private bool disableCharacterControllerDuringDeath = true;

    [Header("Respawn Settings")]
    [SerializeField] private float respawnDelay = 1.5f;
    [SerializeField] private float invulnerabilityAfterRespawn = 1.5f;
    [SerializeField] private bool resetRotationOnRespawn = true;
    [SerializeField] private bool clearRigidbodyVelocity = true;

    [Header("Enemy Reset On Death/Respawn")]
    [SerializeField] private bool clearMobZonesOnDeath = true;
    [SerializeField] private bool forceZoneEnemiesReturnHomeOnDeath = true;
    [SerializeField] private bool repeatEnemyResetAfterRespawnMove = true;

    [Header("Optional Animator")]
    [SerializeField] private Animator playerAnimator;
    [SerializeField] private bool useAnimatorTriggers = false;
    [SerializeField] private string deathTriggerName = "Death";
    [SerializeField] private string respawnTriggerName = "Respawn";

    [Header("Debug")]
    [SerializeField] private bool logRespawnSteps = true;

    private Coroutine respawnRoutine;
    private CharacterController characterController;
    private Rigidbody playerRigidbody;

    public event Action OnRespawnSequenceStarted;
    public event Action OnRespawnSequenceFinished;

    public float RespawnDelay => respawnDelay;
    public bool IsRespawning => respawnRoutine != null;

    private void Awake()
    {
        if (playerHealth == null)
            playerHealth = GetComponent<PlayerHealth>();

        characterController = GetComponent<CharacterController>();
        playerRigidbody = GetComponent<Rigidbody>();

        if (playerAnimator == null)
            playerAnimator = GetComponentInChildren<Animator>();

        if (visualRootToHide == null)
        {
            Animator foundAnimator = GetComponentInChildren<Animator>();

            if (foundAnimator != null)
                visualRootToHide = foundAnimator.gameObject;
        }
    }

    private void OnEnable()
    {
        if (playerHealth != null)
            playerHealth.OnDied += HandlePlayerDied;
    }

    private void OnDisable()
    {
        if (playerHealth != null)
            playerHealth.OnDied -= HandlePlayerDied;
    }

    private void HandlePlayerDied()
    {
        if (respawnRoutine != null)
            return;

        respawnRoutine = StartCoroutine(RespawnRoutine());
    }

    private IEnumerator RespawnRoutine()
    {
        if (logRespawnSteps)
            Debug.Log("PlayerRespawnController: Player death detected.");

        OnRespawnSequenceStarted?.Invoke();

        ResetEnemyZoneCombatState();

        SetGameplayControl(false);
        SetDeathVisibility(false);

        if (useAnimatorTriggers && playerAnimator != null && !string.IsNullOrWhiteSpace(deathTriggerName))
        {
            playerAnimator.SetTrigger(deathTriggerName);
        }

        yield return new WaitForSeconds(respawnDelay);

        MovePlayerToRespawnPoint();

        if (repeatEnemyResetAfterRespawnMove)
        {
            ResetEnemyZoneCombatState();
        }

        if (playerHealth != null)
        {
            playerHealth.RestoreFullHealth();
            playerHealth.SetTemporaryInvulnerability(invulnerabilityAfterRespawn);
        }

        SetDeathVisibility(true);

        if (useAnimatorTriggers && playerAnimator != null && !string.IsNullOrWhiteSpace(respawnTriggerName))
        {
            playerAnimator.SetTrigger(respawnTriggerName);
        }

        SetGameplayControl(true);

        if (logRespawnSteps)
            Debug.Log("PlayerRespawnController: Player respawn completed.");

        OnRespawnSequenceFinished?.Invoke();
        respawnRoutine = null;
    }

    private void ResetEnemyZoneCombatState()
    {
        if (clearMobZonesOnDeath)
        {
            MobZone[] mobZones = FindObjectsOfType<MobZone>(true);

            for (int i = 0; i < mobZones.Length; i++)
            {
                if (mobZones[i] == null)
                    continue;

                mobZones[i].ForceClearPlayer();
            }

            if (logRespawnSteps)
            {
                Debug.Log($"PlayerRespawnController: Cleared {mobZones.Length} MobZone player references.");
            }
        }

        if (forceZoneEnemiesReturnHomeOnDeath)
        {
            ZoneEnemyAI[] zoneEnemies = FindObjectsOfType<ZoneEnemyAI>(true);

            for (int i = 0; i < zoneEnemies.Length; i++)
            {
                if (zoneEnemies[i] == null)
                    continue;

                zoneEnemies[i].ForceReturnHome();
            }

            if (logRespawnSteps)
            {
                Debug.Log($"PlayerRespawnController: Forced {zoneEnemies.Length} ZoneEnemyAI enemies to return home.");
            }
        }
    }

    private void MovePlayerToRespawnPoint()
    {
        if (respawnPoint == null)
        {
            Debug.LogWarning("PlayerRespawnController: Respawn Point atanmadý. Player mevcut pozisyonda canlandýrýldý.");
            return;
        }

        if (characterController != null)
            characterController.enabled = false;

        if (clearRigidbodyVelocity && playerRigidbody != null)
        {
            playerRigidbody.velocity = Vector3.zero;
            playerRigidbody.angularVelocity = Vector3.zero;
        }

        transform.position = respawnPoint.position;

        if (resetRotationOnRespawn)
            transform.rotation = respawnPoint.rotation;

        if (characterController != null && !disableCharacterControllerDuringDeath)
            characterController.enabled = true;
    }

    private void SetGameplayControl(bool enabled)
    {
        if (componentsToDisableDuringDeath != null)
        {
            for (int i = 0; i < componentsToDisableDuringDeath.Length; i++)
            {
                Behaviour component = componentsToDisableDuringDeath[i];

                if (component == null)
                    continue;

                if (component == this)
                    continue;

                if (component == playerHealth)
                    continue;

                component.enabled = enabled;
            }
        }

        if (characterController != null && disableCharacterControllerDuringDeath)
            characterController.enabled = enabled;
    }

    private void SetDeathVisibility(bool visible)
    {
        if (!hideVisualOnDeath)
            return;

        if (visualRootToHide == null)
            return;

        visualRootToHide.SetActive(visible);
    }
}