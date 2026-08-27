using UnityEngine;

namespace EternalClash.World
{
    public class WorldScroller : MonoBehaviour
    {
        [SerializeField] private float scrollSpeed = 3f;
        private bool scrolling = true;

        private void Update()
        {
            if (!scrolling)
                return;

            transform.position += Vector3.left * scrollSpeed * Time.deltaTime;
        }

        public void StopScroll()
        {
            scrolling = false;
        }

        public void ResumeScroll()
        {
            scrolling = true;
        }
    }
}
