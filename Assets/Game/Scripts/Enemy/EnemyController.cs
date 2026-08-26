using UnityEngine;

namespace EternalClash.Enemy
{
    public class EnemyController : EnemyBase
    {
        public bool moveTowardsPlayer = false;

        private Transform player;

        protected override void Awake()
        {
            base.Awake();
            player = GameObject.FindGameObjectWithTag("Player")?.transform;
        }

        private void Update()
        {
            if (moveTowardsPlayer && player != null)
            {
                transform.position = Vector3.MoveTowards(
                    transform.position,
                    player.position,
                    moveSpeed * Time.deltaTime
                );
            }
        }
    }
}
