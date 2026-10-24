using UnityEngine;
#if WYRD_INPUTSYSTEM && ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Wyrdbound
{
    /// <summary>
    /// Device-agnostic input. Works with the Input System package (keyboard, mouse, any gamepad)
    /// and falls back to the legacy Input Manager when the package/backend is not active.
    /// Default bindings mirror the genre: attack / special / cast / dash / interact / call.
    /// </summary>
    public sealed class GameInput
    {
        struct Raw
        {
            public bool attack, special, cast, dash, interact, call, pause, reroll, side, help;
            public bool up, down, left, right, confirm, cancel, click;
            public Vector2 move, stick2;
        }

        Raw now, prev;
        Vector2 lastMouse;
        float repeatT;
        int repeatDir;

        public Vector2 Move;
        public Vector2 Stick2;
        public Vector2 MouseScreen;
        public bool MouseAim = true;
        public bool UsingPad;

        public bool Attack => now.attack;
        public bool AttackDown => now.attack && !prev.attack;
        public bool AttackUp => !now.attack && prev.attack;
        public bool Special => now.special;
        public bool SpecialDown => now.special && !prev.special;
        public bool SpecialUp => !now.special && prev.special;
        public bool CastDown => now.cast && !prev.cast;
        public bool Dash => now.dash;
        public bool DashDown => now.dash && !prev.dash;
        public bool InteractDown => now.interact && !prev.interact;
        public bool CallDown => now.call && !prev.call;
        public bool SidestepDown => now.side && !prev.side;
        public bool HelpDown => now.help && !prev.help;
        public bool PauseDown => now.pause && !prev.pause;
        public bool RerollDown => now.reroll && !prev.reroll;
        public bool ConfirmDown => now.confirm && !prev.confirm;
        public bool CancelDown => now.cancel && !prev.cancel;
        public bool ClickDown => now.click && !prev.click;
        public bool AnyActionHeld => now.attack || now.special || now.cast || now.dash || now.click || now.confirm;

        public bool NavUp, NavDown, NavLeft, NavRight;

        /// <summary>Virtual controller driven by the automated play-test.</summary>
        public sealed class TestPad
        {
            public Vector2 Move;
            public bool Attack, Special, Cast, Dash, Call, Confirm, Cancel, Sidestep;
            /// <summary>When set, the virtual mouse sits here (screen pixels) and mouse-aiming is on.</summary>
            public Vector2? Mouse;
        }

        public TestPad Pad;

        public void Update()
        {
            prev = now;
            now = default;
            Read(ref now);

            Move = now.move.sqrMagnitude > 1f ? now.move.normalized : now.move;
            Stick2 = now.stick2;

            if ((MouseScreen - lastMouse).sqrMagnitude > 9f || now.click)
            {
                if (SaveSystem.Prefs == null || SaveSystem.Prefs.mouseAim) MouseAim = true;
                UsingPad = false;
            }
            lastMouse = MouseScreen;

            int dir = now.up ? 1 : now.down ? 2 : now.left ? 3 : now.right ? 4 : 0;
            NavUp = NavDown = NavLeft = NavRight = false;
            if (dir != 0)
            {
                bool fire = false;
                if (dir != repeatDir) { fire = true; repeatDir = dir; repeatT = 0.38f; }
                else
                {
                    repeatT -= UnityEngine.Time.unscaledDeltaTime;
                    if (repeatT <= 0f) { fire = true; repeatT = 0.085f; }
                }
                if (fire)
                {
                    NavUp = dir == 1; NavDown = dir == 2; NavLeft = dir == 3; NavRight = dir == 4;
                }
            }
            else repeatDir = 0;
        }

        /// <summary>Button glyph for prompts, depending on the last used device.</summary>
        public string Glyph(string action)
        {
            if (UsingPad)
            {
                switch (action)
                {
                    case "interact": return "(B)";
                    case "attack": return "(X)";
                    case "special": return "(Y)";
                    case "dash": return "(A)";
                    case "cast": return "(RT)";
                    case "call": return "(LT)";
                    case "side": return "(RB)";
                    case "help": return "(Start)";
                    case "confirm": return "(A)";
                    case "cancel": return "(B)";
                    case "reroll": return "(Back)";
                    case "pause": return "(Start)";
                }
                return "(?)";
            }
            switch (action)
            {
                case "interact": return "[E]";
                case "attack": return "[LMB/J]";
                case "special": return "[RMB/K]";
                case "dash": return "[Space]";
                case "cast": return "[Q]";
                case "call": return "[F]";
                case "side": return "[C]";
                case "help": return "[H]";
                case "confirm": return "[Enter]";
                case "cancel": return "[Esc]";
                case "reroll": return "[R]";
                case "pause": return "[Esc]";
            }
            return "[?]";
        }

        void Read(ref Raw r)
        {
#if WYRD_INPUTSYSTEM && ENABLE_INPUT_SYSTEM
            var kb = Keyboard.current;
            var ms = Mouse.current;
            var gp = Gamepad.current;
            bool kbUsed = false;
            if (kb != null)
            {
                Vector2 k = Vector2.zero;
                if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) k.x -= 1f;
                if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) k.x += 1f;
                if (kb.sKey.isPressed || kb.downArrowKey.isPressed) k.y -= 1f;
                if (kb.wKey.isPressed || kb.upArrowKey.isPressed) k.y += 1f;
                r.move = k;
                r.attack |= kb.jKey.isPressed;
                r.special |= kb.kKey.isPressed;
                r.cast |= kb.qKey.isPressed || kb.lKey.isPressed;
                r.dash |= kb.spaceKey.isPressed || kb.leftShiftKey.isPressed;
                r.interact |= kb.eKey.isPressed;
                r.call |= kb.fKey.isPressed;
                r.pause |= kb.escapeKey.isPressed || kb.tabKey.isPressed;
                r.reroll |= kb.rKey.isPressed;
                r.side |= kb.cKey.isPressed || kb.leftCtrlKey.isPressed;
                r.help |= kb.hKey.isPressed || kb.f1Key.isPressed;
                r.up |= kb.wKey.isPressed || kb.upArrowKey.isPressed;
                r.down |= kb.sKey.isPressed || kb.downArrowKey.isPressed;
                r.left |= kb.aKey.isPressed || kb.leftArrowKey.isPressed;
                r.right |= kb.dKey.isPressed || kb.rightArrowKey.isPressed;
                r.confirm |= kb.enterKey.isPressed || kb.numpadEnterKey.isPressed || kb.spaceKey.isPressed || kb.eKey.isPressed;
                r.cancel |= kb.escapeKey.isPressed || kb.backspaceKey.isPressed;
                kbUsed = kb.anyKey.isPressed;
            }
            if (ms != null)
            {
                MouseScreen = ms.position.ReadValue();
                r.click |= ms.leftButton.isPressed;
                r.attack |= ms.leftButton.isPressed;
                r.special |= ms.rightButton.isPressed;
                r.cast |= ms.middleButton.isPressed;
                r.side |= ms.backButton.isPressed || ms.forwardButton.isPressed;
            }
            if (gp != null)
            {
                Vector2 ls = gp.leftStick.ReadValue();
                bool padUsed = false;
                if (ls.sqrMagnitude > 0.04f) { if (ls.sqrMagnitude > r.move.sqrMagnitude) r.move = ls; padUsed = true; }
                Vector2 dp = gp.dpad.ReadValue();
                if (dp.sqrMagnitude > 0.1f && r.move.sqrMagnitude < 0.1f) r.move = dp;
                r.stick2 = gp.rightStick.ReadValue();
                if (r.stick2.sqrMagnitude > 0.1f) padUsed = true;

                bool w = gp.buttonWest.isPressed, n = gp.buttonNorth.isPressed, s = gp.buttonSouth.isPressed, e = gp.buttonEast.isPressed;
                bool rt = gp.rightTrigger.isPressed;
                bool rb = gp.rightShoulder.isPressed;
                bool lt = gp.leftTrigger.isPressed || gp.leftShoulder.isPressed;
                r.attack |= w; r.special |= n; r.dash |= s; r.interact |= e;
                r.cast |= rt; r.call |= lt; r.side |= rb;
                r.pause |= gp.startButton.isPressed;
                r.reroll |= gp.selectButton.isPressed;
                r.up |= gp.dpad.up.isPressed || ls.y > 0.6f;
                r.down |= gp.dpad.down.isPressed || ls.y < -0.6f;
                r.left |= gp.dpad.left.isPressed || ls.x < -0.6f;
                r.right |= gp.dpad.right.isPressed || ls.x > 0.6f;
                r.confirm |= s;
                r.cancel |= e;
                padUsed |= w || n || s || e || rt || rb || lt || dp.sqrMagnitude > 0.1f || gp.startButton.isPressed;
                if (padUsed) { UsingPad = true; MouseAim = false; }
            }
            if (kbUsed && !(gp != null && UsingPad && r.move.sqrMagnitude > 0.04f)) UsingPad = false;
#elif ENABLE_LEGACY_INPUT_MANAGER
            Vector2 k = Vector2.zero;
            if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) k.x -= 1f;
            if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) k.x += 1f;
            if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow)) k.y -= 1f;
            if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow)) k.y += 1f;
            Vector2 axis = Vector2.zero;
            try { axis = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical")); } catch { }
            bool padStick = axis.sqrMagnitude > 0.04f && k.sqrMagnitude < 0.01f;
            r.move = padStick ? axis : k;

            bool j0 = Input.GetKey(KeyCode.JoystickButton0), j1 = Input.GetKey(KeyCode.JoystickButton1);
            bool j2 = Input.GetKey(KeyCode.JoystickButton2), j3 = Input.GetKey(KeyCode.JoystickButton3);
            bool j4 = Input.GetKey(KeyCode.JoystickButton4), j5 = Input.GetKey(KeyCode.JoystickButton5);
            bool j6 = Input.GetKey(KeyCode.JoystickButton6), j7 = Input.GetKey(KeyCode.JoystickButton7);

            r.attack = Input.GetKey(KeyCode.J) || Input.GetMouseButton(0) || j2;
            r.special = Input.GetKey(KeyCode.K) || Input.GetMouseButton(1) || j3;
            r.cast = Input.GetKey(KeyCode.Q) || Input.GetKey(KeyCode.L) || Input.GetMouseButton(2) || j5;
            r.dash = Input.GetKey(KeyCode.Space) || Input.GetKey(KeyCode.LeftShift) || j0;
            r.interact = Input.GetKey(KeyCode.E) || j1;
            r.call = Input.GetKey(KeyCode.F) || j4;
            r.pause = Input.GetKey(KeyCode.Escape) || Input.GetKey(KeyCode.Tab) || j7;
            r.reroll = Input.GetKey(KeyCode.R) || j6;
            r.side = Input.GetKey(KeyCode.C) || Input.GetKey(KeyCode.LeftControl);
            r.help = Input.GetKey(KeyCode.H) || Input.GetKey(KeyCode.F1);
            r.up = Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow) || axis.y > 0.6f;
            r.down = Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow) || axis.y < -0.6f;
            r.left = Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow) || axis.x < -0.6f;
            r.right = Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow) || axis.x > 0.6f;
            r.confirm = Input.GetKey(KeyCode.Return) || Input.GetKey(KeyCode.KeypadEnter) || Input.GetKey(KeyCode.Space) || Input.GetKey(KeyCode.E) || j0;
            r.cancel = Input.GetKey(KeyCode.Escape) || Input.GetKey(KeyCode.Backspace) || j1;
            r.click = Input.GetMouseButton(0);
            MouseScreen = Input.mousePosition;

            bool padUsed = padStick || j0 || j1 || j2 || j3 || j4 || j5 || j6 || j7;
            if (padUsed) { UsingPad = true; MouseAim = false; }
            else if (Input.anyKey) UsingPad = false;
#endif
            if (Pad != null)
            {
                if (Pad.Move.sqrMagnitude > 0.01f) r.move = Pad.Move;
                r.attack |= Pad.Attack;
                r.special |= Pad.Special;
                r.cast |= Pad.Cast;
                r.dash |= Pad.Dash;
                r.call |= Pad.Call;
                r.confirm |= Pad.Confirm;
                r.cancel |= Pad.Cancel;
                r.side |= Pad.Sidestep;
                if (Pad.Mouse.HasValue) { MouseScreen = Pad.Mouse.Value; MouseAim = true; UsingPad = false; }
                else MouseAim = false;
            }
        }
    }
}
