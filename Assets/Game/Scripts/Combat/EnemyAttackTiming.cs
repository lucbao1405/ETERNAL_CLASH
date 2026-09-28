using UnityEngine;

public class EnemyAttackTiming : MonoBehaviour
{
    public float attackDelay = 1.2f;
    public float cooldown = 2f;
    private float timer;

    public bool CanAttack()
    {
        return timer <= 0;
    }

    public void TriggerAttack()
    {
        timer = cooldown;
    }

    void Update()
    {
        if(timer > 0) timer -= Time.deltaTime;
    }
}
