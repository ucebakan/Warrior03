using System.Collections;
using UnityEngine;

public class EnemyOrcAI : MonoBehaviour
{
    private enum EnemyState
    {
        Idle,
        Chase,
        Attack,
        ReturnHome
    }

    [Header("References")]
    [SerializeField] private Transform playerTarget;
    [SerializeField] private EnemyOrcAnimatorDriver animatorDriver;
    [SerializeField] private EnemyAttackDamage enemyAttackDamage;

    [Header("Detection")]
    [SerializeField] private float visionRange = 8f;
    [SerializeField] private float loseTargetRange = 10f;
    [SerializeField] private float attackRange = 2f;
    [SerializeField] private float stopAttackRange = 2.4f;

    [Header("Movement")]
    [SerializeField] private float chaseSpeed = 3.5f;
    [SerializeField] private float returnHomeWalkSpeed = 1.8f;
    [SerializeField] private float rotationSpeed = 10f;
    [SerializeField] private float stopDistanceToHome = 0.15f;
    [SerializeField] private float maxChaseDistanceFromHome = 12f;

    [Header("Attack")]
    [SerializeField] private float attackCooldown = 1.2f;
    [SerializeField] private float attackHitDelay = 0.35f;
    [SerializeField] private float attackTotalDuration = 1.0f;

    [Header("Debug")]
    [SerializeField] private bool drawDebugGizmos = true;
    [SerializeField] private bool enableDebugLogs = false;

    [Header("Runtime")]
    [SerializeField] private EnemyState currentState = EnemyState.Idle;

    private Vector3 homePosition;
    private float lastAttackTime = -999f;
    private EnemyState lastLoggedState;
    private bool isForcedReturning;
    private bool isAttackInProgress;
    private Coroutine attackRoutine;

    private void Awake()
    {
        if (animatorDriver == null)
            animatorDriver = GetComponent<EnemyOrcAnimatorDriver>();

        if (enemyAttackDamage == null)
            enemyAttackDamage = GetComponentInChildren<EnemyAttackDamage>();

        if (playerTarget == null)
        {
            GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
            if (playerObject != null)
                playerTarget = playerObject.transform;
        }

        homePosition = transform.position;
        lastLoggedState = currentState;

        if (enableDebugLogs)
        {
            Debug.Log($"{name} AI Awake | playerTarget={(playerTarget != null ? playerTarget.name : "NULL")} | home={homePosition}");
        }
    }

    private void Update()
    {
        if (playerTarget == null)
        {
            SetIdleState();
            return;
        }

        float distanceToPlayer = Vector3.Distance(transform.position, playerTarget.position);
        float distanceToHome = Vector3.Distance(transform.position, homePosition);

        bool playerInVision = distanceToPlayer <= visionRange;
        bool playerInLoseRange = distanceToPlayer <= loseTargetRange;
        bool playerInAttackRange = distanceToPlayer <= attackRange;
        bool playerStillAttackable = distanceToPlayer <= stopAttackRange;
        bool tooFarFromHome = distanceToHome >= maxChaseDistanceFromHome;

        switch (currentState)
        {
            case EnemyState.Idle:
                isForcedReturning = false;

                if (playerInVision && !tooFarFromHome)
                {
                    currentState = EnemyState.Chase;
                }
                else
                {
                    animatorDriver.SetSpeed(0f);
                }
                break;

            case EnemyState.Chase:
                if (tooFarFromHome)
                {
                    isForcedReturning = true;
                    currentState = EnemyState.ReturnHome;
                    break;
                }

                if (!playerInLoseRange)
                {
                    isForcedReturning = true;
                    currentState = EnemyState.ReturnHome;
                    break;
                }

                if (playerInAttackRange)
                {
                    currentState = EnemyState.Attack;
                    break;
                }

                MoveTowards(playerTarget.position, chaseSpeed, 1f);
                break;

            case EnemyState.Attack:
                FaceTarget(playerTarget.position);
                animatorDriver.SetSpeed(0f);

                if (tooFarFromHome)
                {
                    isForcedReturning = true;
                    currentState = EnemyState.ReturnHome;
                    break;
                }

                if (!playerStillAttackable)
                {
                    if (playerInLoseRange && !tooFarFromHome)
                    {
                        currentState = EnemyState.Chase;
                    }
                    else
                    {
                        isForcedReturning = true;
                        currentState = EnemyState.ReturnHome;
                    }

                    break;
                }

                if (!isAttackInProgress && Time.time >= lastAttackTime + attackCooldown)
                {
                    if (enableDebugLogs)
                        Debug.Log($"{name} ATTACK triggered | distanceToPlayer={distanceToPlayer:F2}");

                    if (attackRoutine != null)
                        StopCoroutine(attackRoutine);

                    attackRoutine = StartCoroutine(AttackRoutine());
                    lastAttackTime = Time.time;
                }
                break;

            case EnemyState.ReturnHome:
                float remainingDistanceToHome = Vector3.Distance(transform.position, homePosition);

                if (remainingDistanceToHome <= stopDistanceToHome)
                {
                    transform.position = homePosition;
                    currentState = EnemyState.Idle;
                    animatorDriver.SetSpeed(0f);
                    isForcedReturning = false;
                    break;
                }

                if (!isForcedReturning && playerInVision && !tooFarFromHome)
                {
                    currentState = EnemyState.Chase;
                    break;
                }

                MoveTowards(homePosition, returnHomeWalkSpeed, 0.5f);
                break;
        }

        if (enableDebugLogs && currentState != lastLoggedState)
        {
            Debug.Log(
                $"{name} State: {lastLoggedState} -> {currentState} | " +
                $"distPlayer={distanceToPlayer:F2} | distHome={distanceToHome:F2} | " +
                $"vision={playerInVision} | attack={playerInAttackRange} | forcedReturn={isForcedReturning}"
            );

            lastLoggedState = currentState;
        }
    }

    private IEnumerator AttackRoutine()
    {
        isAttackInProgress = true;

        if (animatorDriver != null)
            animatorDriver.TriggerAttack();

        yield return new WaitForSeconds(attackHitDelay);

        if (enemyAttackDamage != null)
            enemyAttackDamage.DealDamage();

        float remainingTime = Mathf.Max(0f, attackTotalDuration - attackHitDelay);
        yield return new WaitForSeconds(remainingTime);

        isAttackInProgress = false;
        attackRoutine = null;
    }

    private void MoveTowards(Vector3 targetPosition, float moveSpeed, float animationSpeed)
    {
        Vector3 direction = targetPosition - transform.position;
        direction.y = 0f;

        if (direction.sqrMagnitude <= 0.0001f)
        {
            animatorDriver.SetSpeed(0f);
            return;
        }

        direction.Normalize();

        transform.position += direction * moveSpeed * Time.deltaTime;

        Quaternion targetRotation = Quaternion.LookRotation(direction);
        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            targetRotation,
            rotationSpeed * Time.deltaTime
        );

        animatorDriver.SetSpeed(animationSpeed);
    }

    private void FaceTarget(Vector3 targetPosition)
    {
        Vector3 direction = targetPosition - transform.position;
        direction.y = 0f;

        if (direction.sqrMagnitude <= 0.0001f) return;

        Quaternion targetRotation = Quaternion.LookRotation(direction.normalized);
        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            targetRotation,
            rotationSpeed * Time.deltaTime
        );
    }

    private void SetIdleState()
    {
        currentState = EnemyState.Idle;
        isForcedReturning = false;

        if (animatorDriver != null)
            animatorDriver.SetSpeed(0f);
    }

    private void OnDrawGizmosSelected()
    {
        if (!drawDebugGizmos) return;

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, visionRange);

        Gizmos.color = new Color(1f, 0.6f, 0f, 1f);
        Gizmos.DrawWireSphere(transform.position, loseTargetRange);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);

        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position, stopAttackRange);

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(
            homePosition == Vector3.zero ? transform.position : homePosition,
            maxChaseDistanceFromHome
        );
    }
}
