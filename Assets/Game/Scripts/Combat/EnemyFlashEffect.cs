using System.Collections;
using UnityEngine;

namespace EternalClash.Combat
{
    public class EnemyFlashEffect : MonoBehaviour
    {
        public float duration = 0.08f;

        private SpriteRenderer sprite;
        private Color originalColor;
        private bool flashing;

        private void Awake()
        {
            sprite = GetComponentInChildren<SpriteRenderer>();
            if (sprite != null)
                originalColor = sprite.color;
        }

        public void Play()
        {
            if (!flashing)
                StartCoroutine(FlashRoutine());
        }

        private IEnumerator FlashRoutine()
        {
            flashing = true;

            if (sprite != null)
                sprite.color = Color.white;

            yield return new WaitForSeconds(duration);

            if (sprite != null)
                sprite.color = originalColor;

            flashing = false;
        }
    }
}
