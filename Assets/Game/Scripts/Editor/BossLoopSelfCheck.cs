using UnityEditor;
using UnityEngine;
using EternalClash.Combat;
using EternalClash.Data;
using EternalClash.Enemy;
using EternalClash.Stage;
using EternalClash.Wave;

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
            CheckBossChallenge();
            CheckBossRewards();

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

            // Boss phai do duoc trong tam kiem cua Player: ClampBeforePlayer giu boss
            // o attackRange * 0.5, khoang cach do phai <= BasicAttackSystem.attackRange
            // (0.85) khong thi player khong bao gio cham duoc boss de tra don.
            var attackRange = attack != null ? attack.attackRange : 0f;
            Check(attackRange * 0.5f > 0f, "boss has an attack range to clamp at");

            const string playerPrefabPath = "Assets/Game/Prefabs/Player/Player.prefab";
            var playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(playerPrefabPath);
            if (playerPrefab == null)
                throw new System.InvalidOperationException("Missing prefab: " + playerPrefabPath);

            var basicAttack = playerPrefab.GetComponent<EternalClash.Combat.BasicAttackSystem>();
            if (basicAttack == null)
                throw new System.InvalidOperationException("Player prefab lacks BasicAttackSystem");

            var so = new SerializedObject(basicAttack);
            float swordRange = so.FindProperty("attackRange").floatValue;
            Check(attackRange * 0.5f <= swordRange,
                $"boss park distance ({attackRange * 0.5f:0.00}) stays inside the player sword range ({swordRange:0.00})");
        }

        private static void CheckBossChallenge()
        {
            // He so mau: thuong theo man (+15%/man), thu thach boss nhan them x5.
            Check(WaveManager.ComputeHealthMultiplier(1, false) - 1f == 0f, "stage 1 normal HP unscaled");
            Check(Mathf.Abs(WaveManager.ComputeHealthMultiplier(5, false) - 1.6f) < 0.001f, "stage 5 normal HP x1.6");
            Check(WaveManager.ComputeHealthMultiplier(1, true) - BossChallenge.HpMultiplier == 0f,
                "boss challenge stage 1 HP x5");
            Check(Mathf.Abs(WaveManager.ComputeHealthMultiplier(5, true) - 1.6f * BossChallenge.HpMultiplier) < 0.001f,
                "boss challenge stage 5 HP x8");

            // Match ten nut o panel "select boss" voi ten prefab quai that.
            Check(BossChallenge.MatchesPrefab("Slime", "slime"), "panel slime matches Slime prefab");
            Check(BossChallenge.MatchesPrefab("Wolf", "wolf"), "panel wolf matches Wolf prefab");
            Check(BossChallenge.MatchesPrefab("Goblin Mage", "goblin mage"), "panel goblin mage matches Goblin Mage prefab");
            Check(BossChallenge.MatchesPrefab("Boss", "goblin boss"), "panel goblin boss matches Boss prefab");
            Check(BossChallenge.MatchesPrefab("Boss", "boss"), "boss id also matches Boss prefab");
            Check(!BossChallenge.MatchesPrefab("Wolf", "goblin mage"), "wolf does not match goblin mage");
            Check(!BossChallenge.MatchesPrefab("Slime", "goblin boss"), "slime does not match goblin boss");

            // 4 prefab that phai ton tai voi dung ten de man thu thach spawn duoc.
            foreach (string prefabName in new[] { "Slime", "Wolf", "Goblin Mage", "Boss" })
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>($"Assets/Game/Prefabs/Enemy V1/{prefabName}.prefab");
                if (prefab == null)
                    throw new System.InvalidOperationException($"Missing enemy prefab: {prefabName}");

                Check(BossChallenge.MatchesPrefab(prefab.name, prefabName.ToLowerInvariant()),
                    $"{prefabName} prefab name matches its panel id");
            }

            // Trang thai: bat khi chon boss, tat khi huy.
            BossChallenge.Start("Goblin Boss");
            Check(BossChallenge.Active && BossChallenge.EnemyId == "goblin boss", "start activates challenge");
            BossChallenge.Cancel();
            Check(!BossChallenge.Active && BossChallenge.EnemyId == null, "cancel deactivates challenge");
            BossChallenge.Start(null);
            Check(!BossChallenge.Active, "null id does not activate challenge");
        }

        private static void CheckBossRewards()
        {
            // Ruong thu thach boss: uu tien kim cuong, luong tang theo man (tu can bang).
            int runs = 500, gems = 0, normalGems = 0;
            try
            {
                BossChallenge.Start("goblin boss");
                for (int i = 0; i < runs; i++)
                {
                    RewardData reward = RewardGenerator.GenerateStageReward(2);
                    if (reward.type != RewardType.Gem)
                        continue;

                    gems++;
                    // Boss stage 2: luong gem = 3..5 + stage = 5..7, luon cao hon thuong (3..5).
                    Check(reward.amount >= 5 && reward.amount <= 7, "boss gem amount scales with stage (3..5 + stage)");
                }

                BossChallenge.Cancel();
                for (int i = 0; i < runs; i++)
                {
                    if (RewardGenerator.GenerateStageReward(2).type == RewardType.Gem)
                        normalGems++;
                }
            }
            finally
            {
                BossChallenge.Cancel();
            }

            // 55% boss so voi 12% man thuong: o 500 lan quay, ty le boss phai cao ro rang.
            Check(gems > runs * 0.3f, $"boss chest favors gems ({(float)gems / runs:P0} of {runs}, expected ~55%)");
            Check(normalGems < runs * 0.25f, $"normal chest keeps 12% gem rate ({(float)normalGems / runs:P0} of {runs})");
            Check(gems > normalGems * 2, "boss gem rate clearly beats normal stage rate");
        }

        private static void Check(bool condition, string label)
        {
            if (!condition)
                throw new System.InvalidOperationException("Boss loop check failed: " + label);
        }
    }
}
