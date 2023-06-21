using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Drawing;
using dw = System.Drawing;
using System.Drawing.Imaging;

/*
 *  使用此工具之前需先配置好 System.Drawing 相關引用設定
 *  於 Unity 的 Assets 資料夾底下新增名為 csc.rsp (如使用 .NET 3.5 則為 msc.rsp) 的文件
 *  在文件中加入 -r:System.Drawing.dll
 *  儲存後重新啟動 Unity 編輯器。
 *
 *  如果是非 Windows 環境，需要配置動態函式庫路徑
 *  先用文字編輯器開啟 Unity 安裝目錄底下的 MonoBleedingEdge/etc/mono/config
 *  找到 dll=gdiplus 及 dll=gdiplus.dll 的項目
 *  把項目的 target 改成本機安裝的 Mono 函式庫路徑
 *
 *  MacOS: /Library/Frameworks/Mono.framework/Versions/6.6.0/lib/libgdiplus.dylib
 *
 *  注意： 路徑中的 6.6.0 可能會隨著安裝的版本不同而有所改變。
 */
public class GifGenerator : MonoBehaviour
{
    [MenuItem("Assets/Create/GifSheet")]
    public static void CreateGifPlayer()
    {
        // 檢查圖片屬性
        Texture2D texture = Selection.activeObject as Texture2D;
        if (texture == null)
        {
            throw new System.Exception("所選資源非圖片。");
        }
        string path = AssetDatabase.GetAssetPath(texture);
        if (System.IO.Path.GetExtension(path).ToLower() != ".gif")
        {
            throw new System.Exception("需選擇 GIF 圖片。");
        }

        // 讀取圖片影格
        Image image = Image.FromFile(path);
        FrameDimension dimension = new FrameDimension(image.FrameDimensionsList[0]);
        int frameConuts = image.GetFrameCount(dimension);
        if (frameConuts <= 1)
        {
            Debug.Log("此圖片不需要分割。");
            return;
        }

        // 生成適當尺寸的 Atlas
        Vector2Int grids = GetBestGridFitingTwoPower(new Vector2Int(texture.width, texture.height), frameConuts);
        int delay = 0;
        // 將影格繪入新圖片
        using (Bitmap bitmap = new Bitmap(grids.x * texture.width, grids.y * texture.height))
        using (dw.Graphics graphics = dw.Graphics.FromImage(bitmap))
        {
            SpriteMetaData[] sheet = new SpriteMetaData[frameConuts];
            for (int i = 0; i < frameConuts; i++)
            {
                image.SelectActiveFrame(dimension, i);
                Point point = new Point(
                    i % grids.x * texture.width,
                    i / grids.x * texture.height
                );
                sheet[i] = new SpriteMetaData()
                {
                    alignment = (int)SpriteAlignment.Center,
                    name = "frame" + i.ToString(),
                    rect = new Rect(point.X, bitmap.Height - texture.height - point.Y, texture.width, texture.height)
                };

                PropertyItem property = image.GetPropertyItem(0x5100);
                delay += property.Value[0] + property.Value[1] * 256;

                graphics.DrawImage(image, point);
            }
            graphics.Save();
            string newPath = System.IO.Path.ChangeExtension(path, ".png");
            bitmap.Save(newPath, ImageFormat.Png);
            Debug.Log("Save Image:" + newPath);

            AssetDatabase.ImportAsset(newPath, ImportAssetOptions.ForceUpdate);

            // 分割圖片
            TextureImporter importer = AssetImporter.GetAtPath(newPath) as TextureImporter;
            importer.isReadable = true;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Multiple;
            importer.spritesheet = sheet;

            importer.SaveAndReimport();

            // 建立 GifPlayer
            GifPlayer gifImage = new GameObject("Gif Image", typeof(RectTransform), typeof(UnityEngine.UI.Image)).AddComponent<GifPlayer>();

            gifImage.parentTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(newPath);
            gifImage.LoadSpriteSheet();
            gifImage.frameDuration = delay / frameConuts / 100f;

            Canvas canvas = FindObjectOfType<Canvas>();
            if (canvas)
            {
                gifImage.transform.SetParent(canvas.transform);
                ((RectTransform)gifImage.transform).anchoredPosition = Vector2.zero;
                gifImage.GetComponent<UnityEngine.UI.Image>().SetNativeSize();
            }
        }
    }

    private static Vector2Int GetBestGridFitingTwoPower(Vector2Int textureSize, int count)
    {
        Vector2Int size;
        for (int i = 0; i < 16; i++)
        {
            int length = TwoPower(i);
            size = new Vector2Int(length, length);
            if (textureSize.x > size.x) continue;

            int column = size.x / textureSize.x;
            int row = count / column;
            if (count % column != 0) row += 1;

            if (textureSize.y * row <= size.y)
            {
                if (column > count) column = count;
                return new Vector2Int(column, row);
            }
        }

        return default;

        int TwoPower(int times)
        {
            int result = 1;
            for (int i = 0; i < times; i++) result *= 2;
            return result;
        }
    }
}
