using UnityEngine;
using UnityEditor;

public class TestSaveSprite
{
    [MenuItem("Tools/导出精灵")]
    static void SaveSprite()
    {
        foreach (Object obj in Selection.objects)
        {
            string selectionPath = AssetDatabase.GetAssetPath(obj);
            var allAssets = AssetDatabase.LoadAllAssetsAtPath(selectionPath);
            foreach (var asset in allAssets)
            {
                if (asset is Sprite)
                {
                    // 创建导出文件夹
                    string outPath = Application.dataPath + "/outSprite/" + System.IO.Path.GetFileNameWithoutExtension(selectionPath);
                    if (!System.IO.Directory.Exists(outPath))
                    {
                        System.IO.Directory.CreateDirectory(outPath);
                    }

                    var sprite = asset as Sprite;
                    int width = Mathf.CeilToInt(sprite.rect.width);
                    int height = Mathf.CeilToInt(sprite.rect.height);

                    // 创建临时GameObject和SpriteRenderer
                    GameObject go = new GameObject("TempSpriteRenderer");
                    var sr = go.AddComponent<SpriteRenderer>();
                    sr.sprite = sprite;

                    // 创建正交Camera
                    GameObject camGo = new GameObject("TempCamera");
                    var cam = camGo.AddComponent<Camera>();
                    cam.orthographic = true;
                    cam.clearFlags = CameraClearFlags.Color;
                    cam.backgroundColor = new Color(0, 0, 0, 0);
                    cam.cullingMask = 1 << 0; // 默认层

                    // 设置GameObject到(0,0,0)，Camera到(0,0,-10)
                    go.transform.position = Vector3.zero;
                    cam.transform.position = new Vector3(0, 0, -10);

                    // 计算Sprite实际尺寸（单位：世界坐标）
                    float pixelsPerUnit = sprite.pixelsPerUnit;
                    float worldWidth = width / pixelsPerUnit;
                    float worldHeight = height / pixelsPerUnit;

                    // 设置Camera正交尺寸，确保Sprite完整显示
                    cam.orthographicSize = worldHeight / 2f;
                    float aspect = (float)width / height;
                    cam.aspect = aspect;

                    // 创建RenderTexture
                    RenderTexture rt = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
                    cam.targetTexture = rt;

                    // 渲染一帧
                    cam.Render();

                    // 读取像素
                    RenderTexture prev = RenderTexture.active;
                    RenderTexture.active = rt;
                    Texture2D tex = new Texture2D(width, height, TextureFormat.ARGB32, false);
                    tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                    tex.Apply();
                    RenderTexture.active = prev;

                    // 写入PNG
                    System.IO.File.WriteAllBytes(outPath + "/" + sprite.name + ".png", tex.EncodeToPNG());

                    // 清理
                    Object.DestroyImmediate(go);
                    Object.DestroyImmediate(camGo);
                    Object.DestroyImmediate(rt);
                    Object.DestroyImmediate(tex);
                }
            }
        }
    }
}