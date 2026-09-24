using UnityEditor;
using UnityEngine;
using EternalClash.Combat;
using EternalClash.Enemy;

namespace EternalClash.EditorTools
{
    /// <summary>
    /// Self-check cho vong lap Boss moi va diem ngam dan quai. Menu:
    /// Tools &gt; EternalClash &gt; Check Boss Loop Logic
    /// </summary>
    public static class BossLoopSelfCheck
    {
        [MenuItem("Tools/EternalClash/Check Boss Loop Logic")]
        public static void Run()
        {
            CheckMovement();
            CheckRetreatGating();
            CheckEdgeReach();
            CheckAimCenter();
            CheckBossRangedGuard();

            Debug.Log("[BossLoopSelfCheck] Boss loop + aim OK");
        }

        private static void CheckMovement()
        {
            const float approach = 1.2f;
            const float retreat = 6f;
            const float drift = 0.8f;
            const float scroll = -2.5f; // scroll chay ve ben trai nhu trong tran dau
            const float side = 1f;      // boss ben phai player

            // Ban + troi: bu scroll roi tru drift -> roi ve phia player 0.8/giay.
            Check(BossController.ComputeOwnVelocityX(BossController.State.Shoot, side, scroll,
                false, approach, retreat, drift) - 1.7f == 0f, "shoot drifts toward player (net own 1.7)");

            // Player bi knockback: DUNG nhip troi - chi bu scroll, dung yen tren man hinh.
            float paused = BossController.ComputeOwnVelocityX(BossController.State.Shoot, side, scroll,
                true, approach, retreat, drift);
            Check(paused - 2.5f == 0f, "knockback pauses drift (holds screen position)");
            Check(scroll + paused == 0f, "knockback pause nets zero movement");

            // Chay lui: bu ca scroll de thoat duoc ca luc player charge x4.
            float back = BossController.ComputeOwnVelocityX(BossController.State.Retreat, side, scroll,
                false, approach, retreat, drift);
            Check(back - 8.5f == 0f, "retreat compensates scroll (own 8.5)");
            Check(scroll + back - retreat == 0f, "retreat nets full retreat speed");

            // Di vao: tu buoc toi ke ca khi scroll dung.
            Check(BossController.ComputeOwnVelocityX(BossController.State.Enter, side, 0f,
                false, approach, retreat, drift) - (-approach) == 0f, "enter approaches without scroll");

            // Khieu khich: dung cho, chi bu scroll.
            Check(BossController.ComputeOwnVelocityX(BossController.State.Taunt, side, scroll,
                false, approach, retreat, drift) - (-scroll) == 0f, "taunt holds screen position");
        }

        private static void CheckRetreatGating()
        {
            // An dmg luc dang ban/troi/di vao -> lui. Dang lui/khieu khich -> cuong chi.
            Check(BossController.ShouldRetreat(true, BossController.State.Shoot), "damage while shooting retreats");
            Check(BossController.ShouldRetreat(true, BossController.State.Enter), "damage while entering retreats");
            Check(!BossController.ShouldRetreat(true, BossController.State.Retreat), "damage while retreating does not restart retreat");
            Check(!BossController.ShouldRetreat(true, BossController.State.Taunt), "damage while taunting does not restart retreat");
            Check(!BossController.ShouldRetreat(false, BossController.State.Shoot), "no damage never retreats");
        }

        private static void CheckEdgeReach()
        {
            // Di vao tu ngoai man hinh (vx giam dan): dung khi vx <= ria.
            Check(!BossController.ReachedEdgeEnter(1.05f, 0.9f), "enter keeps walking off-screen");
            Check(BossController.ReachedEdgeEnter(0.9f, 0.9f), "enter stops at the rim");
            Check(BossController.ReachedEdgeEnter(0.5f, 0.9f), "enter stops once past the rim");

            // Chay lui ve ben phai (vx tang dan): dung khi vx >= ria. Phai la so sanh
            // nguoc lai voi luc di vao, khong thi boss khong chay lui duoc.
            Check(!BossController.ReachedEdgeRetreat(0.4f, 0.9f), "retreat keeps running from mid-screen");
            Check(BossController.ReachedEdgeRetreat(0.9f, 0.9f), "retreat stops at the rim");
            Check(BossController.ReachedEdgeRetreat(1.2f, 0.9f), "retreat stops once past the rim");
        }

        private static void CheckAimCenter()
        {
            // Dung dung Player.prefab that: root co box chan dat nho (tam y~0.02) +
            // hitbox than lon (tam y~0.61). Dan phai ngam hitbox than, khong phai box chan.
            const string playerPrefab = "Assets/Game/Prefabs/Player/Player.prefab";
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(playerPrefab);
            if (prefab == null)
                throw new System.InvalidOperationException("Missing prefab: " + playerPrefab);

            var go = Object.Instantiate(prefab);
            try
            {
                Vector3 center = EnemyAttack.GetBodyColliderCenter(go.transform);

                Collider2D body = null;
                Collider2D feet = null;
                float bestArea = 0f;
                float worstArea = float.MaxValue;
                foreach (var col in go.GetComponents<Collider2D>())
                {
                    Vector2 ext = col.bounds.extents;
                    float area = ext.x * ext.y;
                    if (area > bestArea) { bestArea = area; body = col; }
                    if (area < worstArea) { worstArea = area; feet = col; }
                }

                if (body == null || feet == null || body == feet)
                    throw new System.InvalidOperationException("Player prefab is expected to have 2 root colliders");

                Check(Mathf.Abs(center.y - body.bounds.center.y) < 0.001f,
                    $"aim uses the body hitbox center (got y={center.y:0.000}, body y={body.bounds.center.y:0.000}, feet y={feet.bounds.center.y:0.000})");
                Check(center.y > feet.bounds.center.y + 0.1f, "aim is clearly above the feet box");
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        private static void CheckBossRangedGuard()
        {
            // Boss la quai bang dan (EnemyAttack co projectilePrefab) nhung
            // EnemyMover.isArcher = 0. Guard khong-keo-sat-nguoi cua Charge phai
            // nhan ra boss la ranged qua attack that, khong thi bấm charge keo
            // boss vao sat player va pha vong vong lap ban tu xa.
            const string bossPrefab = "Assets/Game/Prefabs/Enemy V1/Boss.prefab";
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(bossPrefab);
            if (prefab == null)
                throw new System.InvalidOperationException("Missing prefab: " + bossPrefab);

            var attack = prefab.GetComponent<EternalClash.Combat.EnemyAttack>();
            Check(attack != null && attack.IsRanged,
                "boss is recognized as ranged (projectilePrefab) for the charge pull guard");

            // Hitbox vien dan lech truoc ~0.31 + body hitbox Player lech 0.66: dan
            // spawn trong khoang cach <= ~1.0 thi chet ngay frame dau ("dan bien mat").
            // BossController.ClampBeforePlayer giu boss o dung attackRange nen
            // attackRange phai nam ngoai nguong do.
            Check(attack != null && attack.attackRange >= 1f,
                "boss attackRange keeps spawned bullets clear of the player hitbox");
        }

        private static void Check(bool condition, string label)
        {
            if (!condition)
                throw new System.InvalidOperationException("Boss loop check failed: " + label);
        }
    }
}
