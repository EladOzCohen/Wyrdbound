using UnityEngine;

namespace Wyrdbound
{
    /// <summary>
    /// Font lookup. Drop a .ttf/.otf at Assets/Wyrdbound/Resources/Fonts/Main (and optionally Title)
    /// to restyle the whole game; otherwise the engine's built-in font is used.
    /// </summary>
    public static class Fonts
    {
        static Font main, title;

        public static Font Main
        {
            get
            {
                if (main == null) main = Resources.Load<Font>("Fonts/Main");
                if (main == null) main = Builtin();
                return main;
            }
        }

        public static Font Title
        {
            get
            {
                if (title == null) title = Resources.Load<Font>("Fonts/Title");
                if (title == null) title = Main;
                return title;
            }
        }

        static Font Builtin()
        {
            Font f = null;
            try { f = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); } catch { }
            if (f == null) { try { f = Resources.GetBuiltinResource<Font>("Arial.ttf"); } catch { } }
            if (f == null) f = Font.CreateDynamicFontFromOSFont("Arial", 32);
            return f;
        }
    }
}
