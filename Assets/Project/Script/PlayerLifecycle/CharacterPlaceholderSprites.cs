using UnityEngine;

namespace PowerMath.PlayerLifecycle
{
    public static class CharacterPlaceholderSprites
    {
        private static Sprite _ricko;
        private static Sprite _stellar;
        public static Sprite Resolve(Sprite authored, string id) => authored != null ? authored : Get(id);
        public static Sprite Get(string id)
        {
            if (id == "ricko") return _ricko != null ? _ricko : (_ricko = Create(new Color(.95f, .48f, .24f), "Ricko placeholder"));
            return _stellar != null ? _stellar : (_stellar = Create(new Color(.46f, .65f, 1f), "Stellar placeholder"));
        }
        private static Sprite Create(Color color, string name)
        {
            const int width = 64, height = 96;
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false) { name = name, filterMode = FilterMode.Bilinear };
            var pixels = new Color[width * height];
            for (int y = 0; y < height; y++)
                for (int x = 0; x < width; x++)
                {
                    bool head = (x - 32) * (x - 32) + (y - 73) * (y - 73) < 15 * 15;
                    bool body = y >= 10 && y < 55 && Mathf.Abs(x - 32) < 11 + (55 - y) / 4;
                    pixels[y * width + x] = head || body ? color : Color.clear;
                }
            texture.SetPixels(pixels);
            texture.Apply(false);
            return Sprite.Create(texture, new Rect(0, 0, width, height), new Vector2(.5f, 0), 96, 0, SpriteMeshType.FullRect);
        }
    }
}
