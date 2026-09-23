using System.Collections;
using UnityEngine;
using Spine.Unity;
using EternalClash.Character;
using EternalClash.Combat;
using EternalClash.Dialogue;
using EternalClash.Player;
using EternalClash.Story;
using EternalClash.Wave;
using EternalClash.World;

namespace EternalClash.Stage
{
    /// <summary>
    /// End-of-map meeting: after the last wave is cleared the world keeps
    /// scrolling, the field NPC stands waiting further down the road, the hero
    /// runs up to them, the dialogue plays, and only then the victory flow
    /// (chest -> win popup) continues back to the village.
    /// </summary>
    public class FieldMeetingController : MonoBehaviour
    {
        public static FieldMeetingController Instance { get; private set; }

        private const float NpcAheadDistance = 8f;   // world units ahead of the hero
        private const float MeetDistance = 2.3f;     // stop the run once this close
        private const float MaxRunSeconds = 6f;      // safety cap for the approach
        private const float NoNpcRunSeconds = 2.4f;  // story stages without a body
        private const float NpcGroundSpeed = 2.5f;   // matches WorldScroller.groundSpeed

        private StageCompleteController owner;
        private int stageIndex;
        private bool running;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void InstallSceneHook()
        {
            UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoaded;
            UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;
            EnsureInBattle();
        }

        private static void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene,
            UnityEngine.SceneManagement.LoadSceneMode mode)
        {
            EnsureInBattle();
        }

        private static void EnsureInBattle()
        {
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if (!scene.IsValid() ||
                !string.Equals(scene.name, "Battle", System.StringComparison.OrdinalIgnoreCase))
                return;

            if (FindObjectOfType<FieldMeetingController>() != null)
                return;

            new GameObject("FieldMeetingController (Runtime)")
                .AddComponent<FieldMeetingController>();
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

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        public static void Begin(int stage, StageCompleteController stageOwner)
        {
            if (Instance == null)
                EnsureInBattle();
            if (Instance == null)
            {
                stageOwner.ProceedToVictoryFlow(); // fallback: no meeting possible
                return;
            }

            Instance.StartMeeting(stage, stageOwner);
        }

        private void StartMeeting(int stage, StageCompleteController stageOwner)
        {
            if (running) return;

            owner = stageOwner;
            stageIndex = stage;
            running = true;

            Debug.Log($"[FieldMeeting] Stage {stage} meeting begins — hero keeps running down the road.");
            PartialStopCombat();
            StartCoroutine(MeetingRoutine());
        }

        /// <summary>
        /// Stops everything hostile but keeps the world scrolling and the hero
        /// running so they can physically reach the NPC down the road.
        /// </summary>
        private static void PartialStopCombat()
        {
            var waveManager = FindObjectOfType<WaveManager>();
            if (waveManager != null) waveManager.enabled = false;

            var encounterSpawner = FindObjectOfType<EncounterSpawner>();
            if (encounterSpawner != null) encounterSpawner.enabled = false;

            var loopSpawner = FindObjectOfType<WorldLoopSpawner>();
            if (loopSpawner != null) loopSpawner.enabled = false;

            var player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                var combat = player.GetComponent<CombatController>();
                if (combat != null) combat.enabled = false;
            }
        }

        private IEnumerator MeetingRoutine()
        {
            GameObject hero = GameObject.FindGameObjectWithTag("Player");
            if (hero == null)
            {
                FinishMeeting();
                yield break;
            }

            GameObject npc = ActivateMeetingNpc(hero);

            if (npc != null)
            {
                // The hero is screen-locked (AutoRunner snaps X back home), the
                // world scrolls past. So the waiting NPC walks in with the
                // ground speed until the hero "reaches" them.
                float elapsed = 0f;
                while (elapsed < MaxRunSeconds)
                {
                    elapsed += Time.deltaTime;
                    float gap = npc.transform.position.x - hero.transform.position.x;
                    if (gap <= MeetDistance)
                        break;

                    npc.transform.position += Vector3.left * NpcGroundSpeed * Time.deltaTime;
                    yield return null;
                }
            }
            else
            {
                // Story stage / stage 7 / missing spine: a short walk down the
                // empty road, then the dialogue carries the moment.
                yield return new WaitForSeconds(NoNpcRunSeconds);
            }

            // Hero dung lai (stand) truoc NPC, bat dau hoi thoai. Chi sau khi
            // hoi thoai dong, ProceedToVictoryFlow moi mo animation victory.
            var scroller = FindObjectOfType<WorldScroller>();
            if (scroller != null)
            {
                scroller.StopScroll();
                scroller.SetSpeedMultiplier(0f);
            }

            if (hero != null)
            {
                var runner = hero.GetComponent<AutoRunner>();
                if (runner != null) runner.StopRunning();
            }

            yield return new WaitForSeconds(0.4f);

            if (owner != null)
                owner.PlayMeetingDialogue(stageIndex);
            // owner continues the victory flow when the dialogue closes.
        }

        /// <summary>
        /// Activates the pre-placed field NPC for this stage ahead of the hero,
        /// scaled to the hero's height and facing them. Returns null when this
        /// stage has no body on the road.
        /// </summary>
        private GameObject ActivateMeetingNpc(GameObject hero)
        {
            // Phai dung cung so man voi StoryManager.HasNPCEncounter:
            // 1=Garen, 2=Elara, 3=Ela. Sai so man thi NPC khong bao gio xuat
            // hien (chi co hoi thoai khong than).
            string npcGoName = stageIndex switch
            {
                1 => "FieldNPC_Garen",
                2 => "FieldNPC_Elara",
                3 => "FieldNPC_Ela",
                _ => null
            };

            if (npcGoName == null)
                return null;

            Transform container = GameObject.Find("FieldNPCs")?.transform;
            Transform npc = container != null ? container.Find(npcGoName) : null;
            if (npc == null)
            {
                Debug.LogWarning($"[FieldMeeting] '{npcGoName}' not found in Battle scene; dialogue-only meeting.");
                return null;
            }

            Vector3 position = CombatLaneY.AlignToPlayerY(
                hero.transform.position + Vector3.right * NpcAheadDistance);
            npc.gameObject.SetActive(true);
            npc.position = position;

            MatchHeroHeightAndFaceHero(npc, hero);

            // All field NPC spines (thoren, phuthuylonton, con vo) have a
            // "stand" idle — play it so they don't freeze in the bind pose.
            SkeletonAnimation skeleton = npc.GetComponent<SkeletonAnimation>();
            if (skeleton != null && skeleton.skeleton != null &&
                skeleton.skeleton.Data.FindAnimation("stand") != null)
            {
                skeleton.AnimationState.SetAnimation(0, "stand", true);
            }

            // Spine "con vo" (Ela) duoc ve quay PHAI (mat nam phia ben phai
            // khoi toc trong atlas) - khac Garen/Elara quay trai. Lat bang
            // ScaleX cua chinh skeleton Spine, khong dung localScale am vi no
            // lat quanh pivot cua Transform va de lam lech vi tri hien thi.
            if (skeleton != null && skeleton.skeleton != null && stageIndex == 3)
                skeleton.skeleton.ScaleX = -1f;

            // Field NPC khong co tag Player/Enemy nen CharacterShadowBootstrapper
            // bo qua; them bong giong nhan vat de NPC khong troi khoi mat dat.
            if (npc.GetComponent<CharacterShadow>() == null)
                npc.gameObject.AddComponent<CharacterShadow>();

            return npc.gameObject;
        }

        /// <summary>
        /// Scales the NPC so they stand eye-to-eye with the hero. Cac spine NPC
        /// deu duoc ve quay mat vao duong (trai, ve phia nhan vat chay den)
        /// giong nhau - ke ca Ela - nen khong lat khung hinh NPC nao.
        /// </summary>
        private void MatchHeroHeightAndFaceHero(Transform npc, GameObject hero)
        {
            SkeletonAnimation npcSkeleton = npc.GetComponent<SkeletonAnimation>();
            if (npcSkeleton == null || npcSkeleton.skeletonDataAsset == null)
                return;

            Spine.SkeletonData npcData = npcSkeleton.skeletonDataAsset.GetSkeletonData(true);
            if (npcData == null || npcData.Height <= 0f)
                return;

            float heroWorldHeight = 2f; // sane fallback if the hero has no spine
            SkeletonAnimation heroSkeleton = hero.GetComponentInChildren<SkeletonAnimation>();
            if (heroSkeleton != null && heroSkeleton.skeletonDataAsset != null)
            {
                Spine.SkeletonData heroData = heroSkeleton.skeletonDataAsset.GetSkeletonData(true);
                if (heroData != null && heroData.Height > 0f)
                    heroWorldHeight = heroData.Height * Mathf.Abs(heroSkeleton.transform.localScale.y);
            }

            float scale = (heroWorldHeight * 1.05f) / npcData.Height;
            // Scale duong cho moi NPC; rieng huong mat xu ly rieng o
            // ActivateMeetingNpc (Ela bi lat bang skeleton.ScaleX = -1).
            npc.localScale = new Vector3(Mathf.Abs(scale), Mathf.Abs(scale), 1f);
        }

        /// <summary>Called by StageCompleteController when the dialogue closes.</summary>
        public void NotifyDialogueClosed()
        {
            FinishMeeting();
        }

        private void FinishMeeting()
        {
            running = false;
            if (owner != null)
                owner.ProceedToVictoryFlow();
            owner = null;
        }
    }
}
