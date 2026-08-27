using UnityEngine;
using EternalClash.Character;

namespace EternalClash.Combat
{
    [RequireComponent(typeof(Rigidbody2D))]
    public class KnockbackReceiver : MonoBehaviour
    {
        public float recoveryTime = 0.25f;

        private Rigidbody2D rb;
        private CharacterStateMachine stateMachine;

        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            stateMachine = GetComponent<CharacterStateMachine>();
        }

        public void ApplyKnockback(Vector2 direction, float force)
        {
            if (stateMachine != null)
                stateMachine.ChangeState(CharacterState.Hit);

            rb.velocity = Vector2.zero;
            rb.AddForce(direction.normalized * force, ForceMode2D.Impulse);

            Invoke(nameof(Recover), recoveryTime);
        }

        private void Recover()
        {
            if (stateMachine != null)
                stateMachine.ChangeState(CharacterState.Idle);
        }
    }
}
