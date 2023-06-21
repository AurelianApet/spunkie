using UnityEngine;
using UnityEngine.UI;

public class GifPlayer : MonoBehaviour
{
    public Image targetImage;
    public float frameDuration = 0.5f;
    public bool loop = true;

    [SerializeField]
    public Sprite[] sprites;

    float timer;
    public int index;

    // Update is called once per frame
    void Update()
    {
        timer += Time.deltaTime;
        if (timer > frameDuration)
        {
            timer -= frameDuration;
            index += 1;
            if (index >= sprites.Length)
            {
                if (loop)
                {
                    index -= sprites.Length;
                } else
                {
                    enabled = false;
                    return;
                }
            }
            targetImage.sprite = sprites[index];
        }
    }

    public void Play()
    {
        timer = 0;
        index = 0;
        targetImage.sprite = sprites[0];
        enabled = true;
    }

    private void Reset()
    {
        targetImage = GetComponent<Image>();
    }

#if UNITY_EDITOR
    [Header("Editor"), SerializeField]
    public Texture2D parentTexture;

    [ContextMenu("Load Sprites From Texture")]
    public void LoadSpriteSheet()
    {
        if (parentTexture == null) return;
        string path = UnityEditor.AssetDatabase.GetAssetPath(parentTexture);
        object[] assets = UnityEditor.AssetDatabase.LoadAllAssetsAtPath(path);
        System.Collections.Generic.List<Sprite> spritesList = new System.Collections.Generic.List<Sprite>();
        foreach (object asset in assets)
        {
            Sprite sprite = asset as Sprite;
            if (sprite) spritesList.Add(sprite);
        }
        sprites = spritesList.ToArray();

        targetImage.sprite = sprites[0];
    }
#endif
}
