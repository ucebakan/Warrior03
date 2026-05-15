using UnityEngine;

public class PlayerComboAttack : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Animator animator;

    [Header("Animator")]
    [SerializeField] private string comboStepParameter = "ComboStep";
    [SerializeField] private string attack1StateName = "Attack1";
    [SerializeField] private string attack2StateName = "Attack2";
    [SerializeField] private string attack3StateName = "Attack3";
    [SerializeField] private int upperBodyLayerIndex = 1;

    [Header("Timing")]
    [SerializeField] private float comboResetDelay = 0.75f;
    [SerializeField] private float attackStateEndNormalizedTime = 0.95f;

    private int queuedStep = 0;
    private float lastInputTime = -999f;

    private void Reset()
    {
        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>();
        }
    }

    private void Awake()
    {
        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>();
        }

        ResetCombo();
    }

    private void Update()
    {
        if (animator == null) return;

        int activeAttackStep = GetAttackStepForUpdate(out float normalizedTime);
        bool isAttacking = activeAttackStep > 0;

        if (!isAttacking)
        {
            if (queuedStep > 0)
            {
                animator.SetInteger(comboStepParameter, queuedStep);

                if (Time.time - lastInputTime > comboResetDelay)
                {
                    ResetCombo();
                }
            }

            return;
        }

        if (activeAttackStep == 1 && normalizedTime >= attackStateEndNormalizedTime && queuedStep < 2)
        {
            ResetCombo();
            return;
        }

        if (activeAttackStep == 2 && normalizedTime >= attackStateEndNormalizedTime && queuedStep < 3)
        {
            ResetCombo();
            return;
        }

        if (activeAttackStep == 3 && normalizedTime >= attackStateEndNormalizedTime)
        {
            ResetCombo();
        }
    }

    public void OnAttackButtonPressed()
    {
        if (animator == null) return;

        lastInputTime = Time.time;

        int attackStep = GetAttackStepForInput();

        if (attackStep == 0)
        {
            QueueComboStep(1);
            return;
        }

        if (attackStep == 1)
        {
            QueueComboStep(2);
            return;
        }

        if (attackStep == 2)
        {
            QueueComboStep(3);
            return;
        }
    }

    private void QueueComboStep(int targetStep)
    {
        int maxStep = 3;
        int clampedStep = Mathf.Clamp(targetStep, 1, maxStep);

        queuedStep = Mathf.Max(queuedStep, clampedStep);
        animator.SetInteger(comboStepParameter, queuedStep);
    }

    private int GetAttackStepForInput()
    {
        AnimatorStateInfo currentState = animator.GetCurrentAnimatorStateInfo(upperBodyLayerIndex);
        int currentStep = GetStepFromState(currentState);

        if (animator.IsInTransition(upperBodyLayerIndex))
        {
            AnimatorStateInfo nextState = animator.GetNextAnimatorStateInfo(upperBodyLayerIndex);
            int nextStep = GetStepFromState(nextState);

            if (nextStep > 0)
            {
                return nextStep;
            }
        }

        return currentStep;
    }

    private int GetAttackStepForUpdate(out float normalizedTime)
    {
        AnimatorStateInfo currentState = animator.GetCurrentAnimatorStateInfo(upperBodyLayerIndex);
        int currentStep = GetStepFromState(currentState);
        normalizedTime = currentState.normalizedTime;

        if (animator.IsInTransition(upperBodyLayerIndex))
        {
            AnimatorStateInfo nextState = animator.GetNextAnimatorStateInfo(upperBodyLayerIndex);
            int nextStep = GetStepFromState(nextState);

            if (nextStep > 0)
            {
                normalizedTime = nextState.normalizedTime;
                return nextStep;
            }
        }

        return currentStep;
    }

    private int GetStepFromState(AnimatorStateInfo stateInfo)
    {
        if (stateInfo.IsName(attack1StateName)) return 1;
        if (stateInfo.IsName(attack2StateName)) return 2;
        if (stateInfo.IsName(attack3StateName)) return 3;

        return 0;
    }

    private void ResetCombo()
    {
        queuedStep = 0;

        if (animator != null)
        {
            animator.SetInteger(comboStepParameter, 0);
        }
    }
}