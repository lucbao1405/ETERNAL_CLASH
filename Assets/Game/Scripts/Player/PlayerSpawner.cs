using UnityEngine;
using UnityEngine.Events;
using EternalClash.Skill;

namespace EternalClash.Player
{
    /// <summary>
    /// Handles spawning player prefab and moving player into combat position.
    /// </summary>
    public class PlayerSpawner : MonoBehaviour
    {
        [SerializeField] private GameObject playerPrefab;
        [SerializeField] private Transform spawnPoint;
        [SerializeField] private Transform combatPosition;
        [SerializeField] private Camera targetCamera;
        [SerializeField] private float outsideScreenOffset = 1f;

        [Header("Spawn Movement")]
        [SerializeField] private float moveTargetX = -1f;
        [SerializeField] private float moveSpeed = 5f;
        [SerializeField] private bool moveAfterSpawn = false;
        [SerializeField] private UnityEvent onPlayerReady;

        private GameObject currentPlayer;

        public GameObject CurrentPlayer => currentPlayer;

        private void Start()
        {
            SpawnPlayer();
        }

        public void SpawnPlayer()
        {
            if (playerPrefab == null || spawnPoint == null)
            {
                Debug.LogWarning("PlayerSpawner missing reference");
                return;
            }

            if (targetCamera == null)
                targetCamera = Camera.main;

            Vector3 spawnPosition = spawnPoint.position;
            if (targetCamera != null)
            {
                float cameraLeft = targetCamera.transform.position.x - targetCamera.orthographicSize * targetCamera.aspect;
                spawnPosition.x = cameraLeft - outsideScreenOffset;
            }

            currentPlayer = Instantiate(
                playerPrefab,
                spawnPosition,
                Quaternion.identity
            );

            SkillManager skillManager = currentPlayer.GetComponent<SkillManager>();
            if (skillManager == null)
                skillManager = currentPlayer.AddComponent<SkillManager>();
            skillManager.EnsureSkills();

            // Nut danh (tap/Space) phai luon ton tai tren player spawn runtime.
            if (currentPlayer.GetComponent<InputController>() == null)
                currentPlayer.AddComponent<InputController>();

            EternalClash.Village.EquipmentVisualBinder visualBinder =
                currentPlayer.GetComponent<EternalClash.Village.EquipmentVisualBinder>();
            if (visualBinder == null)
                visualBinder = currentPlayer.AddComponent<EternalClash.Village.EquipmentVisualBinder>();
            visualBinder.ApplySavedEquipment();

            PlayerIntroController intro = currentPlayer.GetComponent<PlayerIntroController>();
            if (intro == null)
                intro = currentPlayer.AddComponent<PlayerIntroController>();

            Vector3 target = combatPosition != null
                ? combatPosition.position
                : new Vector3(moveTargetX, spawnPosition.y, spawnPosition.z);
            target.y = spawnPosition.y;
            intro.BeginIntro(spawnPosition, target, moveSpeed);
            Debug.Log("[PLAYER INTRO] Spawn: " + spawnPosition + " -> Combat: " + target);
            StartCoroutine(WaitForPlayerReady(intro));
        }

        private System.Collections.IEnumerator WaitForPlayerReady(PlayerIntroController intro)
        {
            while (!intro.combatReady)
                yield return null;

            onPlayerReady?.Invoke();
        }
    }
}
