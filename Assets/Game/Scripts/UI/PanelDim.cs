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
    private static PanelDimDriver driver;

    private sealed class PanelDimDriver : MonoBehaviour
    {
        private void LateUpdate() => Tick();
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
        Reset();
        EnsureDriver();
    }

    /// <summary>
    /// Bang tu quen nha nen toi (bi Destroy luc dang tat, coroutine dong bang bi
    /// ngat giua chung...) thi nen toi ket lai vinh vien. Driver nay kiem tra
    /// lai moi khung hinh nen khong con phu thuoc vao viec bang co nha dung hay khong.
    /// </summary>
    private static void EnsureDriver()
    {
        if (driver != null)
            return;

        var host = new GameObject("PanelDim (Runtime)") { hideFlags = HideFlags.HideAndDontSave };
        Object.DontDestroyOnLoad(host);
        driver = host.AddComponent<PanelDimDriver>();
    }

    internal static void Tick()
    {
        int before = owners.Count;
        PurgeDeadOwners();

        // Van con dung chu so huu nhung nen toi lai sai trang thai (vd scene vua
        // load voi Nen_toi bat san) thi chinh lai.
        if (before != owners.Count || OverlayStateWrong())
            Apply();
    }

    private static bool OverlayStateWrong()
    {
        return EnsureOverlay() && overlay.gameObject.activeSelf != (owners.Count > 0);
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (mode == LoadSceneMode.Single)
            Reset();
    }

    private static void Reset()
    {
        owners.Clear();

        // Phai TAT nen toi truoc khi quen tham chieu: co panel giu nen toi trong
        // Awake/OnEnable (chay TRUOC buoc reset nay), xoa danh sach ma khong tat
        // thi nen toi ket lai tren man hinh, khong con ai so huu de tat no.
        Apply();

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
        PurgeDeadOwners();

        if (!EnsureOverlay())
            return;

        bool visible = owners.Count > 0;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        if (visible)
            Debug.Log("[PanelDim] Bat nen toi. Dang giu: " + DescribeOwners());
#endif
        overlay.gameObject.SetActive(visible);
        overlay.alpha = visible ? 1f : 0f;
        overlay.interactable = false;
        overlay.blocksRaycasts = visible;

        // Nen toi phai nam DUOI moi panel (sibling dau tien) de khong ve đe
        // len popup dang mo; no van chan tap qua blocksRaycasts.
        if (visible)
            overlay.transform.SetAsFirstSibling();
    }

    /// <summary>
    /// Bo cac panel da bi huy (doi scene, Destroy khi dang tat nen khong co
    /// OnDisable). Con sot lai thi nen toi bi giu mai khong ai tat duoc.
    /// </summary>
    private static void PurgeDeadOwners()
    {
        owners.RemoveWhere(IsDeadOwner);
    }

    private static bool IsDeadOwner(object owner)
    {
        // Da bi huy (doi scene, Destroy).
        if (owner is Object unityObject && unityObject == null)
            return true;

        // Bang da tat thi khong the con "dang mo" - du no quen goi Release.
        if (owner is Component component)
            return !component.gameObject.activeInHierarchy;

        return owner is GameObject panel && !panel.activeInHierarchy;
    }

    private static string DescribeOwners()
    {
        var names = new List<string>();
        foreach (object owner in owners)
            names.Add(owner is Object unityObject ? unityObject.name + " (" + owner.GetType().Name + ")"
                                                  : owner.ToString());
        return names.Count > 0 ? string.Join(", ", names) : "khong ai";
    }

    private static bool EnsureOverlay()
    {
        if (overlay != null)
            return true;

        // Scene khong co lop nen toi: da tim mot lan roi thi thoi, khong quet lai
        // moi khung hinh (driver goi Tick lien tuc).
        if (warned)
            return false;

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
