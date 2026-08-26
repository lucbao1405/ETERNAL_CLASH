using UnityEngine;

namespace EternalClash.Enemy
{
    public class EnemyAI : MonoBehaviour
    {
        public enum EnemyType
        {
            Melee,
            Ranged,
            Charger
        }

        public EnemyType type;
        public float attackInterval = 2f;

        private float timer;

        private void Update()
        {
            timer += Time.deltaTime;

            if (timer >= attackInterval)
            {
                timer = 0;
                PerformAttack();
            }
        }

        private void PerformAttack()
        {
            switch (type)
            {
                case EnemyType.Melee:
                    Debug.Log("Enemy melee attack");
                    break;

                case EnemyType.Ranged:
                    Debug.Log("Enemy shoot projectile");
                    break;

                case EnemyType.Charger:
                    Debug.Log("Enemy charge attack");
                    break;
            }
        }
    }
}
