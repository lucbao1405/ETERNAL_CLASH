using UnityEngine;

namespace EternalClash.Character
{
    public class CharacterAnimator : MonoBehaviour
    {
        private Animator animator;

        private void Awake()
        {
            animator = GetComponent<Animator>();
        }

        public void Play(CharacterState state)
        {
            if (animator != null)
            {
                animator.SetInteger("State", (int)state);
            }
        }
    }
}
