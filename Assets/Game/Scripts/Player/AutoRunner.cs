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
        [SerializeField] private float maxBackOffset = 1.2f;
        [SerializeField] private float maxForwardOffset = 0.8f;
        [SerializeField] private float maxVerticalOffset = 0.6f;

        private Vector3 homePosition;
        private bool homePositionSet;

        public float RunSpeed => runSpeed;

        private void Update()
        {
            // Player stays in combat position.
            // The world moves instead.
            if (!isRunning)
                return;
        }

        // Lock the player inside a small box around its spawn point. The world
        // scrolls, so the player's transform is effectively its screen position;
        // knockback must never push it off-screen.
        private void LateUpdate()
        {
            if (!homePositionSet)
            {
                homePosition = transform.position;
                homePositionSet = true;
                return;
            }

            float x = Mathf.Clamp(transform.position.x,
                homePosition.x - maxBackOffset, homePosition.x + maxForwardOffset);
            float y = Mathf.Clamp(transform.position.y,
                homePosition.y - maxVerticalOffset, homePosition.y + maxVerticalOffset);

            transform.position = new Vector3(x, y, transform.position.z);
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
