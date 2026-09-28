using UnityEngine;

namespace EternalClash.Enemy
{
    public class EnemyController : MonoBehaviour
    {
        public bool canMove = true;
        public bool isAttacking = false;

        public void StopMove()
        {
            canMove = false;
        }

        public void ResumeMove()
        {
            canMove = true;
        }
    }
}
