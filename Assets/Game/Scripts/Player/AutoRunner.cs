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

        [Header("Screen Lock (prevent knockback from leaving the screen)")]
        [SerializeField] private float maxVerticalOffset = 0.6f;

        private Vector3 homePosition;
        private bool homePositionSet;

        public float RunSpeed => runSpeed;

        private void Update()
        {
            if (!isRunning)
                return;
        }

        private void LateUpdate()
        {
            if (!homePositionSet)
            {
                homePosition = transform.position;
                homePositionSet = true;
                return;
            }

            float x = homePosition.x;
            float y = Mathf.Clamp(transform.position.y,
                homePosition.y - maxVerticalOffset, homePosition.y + maxVerticalOffset);

            transform.position = new Vector3(x, y, homePosition.z);
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

        public void StopChargeMovement()
        {
            // Reset charge speed overrides
        }
    }
}
