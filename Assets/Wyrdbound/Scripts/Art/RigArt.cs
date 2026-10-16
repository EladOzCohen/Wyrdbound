using System.Collections.Generic;
using UnityEngine;

namespace Wyrdbound
{
    public sealed class RigPartDef
    {
        public string Name, Parent;
        public Sprite Sprite;
        public Vector2 Pos;
        public int Order;
        public float Scale = 1f;
    }

    /// <summary>A character broken into jointed parts, ready to be animated by Rig.</summary>
    public sealed class RigDef
    {
        public RigKind Kind;
        public string Id;
        public readonly List<RigPartDef> Parts = new List<RigPartDef>();
        public float LegH, TorH, HeadR, Scale = 1f, Hunch;
        public bool ArmsUp, Robe, Cape, Hair, Wings;
        public int CapeSegs, TatterCount, SerpentSegs;
        public float SegSpacing;

        public RigPartDef Add(string name, string parent, Sprite s, Vector2 pos, int order, float scale = 1f)
        {
            var p = new RigPartDef { Name = name, Parent = parent, Sprite = s, Pos = pos, Order = order, Scale = scale };
            Parts.Add(p);
            return p;
        }
    }

    /// <summary>
    /// Paints characters as cut-out parts (thigh, shin, torso, head, arms, cape segments, items...),
    /// each pivoted at its joint, at 2x resolution with rim light and core shadow.
    /// </summary>
    public static class RigArt
    {
        const float RS = 2f;
        const float PU = 1f / 64f;
        static readonly Dictionary<string, RigDef> cache = new Dictionary<string, RigDef>();

        public static RigDef Get(string id)
        {
            if (cache.TryGetValue(id, out var d)) return d;
            var kind = Specs.Kind(id);
            switch (kind)
            {
                case RigKind.Humanoid: d = Humanoid(id, Specs.Hum(id)); break;
                case RigKind.Quad: d = Quadruped(id, Specs.Quad(id)); break;
                case RigKind.Bird: d = Bird(id); break;
                case RigKind.Serpent: d = Serpent(id); break;
                case RigKind.Ghost: d = Ghost(id); break;
                case RigKind.Blob: d = Blob(id); break;
                case RigKind.Bell: d = Bell(id); break;
                default:
                    d = new RigDef { Kind = RigKind.Static, Id = id };
                    d.Add("root", null, null, Vector2.zero, 0);
                    d.Add("body", "root", CharArt.Get(id), Vector2.zero, 0);
                    break;
            }
            d.Id = id;
            cache[id] = d;
            return d;
        }

        static Pix C(float w, float h) => new Pix(Mathf.CeilToInt(w), Mathf.CeilToInt(h), RS);

        static Sprite Done(Pix p, float jx, float jy, float rim = 0.32f, float core = 0.28f, int outline = 2)
        {
            p.Volume(rim, core);
            if (outline > 0) p.Outline(Pal.Ink, outline);
            return p.ToSprite(new Vector2(jx / p.BW, jy / p.BH), 64f);
        }

        // ------------------------------------------------------------------ limbs

        static Sprite Limb(float len, float r0, float r1, Color c, Color? cap = null, float capR = 0f)
        {
            float rm = Mathf.Max(r0, r1, capR);
            var p = C(rm * 2f + 10f, len + rm * 2f + 10f);
            float jx = p.BW * 0.5f, jy = p.BH - rm - 5f;
            p.Taper(jx, jy, jx, jy - len, r0, r1, c);
            p.Capsule(jx - r0 * 0.35f, jy - 1f, jx - r1 * 0.35f, jy - len + 1f, Mathf.Min(r0, r1) * 0.25f, c.Lit(1.25f).A(0.5f));
            if (cap.HasValue) p.Circle(jx, jy, capR, cap.Value);
            return Done(p, jx, jy);
        }

        static Sprite Shin(float len, float r, Color c, Color boot, float s)
        {
            var p = C(r * 2f + 22f * s, len + r * 2f + 14f);
            float jx = r + 6f, jy = p.BH - r - 5f;
            p.Taper(jx, jy, jx, jy - len, r, r * 0.85f, c);
            p.Capsule(jx - r * 0.3f, jy - 1f, jx - r * 0.3f, jy - len + 1f, r * 0.22f, c.Lit(1.25f).A(0.5f));
            p.Ellipse(jx + 2.5f * s, jy - len - 1f, 6.8f * s, 4.2f * s, boot);
            p.Rect(jx - r, jy - len + 2f * s, jx + r, jy - len + 4.5f * s, boot.Lit(1.3f), 1f);
            return Done(p, jx, jy);
        }

        static Sprite Forearm(float len, float r, Color c, Color skin, bool stump, Color trim, float s)
        {
            var p = C(r * 2f + 14f, len + r * 2f + 16f);
            float jx = p.BW * 0.5f, jy = p.BH - r - 5f;
            p.Taper(jx, jy, jx, jy - len, r, r * 0.88f, c);
            p.Capsule(jx - r * 0.3f, jy - 1f, jx - r * 0.3f, jy - len + 2f, r * 0.22f, c.Lit(1.25f).A(0.5f));
            p.Rect(jx - r * 0.95f, jy - len + 1f, jx + r * 0.95f, jy - len + 4f * s, c.Lit(0.75f), 1f);
            if (stump) p.Rect(jx - r, jy - len - 3f * s, jx + r, jy - len + 1f, trim, 1f);
            else p.Circle(jx, jy - len - 1.2f * s, 3.8f * s, skin);
            return Done(p, jx, jy);
        }

        // ------------------------------------------------------------------ humanoid

        static RigDef Humanoid(string id, Hum h)
        {
            var d = new RigDef { Kind = RigKind.Humanoid };
            float s = h.scale;
            float legH = 20f * s * h.legs, thighL = legH * 0.52f, shinL = legH * 0.48f;
            float lw = 4.6f * s * h.width;
            float torH = 34f * s * h.torso, hr = h.headR * s;
            float sw = 15f * s * h.width * h.shoulders, hw = 11f * s * h.width;
            float armR = 4.2f * s * Mathf.Max(0.8f, h.width * 0.9f);
            float upperL = torH * 0.45f, foreL = torH * 0.4f;
            Color clothDark = h.cloth.Lit(0.72f);
            Color deadMul = new Color(0.32f, 0.36f, 0.52f);
            d.LegH = legH * PU; d.TorH = torH * PU; d.HeadR = hr * PU; d.Scale = s; d.Hunch = h.hunch;
            d.ArmsUp = h.armsUp; d.Robe = h.robe; d.Cape = h.capeOn; d.Hair = h.longHair && !h.hood; d.Wings = h.wings;

            d.Add("root", null, null, Vector2.zero, 0);
            d.Add("hips", "root", null, new Vector2(0f, legH * PU), 0);

            // legs or robe
            if (!h.robe)
            {
                Color legF = h.dark, legN = h.dark.Lit(1.18f);
                if (h.deadHalf) legN = new Color(legN.r * deadMul.r, legN.g * deadMul.g, legN.b * deadMul.b);
                d.Add("thighF", "hips", Limb(thighL, lw, lw * 0.9f, legF), new Vector2(-hw * 0.35f * PU, 0f), -3);
                d.Add("shinF", "thighF", Shin(shinL, lw * 0.9f, legF, legF.Lit(0.6f), s), new Vector2(0f, -thighL * PU), -3);
                d.Add("thighN", "hips", Limb(thighL, lw, lw * 0.9f, legN), new Vector2(hw * 0.35f * PU, 0f), -2);
                d.Add("shinN", "thighN", Shin(shinL, lw * 0.9f, legN, legN.Lit(0.6f), s), new Vector2(0f, -thighL * PU), -2);

                var pel = C(hw * 2.8f + 14f, 16f * s + 12f);
                float pjx = pel.BW * 0.5f, pjy = pel.BH - 6f;
                pel.Poly(new[] { new Vector2(pjx - hw, pjy + 3f), new Vector2(pjx + hw, pjy + 3f), new Vector2(pjx + hw * 1.3f, pjy - 9f * s), new Vector2(pjx - hw * 1.3f, pjy - 9f * s) }, clothDark);
                for (int i = 1; i < 4; i++) pel.Capsule(pjx - hw + i * hw * 0.5f, pjy, pjx - hw * 1.3f + i * hw * 0.65f, pjy - 8f * s, 0.5f * s, clothDark.Lit(0.7f));
                pel.Rect(pjx - hw - 1f, pjy - 1f, pjx + hw + 1f, pjy + 3f * s, h.trim, 1f);
                pel.Rect(pjx + hw * 0.2f, pjy - 1.5f, pjx + hw * 0.55f, pjy + 3.5f * s, h.trim.Lit(1.4f), 1f);
                if (h.deadHalf) pel.SplitTint(pjx, deadMul, 4f);
                d.Add("pelvis", "hips", Done(pel, pjx, pjy), Vector2.zero, -1);
            }
            else
            {
                var sk = C(sw * 2.9f + 18f, legH + 14f);
                float kx = sk.BW * 0.5f, ky = sk.BH - 6f;
                var pts = new List<Vector2> { new Vector2(kx - hw * 1.05f, ky + 3f), new Vector2(kx + hw * 1.05f, ky + 3f) };
                if (h.tattered)
                    for (int i = 0; i <= 6; i++) pts.Add(new Vector2(Mathf.Lerp(kx + sw * 1.3f, kx - sw * 1.3f, i / 6f), ky - legH + 1f + (i % 2 == 1 ? 6f * s : 0f)));
                else { pts.Add(new Vector2(kx + sw * 1.3f, ky - legH + 1f)); pts.Add(new Vector2(kx - sw * 1.3f, ky - legH + 1f)); }
                sk.Poly(pts.ToArray(), h.cloth);
                for (int i = -2; i <= 2; i++) sk.Capsule(kx + i * hw * 0.35f, ky, kx + i * sw * 0.45f, ky - legH + 4f, 0.7f * s, h.cloth.Lit(0.8f));
                sk.Poly(new[] { new Vector2(kx - 2f * s, ky + 2f), new Vector2(kx + 2f * s, ky + 2f), new Vector2(kx + 5f * s, ky - legH + 1f), new Vector2(kx - 5f * s, ky - legH + 1f) }, h.trim.A(0.6f));
                sk.ShadeVertical(ky - legH, ky, 0.75f, 1f);
                if (h.deadHalf) sk.SplitTint(kx, deadMul, 4f);
                d.Add("skirt", "hips", Done(sk, kx, ky), Vector2.zero, -1);
            }

            // torso
            var to = C(sw * 2f + 28f, torH + 26f);
            float tx = to.BW * 0.5f, ty = 8f;
            if (h.robe)
                to.Poly(new[] { new Vector2(tx - sw, ty + torH), new Vector2(tx + sw, ty + torH), new Vector2(tx + hw * 1.08f, ty - 3f), new Vector2(tx - hw * 1.08f, ty - 3f) }, h.cloth);
            else
                to.Poly(new[] { new Vector2(tx - sw, ty + torH), new Vector2(tx + sw, ty + torH), new Vector2(tx + hw, ty - 2f), new Vector2(tx - hw, ty - 2f) }, h.cloth);
            to.Poly(new[] { new Vector2(tx - sw * 0.2f, ty + torH), new Vector2(tx + sw * 0.25f, ty + torH), new Vector2(tx + hw * 0.15f, ty), new Vector2(tx - hw * 0.1f, ty) }, h.cloth.Lit(1.12f).A(0.7f));
            to.Capsule(tx - sw * 0.85f, ty + torH - 2f, tx + sw * 0.85f, ty + torH - 2f, 1.4f * s, h.trim.A(0.8f));
            if (h.robe) to.Poly(new[] { new Vector2(tx - 2f * s, ty + torH - 3f), new Vector2(tx + 2f * s, ty + torH - 3f), new Vector2(tx + 3f * s, ty), new Vector2(tx - 3f * s, ty) }, h.trim.A(0.6f));
            if (h.hood)
                to.Poly(new[] { new Vector2(tx - sw * 0.95f, ty + torH + 2f), new Vector2(tx + sw * 0.8f, ty + torH + 2f), new Vector2(tx + sw * 0.4f, ty + torH - 8f * s), new Vector2(tx - sw * 0.7f, ty + torH - 9f * s) }, h.cloth.Lit(0.85f));
            if (h.fur)
                for (int i = 0; i < 6; i++) to.Circle(tx - sw + i * (sw * 2f / 5f), ty + torH - 1f, 4.5f * s, U.Hex("e8e4dc").Lit(0.85f + (i % 2) * 0.15f));
            if (h.iceShards)
                for (int i = 0; i < 6; i++) to.Poly(new[] { new Vector2(tx - sw + i * sw * 0.4f, ty + torH - 2f), new Vector2(tx - sw + i * sw * 0.4f + 8f * s, ty + torH - 2f), new Vector2(tx - sw + i * sw * 0.4f + 4f * s, ty + torH + 12f * s - (i % 3) * 3f * s) }, U.Hex("dff6ff"));
            if (h.chestRune >= 0)
            {
                float ry = ty + torH * 0.5f + 2f;
                Runes.DrawCentered(to, h.chestRune, tx, ry, 12f * s, 2.2f * s, h.runeColor);
                to.LightGlow(tx, ry, 10f * s, h.runeColor.A(0.55f));
            }
            if (h.necklace)
            {
                to.Ring(tx, ty + torH - 2f, 7f * s, 1.6f * s, Pal.Gold);
                to.Circle(tx, ty + torH - 9f * s, 2.2f * s, U.Hex("ff9ad5"));
            }
            to.ShadeVertical(ty, ty + torH, 0.8f, 1.08f);
            if (h.deadHalf) to.SplitTint(tx + 1f, deadMul, 4f);
            d.Add("torso", "hips", Done(to, tx, ty), Vector2.zero, 0);

            // head
            float hs = Mathf.Max(hr * 4.8f, h.wolfHead ? 100f * s / 1.6f : 0f) + 26f;
            var he = C(hs, hs);
            float hcx = he.BW * 0.5f, hcy = he.BH * 0.46f;
            if (h.halo) { var ha = C(hr * 7f, hr * 7f); ha.Glow(ha.BW * 0.5f, ha.BH * 0.5f, hr * 3.3f, U.Hex("ffe7a3").A(0.6f), 1.6f); d.Add("halo", "torso", ha.ToSprite(new Vector2(0.5f, 0.5f), 64f), new Vector2(0f, (torH + hr * 0.9f) * PU), -9); }
            CharArt.DrawHead(he, h, hcx, hcy, hcx, hr * 1.0f, hcy - hr * 1.05f);
            float neckY = hcy - hr * 0.82f;
            if (h.deadHalf) he.SplitTint(hcx + 1f, deadMul, 4f);
            d.Add("head", "torso", Done(he, hcx, neckY, 0.3f, 0.22f), new Vector2(0f, torH * PU), 1);

            if (d.Hair)
            {
                var hp = C(hr * 2.4f + 10f, torH * 0.7f + hr + 10f);
                float hjx = hp.BW * 0.6f, hjy = hp.BH - 5f;
                hp.Poly(new[] { new Vector2(hjx - hr * 0.9f, hjy), new Vector2(hjx + hr * 0.6f, hjy), new Vector2(hjx + hr * 0.2f, hjy - torH * 0.6f - hr * 0.6f), new Vector2(hjx - hr * 1.1f, hjy - torH * 0.55f - hr * 0.5f) }, h.hair.Lit(0.85f));
                for (int i = 0; i < 4; i++) hp.Capsule(hjx - hr * 0.7f + i * hr * 0.3f, hjy - 2f, hjx - hr * 0.8f + i * hr * 0.28f, hjy - torH * 0.5f, 0.6f * s, h.hair.Lit(0.65f));
                d.Add("hair", "head", Done(hp, hjx, hjy), new Vector2(-hr * 0.55f * PU, hr * 1.2f * PU), -5);
            }

            // arms
            Color armF = clothDark, armN = h.cloth;
            if (h.deadHalf) armN = new Color(armN.r * deadMul.r, armN.g * deadMul.g, armN.b * deadMul.b);
            Color skinN = h.deadHalf ? new Color(h.skin.r * deadMul.r, h.skin.g * deadMul.g, h.skin.b * deadMul.b) : h.skin;
            Color? pad = h.pads ? h.metal : (Color?)null;
            Vector2 shF = new Vector2(-(sw - 3f) * PU, (torH - 3f) * PU), shN = new Vector2((sw - 3f) * PU, (torH - 3f) * PU);
            d.Add("upperF", "torso", Limb(upperL, armR, armR * 0.9f, armF, pad.HasValue ? pad.Value.Lit(0.8f) : (Color?)null, 6f * s), shF, -4);
            d.Add("foreF", "upperF", Forearm(foreL, armR * 0.9f, armF, h.skin.Lit(0.85f), false, h.trim, s), new Vector2(0f, -upperL * PU), -4);
            d.Add("handF", "foreF", null, new Vector2(0f, -(foreL + 1.2f * s) * PU), -4);
            d.Add("upperN", "torso", Limb(upperL, armR, armR * 0.9f, armN, pad, 6f * s), shN, 2);
            d.Add("foreN", "upperN", Forearm(foreL, armR * 0.9f, armN, skinN, h.oneHand, h.trim, s), new Vector2(0f, -upperL * PU), 2);
            d.Add("handN", "foreN", null, new Vector2(0f, -(foreL + 1.2f * s) * PU), 2);

            // held items (near hand), drawn hanging down/forward from the grip
            if (h.sword)
            {
                var it = C(30f * s + 10f, 30f * s + 10f);
                float gx = 6f * s + 3f, gy = it.BH - 6f * s - 3f;
                CharArt.DrawSword(it, h, gx, gy);
                d.Add("item", "handN", Done(it, gx, gy), Vector2.zero, 3);
            }
            if (h.hammer)
            {
                var it = C(24f * s + 8f, 32f * s + 8f);
                float gx = it.BW * 0.5f, gy = 16f * s + 4f;
                CharArt.DrawHammer(it, h, gx, gy);
                d.Add("item", "handN", Done(it, gx, gy), Vector2.zero, 3);
            }
            if (h.staff)
            {
                float down = legH - 6f, up = torH * 0.55f + 30f * s;
                var it = C(16f * s + 10f, down + up + 22f * s);
                float gx = 6f * s + 2f, gy = down + 6f;
                CharArt.DrawStaff(it, h, gx, gy, down, up);
                d.Add("item", "handN", Done(it, gx, gy, 0.3f, 0.2f), Vector2.zero, 3);
            }
            if (h.bow)
            {
                var it = C(26f * s + 10f, 40f * s + 8f);
                float gx = 6f * s + 2f, gy = 12f * s + 4f;
                CharArt.DrawBow(it, h, gx, gy);
                d.Add("item", "handN", Done(it, gx, gy, 0.2f, 0.15f), Vector2.zero, 3);
            }
            if (h.shield)
            {
                var it = C(40f * s, 40f * s);
                CharArt.DrawShield(it, h, it.BW * 0.5f, it.BH * 0.5f);
                d.Add("shield", "foreN", Done(it, it.BW * 0.5f, it.BH * 0.5f), new Vector2(3f * s * PU, -foreL * 0.45f * PU), 4);
            }
            if (h.bowl)
            {
                var it = C(44f * s, 18f * s);
                float bx = it.BW * 0.5f, by = it.BH * 0.4f;
                it.Ellipse(bx, by, 19f * s, 6f * s, U.Hex("8a6a2a"));
                it.Ellipse(bx, by + 3f * s, 17f * s, 4f * s, U.Hex("c8a04a"));
                it.Ellipse(bx, by + 4f * s, 13f * s, 2.2f * s, Pal.Venom);
                d.Add("bowl", "torso", Done(it, bx, by), new Vector2(0f, (torH + hr * 2.6f) * PU), 3);
            }

            // cape (three segments, simulated)
            if (h.capeOn)
            {
                float Lc = torH + legH - 6f;
                float seg = Lc / 3f;
                float[] wd = { sw * 1.5f, sw * 1.9f, sw * 2.3f, sw * 2.7f };
                string parent = "torso";
                Vector2 at = new Vector2(-sw * 0.3f * PU, (torH - 1f) * PU);
                for (int i = 0; i < 3; i++)
                {
                    var cp = C(wd[i + 1] + 14f, seg + 12f);
                    float cjx = cp.BW * 0.5f, cjy = cp.BH - 5f;
                    var pts = new List<Vector2> { new Vector2(cjx - wd[i] * 0.5f, cjy + 1f), new Vector2(cjx + wd[i] * 0.5f, cjy + 1f) };
                    float by = cjy - seg - 3f;
                    if (i == 2 && h.tattered)
                        for (int k = 0; k <= 7; k++) pts.Add(new Vector2(Mathf.Lerp(cjx + wd[3] * 0.5f, cjx - wd[3] * 0.5f, k / 7f), by + (k % 2 == 1 ? 7f * s : 0f)));
                    else { pts.Add(new Vector2(cjx + wd[i + 1] * 0.5f, by)); pts.Add(new Vector2(cjx - wd[i + 1] * 0.5f, by)); }
                    Color cc = h.cape.Lit(1f - i * 0.07f);
                    cp.Poly(pts.ToArray(), cc);
                    for (int k = -1; k <= 1; k++) cp.Capsule(cjx + k * wd[i] * 0.25f, cjy, cjx + k * wd[i + 1] * 0.3f, by + 2f, 0.6f * s, cc.Lit(0.8f));
                    d.Add("cape" + i, parent, Done(cp, cjx, cjy, 0.2f, 0.25f), at, -8);
                    parent = "cape" + i;
                    at = new Vector2(0f, -seg * PU);
                }
                d.CapeSegs = 3;
            }

            if (h.wings)
            {
                for (int side = 0; side < 2; side++)
                {
                    var wp = C(52f * s + 10f, torH + 34f * s + 10f);
                    float wjx = wp.BW - 6f, wjy = wp.BH - 30f * s - 5f;
                    Vector2[] raw = { new Vector2(0, -4), new Vector2(-40, 26), new Vector2(-34 + (side == 0 ? -3 : 3), 8), new Vector2(-44, 4), new Vector2(-30, -8), new Vector2(-36, -20), new Vector2(-8, -torH / s) };
                    var pts = new Vector2[raw.Length];
                    for (int k = 0; k < raw.Length; k++) pts[k] = new Vector2(wjx + raw[k].x * s, wjy + raw[k].y * s);
                    wp.Poly(pts, U.Hex("d6dbe6").Lit(side == 0 ? 0.7f : 1f));
                    for (int k = 0; k < 5; k++) wp.Capsule(wjx - 4f * s, wjy - 2f, wjx - (12 + k * 7) * s, wjy + (20 - k * 9) * s, 0.7f * s, U.Hex("aab2c4"));
                    d.Add(side == 0 ? "wingF" : "wingN", "torso", Done(wp, wjx, wjy, 0.25f, 0.2f), new Vector2(-sw * 0.25f * PU, (torH - 5f) * PU), side == 0 ? -7 : -6);
                }
            }
            return d;
        }

        // ------------------------------------------------------------------ quadruped

        static RigDef Quadruped(string id, Quad q)
        {
            var d = new RigDef { Kind = RigKind.Quad };
            float s = q.scale;
            d.Scale = s;
            float bodyY = 34f * s;
            float legLen = 26f * s, upL = legLen * 0.52f, loL = legLen * 0.48f;
            d.LegH = bodyY * PU;
            Color furD = q.fur.Lit(0.72f);

            d.Add("root", null, null, Vector2.zero, 0);
            var bp = C(100f * s, 64f * s);
            float bx = bp.BW * 0.5f, by = bp.BH * 0.5f;
            bp.Ellipse(bx, by, 44f * s, 20f * s, q.fur);
            bp.Ellipse(bx + 4f * s, by - 10f * s, 34f * s, 10f * s, q.belly);
            if (q.spikes)
                for (int i = 0; i < 8; i++)
                {
                    float x = bx - 34f * s + i * 9f * s;
                    float y = by + 16f * s - Mathf.Abs(i - 3.5f) * 1.5f * s;
                    bp.Poly(new[] { new Vector2(x - 5f * s, y - 4f * s), new Vector2(x + 5f * s, y - 4f * s), new Vector2(x - 2f * s, y + 9f * s) }, q.fur.Lit(0.85f));
                }
            if (q.chest.a > 0f) { bp.Ellipse(bx + 30f * s, by - 6f * s, 14f * s, 14f * s, q.chest); if (q.flame) bp.LightGlow(bx + 30f * s, by - 6f * s, 20f * s, q.chest.A(0.5f)); }
            if (q.chains) bp.Capsule(bx - 30f * s, by + 10f * s, bx + 10f * s, by - 16f * s, 2.2f * s, U.Hex("8d939e"));
            bp.Grain(0.35f, 0.3f, id.Length);
            bp.ShadeVertical(by - 20f * s, by + 20f * s, 0.75f, 1.1f);
            d.Add("body", "root", Done(bp, bx, by), new Vector2(0f, bodyY * PU), 0);

            Vector2[] legAt = { new Vector2(24f, -6f), new Vector2(34f, -6f), new Vector2(-28f, -6f), new Vector2(-18f, -6f) };
            string[] names = { "fF", "fN", "bF", "bN" };
            for (int i = 0; i < 4; i++)
            {
                bool far = i % 2 == 0;
                Color c = far ? furD : q.fur;
                d.Add("up" + names[i], "body", Limb(upL, 6.5f * s, 5.5f * s, c), legAt[i] * s * PU, far ? -2 : 1);
                var lo = C(20f * s, loL + 16f * s);
                float ljx = 7f * s, ljy = lo.BH - 7f * s;
                lo.Taper(ljx, ljy, ljx, ljy - loL, 5.5f * s, 4.5f * s, c);
                lo.Ellipse(ljx + 3f * s, ljy - loL - 1f * s, 7f * s, 3.6f * s, c.Lit(0.7f));
                for (int k = 0; k < 3; k++) lo.Poly(new[] { new Vector2(ljx + (5 + k * 3) * s, ljy - loL - 3f * s), new Vector2(ljx + (6.5f + k * 3) * s, ljy - loL - 3f * s), new Vector2(ljx + (6 + k * 3) * s, ljy - loL - 6f * s) }, Pal.Bone);
                d.Add("lo" + names[i], "up" + names[i], Done(lo, ljx, ljy), new Vector2(0f, -upL * PU), far ? -2 : 1);
            }

            var hp = C(70f * s, 70f * s);
            float njx = 14f * s, njy = 24f * s;
            CharArt.DrawBeastHead(hp, q, njx + 20f * s, njy + 14f * s, njx, njy, false);
            d.Add("head", "body", Done(hp, njx, njy), new Vector2(26f * s * PU, 4f * s * PU), 2);
            var jp = C(34f * s, 18f * s);
            float jjx = 4f * s, jjy = 12f * s;
            jp.Taper(jjx, jjy, jjx + 22f * s, jjy - 5f * s, 5f * s, 3f * s, q.belly.Lit(0.8f));
            for (int i = 0; i < 4; i++) jp.Poly(new[] { new Vector2(jjx + (6 + i * 4) * s, jjy + 1f * s), new Vector2(jjx + (9 + i * 4) * s, jjy + 1f * s), new Vector2(jjx + (7.5f + i * 4) * s, jjy + 5f * s) }, Color.white);
            d.Add("jaw", "head", Done(jp, jjx, jjy), new Vector2((20f + 6f) * s * PU, (14f - 9f) * s * PU), 3);

            string parent = "body";
            Vector2 at = new Vector2(-40f * s * PU, 2f * s * PU);
            for (int i = 0; i < 2; i++)
            {
                var tp = C(30f * s, 18f * s);
                float tjx = tp.BW - 5f * s, tjy = tp.BH * 0.5f;
                tp.Taper(tjx, tjy, tjx - 24f * s, tjy + 3f * s, (6.5f - i * 2.5f) * s, (4f - i * 2f) * s, furD);
                d.Add("tail" + i, parent, Done(tp, tjx, tjy), at, -3);
                parent = "tail" + i;
                at = new Vector2(-22f * s * PU, 3f * s * PU);
            }
            return d;
        }

        // ------------------------------------------------------------------ bird

        static RigDef Bird(string id)
        {
            var d = new RigDef { Kind = RigKind.Bird };
            Color f = U.Hex("1e1a2a");
            d.Add("root", null, null, Vector2.zero, 0);
            var bp = C(64f, 34f);
            float bx = bp.BW * 0.5f, by = bp.BH * 0.5f;
            bp.Poly(new[] { new Vector2(bx - 10f, by + 2f), new Vector2(bx - 30f, by - 6f), new Vector2(bx - 26f, by + 6f) }, f);
            bp.Ellipse(bx, by, 18f, 10f, f);
            bp.Ellipse(bx + 2f, by - 4f, 12f, 4f, f.Lit(1.4f));
            bp.Circle(bx + 16f, by + 6f, 8f, f);
            bp.Poly(new[] { new Vector2(bx + 20f, by + 9f), new Vector2(bx + 32f, by + 4f), new Vector2(bx + 20f, by + 1f) }, U.Hex("3a3446"));
            bp.Glow(bx + 18f, by + 8f, 4f, Pal.Danger);
            bp.Circle(bx + 18f, by + 8f, 1.4f, Color.white);
            d.Add("body", "root", Done(bp, bx, by), new Vector2(0f, 0.55f), 0);
            for (int side = 0; side < 2; side++)
            {
                var wp = C(56f, 48f);
                float wjx = 8f, wjy = 8f;
                Color c = f.Lit(side == 0 ? 1.2f : 1.7f);
                wp.Poly(new[] { new Vector2(wjx, wjy), new Vector2(wjx + 12f, wjy + 38f), new Vector2(wjx + 20f, wjy + 22f), new Vector2(wjx + 40f, wjy + 34f), new Vector2(wjx + 30f, wjy + 8f) }, c);
                for (int k = 0; k < 4; k++) wp.Capsule(wjx + 4f, wjy + 2f, wjx + 14f + k * 8f, wjy + 34f - k * 6f, 0.6f, c.Lit(0.7f));
                d.Add(side == 0 ? "wingF" : "wingN", "body", Done(wp, wjx, wjy, 0.25f, 0.2f), new Vector2(-2f * PU, 4f * PU), side == 0 ? -1 : 1);
            }
            return d;
        }

        // ------------------------------------------------------------------ serpent

        static RigDef Serpent(string id)
        {
            bool larva = id == "larva";
            var d = new RigDef { Kind = RigKind.Serpent, SerpentSegs = larva ? 6 : 10, SegSpacing = larva ? 0.15f : 0.14f };
            Color body = larva ? U.Hex("c8b48a") : U.Hex("3f6b3a"), belly = larva ? U.Hex("e8d8b0") : U.Hex("b8c87a");
            d.Add("root", null, null, Vector2.zero, 0);
            for (int i = 0; i < d.SerpentSegs; i++)
            {
                float t = i / (float)(d.SerpentSegs - 1);
                float r = Mathf.Lerp(11f, 4f, Mathf.Pow(t, 0.8f)) * (larva ? 0.9f : 1f);
                var sp = C(r * 2f + 8f, r * 2f + 8f);
                float c = sp.BW * 0.5f;
                sp.Circle(c, c, r, body.Lit(0.85f + 0.15f * (i % 2)));
                sp.Ellipse(c, c - r * 0.45f, r * 0.75f, r * 0.4f, belly);
                if (!larva && i % 2 == 0) sp.Circle(c - r * 0.2f, c + r * 0.3f, r * 0.25f, body.Lit(1.3f).A(0.6f));
                if (larva) sp.Capsule(c, c + r, c, c - r, 0.6f, body.Lit(0.6f));
                d.Add("seg" + i, "root", Done(sp, c, c * 0.6f, 0.3f, 0.25f), Vector2.zero, -i - 1);
            }
            var hp = C(44f, 34f);
            float hx = 14f, hy = 14f;
            if (larva)
            {
                hp.Circle(hx + 6f, hy, 10f, U.Hex("8a6a4a"));
                for (int i = 0; i < 5; i++) { var dd = U.Dir(-50 + i * 25); hp.Capsule(hx + 6f + dd.x * 8f, hy + dd.y * 8f, hx + 6f + dd.x * 15f, hy + dd.y * 15f, 1.4f, Pal.Bone); }
                hp.Glow(hx + 8f, hy + 2f, 5f, Pal.Venom);
            }
            else
            {
                hp.Ellipse(hx + 6f, hy, 16f, 11f, body);
                hp.Ellipse(hx + 14f, hy - 3f, 11f, 6f, body.Lit(1.15f));
                hp.Poly(new[] { new Vector2(hx + 16f, hy - 6f), new Vector2(hx + 19f, hy - 6f), new Vector2(hx + 17.5f, hy - 13f) }, Color.white);
                hp.Poly(new[] { new Vector2(hx + 21f, hy - 6f), new Vector2(hx + 24f, hy - 6f), new Vector2(hx + 22.5f, hy - 12f) }, Color.white);
                hp.Glow(hx + 10f, hy + 4f, 6f, Pal.Venom.A(0.9f));
                hp.Circle(hx + 10f, hy + 4f, 1.8f, Color.white);
            }
            d.Add("head", "root", Done(hp, hx, hy * 0.7f), Vector2.zero, 1);
            return d;
        }

        // ------------------------------------------------------------------ ghost

        static RigDef Ghost(string id)
        {
            bool wight = id == "wight";
            var d = new RigDef { Kind = RigKind.Ghost, TatterCount = 4 };
            Color body = wight ? U.Hex("5a7ab0") : U.Hex("5fb3a8");
            d.Add("root", null, null, Vector2.zero, 0);
            var bp = C(64f, 84f);
            float bx = bp.BW * 0.5f, by = 22f;
            bp.Glow(bx, by + 30f, 32f, body.A(0.35f), 1.5f);
            bp.Poly(new[] { new Vector2(bx, by + 58f), new Vector2(bx + 22f, by + 38f), new Vector2(bx + 22f, by + 2f), new Vector2(bx - 22f, by + 2f), new Vector2(bx - 20f, by + 30f), new Vector2(bx - 14f, by + 46f) }, body.A(0.9f));
            bp.Ellipse(bx + 6f, by + 36f, 13f, 15f, U.Hex("0b0d12"));
            bp.Glow(bx + 2f, by + 38f, 6f, Color.white); bp.Glow(bx + 12f, by + 38f, 6f, Color.white);
            bp.Circle(bx + 2f, by + 38f, 2f, Color.white); bp.Circle(bx + 12f, by + 38f, 2f, Color.white);
            d.Add("body", "root", Done(bp, bx, by, 0.3f, 0.2f), new Vector2(0f, 0.35f), 0);
            for (int i = 0; i < d.TatterCount; i++)
            {
                var tp = C(16f, 34f);
                float tjx = 8f, tjy = 30f;
                tp.Poly(new[] { new Vector2(tjx - 6f, tjy), new Vector2(tjx + 6f, tjy), new Vector2(tjx + 1f, tjy - 26f + (i % 2) * 6f) }, body.Lit(0.85f).A(0.85f));
                d.Add("tatter" + i, "body", Done(tp, tjx, tjy, 0.2f, 0.2f, 1), new Vector2((-16f + i * 10.5f) * PU, 4f * PU), -1);
            }
            for (int side = 0; side < 2; side++)
            {
                d.Add(side == 0 ? "armF" : "armN", "body", Limb(18f, 4f, 2.5f, body.Lit(side == 0 ? 0.75f : 1f).A(0.9f)), new Vector2((side == 0 ? -14f : 16f) * PU, 40f * PU), side == 0 ? -1 : 1);
            }
            return d;
        }

        // ------------------------------------------------------------------ blob and bell

        static RigDef Blob(string id)
        {
            var d = new RigDef { Kind = RigKind.Blob };
            d.Add("root", null, null, Vector2.zero, 0);
            for (int side = 0; side < 2; side++)
                d.Add(side == 0 ? "legF" : "legN", "root", Limb(8f, 4.5f, 4f, U.Hex("3a4a2a").Lit(side == 0 ? 0.8f : 1f)), new Vector2((side == 0 ? -12f : 10f) * PU, 12f * PU), side == 0 ? -1 : 1);
            var bp = C(80f, 80f);
            float bx = bp.BW * 0.5f, by = 6f;
            bp.Circle(bx, by + 32f, 30f, U.Hex("6a7a4a"));
            bp.Circle(bx - 6f, by + 38f, 22f, U.Hex("8a9a5a"));
            for (int i = 0; i < 5; i++) bp.Circle(bx - 18f + i * 9f, by + 20f + (i % 3) * 10f, 3.5f, Pal.Venom.A(0.8f));
            bp.Capsule(bx - 8f, by + 50f, bx + 14f, by + 26f, 1.2f, Pal.Ink.A(0.7f));
            for (int i = 0; i < 5; i++) bp.Capsule(bx - 4f + i * 4f, by + 42f - i * 4f, bx + i * 4f, by + 46f - i * 4f, 0.9f, Pal.Ink.A(0.7f));
            bp.LightGlow(bx, by + 32f, 26f, Pal.Venom.A(0.25f));
            d.Add("body", "root", Done(bp, bx, by), new Vector2(0f, 8f * PU), 0);
            var hp = C(30f, 30f);
            hp.Circle(15f, 15f, 11f, U.Hex("7a8a5a"));
            hp.Glow(19f, 17f, 5f, Pal.Venom);
            hp.Circle(19f, 17f, 1.6f, Color.white);
            d.Add("head", "body", Done(hp, 12f, 6f), new Vector2(10f * PU, 56f * PU), 1);
            return d;
        }

        static RigDef Bell(string id)
        {
            var d = new RigDef { Kind = RigKind.Bell };
            d.Add("root", null, null, Vector2.zero, 0);
            var post = C(64f, 112f);
            post.Rect(28f, 2f, 36f, 98f, U.Hex("3a3f4a"), 2f);
            post.Rect(8f, 94f, 56f, 102f, U.Hex("3a3f4a"), 2f);
            d.Add("post", "root", Done(post, 32f, 2f), Vector2.zero, -1);
            var bell = C(56f, 56f);
            float bx = 28f, top = 52f;
            bell.Poly(new[] { new Vector2(bx - 12f, top - 4f), new Vector2(bx + 12f, top - 4f), new Vector2(bx + 22f, top - 48f), new Vector2(bx - 22f, top - 48f) }, U.Hex("6a5a3a"));
            bell.Ellipse(bx, top - 48f, 24f, 6f, U.Hex("8a7a4a"));
            bell.Circle(bx, top - 54f + 6f, 5f, U.Hex("3a3020"));
            Runes.DrawCentered(bell, Runes.Hagalaz, bx, top - 26f, 16f, 2.5f, U.Hex("9be7ff"));
            bell.LightGlow(bx, top - 26f, 18f, U.Hex("9be7ff").A(0.5f));
            d.Add("bell", "root", Done(bell, bx, top), new Vector2(0f, 96f * PU), 0);
            return d;
        }
    }
}
