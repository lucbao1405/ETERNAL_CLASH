using UnityEngine;
using EternalClash.Character;

namespace EternalClash.Skill
{
    public class DashSkill : SkillBase
    {
        public float dashForce = 8f;
        private Rigidbody2D rb;
        private CharacterStateMachine stateMachine;

        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            stateMachine = GetComponent<CharacterStateMachine>();
        }

        protected override void Execute()
        {
            if (stateMachine != null)
                stateMachine.ChangeState(CharacterState.Skill);

            if (rb != null)
                rb.AddForce(Vector2.right * dashForce, ForceMode2D.Impulse);
        }
    }
}
