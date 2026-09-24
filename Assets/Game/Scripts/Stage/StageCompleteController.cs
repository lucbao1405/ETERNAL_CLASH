using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using EternalClash.Data;
using EternalClash.Village;
using EternalClash.Core;
using EternalClash.Core.Save;
using EternalClash.Chest;
using EternalClash.World;
using EternalClash.Player;
using EternalClash.Combat;
using EternalClash.Wave;
using EternalClash.BattleResult;

namespace EternalClash.Stage
{
    public class StageCompleteController : MonoBehaviour
    {
        public static StageCompleteController Instance { get; private set; }

        [Header("UI Panels")]
        [SerializeField] private GameObject chestRewardPanel;
        [SerializeField] private GameObject equipmentPreviewPanel;
        [SerializeField] private GameObject stageResultPanel;
        [SerializeField] private GameObject levelUpPanel;
        [SerializeField] private UI.StageResultUI stageResultUIScript;

        [Header("World")]
        [SerializeField] private World.WorldScroller worldScroller;

        [Header("Chest Spawn Drift")]
        [Tooltip("Ruong xuat hien phia truoc nhan vat bao nhieu (world units) sau khi clear het quai.")]
        [SerializeField, Min(1f)] private float chestSpawnAheadDistance = 8f;
        [Tooltip("Toc do ruong troi ve phia nhan vat (world units/giay). Ham duoi bang toc do ground scroll nen giong cach quai troi vao.")]
        [SerializeField, Min(0.5f)] private float chestDriftSpeed = 2.5f;
        [Tooltip("Ruong dung lai cach nhan vat bao nhieu (world units, gan bang khoang cach quai dung lai truoc mat player).")]
        [SerializeField, Min(0.1f)] private float chestStopDistance = 1f;
        [Tooltip("Chieu cao ruong tren duong (world units).")]
        [SerializeField, Min(0.1f)] private float chestWorldHeight = 1.1f;

        private Coroutine chestDriftRoutine;
        private bool chestDriftDone;
        private static GameObject worldChest;
        private const float MaxChestDriftSeconds = 12f;

        public RewardData CurrentReward => currentReward;
        public StageResultData StageResult => stageResult;
        public UI.StageResultUI StageResultUI => stageResultUI;
        private RewardData currentReward;
        private StageResultData stageResult;
        private UI.StageResultUI stageResultUI;
        private bool isProcessing;
        private bool defeatProcessed;

        // End-of-map field meeting state (see FieldMeetingController).
        private DialogueManager battleDialogue;

        // Man vua clear, doc mot lan trong BeginPostStageFlow khi stageLevel chua
        // duoc tang. Duong flow co gap NPC ket thuc SAU khi CompleteStage tang
        // stageLevel, nen ProceedToVictoryFlow phai dung gia tri nay thay vi doc
        // lai SaveManager (se sinh thuong cua man ke tiep).
        private int lastClearedStage = 1;

        private const string BattleSceneName = "Battle";
        private const string ChestSpawnPointName = "ChestSpawnPoint";

        /// <summary>
        /// Tu tao controller trong scene Battle neu scene chua co san, theo dung mau
        /// ma UI.BattlePopupController dang dung. Truoc day class nay khong nam trong
        /// scene nao nen Instance luon null - StageManager.CompleteStage() goi
        /// BeginPostStageFlow() vao chỗ trong, va BattlePopupController khong lay duoc
        /// StageResult de hien thi.
        /// Cac tham chieu UI panel deu duoc kiem tra null truoc khi dung nen ban tu
        /// tao (khong co panel nao) van chay an toan.
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void InstallSceneHook()
        {
            // RuntimeInitializeOnLoadMethod chi chay DUNG MOT LAN sau scene dau tien.
            // Neu game khoi dong o MainMenu hoac Town roi moi vao Battle thi kiem tra
            // ten scene o lan chay do that bai va controller khong bao gio duoc tao,
            // khien StageManager.CompleteStage() goi vao Instance null -> khong co
            // popup thang/thua. Vi vay phai bat them su kien sceneLoaded de kiem tra
            // lai moi lan doi scene.
            UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoaded;
            UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;

            EnsureInBattleScene();
        }

        private static void OnSceneLoaded(
            UnityEngine.SceneManagement.Scene scene,
            UnityEngine.SceneManagement.LoadSceneMode mode)
        {
            EnsureInBattleScene();
        }

        /// <summary>
        /// Tu tao controller trong scene Battle neu scene chua co san, theo dung mau
        /// ma UI.BattlePopupController dang dung. Cac tham chieu UI panel deu duoc
        /// kiem tra null truoc khi dung nen ban tu tao van chay an toan.
        /// </summary>
        private static void EnsureInBattleScene()
        {
            if (Instance != null)
                return;

            UnityEngine.SceneManagement.Scene scene =
                UnityEngine.SceneManagement.SceneManager.GetActiveScene();

            if (!scene.IsValid() ||
                !string.Equals(scene.name, BattleSceneName, System.StringComparison.OrdinalIgnoreCase))
                return;

            if (FindObjectOfType<StageCompleteController>() != null)
                return;

            new GameObject("StageCompleteController (Runtime)")
                .AddComponent<StageCompleteController>();

            Debug.Log("[StageComplete] Da tu tao controller cho scene Battle.");
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }

        private void Start()
        {
            if (chestRewardPanel != null)
                chestRewardPanel.SetActive(false);
            if (equipmentPreviewPanel != null)
                equipmentPreviewPanel.SetActive(false);
            if (stageResultPanel != null)
                stageResultPanel.SetActive(false);
            if (levelUpPanel != null)
                levelUpPanel.SetActive(false);

            if (stageResultUI == null)
            {
                if (stageResultUIScript != null) stageResultUI = stageResultUIScript;
                else if (stageResultPanel != null) stageResultUI = stageResultPanel.GetComponent<UI.StageResultUI>();
            }
        }

        public void BeginPostStageFlow()
        {
            if (isProcessing) return;

            isProcessing = true;
            chapterEndingStep = 0;

            // End-of-map meeting: stages with a field NPC (or a story moment)
            // keep the hero running down the road first; the dialogue plays
            // there, and ProceedToVictoryFlow continues afterwards.
            // StageManager.CompleteStage() goi ham nay TRUOC khi tang stageLevel,
            // nen stageLevel doc o day chinh la man vua clear. Tru di 1 lam le hen
            // mot man: thang man 2 van gap Garen (roi duong phu thuy mo som trong
            // khi chua gap Elara), Elara chi gap o man 3. Boss (man cuoi) duoc
            // kiem tra TRUOC de khong bi NPC/story cua cung so man chiem flow.
            int clearedStage = SaveManager.Instance != null && SaveManager.Instance.Data != null
                ? Mathf.Max(1, SaveManager.Instance.Data.stageLevel) : 1;
            lastClearedStage = clearedStage;

            if (IsBossStage(clearedStage) || // boss stage: open ending plays after the Chieftain falls
                Story.StoryManager.HasNPCEncounter(clearedStage) ||
                Story.StoryManager.IsStoryStage(clearedStage))
            {
                // KHONG StopCombat() o day: no dung ca world + AutoRunner, khien
                // nhan vat dung im thay vi run den NPC. FieldMeetingController
                // tu tat phan chien dau (PartialStopCombat) nhung van giu world
                // cuon + hero run; dung toan bo chi xay khi NPC dung lai, va
                // lan cuoi cung la StopCombat trong victory flow sau hoi thoai.
                EternalClash.Monetization.OfferOverlayUI.Close();
                FieldMeetingController.Begin(clearedStage, this);
                return;
            }

            ProceedToVictoryFlow();
        }

        /// <summary>
        /// Resumes the standard victory flow (chest -> reward -> win popup ->
        /// village). Called directly when the stage has no field meeting, or by
        /// FieldMeetingController once the meeting dialogue has closed.
        /// </summary>
        public void ProceedToVictoryFlow()
        {
            // Ruong (ChestSpawn) "troi ra" giong quai: sau khi clear het quai, ruong
            // xuat hien phia truoc duong roi truot ve phia player theo toc do dat.
            // Hero van chay, world van cuon cho den khi ruong dung lai ben canh;
            // luc do hero moi dung (StopCombat trong flow thang) va popup thang mo.
            if (!chestDriftDone && ResolveChestSprite() != null)
            {
                if (chestDriftRoutine == null)
                    chestDriftRoutine = StartCoroutine(ChestDriftRoutine());
                return;
            }

            OpenVictoryFlow();
        }

        /// <summary>
        /// Flow thang that su: victory anim + ruong -> reward -> win popup. Duoc goi
        /// khi khong co ruong, hoac ngay sau khi ChestSpawn troi toi ben canh hero.
        /// </summary>
        private void OpenVictoryFlow()
        {
            // Animation "victory" chi mo khi flow thang that su chay: khong co
            // NPC thi ngay tai day, co NPC thi FieldMeetingController goi lai
            // SAU khi hoi thoai ket thuc (nhan vat chay toi NPC truoc do).
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
                player.GetComponentInChildren<EternalClash.Animation.PlayerAnimationController>()
                    ?.NotifyVictory();

            if (BattleResultFlowController.Instance != null &&
                BattleResultFlowController.Instance.StartVictoryFlow())
                return;

            StopCombat();
            EternalClash.Monetization.OfferOverlayUI.Close();

            float stageTime = ResolveBattleTime(60f);

            // Khong con thuong them khi thang. Popup bao dung so EXP va vang da kiem
            // duoc trong tran (nhat tu quai va vat pham) - nhung so nay da duoc cong
            // vao tai khoan ngay luc nhat roi, o day chi doc lai de hien thi.
            int earnedExp = PlayerStatSystem.Instance != null
                ? PlayerStatSystem.Instance.SessionExpEarned : 0;
            int earnedGold = GoldSystem.Instance != null
                ? GoldSystem.Instance.SessionGoldEarned : 0;

            currentReward = RewardGenerator.GenerateStageReward(lastClearedStage);

            stageResult = new StageResultData
            {
                stageTime = stageTime,
                earnedExp = earnedExp,
                earnedGold = earnedGold,
                rewards = new System.Collections.Generic.List<RewardData> { currentReward }
            };

            SpawnChest();
        }

        /// <summary>
        /// Dung combat/scroll/spawn. Public de BattleResultFlowController goi lai khi
        /// no tiep quan flow ket qua.
        /// </summary>
        public void StopCombat()
        {
            var scroller = FindObjectOfType<WorldScroller>();
            if (scroller != null)
            {
                scroller.StopScroll();
                scroller.SetSpeedMultiplier(0f);
            }

            var loopController = FindObjectOfType<WorldLoopController>();
            if (loopController != null)
                loopController.StopScroll();

            var loopSpawner = FindObjectOfType<WorldLoopSpawner>();
            if (loopSpawner != null)
                loopSpawner.enabled = false;

            var encounterSpawner = FindObjectOfType<EncounterSpawner>();
            if (encounterSpawner != null)
                encounterSpawner.enabled = false;

            var waveManager = FindObjectOfType<WaveManager>();
            if (waveManager != null)
                waveManager.enabled = false;

            var player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                var autoRunner = player.GetComponent<AutoRunner>();
                if (autoRunner != null)
                    autoRunner.StopRunning();

                var combatController = player.GetComponent<CombatController>();
                if (combatController != null)
                    combatController.enabled = false;
            }
        }

        // ------------------------------------------------------------------
        // ChestSpawn troi ra sau khi clear het quai
        // ------------------------------------------------------------------

        /// <summary>
        /// Tao ruong the gioi tai ChestSpawnPoint (dung chinh art cua marker
        /// "Dichden" tren thanh Progress). Duoc dung o hai noi: buoi gap NPC
        /// (FieldMeetingController truot no cung NPC) va buoc troi rieng khi
        /// khong co NPC. Tra ve null neu khong co sprite/hero.
        /// </summary>
        public GameObject SpawnWorldChest()
        {
            Sprite sprite = ResolveChestSprite();
            GameObject hero = GameObject.FindGameObjectWithTag("Player");
            if (sprite == null || hero == null)
                return null;

            Transform spawnPoint = GameObject.Find(ChestSpawnPointName)?.transform;
            Vector3 start = spawnPoint != null
                ? spawnPoint.position
                : hero.transform.position + Vector3.right * chestSpawnAheadDistance;

            worldChest = new GameObject("ChestSpawnWorld", typeof(SpriteRenderer));
            SpriteRenderer chestRenderer = worldChest.GetComponent<SpriteRenderer>();
            chestRenderer.sprite = sprite;
            chestRenderer.sortingOrder = 10;

            Vector3 spriteSize = chestRenderer.bounds.size;
            if (spriteSize.y > 0.0001f)
                worldChest.transform.localScale =
                    Vector3.one * (chestWorldHeight / spriteSize.y);

            worldChest.transform.position = start;
            // Chan ruong cham duong mat dat chung + bong dem giong nhan vat/NPC.
            worldChest.AddComponent<CombatLaneAligner>();
            worldChest.AddComponent<EternalClash.Character.CharacterShadow>();
            return worldChest;
        }

        /// <summary>Ruong da dung canh hero (buoi gap NPC) - bo qua buoc troi rieng.</summary>
        public void MarkChestArrived() => chestDriftDone = true;

        /// <summary>Khoang cach ruong dung lai truoc mat hero, FieldMeetingController dung chung.</summary>
        public float ChestStopDistance => chestStopDistance;

        /// <summary>
        /// Ruong troi tren duong ve phia player giong cach quai troi vao (khong
        /// co NPC gap cuoi duong): hero van chay, world van cuon trong luc ruong
        /// truot lai; ruong dung thi hero dung + flow thang mo. Bien mat khi
        /// panel "Open Chest" sap hien (DespawnWorldChest goi tu
        /// BattleResultFlowController).
        /// </summary>
        private IEnumerator ChestDriftRoutine()
        {
            if (SpawnWorldChest() == null)
            {
                chestDriftDone = true;
                OpenVictoryFlow();
                yield break;
            }

            GameObject hero = GameObject.FindGameObjectWithTag("Player");
            var scroller = FindObjectOfType<WorldScroller>();
            float stopX = hero.transform.position.x + chestStopDistance;
            float elapsed = 0f;
            while (elapsed < MaxChestDriftSeconds)
            {
                if (worldChest == null)
                    yield break;

                Vector3 pos = worldChest.transform.position;
                // Truot voi toc do dat (giong EnemyMover), nhung khong cham hon
                // chestDriftSpeed: stage gap NPC da dung scroll thi ruong van tu toi.
                float groundSpeed = scroller != null ? scroller.GetGroundVelocity() : 0f;
                pos.x -= Mathf.Max(groundSpeed, chestDriftSpeed) * Time.deltaTime;
                pos.x = Mathf.Max(pos.x, stopX);
                worldChest.transform.position = pos;
                elapsed += Time.deltaTime;
                if (pos.x <= stopX + 0.001f)
                    break;
                yield return null;
            }

            chestDriftRoutine = null;
            chestDriftDone = true;
            Debug.Log("[StageComplete] ChestSpawn da troi toi ben player - hero dung va mo flow thang.");
            OpenVictoryFlow();
        }

        /// <summary>
        /// Bien mat ruong the gioi dung truoc khi panel "Open Chest" hien. Goi tu
        /// BattleResultFlowController ngay truoc khi mo popup ruong thuong.
        /// </summary>
        public static void DespawnWorldChest()
        {
            if (worldChest != null)
                Destroy(worldChest);
            worldChest = null;
        }

        /// <summary>
        /// Sprite ruong lay tu instance ChestSpawn prefabs trong scene (co the bi
        /// doi ten thanh "Dichden"). Khong co thi flow thang chay ngay nhu cu,
        /// khong them buoc troi. Public cho EditMode test.
        /// </summary>
        public static Sprite ResolveChestSprite()
        {
            RectTransform marker = FindChestSpawnRect();
            Image image = marker != null ? marker.GetComponent<Image>() : null;
            return image != null ? image.sprite : null;
        }

        /// <summary>
        /// Tim ChestSpawn trong scene: instance co the bi doi ten thanh "Dichden"
        /// nen kiem tra ca hai ten. Khong co thi flow thang chay ngay nhu cu,
        /// khong them buoc troi. Public cho EditMode test.
        /// </summary>
        public static RectTransform FindChestSpawnRect()
        {
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if (!scene.IsValid())
                return null;

            foreach (GameObject root in scene.GetRootGameObjects())
            {
                Transform found = FindDescendantByName(root.transform, "Dichden") ??
                                  FindDescendantByName(root.transform, "ChestSpawn");
                if (found != null)
                    return found.GetComponent<RectTransform>();
            }

            return null;
        }

        private static Transform FindDescendantByName(Transform parent, string name)
        {
            foreach (Transform child in parent)
            {
                if (string.Equals(child.name, name, System.StringComparison.OrdinalIgnoreCase))
                    return child;

                Transform match = FindDescendantByName(child, name);
                if (match != null)
                    return match;
            }

            return null;
        }

        private void SpawnChest()
        {
            if (BattleResult.BattleResultFlowController.Instance != null)
            {
                BattleResult.BattleResultFlowController.Instance.StartWinFlow();
                return;
            }

            GrantReward();
            ContinueAfterReward();
        }

        /// <summary>
        /// Enables the scene ChestRewardPanel ("Open Chest" prompt). Public so the
        /// runtime chest-spawn flow can re-show it without owning the panel object.
        /// </summary>
        public void ShowChestRewardUI()
        {
            if (chestRewardPanel != null)
                chestRewardPanel.SetActive(true);
        }

        public void OnChestOpened()
        {
            if (chestRewardPanel != null)
                chestRewardPanel.SetActive(false);

            if (currentReward != null && currentReward.type == RewardType.Equipment)
            {
                ShowEquipmentPreview();
            }
            else
            {
                GrantReward();
                ContinueAfterReward();
            }
        }

        private void ShowEquipmentPreview()
        {
            if (equipmentPreviewPanel != null)
                equipmentPreviewPanel.SetActive(true);
        }

        public void OnEquipAccepted()
        {
            if (equipmentPreviewPanel != null)
                equipmentPreviewPanel.SetActive(false);

            EquipmentSystem.Instance?.EquipItem(currentReward.item);

            GrantReward();
            ContinueAfterReward();
        }

        public void OnEquipSkipped()
        {
            if (equipmentPreviewPanel != null)
                equipmentPreviewPanel.SetActive(false);

            GrantReward();
            ContinueAfterReward();
        }

        private void GrantReward()
        {
            if (stageResult == null) return;

            var goldSys = GoldSystem.Instance;

            // Khong cong lai earnedGold / earnedExp: hai con so do la TONG da kiem
            // duoc trong tran va da vao tai khoan ngay luc nhat. Cong them lan nua
            // se thanh nhan doi. O day chi trao phan thuong cua ruong.

            // Trao phan thuong cua ruong theo dung loai. Truoc day chi xu ly Material:
            // phan thuong Gold bi bo qua (popup van hien so nen nguoi choi thay thieu
            // vang), con Gem thi mat han du RewardGenerator sinh no voi ti le 20%.
            if (currentReward != null && currentReward.amount > 0)
            {
                switch (currentReward.type)
                {
                    case RewardType.Gold:
                        goldSys?.AddGold(currentReward.amount);
                        break;

                    case RewardType.Gem:
                        goldSys?.AddGem(currentReward.amount);
                        break;

                    case RewardType.Material:
                        goldSys?.AddMaterials(currentReward.amount, 0);
                        break;

                    case RewardType.Gift:
                        AddGiftReward(currentReward.item, currentReward.amount);
                        break;

                    // Equipment duoc trao rieng qua EquipmentSystem.EquipItem()
                    // trong OnEquipAccepted(), khong xu ly o day.
                }
            }
        }

        private static void AddGiftReward(ItemData item, int amount)
        {
            if (item == null || amount <= 0)
                return;

            SaveData data = SaveManager.Instance?.Data;
            if (data == null)
                return;

            data.inventory ??= new InventorySaveData();
            data.inventory.items ??= new System.Collections.Generic.List<ItemStackSaveData>();
            ItemStackSaveData stack = data.inventory.items.Find(value =>
                value != null && string.Equals(value.itemId, item.itemId, System.StringComparison.OrdinalIgnoreCase));
            if (stack == null)
            {
                stack = new ItemStackSaveData { itemId = item.itemId };
                data.inventory.items.Add(stack);
            }

            stack.amount += amount;
            SaveCoordinator.RequestSave();
        }

        private Sprite ResolveRewardSprite(RewardData reward)
        {
            if (reward == null) return null;

            if (reward.HasItem && !string.IsNullOrEmpty(reward.item.iconSpriteName))
            {
                return Resources.Load<Sprite>(reward.item.iconSpriteName);
            }

            return null;
        }

        private void ContinueAfterReward()
        {
            ShowVictoryFlow();
        }

        /// <summary>
        /// WIN path after the chest reward has been granted: the old StageClearBanner
        /// is no longer used, the victory StageResultPanel opens directly.
        /// </summary>
        private void ShowVictoryFlow()
        {
            if (UI.BattlePopupController.TryShowWin())
            {
                if (stageResultPanel != null)
                    stageResultPanel.SetActive(false);
                return;
            }

            if (stageResultUI != null)
                stageResultUI.ShowVictory();
            else if (stageResultPanel != null)
                stageResultPanel.SetActive(true);
        }

        private void ShowDefeatPopup(float stageTime, int earnedExp)
        {
            if (UI.BattlePopupController.TryShowLose())
            {
                if (stageResultPanel != null)
                    stageResultPanel.SetActive(false);
                return;
            }

            if (stageResultUI != null)
                stageResultUI.ShowDefeat(stageTime, earnedExp);

            if (stageResultPanel != null)
                stageResultPanel.SetActive(true);
        }

        public void OnWinContinueClicked()
        {
            if (stageResultPanel != null)
                stageResultPanel.SetActive(false);

            ReturnToVillage();
        }

        public void OnLevelUpConfirmed()
        {
            if (levelUpPanel != null)
                levelUpPanel.SetActive(false);

            ReturnToVillage();
        }

        public void ReturnToVillage()
        {
            isProcessing = false;
            defeatProcessed = false;

            // Dong bo mau voi Town. THUA (chet / bo cuoc): giu trang thai thuong tich ma
            // luong thua vua dat (ve lang voi 10% mau) - khong ghi de. Con lai (thang):
            // mang dung so mau con lai trong tran ve Town, thieu mau thi tiep tuc hoi.
            bool defeated = StageManager.Instance != null &&
                            StageManager.Instance.CurrentState == StageManager.StageState.Defeat;

            var condition = EternalClash.Core.PlayerConditionSystem.Instance;
            if (!defeated && condition != null)
            {
                var player = GameObject.FindGameObjectWithTag("Player");
                var health = player != null ? player.GetComponent<EternalClash.Character.HealthSystem>() : null;
                if (health != null && !health.IsDead)
                    condition.SetHpAfterBattle(health.CurrentHealth, health.MaxHealth);
            }

            SaveCoordinator.RequestSave();
            EternalClash.Core.SceneLoader.LoadTown();
        }

        /// <summary>
        /// Opens the end-of-map meeting dialogue on the Battle dialogue panel.
        /// Called by FieldMeetingController once the hero has run up to the NPC;
        /// the meeting controller resumes the victory flow when it closes.
        /// </summary>
        public void PlayMeetingDialogue(int clearedStage)
        {
            string[] lines = null;
            string speaker = null;

            if (Story.StoryManager.HasNPCEncounter(clearedStage))
            {
                speaker = Story.StoryManager.GetNPCName(clearedStage);
                lines = Story.StoryManager.GetNPCDialogue(clearedStage);
            }
            else if (Story.StoryManager.IsStoryStage(clearedStage))
            {
                speaker = Story.StoryManager.GetStageName(clearedStage);
                lines = Story.StoryManager.GetStoryDialogue(clearedStage);
            }
            else if (IsBossStage(clearedStage))
            {
                // Boss khong co NPC tren duong nhung van phai mo mot doan hoi thoai:
                // OnMeetingDialogueClosed phu thuoc DialogueCompleted de chay chuoi
                // ket chapter (open ending -> thu cua Ela). Khong co nhanh nay thi
                // pendingEncounterStage khong duoc gan va phan ket khong bao gio chay.
                speaker = Story.StoryManager.GetStageName(clearedStage);
                string narration = Story.StoryManager.GetPostVictoryNarration(clearedStage);
                lines = string.IsNullOrEmpty(narration) ? null : new[] { narration };
            }

            DialogueManager dialogueManager = EnsureDialogueManager();
            if (dialogueManager == null || lines == null || lines.Length == 0)
            {
                Debug.LogWarning("[FieldMeeting] Dialogue unavailable; continuing victory flow.");
                FieldMeetingController.Instance?.NotifyDialogueClosed();
                return;
            }

            pendingEncounterStage = clearedStage;

            dialogueManager.DialogueCompleted -= OnMeetingDialogueClosed;
            dialogueManager.DialogueCompleted += OnMeetingDialogueClosed;
            dialogueManager.OpenDialogue(speaker, null, lines);

            Debug.Log($"[FieldMeeting] Stage {clearedStage} meeting dialogue started ({speaker}).");
        }

        private int pendingEncounterStage;

        /// <summary>
        /// Boss = man cuoi cung cua stageCatalog. Doc dong tu StageManager de
        /// them/giam so man khong phai sua code o day.
        /// </summary>
        private static bool IsBossStage(int clearedStage) =>
            StageManager.Instance != null
                ? clearedStage >= StageManager.Instance.MaxStageLevel
                : clearedStage >= 5;

        private void OnMeetingDialogueClosed()
        {
            var dialogueManager = battleDialogue;
            if (dialogueManager != null)
                dialogueManager.DialogueCompleted -= OnMeetingDialogueClosed;

            // Boss stage clears Chapter 1: open ending, then her letter, then the
            // usual victory flow back to the village.
            if (IsBossStage(pendingEncounterStage) && chapterEndingStep < 2)
            {
                if (dialogueManager == null)
                {
                    FieldMeetingController.Instance?.NotifyDialogueClosed();
                    return;
                }

                if (chapterEndingStep == 0)
                {
                    chapterEndingStep = 1;
                    dialogueManager.DialogueCompleted += OnMeetingDialogueClosed;
                    Story.StoryManager.ShowChapter1OpenEnding(dialogueManager);
                    return;
                }

                chapterEndingStep = 2;
                dialogueManager.DialogueCompleted += OnMeetingDialogueClosed;
                Story.StoryManager.ShowLetterFromHer(dialogueManager);
                return;
            }

            FieldMeetingController.Instance?.NotifyDialogueClosed();
        }

        private int chapterEndingStep;

        /// <summary>
        /// The Battle scene normally gets its dialogue panel from the thoai
        /// prefab; this only builds a fallback panel (auto-assign compatible
        /// name "thoai_runtime") when none exists.
        /// </summary>
        private DialogueManager EnsureDialogueManager()
        {
            if (battleDialogue == null)
            {
                // The thoai prefab's DialogueManager sits on a panel that is
                // deactivated right after Awake, so plain FindObjectOfType
                // misses it. Any scene-object instance is valid, active or not.
                // Any scene-object instance is valid, active or not — but it must
                // have run Awake at least once: an inactive object never Awake'd,
                // so its references are still the prefab defaults (null panel) and
                // every OpenDialogue would fail ValidateReferences silently.
                foreach (DialogueManager candidate in Resources.FindObjectsOfTypeAll<DialogueManager>())
                {
                    if (candidate != null && candidate.gameObject.scene.IsValid() &&
                        (candidate.gameObject.activeInHierarchy || candidate.IsInitialized))
                    {
                        battleDialogue = candidate;
                        break;
                    }
                }
            }

            if (battleDialogue != null)
                return battleDialogue;

            Canvas canvas = FindObjectOfType<Canvas>();
            if (canvas == null)
            {
                Debug.LogError("[FieldMeeting] No Canvas in Battle scene; dialogue skipped.");
                return null;
            }

            GameObject panel = new GameObject("thoai_runtime", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            panel.transform.SetParent(canvas.transform, false);

            RectTransform rect = panel.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            // Authored position doubles as the hidden position; DialogueManager
            // derives the shown position 525 units below it (=> bottom of screen).
            rect.anchoredPosition = new Vector2(0f, 745f);
            rect.sizeDelta = new Vector2(1080f, 340f);

            Image image = panel.GetComponent<Image>();
            image.color = new Color(0.15f, 0.10f, 0.07f, 0.97f);

            Outline outline = panel.AddComponent<Outline>();
            outline.effectColor = new Color(0.85f, 0.62f, 0.25f, 1f);
            outline.effectDistance = new Vector2(3f, -3f);

            GameObject nameGo = new GameObject("Ten_Nhan_Vat", typeof(RectTransform), typeof(CanvasRenderer));
            nameGo.transform.SetParent(panel.transform, false);
            TMP_Text nameText = nameGo.AddComponent<TextMeshProUGUI>();
            nameText.text = string.Empty;
            nameText.fontSize = 44f;
            nameText.fontStyle = FontStyles.Bold;
            nameText.color = new Color(0.96f, 0.88f, 0.60f);
            nameText.alignment = TextAlignmentOptions.TopLeft;
            nameText.raycastTarget = false;
            RectTransform nameRect = (RectTransform)nameGo.transform;
            nameRect.anchorMin = new Vector2(0f, 1f);
            nameRect.anchorMax = new Vector2(1f, 1f);
            nameRect.pivot = new Vector2(0f, 1f);
            nameRect.anchoredPosition = new Vector2(24f, -14f);
            nameRect.sizeDelta = new Vector2(-48f, 60f);

            GameObject bodyGo = new GameObject("Noi_Dung", typeof(RectTransform), typeof(CanvasRenderer));
            bodyGo.transform.SetParent(panel.transform, false);
            TMP_Text bodyText = bodyGo.AddComponent<TextMeshProUGUI>();
            bodyText.text = string.Empty;
            bodyText.fontSize = 34f;
            bodyText.color = new Color(0.96f, 0.92f, 0.85f);
            bodyText.alignment = TextAlignmentOptions.TopLeft;
            bodyText.raycastTarget = false;
            RectTransform bodyRect = (RectTransform)bodyGo.transform;
            bodyRect.anchorMin = new Vector2(0f, 0f);
            bodyRect.anchorMax = new Vector2(1f, 1f);
            bodyRect.offsetMin = new Vector2(24f, 20f);
            bodyRect.offsetMax = new Vector2(-24f, -84f);

            battleDialogue = panel.AddComponent<DialogueManager>();

            Debug.Log("[FieldEncounter] Runtime dialogue panel created for the Battle scene.");
            return battleDialogue;
        }

        public void BeginDefeatFlow()
        {
            if (defeatProcessed) return;

            // Neu co BattleResultFlowController (he thong popup moi) trong scene thi
            // de no dieu khien toan bo flow thua (lose popup, khong chay chest).
            if (BattleResultFlowController.Instance != null &&
                BattleResultFlowController.Instance.StartDefeatFlow())
                return;

            defeatProcessed = true;

            StopCombat();

            float stageTime = ResolveBattleTime(0f);

            // Thua cung giu nguyen nhung gi da kiem duoc trong tran, khong bi tru.
            // Cach tinh giong het luong thang - chi khac la khong co ruong phan thuong.
            int earnedExp = PlayerStatSystem.Instance != null
                ? PlayerStatSystem.Instance.SessionExpEarned : 0;
            int earnedGold = GoldSystem.Instance != null
                ? GoldSystem.Instance.SessionGoldEarned : 0;

            stageResult = new StageResultData
            {
                stageTime = stageTime,
                earnedExp = earnedExp,
                earnedGold = earnedGold,
                rewards = new System.Collections.Generic.List<RewardData>()
            };

            MarkPlayerInjured();

            // Khong cong lai gi o day: EXP va vang deu da vao tai khoan ngay luc
            // nhat trong tran. Cong them nua se thanh nhan doi.
            ShowDefeatPopup(stageTime, earnedExp);

            Debug.Log("[DEFEAT] Defeat result UI shown.");
        }

        /// <summary>
        /// Danh dau Player bi thuong (Injured) de hoi phuc khi ve lang.
        /// Public cho BattleResultFlowController dung lai khi no tiep quan flow thua.
        /// </summary>
        public void MarkPlayerInjured()
        {
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                var health = player.GetComponent<EternalClash.Character.HealthSystem>();
                int maxHp = health != null ? health.MaxHealth : 100;
                EternalClash.Core.PlayerConditionSystem.Instance?.MarkInjured(maxHp);
            }
            else
            {
                int maxHp = PlayerStatSystem.Instance != null
                    ? PlayerStatSystem.Instance.TotalMaxHealth : 100;
                EternalClash.Core.PlayerConditionSystem.Instance?.MarkInjured(maxHp);
            }
        }

        public void OnDefeatContinueClicked()
        {
            ReturnToVillage();
        }

        private float ResolveBattleTime(float fallback)
        {
            if (StageManager.Instance != null)
                return StageManager.Instance.GetBattleTime();

            var progress = FindObjectOfType<StageProgressController>();
            return progress != null ? progress.GetStageTime() : fallback;
        }
    }

    public class StageResultData
    {
        public float stageTime;
        public int earnedExp;
        public int earnedGold;
        public System.Collections.Generic.List<RewardData> rewards;
    }
}
