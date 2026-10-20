using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Wyrdbound.EditorTools
{
    /// <summary>
    /// Builds what players download: a clean Windows build with the game icon and version, ready for the
    /// installer (installer/Wyrdbound.iss) and the portable zip. Run everything with tools/make-release.ps1.
    /// </summary>
    public static class ReleaseTools
    {
        public const string Version = "0.3.0";
        public const string OutDir = "Builds/Release/Wyrdbound";
        const string IconPng = "Assets/Wyrdbound/Branding/Icon.png";
        const string IconIco = "installer/Wyrdbound.ico";

        [MenuItem("Wyrdbound/Build Release (for players)")]
        public static void BuildRelease()
        {
            WyrdboundSetup.Ensure(true);
            MakeIcon();
            PlayerSettings.bundleVersion = Version;
            PlayerSettings.companyName = "Wyrdbound";
            PlayerSettings.productName = "Wyrdbound";
            var icon = AssetDatabase.LoadAssetAtPath<Texture2D>(IconPng);
            if (icon != null) PlayerSettings.SetIcons(NamedBuildTarget.Unknown, new[] { icon }, IconKind.Any);

            if (Directory.Exists(OutDir)) Directory.Delete(OutDir, true);
            var opts = new BuildPlayerOptions
            {
                scenes = new[] { "Assets/Scenes/Main.unity" },
                locationPathName = OutDir + "/Wyrdbound.exe",
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.None
            };
            var report = BuildPipeline.BuildPlayer(opts);
            // folders Unity writes next to the build that must not ship to players
            foreach (var dir in Directory.GetDirectories(OutDir))
                if (dir.Contains("DoNotShip") || dir.Contains("DontShip")) Directory.Delete(dir, true);
            Debug.Log("[Wyrdbound] Release " + Version + " build " + report.summary.result + " — " + report.summary.totalSize / (1024 * 1024) + " MB, " + report.summary.totalErrors + " errors");
            if (Application.isBatchMode) EditorApplication.Exit(report.summary.result == BuildResult.Succeeded ? 0 : 1);
        }

        /// <summary>The game icon: the rune Perthro (fate, the wyrd) glowing on a dark shield ringed in gold.</summary>
        [MenuItem("Wyrdbound/Make Icon")]
        public static void MakeIcon()
        {
            const int S = 256;
            var p = new Pix(S, S);
            var bgIn = new Color(0.09f, 0.16f, 0.19f, 1f);
            var bgOut = new Color(0.02f, 0.03f, 0.04f, 1f);
            var gold = new Color(0.86f, 0.68f, 0.32f, 1f);
            for (int y = 0; y < S; y++)
                for (int x = 0; x < S; x++)
                {
                    float dx = (x + 0.5f - S * 0.5f) / (S * 0.5f), dy = (y + 0.5f - S * 0.5f) / (S * 0.5f);
                    float d = Mathf.Sqrt(dx * dx + dy * dy);
                    Color c;
                    if (d < 0.86f) c = Color.Lerp(bgIn, bgOut, d / 0.86f);
                    else if (d < 0.95f) c = Color.Lerp(gold, gold * 0.55f, Mathf.Abs(d - 0.905f) / 0.045f);
                    else c = gold * 0.55f;
                    c.a = Mathf.Clamp01((0.985f - d) * S * 0.5f);
                    p.P[y * S + x] = c;
                }
            var glow = new Color(0.44f, 0.95f, 1f, 0.3f);
            Runes.DrawCentered(p, Runes.Perthro, S * 0.5f, S * 0.5f, S * 0.56f, 26f, glow);
            Runes.DrawCentered(p, Runes.Perthro, S * 0.5f, S * 0.5f, S * 0.56f, 13f, new Color(0.62f, 0.98f, 1f, 1f));

            var tex = new Texture2D(S, S, TextureFormat.RGBA32, false);
            tex.SetPixels(p.P);
            tex.Apply();
            byte[] png = tex.EncodeToPNG();
            Object.DestroyImmediate(tex);

            Directory.CreateDirectory(Path.GetDirectoryName(IconPng));
            File.WriteAllBytes(IconPng, png);
            Directory.CreateDirectory(Path.GetDirectoryName(IconIco));
            File.WriteAllBytes(IconIco, PngToIco(png, S));
            AssetDatabase.ImportAsset(IconPng);
            var ti = (TextureImporter)AssetImporter.GetAtPath(IconPng);
            if (ti != null && !ti.alphaIsTransparency) { ti.alphaIsTransparency = true; ti.SaveAndReimport(); }
        }

        /// <summary>A Windows .ico holding one 256px PNG image (supported since Windows Vista).</summary>
        static byte[] PngToIco(byte[] png, int size)
        {
            using (var ms = new MemoryStream())
            using (var w = new BinaryWriter(ms))
            {
                w.Write((short)0); w.Write((short)1); w.Write((short)1);      // ICONDIR: reserved, type=icon, count
                w.Write((byte)(size >= 256 ? 0 : size)); w.Write((byte)(size >= 256 ? 0 : size));
                w.Write((byte)0); w.Write((byte)0);                            // palette, reserved
                w.Write((short)1); w.Write((short)32);                         // planes, bits per pixel
                w.Write(png.Length); w.Write(6 + 16);                          // size, offset
                w.Write(png);
                return ms.ToArray();
            }
        }
    }
}
