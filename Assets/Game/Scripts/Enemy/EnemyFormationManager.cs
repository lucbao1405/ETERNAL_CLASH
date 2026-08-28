using UnityEngine;

namespace EternalClash.Enemy
{
    public class EnemyFormationManager : MonoBehaviour
    {
        public static EnemyFormationManager Instance;

        private bool isLocked;

        private void Awake()
        {
            if (Instance == null)
                Instance = this;
            else
                Destroy(gameObject);
        }

        public void LockFormation()
        {
            isLocked = true;
        }

        public void UnlockFormation()
        {
            isLocked = false;
        }

        public bool IsLocked()
        {
            return isLocked;
        }
    }
}
