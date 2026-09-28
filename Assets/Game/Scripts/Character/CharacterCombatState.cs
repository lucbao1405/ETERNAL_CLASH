using UnityEngine;

namespace EternalClash.Character
{
    public class CharacterCombatState : MonoBehaviour
    {
        private CharacterStateMachine stateMachine;

        private void Awake()
        {
            stateMachine = GetComponent<CharacterStateMachine>();
        }

        public void SetAttack()
        {
            if(stateMachine != null)
                stateMachine.ChangeState(CharacterState.Attack);
        }

        public void SetSkill()
        {
            if(stateMachine != null)
                stateMachine.ChangeState(CharacterState.Skill);
        }

        public void SetHit()
        {
            if(stateMachine != null)
                stateMachine.ChangeState(CharacterState.Hit);
        }

        public void SetDead()
        {
            if(stateMachine != null)
                stateMachine.ChangeState(CharacterState.Dead);
        }
    }
}
