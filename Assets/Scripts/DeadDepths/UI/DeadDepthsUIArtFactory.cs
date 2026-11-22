using System.Collections.Generic;
using UnityEngine;

namespace DeadDepths.UI
{
    /// <summary>
    /// Generates crisp anti-aliased SDF procedural sprites and vector icons at runtime
    /// with HideFlags.HideAndDontSave for publish-grade uGUI panels, bars, rings, and icons.
    /// </summary>
    public static class DeadDepthsUIArtFactory
    {
        private static readonly List<Object> createdAssets = new List<Object>();

        public static Sprite ChamferedCardSprite { get; private set; }
        public static Sprite BarTrackSprite { get; private set; }
        public static Sprite BarFillSprite { get; private set; }
        public static Sprite CircleRingSprite { get; private set; }
        public static Sprite CircleSolidSprite { get; private set; }
        public static Sprite RadialVignetteSprite { get; private set; }
        public static Sprite HorizontalScrimSprite { get; private set; }
        public static Sprite RankSealSprite { get; private set; }

        public static Sprite IconBandage { get; private set; }
        public static Sprite IconMedkit { get; private set; }
        public static Sprite IconAdrenaline { get; private set; }
        public static Sprite IconSupplyIntel { get; private set; }
        public static Sprite IconFlashlight { get; private set; }
        public static Sprite IconHelipad { get; private set; }
        public static Sprite IconAim { get; private set; }

        public static void EnsureInitialized()
        {
            if (ChamferedCardSprite != null) return;

            ChamferedCardSprite = CreateChamferedCardSprite();
            BarTrackSprite = CreateBarTrackSprite();
            BarFillSprite = CreateBarFillSprite();
            CircleRingSprite = CreateCircleRingSprite();
            CircleSolidSprite = CreateCircleSolidSprite();
            RadialVignetteSprite = CreateRadialVignetteSprite();
            HorizontalScrimSprite = CreateHorizontalScrimSprite();
            RankSealSprite = CreateRankSealSprite();

            IconBandage = CreateBandageIcon();
            IconMedkit = CreateMedkitIcon();
            IconAdrenaline = CreateAdrenalineIcon();
            IconSupplyIntel = CreateSupplyIntelIcon();
            IconFlashlight = CreateFlashlightIcon();
            IconHelipad = CreateHelipadIcon();
            IconAim = CreateAimIcon();
        }

        public static void DisposeAll()
        {
            for (int i = 0; i < createdAssets.Count; i++)
            {
                if (createdAssets[i] != null)
                {
                    if (Application.isPlaying) Object.Destroy(createdAssets[i]);
                    else Object.DestroyImmediate(createdAssets[i]);
                }
            }
            createdAssets.Clear();

            ChamferedCardSprite = null;
            BarTrackSprite = null;
            BarFillSprite = null;
            CircleRingSprite = null;
            CircleSolidSprite = null;
            RadialVignetteSprite = null;
            HorizontalScrimSprite = null;
            RankSealSprite = null;
            IconBandage = null;
            IconMedkit = null;
            IconAdrenaline = null;
            IconSupplyIntel = null;
            IconFlashlight = null;
            IconHelipad = null;
            IconAim = null;
        }

        private static Texture2D NewTexture(int w, int h, FilterMode filter = FilterMode.Bilinear)
        {
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false)
            {
                hideFlags = HideFlags.HideAndDontSave,
                wrapMode = TextureWrapMode.Clamp,
                filterMode = filter
            };
            createdAssets.Add(tex);
            return tex;
        }

        private static Sprite MakeSprite(Texture2D tex, Vector4 border = default)
        {
            Sprite sp = Sprite.Create(
                tex,
                new Rect(0, 0, tex.width, tex.height),
                new Vector2(0.5f, 0.5f),
                100f,
                0,
                SpriteMeshType.FullRect,
                border);
            sp.hideFlags = HideFlags.HideAndDontSave;
            createdAssets.Add(sp);
            return sp;
        }

        private static float SdfChamferBox(Vector2 p, Vector2 halfSize, float chamfer)
        {
            Vector2 d = new Vector2(Mathf.Abs(p.x), Mathf.Abs(p.y)) - halfSize;
            float boxDist = Mathf.Max(d.x, d.y);
            float diagDist = (d.x + d.y + chamfer) * 0.70710678f;
            return Mathf.Max(boxDist, diagDist);
        }

        private static Sprite CreateChamferedCardSprite()
        {
            const int size = 128;
            Texture2D tex = NewTexture(size, size);
            Color[] px = new Color[size * size];

            Color fillTop = new Color(0.082f, 0.102f, 0.130f, 0.90f);
            Color fillBot = new Color(0.051f, 0.063f, 0.078f, 0.92f);
            Color borderCol = new Color(0.165f, 0.196f, 0.239f, 1.0f);
            Color topHighlight = new Color(1f, 1f, 1f, 0.10f);

            Vector2 half = new Vector2(size * 0.5f - 2f, size * 0.5f - 2f);
            for (int y = 0; y < size; y++)
            {
                float ny = y / (float)(size - 1);
                Color body = Color.Lerp(fillBot, fillTop, ny);
                for (int x = 0; x < size; x++)
                {
                    Vector2 p = new Vector2(x + 0.5f - size * 0.5f, y + 0.5f - size * 0.5f);
                    float sd = SdfChamferBox(p, half, 12f);

                    float outerAlpha = Mathf.Clamp01(0.5f - sd);
                    float borderMask = Mathf.Clamp01(1f - Mathf.Abs(sd + 1.5f) * 0.6f);

                    Color c = Color.Lerp(body, borderCol, borderMask * 0.95f);
                    if (y >= size - 6 && y <= size - 3 && sd < -1.5f)
                    {
                        c = Color.Lerp(c, Color.white, topHighlight.a);
                    }
                    c.a *= outerAlpha;
                    px[y * size + x] = c;
                }
            }

            tex.SetPixels(px);
            tex.Apply();
            return MakeSprite(tex, new Vector4(24, 24, 24, 24));
        }

        private static Sprite CreateBarTrackSprite()
        {
            const int w = 64, h = 32;
            Texture2D tex = NewTexture(w, h);
            Color[] px = new Color[w * h];
            Vector2 half = new Vector2(w * 0.5f - 1.5f, h * 0.5f - 1.5f);

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    Vector2 p = new Vector2(x + 0.5f - w * 0.5f, y + 0.5f - h * 0.5f);
                    float sd = SdfChamferBox(p, half, 4f);
                    float alpha = Mathf.Clamp01(0.5f - sd);
                    float border = Mathf.Clamp01(1f - Mathf.Abs(sd + 1.2f) * 0.75f);
                    Color c = Color.Lerp(new Color(0.05f, 0.06f, 0.09f, 0.88f), new Color(0.18f, 0.22f, 0.28f, 0.96f), border);
                    c.a *= alpha;
                    px[y * w + x] = c;
                }
            }

            tex.SetPixels(px);
            tex.Apply();
            return MakeSprite(tex, new Vector4(10, 8, 10, 8));
        }

        private static Sprite CreateBarFillSprite()
        {
            const int w = 64, h = 32;
            Texture2D tex = NewTexture(w, h);
            Color[] px = new Color[w * h];

            for (int y = 0; y < h; y++)
            {
                float ny = y / (float)(h - 1);
                float shade = Mathf.Lerp(0.78f, 1.0f, ny);
                float topGlow = ny > 0.78f ? 0.14f : 0f;
                for (int x = 0; x < w; x++)
                {
                    Color c = new Color(Mathf.Min(1f, shade + topGlow), Mathf.Min(1f, shade + topGlow), Mathf.Min(1f, shade + topGlow), 1f);
                    px[y * w + x] = c;
                }
            }

            tex.SetPixels(px);
            tex.Apply();
            return MakeSprite(tex, new Vector4(6, 4, 6, 4));
        }

        private static Sprite CreateCircleRingSprite()
        {
            const int size = 128;
            Texture2D tex = NewTexture(size, size);
            Color[] px = new Color[size * size];
            Vector2 c = new Vector2(0.5f, 0.5f);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float d = Vector2.Distance(new Vector2((x + 0.5f) / size, (y + 0.5f) / size), c);
                    float ringDist = Mathf.Abs(d - 0.42f);
                    float a = Mathf.Clamp01((0.055f - ringDist) * 45f);
                    px[y * size + x] = new Color(1f, 1f, 1f, a);
                }
            }

            tex.SetPixels(px);
            tex.Apply();
            return MakeSprite(tex);
        }

        private static Sprite CreateCircleSolidSprite()
        {
            const int size = 64;
            Texture2D tex = NewTexture(size, size);
            Color[] px = new Color[size * size];
            Vector2 c = new Vector2(0.5f, 0.5f);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float d = Vector2.Distance(new Vector2((x + 0.5f) / size, (y + 0.5f) / size), c);
                    float a = Mathf.Clamp01((0.46f - d) * 32f);
                    px[y * size + x] = new Color(1f, 1f, 1f, a);
                }
            }

            tex.SetPixels(px);
            tex.Apply();
            return MakeSprite(tex);
        }

        private static Sprite CreateRadialVignetteSprite()
        {
            const int size = 256;
            Texture2D tex = NewTexture(size, size);
            Color[] px = new Color[size * size];
            Vector2 c = new Vector2(0.5f, 0.5f);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    Vector2 uv = new Vector2((x + 0.5f) / size, (y + 0.5f) / size);
                    float d = Vector2.Distance(uv, c) * 1.42f;
                    float a = Mathf.Pow(Mathf.Clamp01((d - 0.38f) / 0.62f), 1.8f);
                    px[y * size + x] = new Color(0.85f, 0.08f, 0.08f, a);
                }
            }

            tex.SetPixels(px);
            tex.Apply();
            return MakeSprite(tex);
        }

        private static Sprite CreateHorizontalScrimSprite()
        {
            const int w = 256, h = 16;
            Texture2D tex = NewTexture(w, h);
            Color[] px = new Color[w * h];

            for (int x = 0; x < w; x++)
            {
                float t = x / (float)(w - 1);
                float alpha = Mathf.Pow(1f - t, 1.45f) * 0.94f;
                Color col = new Color(0.035f, 0.045f, 0.065f, alpha);
                for (int y = 0; y < h; y++)
                {
                    px[y * w + x] = col;
                }
            }

            tex.SetPixels(px);
            tex.Apply();
            return MakeSprite(tex);
        }

        private static Sprite CreateRankSealSprite()
        {
            const int size = 128;
            Texture2D tex = NewTexture(size, size);
            Color[] px = new Color[size * size];
            Vector2 half = new Vector2(54f, 54f);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    Vector2 p = new Vector2(x + 0.5f - 64f, y + 0.5f - 64f);
                    float sd = SdfChamferBox(p, half, 26f);
                    float alpha = Mathf.Clamp01(0.5f - sd);
                    float ring = Mathf.Clamp01(1f - Mathf.Abs(sd + 4f) * 0.55f);
                    Color c = Color.Lerp(new Color(0.12f, 0.14f, 0.18f, 0.92f), Color.white, ring * 0.85f);
                    c.a *= alpha;
                    px[y * size + x] = c;
                }
            }

            tex.SetPixels(px);
            tex.Apply();
            return MakeSprite(tex);
        }

        private static Sprite CreateBandageIcon()
        {
            return DrawIcon(64, (x, y) =>
            {
                Vector2 p = new Vector2(x - 32f, y - 32f);
                // Rotated bandage strip + center pad
                float rad = 45f * Mathf.Deg2Rad;
                Vector2 rp = new Vector2(p.x * Mathf.Cos(rad) - p.y * Mathf.Sin(rad), p.x * Mathf.Sin(rad) + p.y * Mathf.Cos(rad));
                float strip = Mathf.Max(Mathf.Abs(rp.x) - 22f, Mathf.Abs(rp.y) - 8f);
                if (strip > 0.5f) return Color.clear;
                bool isPad = Mathf.Abs(rp.x) < 7f && Mathf.Abs(rp.y) < 7f;
                float a = Mathf.Clamp01(0.5f - strip);
                Color col = isPad ? new Color(1f, 0.96f, 0.90f, a) : new Color(0.86f, 0.80f, 0.72f, a);
                return col;
            });
        }

        private static Sprite CreateMedkitIcon()
        {
            return DrawIcon(64, (x, y) =>
            {
                Vector2 p = new Vector2(x - 32f, y - 32f);
                float box = SdfChamferBox(p, new Vector2(21f, 16f), 4f);
                if (box > 1f) return Color.clear;
                float a = Mathf.Clamp01(0.5f - box);
                bool cross = (Mathf.Abs(p.x) < 3.5f && Mathf.Abs(p.y) < 10f) || (Mathf.Abs(p.y) < 3.5f && Mathf.Abs(p.x) < 10f);
                return cross ? new Color(0.95f, 0.25f, 0.25f, a) : new Color(0.92f, 0.95f, 0.98f, a);
            });
        }

        private static Sprite CreateAdrenalineIcon()
        {
            return DrawIcon(64, (x, y) =>
            {
                Vector2 p = new Vector2(x - 32f, y - 32f);
                // Lightning bolt silhouette
                bool upper = p.y >= 0f && p.y <= 22f && Mathf.Abs(p.x - (p.y * 0.35f - 2f)) < (6f - p.y * 0.15f);
                bool lower = p.y < 0f && p.y >= -22f && Mathf.Abs(p.x - (p.y * 0.35f + 2f)) < (6f + p.y * 0.15f);
                bool bridge = Mathf.Abs(p.y) < 4f && Mathf.Abs(p.x) < 9f;
                if (upper || lower || bridge)
                    return new Color(0.32f, 0.88f, 1.0f, 1f);
                return Color.clear;
            });
        }

        private static Sprite CreateSupplyIntelIcon()
        {
            return DrawIcon(64, (x, y) =>
            {
                Vector2 p = new Vector2(x - 32f, y - 32f);
                float box = SdfChamferBox(p, new Vector2(18f, 18f), 4f);
                if (box > 1f) return Color.clear;
                float a = Mathf.Clamp01(0.5f - box);
                bool band = Mathf.Abs(p.y - 4f) < 2.2f || Mathf.Abs(p.x) < 2.2f;
                return band ? new Color(1f, 0.85f, 0.28f, a) : new Color(0.85f, 0.65f, 0.20f, a);
            });
        }

        private static Sprite CreateFlashlightIcon()
        {
            return DrawIcon(64, (x, y) =>
            {
                Vector2 p = new Vector2(x - 32f, y - 32f);
                bool handle = p.x >= -20f && p.x <= 4f && Mathf.Abs(p.y) < 5.5f;
                bool head = p.x > 4f && p.x <= 14f && Mathf.Abs(p.y) < (5.5f + (p.x - 4f) * 0.45f);
                bool beam = p.x > 16f && p.x <= 26f && Mathf.Abs(p.y) < (p.x - 10f) * 0.65f;
                if (handle || head) return new Color(0.92f, 0.95f, 1f, 1f);
                if (beam) return new Color(1f, 0.90f, 0.45f, 0.7f);
                return Color.clear;
            });
        }

        private static Sprite CreateHelipadIcon()
        {
            return DrawIcon(64, (x, y) =>
            {
                Vector2 p = new Vector2(x - 32f, y - 32f);
                float dist = p.magnitude;
                bool ring = Mathf.Abs(dist - 22f) < 2.5f;
                bool leftBar = Mathf.Abs(p.x + 7f) < 2.6f && Mathf.Abs(p.y) < 12f;
                bool rightBar = Mathf.Abs(p.x - 7f) < 2.6f && Mathf.Abs(p.y) < 12f;
                bool midBar = Mathf.Abs(p.y) < 2.6f && Mathf.Abs(p.x) <= 7f;
                if (ring || leftBar || rightBar || midBar) return new Color(0.28f, 0.95f, 0.55f, 1f);
                return Color.clear;
            });
        }

        private static Sprite CreateAimIcon()
        {
            return DrawIcon(64, (x, y) =>
            {
                Vector2 p = new Vector2(x - 32f, y - 32f);
                float dist = p.magnitude;
                bool ring = Mathf.Abs(dist - 16f) < 2.2f;
                bool tickH = Mathf.Abs(p.y) < 1.8f && Mathf.Abs(p.x) > 8f && Mathf.Abs(p.x) < 24f;
                bool tickV = Mathf.Abs(p.x) < 1.8f && Mathf.Abs(p.y) > 8f && Mathf.Abs(p.y) < 24f;
                bool dot = dist < 3f;
                if (ring || tickH || tickV || dot) return new Color(1f, 0.85f, 0.35f, 1f);
                return Color.clear;
            });
        }

        private static Sprite DrawIcon(int size, System.Func<int, int, Color> pixelFunc)
        {
            Texture2D tex = NewTexture(size, size);
            Color[] px = new Color[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    px[y * size + x] = pixelFunc(x, y);
                }
            }
            tex.SetPixels(px);
            tex.Apply();
            return MakeSprite(tex);
        }
    }
}
