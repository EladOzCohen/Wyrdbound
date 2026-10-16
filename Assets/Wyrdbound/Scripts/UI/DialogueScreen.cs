using System;
using UnityEngine;
using UnityEngine.UI;

namespace Wyrdbound
{
    /// <summary>Conversation box with portraits, a typewriter, and optional choices at the end.</summary>
    public sealed class DialogueScreen : UIScreen
    {
        readonly Convo convo;
        readonly Action done;
        int idx = -1;
        float shown;
        string full = "";
        Text body, nameText;
        Image portraitL, portraitR, namePlate;
        RectTransform panel, choiceRoot;
        MenuList choices;
        bool finished;
        float tickT;

        public DialogueScreen(Convo c, Action done)
        {
            convo = c;
            this.done = done;
        }

        protected override void Create()
        {
            var dim = Dim(0.35f);
            portraitL = UIK.Img(Root, null, Color.white, "portraitL");
            portraitL.preserveAspect = true;
            portraitR = UIK.Img(Root, null, Color.white, "portraitR");
            portraitR.preserveAspect = true;
            portraitL.gameObject.SetActive(false);
            portraitR.gameObject.SetActive(false);

            panel = UIK.Panel(Root, "panel").rectTransform;
            panel.Place(new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0, 40), new Vector2(1500, 290));
            namePlate = UIK.Img(panel, SpriteLib.Rounded, Pal.Gold.A(0.9f), "nameplate");
            namePlate.rectTransform.Place(new Vector2(0f, 1f), new Vector2(0f, 0.5f), new Vector2(60, 0), new Vector2(380, 54));
            nameText = UIK.Txt(namePlate.transform, "", 30, Pal.Ink, TextAnchor.MiddleCenter, FontStyle.Bold, "name", true);
            nameText.rectTransform.Stretch();
            nameText.GetComponent<Shadow>().enabled = false;
            body = UIK.Txt(panel, "", 32, Pal.Text, TextAnchor.UpperLeft, FontStyle.Normal, "body");
            body.rectTransform.Stretch(64, 40, 64, 56);
            body.lineSpacing = 1.15f;
            var hint = UIK.Txt(panel, "▼", 26, Pal.Gold, TextAnchor.LowerRight, FontStyle.Bold, "hint");
            hint.rectTransform.Stretch(0, 14, 30, 0);
            choiceRoot = UIK.Rect(Root, "choices").Place(new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0, 360), new Vector2(900, 300));
            Next();
        }

        void Next()
        {
            idx++;
            if (idx >= convo.Lines.Length)
            {
                if (convo.Choices != null && convo.Choices.Length > 0 && choices == null) { ShowChoices(); return; }
                Finish(-1);
                return;
            }
            var line = convo.Lines[idx];
            full = Story.Expand(line.Text);
            shown = 0f;
            body.text = "";
            bool narr = string.IsNullOrEmpty(line.Who);
            body.fontStyle = narr ? FontStyle.Italic : FontStyle.Normal;
            body.color = narr ? Pal.Dim.Lit(1.2f) : Pal.Text;
            namePlate.gameObject.SetActive(!narr);
            if (!narr)
            {
                var who = Story.Who(line.Who);
                nameText.text = who.Name.ToUpper();
                namePlate.color = who.Color;
                bool right = line.Who == "narfi";
                var sp = string.IsNullOrEmpty(who.Art) ? null : CharArt.Get(who.Art, true);
                var img = right ? portraitR : portraitL;
                var other = right ? portraitL : portraitR;
                if (sp != null)
                {
                    img.sprite = sp;
                    float h = Mathf.Clamp(sp.rect.height * 4.2f, 420f, 720f);
                    float w = h * sp.rect.width / sp.rect.height;
                    img.rectTransform.Place(new Vector2(right ? 1f : 0f, 0f), new Vector2(right ? 1f : 0f, 0f), new Vector2(right ? -120f : 120f, 250f), new Vector2(w, h));
                    img.rectTransform.localScale = new Vector3(right ? -1f : 1f, 1f, 1f);
                    img.color = Color.white;
                    img.gameObject.SetActive(true);
                }
                else img.gameObject.SetActive(false);
                if (other.sprite != null) other.color = new Color(0.45f, 0.45f, 0.5f, 1f);
                namePlate.rectTransform.anchorMin = namePlate.rectTransform.anchorMax = new Vector2(right ? 1f : 0f, 1f);
                namePlate.rectTransform.pivot = new Vector2(right ? 1f : 0f, 0.5f);
                namePlate.rectTransform.anchoredPosition = new Vector2(right ? -60f : 60f, 0f);
            }
        }

        void ShowChoices()
        {
            choices = new MenuList();
            for (int i = 0; i < convo.Choices.Length; i++)
            {
                int k = i;
                var rt = UIK.Rect(choiceRoot, "c").Place(new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0, (convo.Choices.Length - 1 - i) * 70f), new Vector2(900, 62));
                var bg = UIK.Img(rt, SpriteLib.Panel, Color.white, "bg");
                bg.rectTransform.Stretch();
                var t = UIK.Txt(rt, convo.Choices[i], 30, Pal.Text, TextAnchor.MiddleCenter, FontStyle.Bold, "t");
                t.rectTransform.Stretch(20, 0, 20, 0);
                choices.Add(rt, () => Finish(k), on => { t.color = on ? Pal.Gold : Pal.Dim; rt.localScale = on ? new Vector3(1.04f, 1.04f, 1f) : Vector3.one; });
            }
            choices.Refresh();
        }

        void Finish(int choice)
        {
            if (finished) return;
            finished = true;
            Close();
            if (choice >= 0) convo.OnChoice?.Invoke(choice);
            done?.Invoke();
        }

        public override void Tick(GameInput inp)
        {
            if (choices != null) { choices.Tick(inp); return; }
            float dt = Time.unscaledDeltaTime;
            if (shown < full.Length)
            {
                shown = Mathf.Min(full.Length, shown + dt * 60f);
                tickT -= dt;
                if (tickT <= 0f) { tickT = 0.07f; Game.I.Audio.Play("ui_move", 0.12f, 0.6f, 0.2f); }
            }
            body.text = Visible(full, (int)shown);
            bool adv = inp.ConfirmDown || inp.ClickDown || inp.AttackDown;
            if (inp.CancelDown)
            {
                idx = convo.Lines.Length - 1;
                shown = full.Length;
                Next();
                return;
            }
            if (!adv) return;
            if (shown < full.Length) shown = full.Length;
            else Next();
        }

        /// <summary>Reveal n visible characters without breaking rich-text tags.</summary>
        static string Visible(string s, int n)
        {
            if (n >= s.Length) return s;
            var sb = new System.Text.StringBuilder();
            var stack = new System.Collections.Generic.Stack<string>();
            int count = 0;
            for (int i = 0; i < s.Length; i++)
            {
                if (s[i] == '<')
                {
                    int close = s.IndexOf('>', i);
                    if (close > i)
                    {
                        string tag = s.Substring(i, close - i + 1);
                        if (count >= n && !tag.StartsWith("</")) break;
                        sb.Append(tag);
                        if (tag.StartsWith("</")) { if (stack.Count > 0) stack.Pop(); }
                        else
                        {
                            string name = tag.Substring(1, tag.Length - 2);
                            int eq = name.IndexOf('=');
                            stack.Push(eq >= 0 ? name.Substring(0, eq) : name);
                        }
                        i = close;
                        continue;
                    }
                }
                if (count >= n) break;
                sb.Append(s[i]);
                count++;
            }
            while (stack.Count > 0) sb.Append("</" + stack.Pop() + ">");
            return sb.ToString();
        }
    }
}
