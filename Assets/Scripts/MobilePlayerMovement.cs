using UnityEngine;

public class MobilePlayerMovement : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Animator animator;
    [SerializeField] private CharacterController characterController;
    [SerializeField] private Transform cameraTransform;

    [Header("Movement Settings")]
    [SerializeField] private float walkSpeed = 2.2f;
    [SerializeField] private float runSpeed = 4.5f;
    [SerializeField] private float rotationSpeed = 12f;
    [SerializeField] private float moveDeadZone = 0.1f;

    [Header("Gravity Settings")]
    [SerializeField] private float gravity = -25f;
    [SerializeField] private float groundedForce = -2f;

    [Header("Animation Settings")]
    [SerializeField] private float animatorDampTime = 0.08f;
    [SerializeField] private string speedParameterName = "Speed";
    [SerializeField] private string attackTriggerName = "Attack1";

    [Header("Attack Settings")]
    [SerializeField] private float attackCooldown = 0.35f;

    private float lastAttackTime = -999f;
    private float verticalVelocity;

    private void Awake()
    {
        if (characterController == null)
            characterController = GetComponent<CharacterController>();

        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        if (cameraTransform == null && Camera.main != null)
            cameraTransform = Camera.main.transform;

        if (animator != null)
            animator.applyRootMotion = false;
    }

    private void Update()
    {
        HandleAttackInput();

        Vector2 joystickInput = MobileInputState.MoveInput;
        float inputMagnitude = Mathf.Clamp01(joystickInput.magnitude);

        if (inputMagnitude < moveDeadZone)
            inputMagnitude = 0f;

        Vector3 moveDirection = GetCameraRelativeDirection(joystickInput);
        Vector3 horizontalMove = Vector3.zero;

        if (inputMagnitude > 0f)
        {
            float moveSpeed = CalculateMoveSpeed(inputMagnitude);
            horizontalMove = moveDirection * moveSpeed;

            Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetRotation,
                rotationSpeed * Time.deltaTime
            );
        }

        ApplyGravity();

        Vector3 finalMove = horizontalMove;
        finalMove.y = verticalVelocity;

        if (characterController != null)
            characterController.Move(finalMove * Time.deltaTime);
        else
            transform.position += finalMove * Time.deltaTime;

        UpdateAnimator(inputMagnitude);
    }

    private void ApplyGravity()
    {
        if (characterController == null)
            return;

        if (characterController.isGrounded && verticalVelocity < 0f)
        {
            verticalVelocity = groundedForce;
        }

        verticalVelocity += gravity * Time.deltaTime;
    }

    private void HandleAttackInput()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            TryAttack();
        }
    }

    public void OnAttackButtonPressed()
    {
        TryAttack();
    }

    private void TryAttack()
    {
        if (animator == null) return;

        if (Time.time < lastAttackTime + attackCooldown)
            return;

        animator.ResetTrigger(attackTriggerName);
        animator.SetTrigger(attackTriggerName);

        lastAttackTime = Time.time;
    }

    private float CalculateMoveSpeed(float inputMagnitude)
    {
        if (inputMagnitude <= 0.5f)
        {
            float t = inputMagnitude / 0.5f;
            return Mathf.Lerp(0f, walkSpeed, t);
        }
        else
        {
            float t = (inputMagnitude - 0.5f) / 0.5f;
            return Mathf.Lerp(walkSpeed, runSpeed, t);
        }
    }

    private Vector3 GetCameraRelativeDirection(Vector2 input)
    {
        Vector3 inputDirection = new Vector3(input.x, 0f, input.y);

        if (inputDirection.sqrMagnitude <= 0.0001f)
            return Vector3.zero;

        if (cameraTransform == null)
            return inputDirection.normalized;

        Vector3 camForward = cameraTransform.forward;
        Vector3 camRight = cameraTransform.right;

        camForward.y = 0f;
        camRight.y = 0f;

        camForward.Normalize();
        camRight.Normalize();

        Vector3 moveDirection = (camForward * input.y) + (camRight * input.x);
        return moveDirection.normalized;
    }

    private void UpdateAnimator(float inputMagnitude)
    {
        if (animator == null) return;

        animator.SetFloat(speedParameterName, inputMagnitude, animatorDampTime, Time.deltaTime);
    }
}