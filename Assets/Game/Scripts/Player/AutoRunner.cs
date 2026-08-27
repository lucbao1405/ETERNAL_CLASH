using UnityEngine;

namespace EternalClash.Player
{
    /// <summary>
    /// Keeps player combat position. World movement is handled by WorldScroller.
    /// </summary>
    public class AutoRunner : MonoBehaviour
    {
        [SerializeField] private float runSpeed = 2.5f;
        private bool isRunning = true;

        public float RunSpeed => runSpeed;

        private void Update()
        {
            // Player stays in combat position.
            // The world moves instead.
            if (!isRunning)
                return;
        }

        public void StopRunning()
        {
            isRunning = false;
        }

        public void ResumeRunning()
        {
            isRunning = true;
        }

        public void SetSpeed(float speed)
        {
            runSpeed = speed;
        }
    }
}
