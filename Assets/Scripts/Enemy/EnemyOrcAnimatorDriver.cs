using UnityEngine;

public class EnemyOrcAnimatorDriver : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Animator animator;

    [Header("Parameters")]
    [SerializeField] private string speedParameter = "Speed";
    [SerializeField] private string attackTrigger = "Attack1";

    [Header("Settings")]
    [SerializeField] private float animatorDampTime = 0.08f;

    [Header("Debug")]
    [SerializeField] private bool enableDebugControls = true;
    [SerializeField] private KeyCode idleKey = KeyCode.Alpha3;
    [SerializeField] private KeyCode walkKey = KeyCode.Alpha1;
    [SerializeField] private KeyCode runKey = KeyCode.Alpha2;
    [SerializeField] private KeyCode attackKey = KeyCode.K;

    [Header("Runtime")]
    [SerializeField] private float currentSpeed = 0f;

    public float CurrentSpeed => currentSpeed;

    private void Awake()
    {
        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        if (animator != null)
            animator.applyRootMotion = false;
    }

    private void Update()
    {
        if (enableDebugControls)
        {
            HandleDebugInput();
        }

        UpdateAnimator();
    }

    private void HandleDebugInput()
    {
        if (Input.GetKeyDown(idleKey))
            SetSpeed(0f);

        if (Input.GetKeyDown(walkKey))
            SetSpeed(0.5f);

        if (Input.GetKeyDown(runKey))
            SetSpeed(1f);

        if (Input.GetKeyDown(attackKey))
            TriggerAttack();
    }

    private void UpdateAnimator()
    {
        if (animator == null) return;

        animator.SetFloat(speedParameter, currentSpeed, animatorDampTime, Time.deltaTime);
    }

    public void SetSpeed(float newSpeed)
    {
        currentSpeed = Mathf.Clamp01(newSpeed);
    }

    public void TriggerAttack()
    {
        if (animator == null) return;

        animator.ResetTrigger(attackTrigger);
        animator.SetTrigger(attackTrigger);
    }
}
