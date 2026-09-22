using System.Collections.Generic;
using UnityEngine;

namespace EternalClash.Enemy
{
    public class EnemyMover : MonoBehaviour
    {
        /// <summary>
        /// Tat ca quai dang song tren map, dung de xep hang. Quai deu spawn cung 1 diem
        /// va la Kinematic + trigger nen vat ly khong tach chung ra; khong xep hang thi
        /// ca dot quai dung chong khit len nhau o cung mot cho truoc mat Player.
        /// </summary>
        private static readonly List<EnemyMover> activeMovers = new List<EnemyMover>();
        private static EternalClash.World.WorldScroller cachedScroller;

        [SerializeField] private float archerStopDistance = 4.5f;
        [SerializeField] private bool isArcher = false;
        [SerializeField] private float playerStopDistance = 0.75f;
        [Tooltip("Khi Player charge: quai can chien duoc trôi sát hơn bình thường (để liên tục lọt cửa sổ charge hitbox) nhưng vẫn bị chặn trước mặt Player, không được xuyên qua.")]
        [SerializeField] private float chargeFlowStopDistance = 0.2f;

        [Header("Queue")]
        [Tooltip("Khoang cach giua 2 quai dung lien nhau trong hang.")]
        [SerializeField] private float queueSpacing = 0.8f;
        [Tooltip("Toc do lui ve cho cua minh khi dang dung lan vao cho cua con phia truoc.")]
        [SerializeField] private float queueSettleSpeed = 3f;

        [Header("Death")]
        [Tooltip("Toc do văng ra sau (xa Player) lúc xác mới chết.")]
        [SerializeField] private float deathKnockbackSpeed = 9f;
        [Tooltip("Gia tốc hãm cú văng; hết phần này thì xác chỉ còn trượt theo world scroll.")]
        [SerializeField] private float deathKnockbackDecel = 20f;
        [Tooltip("Độ cao vòng cung nhỏ khi văng. 0 = trượt sát đất.")]
        [SerializeField] private float deathHopHeight = 0.6f;

        private EnemyController controller;
        private EnemyHealthSystem health;
        private Rigidbody2D rb;
        private Transform player;
        private EternalClash.Player.PlayerChargeController chargeController;
        private bool isPaused;
        private bool isDrifting;
        private float knockbackVelocity;
        private float knockbackTravel;
        private float knockbackTotal;
        private float lockedY;

        private void OnEnable()
        {
            if (!activeMovers.Contains(this))
                activeMovers.Add(this);
        }

        private void OnDisable()
        {
            activeMovers.Remove(this);
        }

        private void Awake()
        {
            controller = GetComponent<EnemyController>();
            health = GetComponent<EnemyHealthSystem>();
            rb = GetComponent<Rigidbody2D>();
            GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
            player = playerObject != null ? playerObject.transform : null;
            // Player defines the shared combat lane. This also corrects enemies
            // that were already present in the scene before a runtime spawn.
            lockedY = CombatLaneY.GetPlayerY(transform.position.y);
            if (rb != null)
            {
                rb.gravityScale = 0f;
                rb.constraints |= RigidbodyConstraints2D.FreezePositionY | RigidbodyConstraints2D.FreezeRotation;
                rb.position = new Vector2(rb.position.x, lockedY);
            }

            // Dat pivot ngang Player chua du (moi prefab pivot khac nhau): sau khi hinh
            // anh duoc ve, CombatLaneAligner dich quai de CHAN cham duong mat dat.
            if (GetComponent<CombatLaneAligner>() == null)
                gameObject.AddComponent<CombatLaneAligner>();
        }

        /// <summary>Doi do cao duong di cua quai (goi boi CombatLaneAligner).</summary>
        public void SetLaneY(float y)
        {
            lockedY = y;
            if (rb != null)
                rb.position = new Vector2(rb.position.x, y);
            transform.position = new Vector3(transform.position.x, y, transform.position.z);
        }

        private void FixedUpdate()
        {
            // Xac quai: chi trôi theo world scroll, khong xep hang/clamp quanh
            // Player nen no troi xuyen qua Player cho toi khi bi destroy.
            if (isDrifting)
            {
                DriftWithWorld();
                return;
            }

            if (isPaused) return;
            if (EnemyFormationManager.Instance != null && EnemyFormationManager.Instance.IsLocked()) return;
            if (controller != null && !controller.canMove) return;

            // Tim 1 lan roi dung chung cho moi quai. FindObjectOfType duyet toan scene,
            // goi moi FixedUpdate cho tung quai rat ton CPU tren dien thoai.
            // Doi scene thi WorldScroller cu bi huy -> "== null" -> tim lai.
            if (cachedScroller == null)
                cachedScroller = FindObjectOfType<EternalClash.World.WorldScroller>();
            var scroller = cachedScroller;
            float worldVelocityX = scroller != null ? scroller.GetWorldVelocity().x : 0f;
            float finalVelocity = worldVelocityX;

            float nextX = rb != null ? rb.position.x : transform.position.x;
            nextX += finalVelocity * Time.fixedDeltaTime;
            nextX = ClampBeforePlayer(nextX);

            if (rb != null)
            {
                Vector2 current = rb.position;
                if (!Mathf.Approximately(current.y, lockedY))
                    rb.position = new Vector2(current.x, lockedY);
                rb.MovePosition(new Vector2(nextX, lockedY));
            }
            else
            {
                transform.position = new Vector3(nextX, lockedY, transform.position.z);
            }
        }

        private float ClampBeforePlayer(float nextX)
        {
            if (player == null)
            {
                GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
                player = playerObject != null ? playerObject.transform : null;
            }

            if (player == null)
                return nextX;

            // Trong lúc Player charge: quai can chien van duoc trôi vào sát hơn bình
            // thường (để liên tục lọt cửa sổ charge hitbox quét phía trước thay vì
            // dừng ở con đầu tiên), nhưng vẫn bị chặn trước mặt Player: quai chưa
            // chết không được trôi xuyên qua Player rồi đánh từ phía sau. Quai tầm
            // xa (Goblin Mage/Archer) giữ nguyên khoảng cách dừng: nếu không, con
            // ngoài màn hình bị world scroll (x4 khi charge) hút thẳng vào Player.
            bool chargingFlow = false;
            if (chargeController == null)
                chargeController = player.GetComponent<EternalClash.Player.PlayerChargeController>();
            if (chargeController != null && chargeController.IsCharging && !isArcher)
                chargingFlow = true;

            float currentX = rb != null ? rb.position.x : transform.position.x;
            float side = Mathf.Sign(currentX - player.position.x);
            if (Mathf.Abs(side) < 0.01f)
                side = Mathf.Sign(nextX - player.position.x);
            if (Mathf.Abs(side) < 0.01f)
                side = -1f;

            // Xep hang: con dung dau dung sat Player, moi con sau lui them queueSpacing.
            // Quai xa (Goblin Mage/Archer) dung lai o archerStopDistance de ban tu xa
            // thay vi di san sat Player nhu quai can chien.
            int queueIndex = GetQueueIndex(currentX, side);
            float stopDistance = isArcher
                ? archerStopDistance
                : chargingFlow ? chargeFlowStopDistance : playerStopDistance;
            float stopX = player.position.x + side * (stopDistance + queueIndex * queueSpacing);

            // Con dau hang giu nguyen cach cu: khong bao gio duoc vuot qua cho dung.
            // Con phia sau neu dang lan vao cho con truoc (vd sau khi bi day lui, hang
            // bi xao tron) thi lui ve tu tu thay vi bi dich chuyen tuc thoi.
            bool insideSlot = side > 0f ? currentX < stopX : currentX > stopX;
            if (queueIndex > 0 && insideSlot)
                return Mathf.MoveTowards(currentX, stopX, queueSettleSpeed * Time.fixedDeltaTime);

            return side > 0f ? Mathf.Max(nextX, stopX) : Mathf.Min(nextX, stopX);
        }

        /// <summary>
        /// So quai con song dung truoc minh (gan Player hon) o cung phia.
        /// Hai con dung trung vi tri (vua spawn cung luc) thi xet theo InstanceID
        /// de thu tu luon co dinh, khong doi qua doi lai giua cac frame.
        /// </summary>
        private int GetQueueIndex(float currentX, float side)
        {
            float myDistance = Mathf.Abs(currentX - player.position.x);
            int myId = GetInstanceID();
            int index = 0;

            foreach (EnemyMover other in activeMovers)
            {
                if (other == null || other == this || other.IsDead)
                    continue;

                // Xep hang rieng theo loai: archer chi tinh cac archer dung truoc,
                // neu khong no bi day lui sau moi quai can chien va mat tam ban.
                if (other.isArcher != isArcher)
                    continue;

                float otherX = other.rb != null ? other.rb.position.x : other.transform.position.x;
                if (Mathf.Sign(otherX - player.position.x) != side)
                    continue;

                float otherDistance = Mathf.Abs(otherX - player.position.x);
                bool ahead = Mathf.Abs(otherDistance - myDistance) < 0.001f
                    ? other.GetInstanceID() < myId
                    : otherDistance < myDistance;

                if (ahead)
                    index++;
            }

            return index;
        }

        private bool IsDead => health != null && health.IsDead;

        /// <summary>
        /// Day quai ra xa vi tri origin (dung khi player hoi sinh): con nao dang
        /// dung gan hon minDistance duoc dich chuyen tuc thoi ra vung bien cung
        /// phia. Quai la Kinematic di theo world scroll nen chi can dat lai vi
        /// tri, hang tu xep lai dan nhu sau khi bi day lui thong thuong.
        /// </summary>
        public void PushAwayFrom(Vector2 origin, float minDistance)
        {
            if (IsDead)
                return;

            float currentX = rb != null ? rb.position.x : transform.position.x;
            if (Mathf.Abs(currentX - origin.x) >= minDistance)
                return;

            float side = Mathf.Sign(currentX - origin.x);
            if (Mathf.Abs(side) < 0.01f)
                side = 1f;
            float targetX = origin.x + side * minDistance;

            if (rb != null)
            {
                rb.position = new Vector2(targetX, lockedY);
                // Ghi de lenh MovePosition dang treo tu FixedUpdate truoc (target
                // la vi tri sat Player tinh truoc khi tran dong bang), nguoi thi
                // quai bi keo ve vi tri cu ngay buoc vat ly ke tiep.
                rb.MovePosition(new Vector2(targetX, lockedY));
            }
            transform.position = new Vector3(targetX, lockedY, transform.position.z);
        }

        public void StopMovement() => isPaused = true;
        public void ResumeMovement() => isPaused = false;
        public void PauseMovement(float duration){isPaused=true;Invoke(nameof(ResumeMovement),duration);}

        /// <summary>
        /// Chuyen sang che do xac: boc văng ra sau (xa Player) với tốc độ
        /// deathKnockbackSpeed, hãm dần theo deathKnockbackDecel; khi hết cú
        /// văng thì chỉ còn trượt theo world scroll cho tới khi bị destroy.
        /// rb da bi tat simulated khi chet nen dich thang transform.
        /// </summary>
        public void StartDrift()
        {
            isDrifting = true;
            float side = Mathf.Sign(GetDriftSideX() - GetPlayerX());
            if (Mathf.Abs(side) < 0.01f)
                side = 1f;
            knockbackVelocity = side * deathKnockbackSpeed;
            knockbackTravel = 0f;
            knockbackTotal = Mathf.Abs(knockbackVelocity) > 0.001f
                ? knockbackVelocity * knockbackVelocity / (2f * deathKnockbackDecel)
                : 0f;
        }

        private void DriftWithWorld()
        {
            if (cachedScroller == null)
                cachedScroller = FindObjectOfType<EternalClash.World.WorldScroller>();
            float worldVelocityX = cachedScroller != null ? cachedScroller.GetWorldVelocity().x : 0f;

            Vector3 position = transform.position;
            position.x += (worldVelocityX + knockbackVelocity) * Time.fixedDeltaTime;

            if (Mathf.Abs(knockbackVelocity) > 0.001f)
            {
                knockbackTravel += Mathf.Abs(knockbackVelocity) * Time.fixedDeltaTime;
                if (deathHopHeight > 0f && knockbackTotal > 0f)
                {
                    // Vòng cung nhảy nhỏ: 0 ở đầu/cuối cú văng, cao nhất ở giữa.
                    float t = Mathf.Clamp01(knockbackTravel / knockbackTotal);
                    position.y = lockedY + deathHopHeight * 4f * t * (1f - t);
                }
                knockbackVelocity = Mathf.MoveTowards(
                    knockbackVelocity, 0f, deathKnockbackDecel * Time.fixedDeltaTime);
            }
            else
            {
                position.y = lockedY;
                knockbackVelocity = 0f;
            }

            transform.position = position;
        }

        private float GetPlayerX()
        {
            if (player == null)
            {
                GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
                player = playerObject != null ? playerObject.transform : null;
            }
            return player != null ? player.position.x : transform.position.x;
        }

        private float GetDriftSideX() => rb != null ? rb.position.x : transform.position.x;
        public bool IsArcher=>isArcher;
        public float ArcherStopDistance=>archerStopDistance;
    }
}
