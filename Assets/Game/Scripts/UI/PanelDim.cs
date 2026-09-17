using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Ref-counted full-screen dim shown while any modal panel is open.
/// The overlay is the scene's "Nen_toi" (Town) or "Dim" (Battle) object; it
/// blocks touches to the UI behind the panel. Owners register on panel open
/// (OnEnable) and unregister on close (OnDisable), so the dim hides only when
/// the last panel closes and overlapping panels never fight over it.
/// </summary>
public static class PanelDim
{
    private static readonly HashSet<object> owners = new HashSet<object>();
    private static CanvasGroup overlay;
    private static bool warned;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
        Reset();
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (mode == LoadSceneMode.Single)
            Reset();
    }

    private static void Reset()
    {
        owners.Clear();
        overlay = null;
        warned = false;
    }

    /// <summary>Marks a panel as open; shows the dim if this is the first open panel.</summary>
    public static void Acquire(object owner)
    {
        if (owner == null || !owners.Add(owner))
            return;

        Apply();
    }

    /// <summary>Marks a panel as closed; hides the dim when no panel remains open.</summary>
    public static void Release(object owner)
    {
        if (owner == null || !owners.Remove(owner))
            return;

        Apply();
    }

    private static void Apply()
    {
        if (!EnsureOverlay())
            return;

        bool visible = owners.Count > 0;
        overlay.gameObject.SetActive(visible);
        overlay.alpha = visible ? 1f : 0f;
        overlay.interactable = false;
        overlay.blocksRaycasts = visible;
    }

    private static bool EnsureOverlay()
    {
        if (overlay != null)
            return true;

        Scene scene = SceneManager.GetActiveScene();
        if (!scene.IsValid())
            return false;

        Transform found = FindByName(scene, "Nen_toi") ?? FindByName(scene, "Dim");
        if (found == null)
        {
            if (!warned)
            {
                warned = true;
                Debug.LogWarning("[PanelDim] No dim overlay ('Nen_toi'/'Dim') in scene; panels will not block background taps.");
            }
            return false;
        }

        overlay = found.GetComponent<CanvasGroup>();
        if (overlay == null)
            overlay = found.gameObject.AddComponent<CanvasGroup>();

        return true;
    }

    private static Transform FindByName(Scene scene, string name)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            Transform[] transforms = root.GetComponentsInChildren<Transform>(true);
            foreach (Transform candidate in transforms)
            {
                if (candidate.name == name)
                    return candidate;
            }
        }

        return null;
    }
}
