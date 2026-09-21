using System.Collections;
using UnityEngine;
using EternalClash.Animation;
using EternalClash.Combat;
using EternalClash.UI;
using EternalClash.World;

namespace EternalClash.Enemy
{
    /// <summary>
    /// Vong lap Boss (kieu Postknight): xuat hien -> danh 1 phat -> dung cho player danh
    /// -> bi danh thi chay lui ra khoi man hinh -> dung lai khieu khich -> lao vao danh
    /// tiep -> lap lai den khi chet.
    ///
    /// Boss tu dieu khien vi tri X cua minh (kem bu world scroll) thay vi di theo
    /// EnemyMover, vi no phai chay lui/ lao vao theo kich ban chu khong xep hang doi.
    /// Sat thuong van di qua EnemyAttack.AttackPlayer() de dung chung dmg/knockback/
    /// projectile nhu quai thuong.
    /// </summary>
    public class BossController : MonoBehaviour
    {
        private enum State
        {
            Enter,   // dang xuat hien, di vao tam danh
            Stand,   // dung yen cho player danh
            Retreat, // chay lui ra khoi man hinh
            Taunt,   // dung ngoai man hinh khieu khich
            Charge,  // lao vao tam danh
            Attack   // dang danh
        }

        [Header("Toc do (don vi/giay)")]
        [Tooltip("Toc do tu buoc vao man hinh luc xuat hien (world scroll cong them).")]
        [SerializeField] private float approachSpeed = 1.2f;

        [Tooltip("Toc do NET khi chay lui. Da tu bu lai world scroll nen ke ca khi player " +
                 "charge (scroll x4) boss van thoat ra duoc.")]
        [SerializeField] private float retreatSpeed = 6f;

        [Tooltip("Toc do tu lao vao sau khi khieu khich.")]
        [SerializeField] private float chargeSpeed = 4f;

        [Header("Khung man hinh")]
        [Tooltip("Qua day (viewport 0..1) tinh la da ra khoi man hinh.")]
        [SerializeField] private float exitViewportMargin = 0.15f;

        private State state = State.Enter;
        private bool pendingRetreat; // player vua danh trung: het state hien tai thi lui
        private bool damagedOnce;
        private bool revealed;       // thanh mau da hien len
        private bool dead;
        private int lastHealth = -1;

        private EnemyController controller;
        private EnemyHealthSystem health;
        private EnemyAttack attack;
        private Rigidbody2D rb;
        private BossAnimationController anim;
        private Transform player;
        private Camera cam;
        private WorldScroller scroller;

        private void Awake()
        {
            controller = GetComponent<EnemyController>();
            health = GetComponent<EnemyHealthSystem>();
            attack = GetComponent<EnemyAttack>();
            rb = GetComponent<Rigidbody2D>();
            anim = GetComponentInParent<BossAnimationController>();
            if (anim == null)
                anim = GetComponentInChildren<BossAnimationController>();

            // Boss tu chay theo kich ban: tat EnemyMover (no ap world scroll + xep hang
            // hang doi quai thuong). Awake cua EnemyMover van chay va giu boss dung lane Y.
            if (controller != null)
                controller.StopMove();

            // BossController owns the boss's X movement. Leaving EnemyMover
            // enabled lets its queue clamp compete with the boss retreat/charge
            // loop and can leave the boss parked out of attack range.
            EnemyMover mover = GetComponent<EnemyMover>();
            if (mover != null)
                mover.enabled = false;

            // EnemyAttack co Update tu ban theo tam rieng; o day chi goi AttackPlayer()
            // theo kich ban nen tat Update di (van goi duoc ham public khi disabled).
            if (attack != null)
                attack.enabled = false;
        }

        private void OnEnable()
        {
            if (health != null)
            {
                health.OnHealthChanged += OnHealthChanged;
                health.OnDeath += OnBossDeath;
            }
        }

        private void OnDisable()
        {
            if (health != null)
            {
                health.OnHealthChanged -= OnHealthChanged;
                health.OnDeath -= OnBossDeath;
            }
        }

        private void Start()
        {
            StartCoroutine(RunLoop());
        }

        private void Update()
        {
            if (dead || revealed || health == null)
                return;

            // Thanh mau boss len khi boss chinh thuc tran dau: lo mat dat hoac da an don dau tien.
            if (IsOnScreen() || damagedOnce)
            {
                revealed = true;
                BossHealthBarUI.ShowFor(health);
            }
        }

        // ----- Vong lap chinh ------------------------------------------------

        private IEnumerator RunLoop()
        {
            yield return EnterRoutine();

            while (!dead)
            {
                if (!pendingRetreat)
                    yield return AttackRoutine(); // vao tam roi: danh 1 phat

                if (pendingRetreat)
                {
                    yield return RetreatRoutine(); // bi player danh -> chay lui
                    if (dead) yield break;

                    if (anim != null)
                        anim.SetFacingAway(false); // quay lai ve phia player de khieu khich
                    yield return TauntRoutine();   // ra khoi man hinh -> dung lai khieu khich
                    if (dead) yield break;

                    yield return ChargeRoutine();  // roi lao vao danh tiep
                }
                else
                {
                    yield return StandRoutine();   // dung cho player danh
                    if (dead) yield break;
                }
            }
        }

        private IEnumerator EnterRoutine()
        {
            state = State.Enter;
            while (!dead && !pendingRetreat && !IsInAttackRange())
                yield return null;
        }

        private IEnumerator AttackRoutine()
        {
            state = State.Attack;

            if (attack != null)
                attack.AttackPlayer(); // NotifyAttack -> Spine "shot"; dmg den giua clip

            float duration = anim != null ? anim.GetAttackDuration() : 0f;
            yield return new WaitForSeconds(duration > 0f ? duration + 0.1f : 0.8f);
        }

        private IEnumerator StandRoutine()
        {
            state = State.Stand;
            while (!dead && !pendingRetreat)
                yield return null;
        }

        private IEnumerator RetreatRoutine()
        {
            pendingRetreat = false;
            state = State.Retreat;
            if (anim != null)
                anim.SetFacingAway(true); // chay lui thi quay mat di, clip run moi tu nhien
            while (!dead && !IsOffScreen())
                yield return null;
        }

        private IEnumerator TauntRoutine()
        {
            state = State.Taunt;
            float duration = anim != null ? anim.PlayTaunt() : 0f;
            yield return new WaitForSeconds(duration > 0f ? duration : 1.2f);
        }

        private IEnumerator ChargeRoutine()
        {
            state = State.Charge;
            while (!dead && !IsInAttackRange())
                yield return null;
        }

        // ----- Di chuyen -----------------------------------------------------

        private void FixedUpdate()
        {
            if (dead || rb == null)
                return;

            float scroll = GetScrollVelocity();
            float nextX = rb.position.x + (scroll + OwnVelocityX(scroll)) * Time.fixedDeltaTime;
            nextX = ClampBeforePlayer(nextX);

            rb.MovePosition(new Vector2(nextX, rb.position.y));
        }

        /// <summary>Toc do rieng cua boss, da tinh ca huong va bu world scroll.</summary>
        private float OwnVelocityX(float scroll)
        {
            float side = SideToPlayer(); // 1 = boss ben phai player
            switch (state)
            {
                case State.Enter:
                    // World scroll mang vao san, van tu buoc toi de khong bi tre khi
                    // scroll dung (Shield, StageComplete...).
                    return -approachSpeed * side;

                case State.Retreat:
                    // Chay lui voi toc do NET: bu lai ca scroll. khong thi luc player
                    // charge (scroll x4) boss bi keo tro lai thay vi chay ra khoi man hinh.
                    return retreatSpeed * side - scroll;

                case State.Charge:
                    return -chargeSpeed * side;

                default:
                    // Dung cho / danh / khieu khich: giu nguyen tren man hinh.
                    return -scroll;
            }
        }

        /// <summary>Khong bao gio lao xuyen qua player: giu cach toi thieu nua tam danh.</summary>
        private float ClampBeforePlayer(float nextX)
        {
            Transform target = GetPlayer();
            if (target == null)
                return nextX;

            float side = SideToPlayer();
            float minSeparation = attack != null ? attack.attackRange * 0.5f : 0.4f;
            float limitX = target.position.x + side * minSeparation;
            return side > 0f ? Mathf.Max(nextX, limitX) : Mathf.Min(nextX, limitX);
        }

        // ----- Phan hoi mau ---------------------------------------------------

        private void OnHealthChanged(int current, int max)
        {
            bool damaged = lastHealth >= 0 && current < lastHealth;
            if (damaged)
                damagedOnce = true;

            // Bi player danh: chi lui khi boss dang tan cong vao (Enter) hoac dang dung
            // cho (Stand). Khi dang lui/khieu khich/lao vao thi boss cuong chi - giu
            // dung kich ban nhu Postknight.
            if (damaged && !dead && (state == State.Enter || state == State.Stand))
                pendingRetreat = true;

            lastHealth = current;
        }

        private void OnBossDeath()
        {
            dead = true;
            BossHealthBarUI.Hide();
        }

        // ----- Tien ich --------------------------------------------------------

        private Transform GetPlayer()
        {
            if (player == null)
            {
                GameObject obj = GameObject.FindGameObjectWithTag("Player");
                player = obj != null ? obj.transform : null;
            }

            return player;
        }

        private float SideToPlayer()
        {
            Transform target = GetPlayer();
            if (target == null)
                return 1f;

            float side = Mathf.Sign(transform.position.x - target.position.x);
            return Mathf.Abs(side) < 0.01f ? 1f : side;
        }

        private float GetScrollVelocity()
        {
            if (scroller == null)
                scroller = FindObjectOfType<WorldScroller>();

            return scroller != null ? scroller.GetWorldVelocity().x : 0f;
        }

        private bool IsInAttackRange()
        {
            Transform target = GetPlayer();
            if (target == null)
                return false;

            float range = attack != null ? attack.attackRange : 1.2f;
            return Mathf.Abs(transform.position.x - target.position.x) <= range;
        }

        private bool IsOffScreen()
        {
            if (cam == null)
                cam = Camera.main;
            if (cam == null)
                return false;

            float vx = cam.WorldToViewportPoint(transform.position).x;
            return vx > 1f + exitViewportMargin || vx < -exitViewportMargin;
        }

        private bool IsOnScreen()
        {
            if (cam == null)
                cam = Camera.main;
            if (cam == null)
                return false;

            float vx = cam.WorldToViewportPoint(transform.position).x;
            return vx >= 0f && vx <= 1f;
        }
    }
}
