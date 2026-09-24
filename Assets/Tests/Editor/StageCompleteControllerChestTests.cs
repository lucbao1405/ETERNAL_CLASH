using EternalClash.Stage;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace EternalClash.Tests.EditMode
{
    public class StageCompleteControllerChestTests
    {
        [Test]
        public void FindChestSpawnRect_ResolvesDichdenAndChestSpawn()
        {
            // Ruong trong scene Battle la instance ChestSpawn prefabs da doi ten
            // thanh "Dichden" - finder phai nhan dien duoc ca hai ten.
            GameObject canvas = new GameObject("TestCanvas", typeof(RectTransform), typeof(Canvas));
            try
            {
                GameObject chest = new GameObject("Dichden", typeof(RectTransform), typeof(Image));
                chest.transform.SetParent(canvas.transform, false);

                Assert.AreEqual(chest.GetComponent<RectTransform>(),
                    StageCompleteController.FindChestSpawnRect());

                // Instance khong doi ten van duoc tim thay.
                chest.name = "ChestSpawn";
                Assert.AreEqual(chest.GetComponent<RectTransform>(),
                    StageCompleteController.FindChestSpawnRect());

                // Doi ten khac thi khong tim thay (flow thang chay ngay nhu cu).
                chest.name = "KhongPhaiRuong";
                Assert.IsNull(StageCompleteController.FindChestSpawnRect());
            }
            finally
            {
                Object.DestroyImmediate(canvas);
            }
        }

        [Test]
        public void ResolveChestSprite_TakesSpriteFromMarkerImage()
        {
            // Ruong the gioi troi tren duong dung chinh sprite cua marker UI;
            // marker khong co Image/sprite thi khong co buoc troi.
            GameObject canvas = new GameObject("TestCanvas", typeof(RectTransform), typeof(Canvas));
            try
            {
                Assert.IsNull(StageCompleteController.ResolveChestSprite());

                GameObject chest = new GameObject("Dichden", typeof(RectTransform), typeof(Image));
                chest.transform.SetParent(canvas.transform, false);

                Sprite sprite = Sprite.Create(new Texture2D(8, 8), new Rect(0, 0, 8, 8),
                    new Vector2(0.5f, 0.5f), 100f);
                chest.GetComponent<Image>().sprite = sprite;

                Assert.AreEqual(sprite, StageCompleteController.ResolveChestSprite());

                Object.DestroyImmediate(sprite);
            }
            finally
            {
                Object.DestroyImmediate(canvas);
            }
        }
    }
}
