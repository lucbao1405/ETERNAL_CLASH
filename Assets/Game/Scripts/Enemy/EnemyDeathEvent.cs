using System;
using UnityEngine;

namespace EternalClash.Enemy
{
    public static class EnemyDeathEvent
    {
        public static event Action<GameObject> OnEnemyKilled;

        public static void Raise(GameObject enemy)
        {
            OnEnemyKilled?.Invoke(enemy);
        }
    }
}
