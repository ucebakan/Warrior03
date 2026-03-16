using UnityEngine;

public class PlayerMobileAttack : MonoBehaviour
{
    [SerializeField] private Animator animator;
    [SerializeField] private string attackTriggerName = "Attack1";

    public void TriggerAttack()
    {
        if (animator == null) return;

        animator.SetTrigger(attackTriggerName);
    }
}