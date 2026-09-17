using UnityEngine;

namespace EternalClash.Character
{
    public class CharacterStateMachine : MonoBehaviour
    {
        public CharacterState CurrentState { get; private set; }

        private Animator animator;

        private void Awake()
        {
            animator = GetComponent<Animator>();
        }

        private void Start()
        {
            ChangeState(CharacterState.Idle);
        }

        public void ChangeState(CharacterState state)
        {
            CurrentState = state;

            if (animator != null)
            {
                animator.SetInteger("State", (int)state);
            }
        }
    }
}
