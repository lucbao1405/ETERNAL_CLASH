using System;
using UnityEngine;
using UnityEngine.UI;

namespace EternalClash.Monetization
{
    /// <summary>
    /// Popup chung "xem quang cao nhan thuong?" dung cho ca X2 cuoi tran va
    /// hoi sinh giua tran. Mo tren canvas rieng (sortingOrder 500) nen luon
    /// nam tren cac popup khac. Dong bang game do ben goi tu quan ly
    /// (Time.timeScale) - overlay nay chi lam UI.
    /// </summary>
    internal static class OfferOverlayUI
    {
        private static GameObject activeOverlay;

        internal static bool IsOpen => activeOverlay != null;

        /// <param name="onWatchClicked">Duoc goi khi nguoi choi dong y xem ad.
        /// Ben goi se tiep tuc voi AdsService.ShowRewarded.</param>
        /// <param name="autoDeclineSeconds">> 0: khong chon trong thoi gian do
        /// thi panel tu dong coi nhu tu choi (vd offer hoi sinh 5s). 0 = khong
        /// dem nguoc. Chi ap dung cho panel scene.</param>
        internal static void Show(string title, string subtitle, string watchLabel,
            Action onWatchClicked, Action onDeclined, float autoDeclineSeconds = 0f)
        {
            Close();

            // Uu tien panel scene "Ad_Offer" (art theo game, sua truc tiep trong
            // Editor). Scene khong co thi dung overlay sinh runtime ben duoi.
            AdOfferPanel scenePanel = FindScenePanel();
            if (scenePanel != null)
            {
                scenePanel.Present(title, subtitle, watchLabel, autoDeclineSeconds, success =>
                {
                    if (success)
                        onWatchClicked?.Invoke();
                    else
                        onDeclined?.Invoke();
                });
                return;
            }

            GameObject root = MonetizationUI.CreateDimmedRoot(MonetizationUI.OverlayCanvas.transform,
                "OfferOverlay");
            activeOverlay = root;

            MonetizationUI.CreateCenterPanel(root.transform, new Vector2(760f, 620f));
            Transform panel = root.transform.Find("Panel");

            MonetizationUI.CreateLabel(panel, title, 52, Color.white, new Vector2(0f, 180f),
                new Vector2(680f, 120f));
            MonetizationUI.CreateLabel(panel, subtitle, 36, new Color(0.85f, 0.85f, 0.9f),
                new Vector2(0f, 40f), new Vector2(660f, 200f));

            MonetizationUI.CreateButton(panel, watchLabel, new Vector2(0f, -150f),
                new Vector2(520f, 110f), new Color(0.2f, 0.65f, 0.25f), () =>
                {
                    Close();
                    onWatchClicked?.Invoke();
                });

            MonetizationUI.CreateButton(panel, "Skip", new Vector2(0f, -260f),
                new Vector2(520f, 90f), new Color(0.45f, 0.45f, 0.5f), () =>
                {
                    Close();
                    onDeclined?.Invoke();
                });
        }

        private static AdOfferPanel FindScenePanel()
        {
            foreach (AdOfferPanel panel in Resources.FindObjectsOfTypeAll<AdOfferPanel>())
            {
                if (panel != null && panel.gameObject.scene.IsValid())
                    return panel;
            }

            return null;
        }

        internal static void Close()
        {
            AdOfferPanel scenePanel = FindScenePanel();
            if (scenePanel != null)
                scenePanel.Dismiss();

            if (activeOverlay != null)
                UnityEngine.Object.Destroy(activeOverlay);
            activeOverlay = null;
        }
    }
}
