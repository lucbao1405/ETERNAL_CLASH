using System.Collections;
using UnityEngine;
using EternalClash.Animation;
using EternalClash.Combat;
using EternalClash.UI;
using EternalClash.World;

namespace EternalClash.Enemy
{
    /// <summary>
    /// Vong lap Boss (ranged): xuat hien -> dung o ria man hinh -> ban lien tuc ve phia
    /// player (vua ban vua troi lai gan; player vua bi knockback thi tam dung nhip troi
    /// de player co cho trut vien) -> an dmg tu player thi chay lui ra ria man hinh ->
    /// khieu khich -> ban tiep -> lap lai den khi chet.
    ///
    /// Boss tu dieu khien vi tri X cua minh (kem bu world scroll) thay vi di theo
    /// EnemyMover. Sat thuong van di qua EnemyAttack.AttackPlayer() de dung chung
    /// dmg/knockback/projectile nhu quai thuong.
    /// </summary>
    public class BossController : MonoBehaviour
    {
        internal enum State
        {
            Enter,   // dang xuat hien, di vao den ria man hinh
            Shoot,   // ban lien tuc + troi lai gan player
            Retreat, // an dmg tu player -> chay lui ra ria man hinh
            Taunt    // dung o ria man hinh khieu khich
        }

        [Header("Toc do (don vi/giay)")]
        [Tooltip("Toc do tu buoc vao man hinh luc xuat hien (world scroll cong them).")]
        [SerializeField] private float approachSpeed = 1.2f;

        [Tooltip("Nghi giua hai phat ban o state Shoot. Phat dau tien bay ngay khi vao state.")]
        [SerializeField] private float shootInterval = 2f;

        [Tooltip("Toc do troi ve phia player TRUC LUC dan (state Shoot).")]
        [SerializeField] private float attackDriftSpeed = 0.8f;

        [Tooltip("Toc do NET khi chay lui. Da tu bu lai world scroll nen ke ca khi player " +
                 "charge (scroll x4) boss van thoat ra duoc.")]
        [SerializeField] private float retreatSpeed = 6f;

        [Header("Khung man hinh")]
        [Tooltip("Vi tri viewport X (0..1) cua 'ria man hinh': boss di vao den day de bat " +
                 "dau ban, va cung chay lui den day de khieu khich.")]
        [SerializeField] private float edgeViewportX = 0.9f;

        private State state = State.Enter;
        private bool pendingRetreat; // player vua danh trung: lui ngay sau frame hien tai
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
            // enabled lets its queue clamp compete with the boss retreat loop
            // and can leave the boss parked out of shooting position.
            EnemyMover mover = GetComponent<EnemyMover>();
            if (mover != null)
                mover.enabled = false;

            // EnemyAttack co Update tu ban theo tam rieng; o day goi AttackPlayer()
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
                yield return ShootRoutine();   // ban lien tuc + troi lai gan player
                if (dead) yield break;

                yield return RetreatRoutine(); // an dmg tu player -> chay lui ra ria
                if (dead) yield break;

                if (anim != null)
                    anim.SetFacingAway(false); // quay lai ve phia player de khieu khich
                yield return TauntRoutine();   // dung ria man hinh khieu khich
                if (dead) yield break;
                // Het khieu khich: lap lai, phat ban dau tien bay ngay.
            }
        }

        private IEnumerator EnterRoutine()
        {
            state = State.Enter;
            while (!dead && !pendingRetreat && !IsAtEdgeEnter())
                yield return null;
        }

        private IEnumerator ShootRoutine()
        {
            state = State.Shoot;
            float cooldown = 0f; // phat dau tien bay ngay khi vao state
            while (!dead && !pendingRetreat)
            {
                if (cooldown <= 0f)
                {
                    if (attack != null)
                        attack.AttackPlayer();
                    cooldown = shootInterval;
                }

                cooldown -= Time.deltaTime;
                yield return null;
            }
        }

        private IEnumerator RetreatRoutine()
        {
            pendingRetreat = false;
            state = State.Retreat;
            if (anim != null)
                anim.SetFacingAway(true); // chay lui thi quay mat di, clip run moi tu nhien
            while (!dead && !IsAtEdgeRetreat())
                yield return null;
        }

        private IEnumerator TauntRoutine()
        {
            state = State.Taunt;
            float duration = anim != null ? anim.PlayTaunt() : 0f;
            yield return new WaitForSeconds(duration > 0f ? duration : 1.2f);
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

        private float OwnVelocityX(float scroll)
        {
            return ComputeOwnVelocityX(state, SideToPlayer(), scroll, IsPlayerKnockedBack(),
                approachSpeed, retreatSpeed, attackDriftSpeed);
        }

        /// <summary>Toc do rieng cua boss theo state (pure de tu kiem tra).</summary>
        internal static float ComputeOwnVelocityX(State state, float side, float scroll,
            bool playerKnockedBack, float approachSpeed, float retreatSpeed, float driftSpeed)
        {
            switch (state)
            {
                case State.Enter:
                    // World scroll mang vao san, van tu buoc toi de khong bi tre khi
                    // scroll dung (Shield, StageComplete...).
                    return -approachSpeed * side;

                case State.Retreat:
                    // Chay lui voi toc do NET: bu lai ca scroll, khong thi luc player
                    // charge (scroll x4) boss bi keo tro lai thay vi chay ra ria.
                    return retreatSpeed * side - scroll;

                case State.Shoot:
                    // Vua ban vua troi ve phia player. Player vua bi knockback (dang
                    // bi day lui) thi DUNG nhip troi: chi bu scroll, giu nguyen vi tri
                    // tren man hinh de player kip phuc hoi.
                    float drift = playerKnockedBack ? 0f : driftSpeed;
                    return -scroll - drift * side;

                default:
                    // Khieu khich: dung cho, chi bu scroll de khong truot tren dat.
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

            // An dmg tu player (dang di vao / ban / troi): chay lui ra ria man hinh.
            // Dang lui/khieu khich thi cuong chi - khong nguot kich ban giua chung.
            if (damaged && !dead && ShouldRetreat(damaged, state))
                pendingRetreat = true;

            lastHealth = current;
        }

        /// <summary>An dmg o state nao thi duoc phep lui? (pure de tu kiem tra)</summary>
        internal static bool ShouldRetreat(bool damaged, State state)
        {
            return damaged && (state == State.Enter || state == State.Shoot);
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

        private bool IsPlayerKnockedBack()
        {
            if (scroller == null)
                scroller = FindObjectOfType<WorldScroller>();

            // Player bi knockback luon bat recoil cua world scroll (KnockbackReceiver
            // goi TriggerKnockback) nen day la tin hieu chung cho moi nguon don.
            return scroller != null && scroller.IsKnockbackActive;
        }

        private float ViewportX()
        {
            if (cam == null)
                cam = Camera.main;

            // Khong co camera: coi nhu van dang o ngoai man hinh ben phai.
            return cam != null ? cam.WorldToViewportPoint(transform.position).x : 1f;
        }

        /// <summary>Di vao tu ngoai man hinh (vx giam dan): den ria khi vx &lt;= edge.</summary>
        private bool IsAtEdgeEnter()
        {
            return ReachedEdgeEnter(ViewportX(), edgeViewportX);
        }

        /// <summary>Chay lui ve ben phai (vx tang dan): den ria khi vx &gt;= edge.</summary>
        private bool IsAtEdgeRetreat()
        {
            return ReachedEdgeRetreat(ViewportX(), edgeViewportX);
        }

        internal static bool ReachedEdgeEnter(float viewportX, float edge)
        {
            return viewportX <= edge;
        }

        internal static bool ReachedEdgeRetreat(float viewportX, float edge)
        {
            return viewportX >= edge;
        }

        private bool IsOnScreen()
        {
            float vx = ViewportX();
            return vx >= 0f && vx <= 1f;
        }
    }
}
