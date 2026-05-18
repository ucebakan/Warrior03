using UnityEngine;

[RequireComponent(typeof(ZoneMobMember))]
public class ZoneEnemyAI : MonoBehaviour
{
    private enum State
    {
        Idle,
        Roam,
        Chase,
        Attack,
        ReturnHome
    }

    [Header("References")]
    [SerializeField] private ZoneMobMember zoneMember;
    [SerializeField] private Animator animator;
    [SerializeField] private EnemyHealth enemyHealth;

    [Header("Movement")]
    [SerializeField] private float walkSpeed = 1.2f;
    [SerializeField] private float chaseSpeed = 2.2f;
    [SerializeField] private float rotationSpeed = 10f;
    [SerializeField] private float roamRadius = 3f;
    [SerializeField] private float returnStopDistance = 0.15f;
    [SerializeField] private float attackRange = 1.2f;

    [Header("Idle / Roam")]
    [SerializeField] private float idleDurationMin = 1.2f;
    [SerializeField] private float idleDurationMax = 2.5f;

    [Header("Attack")]
    [SerializeField] private int damage = 1;
    [SerializeField] private float attackCooldown = 2f;
    [SerializeField] private string attackTriggerName = "";

    [Header("Player Death / Zone Safety")]
    [SerializeField] private bool forceReturnWhenPlayerIsInvalid = true;
    [SerializeField] private bool forceReturnWhenPlayerOutsideZone = true;
    [SerializeField] private float zoneOutsideTolerance = 0.15f;
    [SerializeField] private bool blockAttackWhilePlayerDeadOrRespawning = true;

    [Header("Hit Reaction")]
    [SerializeField] private bool enableHitReaction = true;
    [SerializeField] private float hitReactionDuration = 0.10f;
    [SerializeField] private float hitReactionDistance = 0.16f;
    [SerializeField] private float hitReactionMoveSpeed = 6f;
    [SerializeField] private float hitReactionCooldown = 0.05f;

    [Header("Ground")]
    [SerializeField] private LayerMask groundMask = ~0;
    [SerializeField] private float groundCheckStartHeight = 5f;
    [SerializeField] private float groundCheckDistance = 20f;
    [SerializeField] private float visualGroundClearance = 0.02f;
    [SerializeField] private bool ignoreParticleRenderers = true;

    [Header("Animator")]
    [SerializeField] private bool useSpeedParameter = true;
    [SerializeField] private string speedParameterName = "Speed";

    [Header("Debug")]
    [SerializeField] private bool logStateChanges = false;

    private State currentState;
    private State lastLoggedState;

    private Vector3 roamTarget;
    private float idleTimer;
    private float nextAttackTime;

    private bool isHitReacting;
    private float hitReactionEndTime;
    private float nextHitReactionTime;
    private Vector3 hitReactionTargetPosition;

    private void Reset()
    {
        AutoSetupReferences();
    }

    private void Awake()
    {
        AutoSetupReferences();
        EnterIdle();
        lastLoggedState = currentState;
    }

    private void Update()
    {
        if (zoneMember == null)
            return;

        if (isHitReacting)
        {
            UpdateHitReaction();
            return;
        }

        Transform player = zoneMember.CurrentPlayer;
        bool hasPlayerInZone = zoneMember.HasPlayerInZone && player != null;

        if (hasPlayerInZone && !IsPlayerValidForCombat(player))
        {
            if (forceReturnWhenPlayerIsInvalid)
            {
                ForceReturnHome();
            }
            else
            {
                EnterIdle();
            }

            return;
        }

        if (hasPlayerInZone && forceReturnWhenPlayerOutsideZone && !IsPlayerActuallyInsideZone(player))
        {
            if (zoneMember.MobZone != null)
            {
                zoneMember.MobZone.ForceClearPlayer();
            }

            ForceReturnHome();
            return;
        }

        if (hasPlayerInZone)
        {
            HandleCombat(player);
            LogStateIfChanged();
            return;
        }

        HandleNoPlayerBehavior();
        LogStateIfChanged();
    }

    private void AutoSetupReferences()
    {
        if (zoneMember == null)
        {
            zoneMember = GetComponent<ZoneMobMember>();
        }

        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>();
        }

        if (enemyHealth == null)
        {
            enemyHealth = GetComponentInChildren<EnemyHealth>();
        }
    }

    private bool IsPlayerValidForCombat(Transform player)
    {
        if (player == null)
            return false;

        if (!blockAttackWhilePlayerDeadOrRespawning)
            return true;

        PlayerHealth playerHealth = player.GetComponent<PlayerHealth>();

        if (playerHealth == null)
            playerHealth = player.GetComponentInChildren<PlayerHealth>();

        if (playerHealth == null)
            playerHealth = player.GetComponentInParent<PlayerHealth>();

        if (playerHealth != null && playerHealth.IsDead)
            return false;

        PlayerRespawnController respawnController = player.GetComponent<PlayerRespawnController>();

        if (respawnController == null)
            respawnController = player.GetComponentInChildren<PlayerRespawnController>();

        if (respawnController == null)
            respawnController = player.GetComponentInParent<PlayerRespawnController>();

        if (respawnController != null && respawnController.IsRespawning)
            return false;

        return true;
    }

    private bool IsPlayerActuallyInsideZone(Transform player)
    {
        if (player == null)
            return false;

        if (zoneMember == null || zoneMember.MobZone == null)
            return false;

        return zoneMember.MobZone.IsTransformInsideZone(player, zoneOutsideTolerance);
    }

    private void HandleCombat(Transform player)
    {
        if (!IsPlayerValidForCombat(player))
        {
            ForceReturnHome();
            return;
        }

        if (forceReturnWhenPlayerOutsideZone && !IsPlayerActuallyInsideZone(player))
        {
            if (zoneMember.MobZone != null)
            {
                zoneMember.MobZone.ForceClearPlayer();
            }

            ForceReturnHome();
            return;
        }

        float distanceToPlayer = DistanceXZ(transform.position, player.position);

        if (distanceToPlayer <= attackRange)
        {
            currentState = State.Attack;
            SetAnimatorSpeed(0f);
            FaceTarget(player.position);
            StickToGroundAtCurrentPosition();
            TryAttack(player);
            return;
        }

        currentState = State.Chase;
        Vector3 target = ClampPointInsideZone(player.position);
        MoveTo(target, chaseSpeed);
    }

    private void HandleNoPlayerBehavior()
    {
        if (currentState == State.Chase || currentState == State.Attack || currentState == State.ReturnHome)
        {
            float distanceToHome = DistanceXZ(transform.position, zoneMember.HomePosition);

            if (distanceToHome > returnStopDistance)
            {
                currentState = State.ReturnHome;
                MoveTo(zoneMember.HomePosition, chaseSpeed);
                return;
            }

            EnterIdle();
            return;
        }

        if (currentState == State.Idle)
        {
            idleTimer -= Time.deltaTime;
            SetAnimatorSpeed(0f);
            StickToGroundAtCurrentPosition();

            if (idleTimer <= 0f)
            {
                PickRoamTarget();
            }

            return;
        }

        if (currentState == State.Roam)
        {
            float distanceToRoamTarget = DistanceXZ(transform.position, roamTarget);

            if (distanceToRoamTarget <= 0.1f)
            {
                EnterIdle();
                return;
            }

            MoveTo(roamTarget, walkSpeed);
            return;
        }

        EnterIdle();
    }

    private void EnterIdle()
    {
        currentState = State.Idle;
        idleTimer = Random.Range(idleDurationMin, idleDurationMax);
        SetAnimatorSpeed(0f);
    }

    public void ForceReturnHome()
    {
        isHitReacting = false;

        if (zoneMember == null)
        {
            EnterIdle();
            return;
        }

        float distanceToHome = DistanceXZ(transform.position, zoneMember.HomePosition);

        if (distanceToHome > returnStopDistance)
        {
            currentState = State.ReturnHome;
            SetAnimatorSpeed(0.5f);
        }
        else
        {
            transform.position = GetGroundAdjustedPosition(zoneMember.HomePosition);
            EnterIdle();
        }
    }

    private void PickRoamTarget()
    {
        Vector2 randomCircle = Random.insideUnitCircle * roamRadius;
        Vector3 candidate = zoneMember.HomePosition + new Vector3(randomCircle.x, 0f, randomCircle.y);
        roamTarget = ClampPointInsideZone(candidate);
        currentState = State.Roam;
    }

    private void MoveTo(Vector3 targetWorldPosition, float moveSpeed)
    {
        Vector3 clampedTarget = ClampPointInsideZone(targetWorldPosition);
        Vector3 groundedTarget = GetGroundAdjustedPosition(clampedTarget);

        Vector3 current = transform.position;
        Vector3 next = Vector3.MoveTowards(current, groundedTarget, moveSpeed * Time.deltaTime);

        next = ClampPointInsideZone(next);
        next = GetGroundAdjustedPosition(next);

        transform.position = next;
        FaceTarget(groundedTarget);
        SetAnimatorSpeed(moveSpeed);
    }

    private void FaceTarget(Vector3 targetWorldPosition)
    {
        Vector3 direction = targetWorldPosition - transform.position;
        direction.y = 0f;

        if (direction.sqrMagnitude <= 0.0001f)
            return;

        Quaternion targetRotation = Quaternion.LookRotation(direction.normalized);
        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            targetRotation,
            rotationSpeed * Time.deltaTime
        );
    }

    private void TryAttack(Transform player)
    {
        if (!IsPlayerValidForCombat(player))
            return;

        if (Time.time < nextAttackTime)
            return;

        nextAttackTime = Time.time + attackCooldown;

        if (animator != null && !string.IsNullOrEmpty(attackTriggerName))
        {
            animator.SetTrigger(attackTriggerName);
        }

        player.SendMessage("TakeDamage", damage, SendMessageOptions.DontRequireReceiver);
        player.SendMessage("TakeDamage", (float)damage, SendMessageOptions.DontRequireReceiver);
    }

    public void TakeDamage(int damageAmount)
    {
        if (enemyHealth == null)
        {
            enemyHealth = GetComponentInChildren<EnemyHealth>();
        }

        if (enemyHealth != null)
        {
            enemyHealth.TakeDamage(damageAmount);
        }

        StartHitReactionFromCurrentPlayer();
    }

    public void TakeDamage(float damageAmount)
    {
        if (enemyHealth == null)
        {
            enemyHealth = GetComponentInChildren<EnemyHealth>();
        }

        if (enemyHealth != null)
        {
            enemyHealth.TakeDamage(Mathf.RoundToInt(damageAmount));
        }

        StartHitReactionFromCurrentPlayer();
    }

    public void TakeHitReaction()
    {
        StartHitReactionFromCurrentPlayer();
    }

    private void StartHitReactionFromCurrentPlayer()
    {
        if (!enableHitReaction)
            return;

        if (Time.time < nextHitReactionTime)
            return;

        nextHitReactionTime = Time.time + hitReactionCooldown;

        Vector3 sourcePosition = transform.position - transform.forward;

        if (zoneMember != null && zoneMember.CurrentPlayer != null && IsPlayerValidForCombat(zoneMember.CurrentPlayer))
        {
            sourcePosition = zoneMember.CurrentPlayer.position;
        }

        Vector3 pushDirection = transform.position - sourcePosition;
        pushDirection.y = 0f;

        if (pushDirection.sqrMagnitude <= 0.0001f)
        {
            pushDirection = -transform.forward;
        }

        pushDirection.Normalize();

        Vector3 rawTarget = transform.position + pushDirection * hitReactionDistance;
        Vector3 clampedTarget = ClampPointInsideZone(rawTarget);

        hitReactionTargetPosition = GetGroundAdjustedPosition(clampedTarget);
        hitReactionEndTime = Time.time + hitReactionDuration;
        isHitReacting = true;

        currentState = State.Idle;
        nextAttackTime = Mathf.Max(nextAttackTime, Time.time + (hitReactionDuration * 0.5f));
        SetAnimatorSpeed(0f);
    }

    private void UpdateHitReaction()
    {
        Vector3 next = Vector3.MoveTowards(
            transform.position,
            hitReactionTargetPosition,
            hitReactionMoveSpeed * Time.deltaTime
        );

        next = ClampPointInsideZone(next);
        next = GetGroundAdjustedPosition(next);

        transform.position = next;
        SetAnimatorSpeed(0f);
        StickToGroundAtCurrentPosition();

        if (Time.time >= hitReactionEndTime || DistanceXZ(transform.position, hitReactionTargetPosition) <= 0.02f)
        {
            isHitReacting = false;
            StickToGroundAtCurrentPosition();
        }
    }

    private Vector3 ClampPointInsideZone(Vector3 worldPoint)
    {
        if (zoneMember == null || zoneMember.MobZone == null || zoneMember.MobZone.ZoneCenter == null)
            return worldPoint;

        Vector3 center = zoneMember.MobZone.ZoneCenter.position;
        float zoneRadius = zoneMember.MobZone.GetZoneRadius();

        Vector3 flatOffset = new Vector3(
            worldPoint.x - center.x,
            0f,
            worldPoint.z - center.z
        );

        if (flatOffset.magnitude > zoneRadius)
        {
            flatOffset = flatOffset.normalized * Mathf.Max(0f, zoneRadius - 0.05f);
        }

        return new Vector3(
            center.x + flatOffset.x,
            worldPoint.y,
            center.z + flatOffset.z
        );
    }

    private Vector3 GetGroundAdjustedPosition(Vector3 targetWorldPosition)
    {
        Vector3 groundPoint;

        if (TryGetGroundPoint(targetWorldPosition, out groundPoint))
        {
            float rootToVisualBottomOffset = GetRootToVisualBottomOffset();

            return new Vector3(
                targetWorldPosition.x,
                groundPoint.y + rootToVisualBottomOffset + visualGroundClearance,
                targetWorldPosition.z
            );
        }

        return new Vector3(
            targetWorldPosition.x,
            transform.position.y,
            targetWorldPosition.z
        );
    }

    private void StickToGroundAtCurrentPosition()
    {
        Vector3 adjusted = GetGroundAdjustedPosition(transform.position);
        transform.position = adjusted;
    }

    private float GetRootToVisualBottomOffset()
    {
        Renderer[] renderers = GetComponentsInChildren<Renderer>(true);

        bool foundRenderer = false;
        float lowestY = float.MaxValue;

        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];

            if (ignoreParticleRenderers && renderer is ParticleSystemRenderer)
            {
                continue;
            }

            if (!renderer.enabled)
            {
                continue;
            }

            if (renderer.bounds.size == Vector3.zero)
            {
                continue;
            }

            foundRenderer = true;
            lowestY = Mathf.Min(lowestY, renderer.bounds.min.y);
        }

        if (!foundRenderer)
        {
            return 0f;
        }

        return transform.position.y - lowestY;
    }

    private bool TryGetGroundPoint(Vector3 worldPosition, out Vector3 groundPoint)
    {
        Vector3 rayStart = new Vector3(
            worldPosition.x,
            worldPosition.y + groundCheckStartHeight,
            worldPosition.z
        );

        Ray ray = new Ray(rayStart, Vector3.down);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, groundCheckDistance, groundMask, QueryTriggerInteraction.Ignore))
        {
            groundPoint = hit.point;
            return true;
        }

        groundPoint = worldPosition;
        return false;
    }

    private float DistanceXZ(Vector3 a, Vector3 b)
    {
        Vector3 flatA = new Vector3(a.x, 0f, a.z);
        Vector3 flatB = new Vector3(b.x, 0f, b.z);
        return Vector3.Distance(flatA, flatB);
    }

    private void SetAnimatorSpeed(float value)
    {
        if (!useSpeedParameter || animator == null || string.IsNullOrEmpty(speedParameterName))
            return;

        animator.SetFloat(speedParameterName, value);
    }

    private void LogStateIfChanged()
    {
        if (!logStateChanges)
            return;

        if (currentState == lastLoggedState)
            return;

        Debug.Log($"{name} | ZoneEnemyAI State: {lastLoggedState} -> {currentState}", this);
        lastLoggedState = currentState;
    }
}