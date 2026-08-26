using UnityEngine;

namespace EternalClash.World
{
    /// <summary>
    /// Moves world objects to create the feeling that the player is traveling.
    /// Player stays in a fixed position like Postknight-style gameplay.
    /// </summary>
    public class WorldScroller : MonoBehaviour
    {
        [SerializeField] private float scrollSpeed = 2.5f;

        private bool isScrolling = true;

        private void Update()
        {
            if (!isScrolling)
                return;

            transform.position += Vector3.left * scrollSpeed * Time.deltaTime;
        }

        public void StopScroll()
        {
            isScrolling = false;
        }

        public void ResumeScroll()
        {
            isScrolling = true;
        }
    }
}
