using EternalClash.Item;
using UnityEngine;

namespace EternalClash.Loot
{
    /// <summary>
    /// Chuyen dong cua vat pham roi tu quai, kieu Postknight:
    ///   1. Bung ra: bay vong cung tu than quai xuong dat, nay them vai cai nho,
    ///      theo huong nguoc chieu cuon map.
    ///   2. Dung yen: nam im tai cho trong restDuration giay.
    ///   3. Bay ve: bay vong cung ve giua nguoi Player, cang luc cang nhanh, nho dan,
    ///      toi noi thi nhat luon (khong phu thuoc va cham trigger).
    /// Khong dung vat ly vi collider cua item la trigger nen se roi xuyen dat.
    /// </summary>
    public class LootDropMotion : MonoBehaviour
    {
        private const float BOUNCE_DECAY = 0.3f;
        private const float END_SCALE = 0.45f;

        private enum Phase { Burst, Rest, Fly }

        private ItemPickup pickup;
        private Transform player;
        private Collider2D playerCollider;

        private Phase phase;
        private float elapsed;

        // Bung ra + dung yen
        private Vector3 spawnPosition;
        private float groundY;
        private float distanceX;
        private float hopDuration;
        private float restDuration;
        private float[] heights;
        private float[] hopEnds;

        // Bay ve
        private float flyDuration;
        private float flyArcHeight;
        private Vector3 flyStart;
        private Vector3 startScale;

        public void Play(
            float groundY,
            float distanceX,
            float burstHeight,
            int bounceCount,
            float hopDuration,
            float restDuration,
            float flyDuration,
            float flyArcHeight)
        {
            pickup = GetComponent<ItemPickup>();
            if (pickup != null)
                pickup.externallyDriven = true;

            spawnPosition = transform.position;
            startScale = transform.localScale;
            this.groundY = groundY;
            this.distanceX = distanceX;
            this.hopDuration = Mathf.Max(0.05f, hopDuration);
            this.restDuration = Mathf.Max(0f, restDuration);
            this.flyDuration = Mathf.Max(0.05f, flyDuration);
            this.flyArcHeight = flyArcHeight;

            BuildHops(Mathf.Max(0f, burstHeight), Mathf.Max(0, bounceCount));

            phase = Phase.Burst;
            elapsed = 0f;
        }

        /// <summary>
        /// Mat dat ma vat pham se dung (tam vat pham khi nam tren dat). Chi dung
        /// sau khi Play duoc goi; bong dem dung gia tri nay de neo xuong dat.
        /// </summary>
        public bool TryGetGroundY(out float y)
        {
            y = groundY;
            return heights != null;
        }

        /// <summary>
        /// Buoc 0 la vong bay tu than quai xuong dat, cac buoc sau la cu nay nho.
        /// Thoi gian moi buoc ti le voi can bac hai do cao (giong roi tu do).
        /// </summary>
        private void BuildHops(float burstHeight, int bounceCount)
        {
            int count = 1 + bounceCount;
            heights = new float[count];
            hopEnds = new float[count];
            float[] weights = new float[count];

            // Buoc dau con phai roi tu than quai xuong dat nen tinh ca doan do.
            float fall = Mathf.Max(0f, spawnPosition.y - groundY);
            float height = burstHeight;
            float totalWeight = 0f;
            for (int i = 0; i < count; i++)
            {
                heights[i] = height;
                weights[i] = Mathf.Sqrt(height + (i == 0 ? fall * 0.5f : 0f)) + 0.0001f;
                totalWeight += weights[i];
                height *= BOUNCE_DECAY;
            }

            float accumulated = 0f;
            for (int i = 0; i < count; i++)
            {
                accumulated += weights[i] / totalWeight;
                hopEnds[i] = accumulated;
            }
        }

        private void Update()
        {
            if (heights == null)
                return;

            elapsed += Time.deltaTime;

            switch (phase)
            {
                case Phase.Burst:
                case Phase.Rest:
                    UpdateOnGround();
                    break;

                case Phase.Fly:
                    UpdateFly();
                    break;
            }
        }

        private void UpdateOnGround()
        {
            // Khong troi theo mat dat: nay xong la dung yen tai cho cho toi luc bay ve.
            float t = Mathf.Clamp01(elapsed / hopDuration);
            float easedX = 1f - (1f - t) * (1f - t);

            transform.position = new Vector3(
                spawnPosition.x + distanceX * easedX,
                GetHopY(t),
                spawnPosition.z);

            if (t >= 1f)
                phase = Phase.Rest;

            if (elapsed >= hopDuration + restDuration && FindPlayer())
                BeginFly();
        }

        private float GetHopY(float t)
        {
            float hopStart = 0f;
            for (int i = 0; i < hopEnds.Length; i++)
            {
                if (t <= hopEnds[i])
                {
                    float local = Mathf.InverseLerp(hopStart, hopEnds[i], t);
                    float baseY = i == 0 ? Mathf.Lerp(spawnPosition.y, groundY, local) : groundY;
                    return baseY + heights[i] * 4f * local * (1f - local);
                }

                hopStart = hopEnds[i];
            }

            return groundY;
        }

        private void BeginFly()
        {
            phase = Phase.Fly;
            elapsed = 0f;
            flyStart = transform.position;
        }

        private void UpdateFly()
        {
            if (!FindPlayer())
                return;

            Vector3 target = GetPlayerCenter();
            target.z = flyStart.z;

            float t = Mathf.Clamp01(elapsed / flyDuration);
            // Tang toc dan: luc dau bay cham, cang gan Player cang nhanh (cam giac bi hut).
            float eased = t * t;

            // Duong cong Bezier bac 2, diem dieu khien nam phia tren doan thang
            // de item bay vong len roi lao vao nguoi Player.
            Vector3 control = (flyStart + target) * 0.5f + Vector3.up * flyArcHeight;
            float u = 1f - eased;
            transform.position = u * u * flyStart + 2f * u * eased * control + eased * eased * target;
            transform.localScale = startScale * Mathf.Lerp(1f, END_SCALE, eased);

            if (t >= 1f)
            {
                pickup?.CollectBy(player.gameObject);

                // Item khong co ItemPickup thi van phai bien mat.
                if (pickup == null)
                    Destroy(gameObject);
            }
        }

        private bool FindPlayer()
        {
            if (player != null)
                return true;

            GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
            if (playerObject == null)
                return false;

            player = playerObject.transform;
            playerCollider = playerObject.GetComponent<Collider2D>();
            return true;
        }

        private Vector3 GetPlayerCenter()
        {
            if (playerCollider != null && playerCollider.enabled)
                return playerCollider.bounds.center;

            return player.position;
        }
    }
}
