using UnityEngine;

public enum CombatState
{
    Run,
    Attack,
    Shield,
    Charge,
    Hurt,
    Dead
}

public class CombatStateMachine : MonoBehaviour
{
    public CombatState state = CombatState.Run;

    public void ChangeState(CombatState newState)
    {
        state = newState;
    }

    public bool CanMove()
    {
        return state == CombatState.Run;
    }
}
