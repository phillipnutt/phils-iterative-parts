using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace PIP
{
    // PIP's own look for its window: flat dark panels, PIP orange, and
    // Bahnschrift (shipped with Windows) for names and numbers. Everything is
    // drawn from textures made here, so nothing ships but the code.
    // Built once and kept for the whole session (textures survive scene changes).
    static class PipSkin
    {
        public static readonly Color Void = Hex(0x090c13), Panel = Hex(0x111725), Panel2 = Hex(0x171f31), Panel3 = Hex(0x1e2840),
            Rail = Hex(0x0f1421), Hero = Hex(0x141b2b), Line = Hex(0x263049), Line2 = Hex(0x334063),
            Ink = Hex(0xe8ecf5), Ink2 = Hex(0x9ba7c0), Ink3 = Hex(0x66718b),
            Amber = Hex(0xffa62b), AmberInk = Hex(0x2a1700), Good = Hex(0x62d595), Bad = Hex(0xff7366), Warn = Hex(0xffcf5a);

        // Orange washed over a background, as an opaque color.
        public static Color Wash(Color under, float amount = 0.13f) => Color.Lerp(under, Amber, amount);
        public static readonly Color AmberLine = Color.Lerp(Panel, Amber, 0.45f);

        public static GUISkin Skin { get; private set; }
        public static Font Display, Body;

        public static GUIStyle Window, Title, TitleSub, Close, Pill,
            NewProgram, NewProgramOn, Row, RowOn, Group, Name, Meta, MkBadge, MkBadgeZero, BigMk,
            Eyebrow, Section, SectionRight, NameField, Dice, Tile, TileOn, TileSym, TileSymOn, TileLabel,
            TileLocked, TileSymLocked, TileLabelLocked, GoOff,
            SegBox, Seg, SegOn, SegLocked, SegLabel, LookCard, CardTitle, CardSub, Hint, Chevron,
            Caption, MkStrip, MkStripPast, MkStripOn, StripLabel,
            PointLabel, PointLabelOff, Stepper, StepButton, StepValue, Value, ValueTight, ValueSmall, SubLabel, SubValue,
            FootKey, FootValue, FootUnit, Compare, CompareWarn, CompareBad, Go,
            PickerTitle, Search, Chip, ChipOn, LookRow, LookRowOn, Tag, TagGood, TagWarn, TagBad, PickerNote,
            Big, XpLine, ProgChip, StatHead, StatHeadRight, StatLabel, StatSub, StatNow, StatTop,
            Small, Retire, Confirm, ConfirmButton, ConfirmDanger, Tooltip, Toast, Empty,
            SliderTrack, SliderThumb, TitleBox, RailBox, FootBox, SymBox, HeroBox, HeroSym, ThumbBox,
            RowCaption, Placeholder, TrackLabel, TrackLabelOn;

        static readonly Dictionary<Color, Texture2D> solids = new Dictionary<Color, Texture2D>();
        public static Texture2D Lock, Hatch, DiceIcon, PencilIcon;

        public static void Init()
        {
            if (Skin != null) return;
            Display = OsFont("Bahnschrift") ?? HighLogic.Skin.font;
            Body = OsFont("Segoe UI") ?? HighLogic.Skin.font;

            Skin = UnityEngine.Object.Instantiate(HighLogic.Skin);
            Skin.hideFlags = HideFlags.HideAndDontSave;
            Skin.font = Body;
            Skin.settings.cursorColor = Amber;
            Skin.settings.selectionColor = new Color(1f, 0.65f, 0.17f, 0.35f);
            Skin.verticalScrollbar = new GUIStyle { fixedWidth = 6, margin = new RectOffset(4, 2, 2, 2), normal = { background = Round(Panel2, Panel2, 3) }, border = new RectOffset(3, 3, 3, 3) };
            Skin.verticalScrollbarThumb = new GUIStyle { fixedWidth = 6, normal = { background = Round(Line2, Line2, 3) }, border = new RectOffset(3, 3, 3, 3) };
            Skin.verticalScrollbarUpButton = new GUIStyle();
            Skin.verticalScrollbarDownButton = new GUIStyle();
            Skin.horizontalScrollbar = new GUIStyle { fixedHeight = 0 };
            Skin.horizontalScrollbarThumb = new GUIStyle();
            Skin.horizontalScrollbarLeftButton = new GUIStyle();
            Skin.horizontalScrollbarRightButton = new GUIStyle();
            Skin.scrollView = new GUIStyle();

            Lock = LockTexture();
            Hatch = HatchTexture();
            DiceIcon = DiceTexture();
            PencilIcon = PencilTexture();

            Window = Box(Round(Panel, Line, 10), 11);
            Title = Text(Display, 19, Ink, FontStyle.Bold);
            TitleSub = Text(Body, 13, Ink3);
            Close = Button(null, Round(Panel3, Panel3, 6), 6, Text(Body, 20, Ink2, align: TextAnchor.MiddleCenter));
            Close.hover.textColor = Ink;
            Pill = Box(Round(Panel3, Panel3, 13), 13, Text(Display, 15, Ink, align: TextAnchor.MiddleCenter));
            Pill.padding = new RectOffset(12, 12, 0, 0);

            NewProgram = Button(Round(Rail, Line2, 8), Round(Rail, AmberLine, 8), 8, Text(Body, 14, Ink2, FontStyle.Bold, TextAnchor.MiddleCenter));
            NewProgram.hover.textColor = Ink;
            NewProgramOn = Button(Round(Wash(Rail), AmberLine, 8), null, 8, Text(Body, 14, Amber, FontStyle.Bold, TextAnchor.MiddleCenter));
            Row = Button(null, Round(Panel2, Panel2, 8), 8, new GUIStyle());
            RowOn = Button(Round(Panel3, Panel3, 8), null, 8, new GUIStyle());
            Group = Text(Body, 11, Ink3);
            Name = Text(Body, 14, Ink, FontStyle.Bold);
            Meta = Text(Body, 12, Ink3);
            MkBadge = Box(Round(Wash(Panel3, 0.16f), Wash(Panel3, 0.16f), 5), 5, Text(Display, 13, Amber, FontStyle.Bold, TextAnchor.MiddleCenter));
            MkBadgeZero = Box(Round(Panel3, Panel3, 5), 5, Text(Display, 13, Ink2, FontStyle.Bold, TextAnchor.MiddleCenter));
            BigMk = Box(Round(Amber, Amber, 7), 7, Text(Display, 22, AmberInk, FontStyle.Bold, TextAnchor.MiddleCenter));

            Eyebrow = Text(Body, 12, Ink3);
            Section = Text(Body, 11, Ink3);
            SectionRight = Text(Body, 12, Ink3, align: TextAnchor.MiddleRight);
            NameField = Field(Text(Display, 24, Ink, FontStyle.Bold), 8);
            Dice = Button(Round(Panel, Line2, 8), Round(Panel, AmberLine, 8), 8, Text(Display, 13, Ink2, FontStyle.Bold, TextAnchor.MiddleCenter));
            Dice.hover.textColor = Amber;
            Tile = Button(Round(Panel2, Line, 7), Round(Panel2, Line2, 7), 7, new GUIStyle());
            TileOn = Button(Round(Wash(Panel), Amber, 7), null, 7, new GUIStyle());
            TileSym = Text(Display, 16, Ink, FontStyle.Bold, TextAnchor.MiddleCenter);
            TileSymOn = Text(Display, 16, Amber, FontStyle.Bold, TextAnchor.MiddleCenter);
            TileLabel = Text(Body, 11, Ink3, align: TextAnchor.MiddleCenter);
            // Types not researched yet: dimmed, still clickable to look around.
            TileLocked = Button(Round(Void, Line, 7), Round(Void, Line2, 7), 7, new GUIStyle());
            TileSymLocked = Text(Display, 16, new Color(Ink3.r, Ink3.g, Ink3.b, 0.55f), FontStyle.Bold, TextAnchor.MiddleCenter);
            TileLabelLocked = Text(Body, 11, new Color(Ink3.r, Ink3.g, Ink3.b, 0.55f), align: TextAnchor.MiddleCenter);

            SegBox = Box(Round(Void, Line, 8), 8);
            Seg = Button(null, null, 5, Text(Body, 13, Ink2, align: TextAnchor.MiddleCenter));
            Seg.hover.textColor = Ink;
            SegOn = Button(Round(Panel3, Line2, 5), null, 5, Text(Body, 13, Ink, FontStyle.Bold, TextAnchor.MiddleCenter));
            SegLocked = Text(Body, 13, new Color(Ink3.r, Ink3.g, Ink3.b, 0.75f));
            SegLabel = Text(Body, 13, Ink2);

            LookCard = Button(Round(Panel2, Line, 9), Round(Panel2, AmberLine, 9), 9, new GUIStyle());
            CardTitle = Text(Body, 14, Ink, FontStyle.Bold);
            CardSub = Text(Body, 12, Ink3);
            Hint = Text(Body, 12, Ink3, wrap: true);
            Chevron = Text(Body, 20, Ink2, align: TextAnchor.MiddleCenter);

            Caption = Text(Body, 12, Ink2);
            MkStrip = Button(Round(Panel2, Panel2, 4), Round(Panel3, Panel3, 4), 4, Text(Display, 12, Ink3, align: TextAnchor.MiddleCenter));
            MkStripPast = Button(Round(Wash(Panel2, 0.18f), Wash(Panel2, 0.18f), 4), Round(Wash(Panel2, 0.28f), Wash(Panel2, 0.28f), 4), 4, Text(Display, 12, Ink2, align: TextAnchor.MiddleCenter));
            MkStripOn = Button(Round(Amber, Amber, 4), null, 4, Text(Display, 12, AmberInk, FontStyle.Bold, TextAnchor.MiddleCenter));
            StripLabel = Text(Body, 11, Ink3);

            PointLabel = Text(Body, 14, Ink, FontStyle.Bold, wrap: true);
            PointLabelOff = Text(Body, 14, Ink3, FontStyle.Bold, wrap: true);
            Stepper = Box(Round(Void, Line, 7), 7);
            StepButton = Button(null, null, 0, Text(Body, 17, Ink2, align: TextAnchor.MiddleCenter));
            StepButton.hover.textColor = Amber;
            StepValue = Text(Display, 15, Ink, FontStyle.Bold, TextAnchor.MiddleCenter);
            Value = Text(Display, 19, Amber, FontStyle.Bold, TextAnchor.MiddleRight);
            ValueTight = Text(Display, 14, Amber, FontStyle.Bold, TextAnchor.MiddleRight);
            ValueSmall = Text(Body, 11, Ink3, align: TextAnchor.MiddleRight);
            SubLabel = Text(Body, 12, Ink3);
            SubValue = Text(Body, 12, Ink2, align: TextAnchor.MiddleRight);

            FootKey = Text(Body, 11, Ink3);
            FootValue = Text(Display, 22, Ink, FontStyle.Bold);
            FootUnit = Text(Body, 13, Ink3);
            Compare = Text(Body, 13, Ink2, wrap: true);
            CompareWarn = Text(Body, 13, Warn, wrap: true);
            CompareBad = Text(Body, 13, Bad, wrap: true);
            Go = Button(Round(Amber, Amber, 9), Round(Color.Lerp(Amber, Color.white, 0.12f), Color.Lerp(Amber, Color.white, 0.12f), 9), 9,
                Text(Display, 17, AmberInk, FontStyle.Bold, TextAnchor.MiddleCenter));
            Go.active.background = Round(Color.Lerp(Amber, Color.black, 0.1f), Color.Lerp(Amber, Color.black, 0.1f), 9);
            Go.active.textColor = AmberInk;
            // Start while the design can't be started: gray, no hover.
            GoOff = Button(Round(Panel3, Line2, 9), Round(Panel3, Line2, 9), 9, Text(Display, 17, Ink3, FontStyle.Bold, TextAnchor.MiddleCenter));
            GoOff.hover.textColor = Ink3;
            GoOff.active.background = GoOff.normal.background;
            GoOff.active.textColor = Ink3;

            PickerTitle = Text(Display, 20, Ink, FontStyle.Bold);
            Search = Field(Text(Body, 14, Ink), 8);
            Search.padding = new RectOffset(10, 10, 0, 0);
            Chip = Button(Round(Panel, Line2, 12), Round(Panel2, Line2, 12), 12, Text(Body, 12, Ink2, align: TextAnchor.MiddleCenter));
            ChipOn = Button(Round(Wash(Panel), AmberLine, 12), null, 12, Text(Body, 12, Amber, align: TextAnchor.MiddleCenter));
            LookRow = Button(null, Round(Panel3, Panel3, 8), 8, new GUIStyle());
            LookRowOn = Button(Round(Wash(Panel2), Wash(Panel2), 8), null, 8, new GUIStyle());
            Tag = Box(Round(Panel3, Panel3, 4), 4, Text(Body, 11, Ink2, align: TextAnchor.MiddleCenter));
            TagGood = Box(Round(Color.Lerp(Panel2, Good, 0.14f), Color.Lerp(Panel2, Good, 0.14f), 4), 4, Text(Body, 11, Good, align: TextAnchor.MiddleCenter));
            TagWarn = Box(Round(Color.Lerp(Panel2, Warn, 0.14f), Color.Lerp(Panel2, Warn, 0.14f), 4), 4, Text(Body, 11, Warn, align: TextAnchor.MiddleCenter));
            TagBad = Box(Round(Color.Lerp(Panel2, Bad, 0.14f), Color.Lerp(Panel2, Bad, 0.14f), 4), 4, Text(Body, 11, Bad, align: TextAnchor.MiddleCenter));
            foreach (GUIStyle t in new[] { Tag, TagGood, TagWarn, TagBad }) t.padding = new RectOffset(7, 7, 0, 0);
            PickerNote = Text(Body, 12, Ink3, wrap: true);

            Big = Text(Display, 46, Ink, FontStyle.Bold);
            XpLine = Text(Body, 14, Ink2, wrap: true);
            ProgChip = Box(Round(Panel2, Line, 12), 12, Text(Body, 12, Ink2, align: TextAnchor.MiddleCenter));
            ProgChip.padding = new RectOffset(10, 10, 0, 0);
            ProgChip.richText = true;
            StatHead = Text(Body, 11, Ink3);
            StatHeadRight = Text(Body, 11, Ink3, align: TextAnchor.MiddleRight);
            StatLabel = Text(Body, 14, Ink, FontStyle.Bold);
            StatSub = Text(Body, 11, Ink3);
            StatNow = Text(Display, 18, Amber, FontStyle.Bold, TextAnchor.MiddleRight);
            StatTop = Text(Display, 15, Ink3, align: TextAnchor.MiddleRight);

            Small = Text(Body, 12, Ink3, wrap: true);
            Retire = Button(null, Round(Color.Lerp(Panel, Bad, 0.08f), Color.Lerp(Panel, Bad, 0.08f), 6), 6, Text(Body, 13, Ink3, align: TextAnchor.MiddleCenter));
            Retire.hover.textColor = Bad;
            Confirm = Text(Body, 13, Bad, wrap: true);
            ConfirmButton = Button(Round(Panel, Line2, 6), Round(Panel2, Line2, 6), 6, Text(Body, 13, Ink, align: TextAnchor.MiddleCenter));
            ConfirmDanger = Button(Round(Panel, Color.Lerp(Panel, Bad, 0.5f), 6), Round(Color.Lerp(Panel, Bad, 0.1f), Color.Lerp(Panel, Bad, 0.5f), 6), 6, Text(Body, 13, Bad, FontStyle.Bold, TextAnchor.MiddleCenter));
            Tooltip = Box(Round(Panel3, Line2, 7), 7, Text(Body, 13, Ink, wrap: true));
            Tooltip.padding = new RectOffset(10, 10, 7, 7);
            Toast = Box(Round(Panel3, Line2, 8), 8, Text(Body, 13, Ink, align: TextAnchor.MiddleCenter));
            Toast.padding = new RectOffset(14, 14, 0, 0);
            Empty = Text(Body, 14, Ink3, wrap: true);

            SliderTrack = new GUIStyle { padding = new RectOffset(0, 0, 0, 0) };
            SliderThumb = new GUIStyle { fixedWidth = 16, fixedHeight = 16, normal = { background = Dot(16, Amber, Panel) } };
            SliderThumb.hover.background = Dot(16, Color.Lerp(Amber, Color.white, 0.15f), Panel);
            SliderThumb.active.background = SliderThumb.hover.background;
            TitleBox = Box(Round(Panel2, Panel2, 10), 11);
            RailBox = Box(Round(Rail, Rail, 10), 11);
            FootBox = Box(Round(Panel2, Panel2, 10), 11);
            SymBox = Box(Round(Panel3, Line2, 6), 7, Text(Display, 13, Ink2, FontStyle.Bold, TextAnchor.MiddleCenter));
            HeroBox = Box(Round(Hero, Line, 10), 11);
            HeroSym = Text(Display, 44, Line2, FontStyle.Bold, TextAnchor.MiddleCenter);
            ThumbBox = Box(Round(Hero, Line, 7), 8);
            RowCaption = Text(Body, 11, Ink3, wrap: true);
            Placeholder = Text(Body, 14, Ink3);
            TrackLabel = Text(Display, 11, Ink3, align: TextAnchor.MiddleRight);
            TrackLabelOn = Text(Display, 11, Amber, FontStyle.Bold, TextAnchor.MiddleRight);
        }

        #region Drawing

        public static Texture2D Solid(Color c)
        {
            if (!solids.TryGetValue(c, out Texture2D t) || t == null) solids[c] = t = Make(1, 1, (x, y) => c);
            return t;
        }

        public static void Fill(Rect r, Color c)
        {
            if (Event.current.type == EventType.Repaint) GUI.DrawTexture(r, Solid(c));
        }

        public static void Draw(Rect r, GUIStyle s, string text = null)
        {
            if (Event.current.type == EventType.Repaint) s.Draw(r, text == null ? GUIContent.none : new GUIContent(text), false, false, false, false);
        }

        // A bar with rounded ends where it's wide enough to show them.
        static readonly Dictionary<Color, GUIStyle> bars = new Dictionary<Color, GUIStyle>();
        public static void Bar(Rect r, Color c)
        {
            if (r.width <= 0 || Event.current.type != EventType.Repaint) return;
            if (r.width < r.height + 2) { GUI.DrawTexture(r, Solid(c)); return; }
            if (!bars.TryGetValue(c, out GUIStyle s)) bars[c] = s = Box(Round(c, c, 3), 3);
            s.Draw(r, false, false, false, false);
        }

        public static void Tiled(Rect r, Texture2D tex)
        {
            if (Event.current.type == EventType.Repaint && r.width > 0)
                GUI.DrawTextureWithTexCoords(r, tex, new Rect(0, 0, r.width / tex.width, r.height / tex.height));
        }

        public static float Width(GUIStyle s, string text) => s.CalcSize(new GUIContent(text)).x;

        #endregion

        #region Making styles and textures

        static Font OsFont(string name)
        {
            try
            {
                if (!Font.GetOSInstalledFontNames().Contains(name)) return null;
                Font f = Font.CreateDynamicFontFromOSFont(name, 14);
                if (f != null) f.hideFlags = HideFlags.HideAndDontSave;
                return f;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[PIP] Font {name} unavailable: {e.Message}");
                return null;
            }
        }

        static Color Hex(int rgb) => new Color(((rgb >> 16) & 255) / 255f, ((rgb >> 8) & 255) / 255f, (rgb & 255) / 255f);

        static GUIStyle Text(Font font, int size, Color color, FontStyle style = FontStyle.Normal, TextAnchor align = TextAnchor.MiddleLeft, bool wrap = false)
        {
            var s = new GUIStyle { font = font, fontSize = size, fontStyle = style, alignment = align, wordWrap = wrap, clipping = TextClipping.Clip };
            s.normal.textColor = s.hover.textColor = s.active.textColor = s.focused.textColor = color;
            return s;
        }

        static GUIStyle Box(Texture2D bg, int border, GUIStyle text = null)
        {
            var s = new GUIStyle(text ?? new GUIStyle()) { border = new RectOffset(border, border, border, border) };
            s.normal.background = bg;
            return s;
        }

        static GUIStyle Button(Texture2D normal, Texture2D hover, int border, GUIStyle text)
        {
            var s = new GUIStyle(text) { border = new RectOffset(border, border, border, border) };
            s.normal.background = normal;
            s.hover.background = hover ?? normal;
            s.active.background = hover ?? normal;
            return s;
        }

        static GUIStyle Field(GUIStyle text, int radius)
        {
            var s = new GUIStyle(text) { border = new RectOffset(radius, radius, radius, radius), padding = new RectOffset(12, 12, 0, 0), clipping = TextClipping.Clip };
            s.normal.background = Round(Void, Line2, radius);
            s.hover.background = Round(Void, Color.Lerp(Line2, Ink3, 0.4f), radius);
            s.focused.background = Round(Void, AmberLine, radius);
            s.active.background = s.focused.background;
            return s;
        }

        static Texture2D Make(int w, int h, Func<int, int, Color> pixel, TextureWrapMode wrap = TextureWrapMode.Clamp)
        {
            var t = new Texture2D(w, h, TextureFormat.ARGB32, false) { wrapMode = wrap, filterMode = FilterMode.Bilinear, hideFlags = HideFlags.HideAndDontSave };
            var px = new Color[w * h];
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++) px[y * w + x] = pixel(x, y);
            t.SetPixels(px);
            t.Apply(false, true);
            return t;
        }

        // Signed distance from a pixel centre to a rounded rectangle filling the texture.
        static float RoundDistance(int x, int y, int size, float radius)
        {
            float half = size / 2f;
            float qx = Mathf.Abs(x + 0.5f - half) - (half - radius), qy = Mathf.Abs(y + 0.5f - half) - (half - radius);
            float outside = new Vector2(Mathf.Max(qx, 0), Mathf.Max(qy, 0)).magnitude;
            return outside + Mathf.Min(Mathf.Max(qx, qy), 0) - radius;
        }

        // A rounded rectangle with a 1-pixel border, for nine-slicing (border = radius + 1).
        public static Texture2D Round(Color fill, Color border, int radius)
        {
            int size = 2 * radius + 3;
            return Make(size, size, (x, y) =>
            {
                float d = RoundDistance(x, y, size, radius);
                Color c = Color.Lerp(fill, border, Mathf.Clamp01(d + 1.5f));
                c.a *= Mathf.Clamp01(0.5f - d);
                return c;
            });
        }

        // A die: a rounded square outline with four pips, for "suggest a name".
        static Texture2D DiceTexture()
        {
            const int size = 22;
            var pips = new[] { new Vector2(7.5f, 7.5f), new Vector2(14.5f, 14.5f), new Vector2(14.5f, 7.5f), new Vector2(7.5f, 14.5f) };
            return Make(size, size, (x, y) =>
            {
                float px = x + 0.5f, py = y + 0.5f;
                float qx = Mathf.Abs(px - 11) - 5.5f, qy = Mathf.Abs(py - 11) - 5.5f;
                float d = new Vector2(Mathf.Max(qx, 0), Mathf.Max(qy, 0)).magnitude + Mathf.Min(Mathf.Max(qx, qy), 0) - 3.5f;
                float a = Mathf.Clamp01(1.3f - Mathf.Abs(d));
                foreach (Vector2 pip in pips) a = Mathf.Max(a, Mathf.Clamp01(1.9f - (new Vector2(px, py) - pip).magnitude));
                Color c = Ink2;
                c.a = a;
                return c;
            });
        }

        // A pencil, point down to the left, for "rename". Its body is a capsule; the
        // point tapers to nothing; a thin gap near the top marks the eraser.
        static Texture2D PencilTexture()
        {
            const int size = 22;
            Vector2 tip = new Vector2(4, 4), shoulder = new Vector2(8.5f, 8.5f), end = new Vector2(17.5f, 17.5f);
            return Make(size, size, (x, y) =>
            {
                var p = new Vector2(x + 0.5f, y + 0.5f);
                float along = Vector2.Dot(p - tip, (end - tip).normalized);
                float off = (p - tip - (end - tip).normalized * Mathf.Clamp(along, 0, (end - tip).magnitude)).magnitude;
                float shoulderAt = (shoulder - tip).magnitude;
                float radius = along < shoulderAt ? Mathf.Lerp(0.4f, 2.6f, Mathf.Clamp01(along / shoulderAt)) : 2.6f;
                float a = Mathf.Clamp01(radius + 0.5f - off);
                if (along > (end - tip).magnitude) a = 0;
                // The eraser band.
                float band = Mathf.Abs(along - ((end - tip).magnitude - 3.2f));
                if (band < 0.6f) a *= 0.15f;
                Color c = Ink2;
                c.a = a;
                return c;
            });
        }

        // The slider knob: a filled dot with a ring the color of the panel.
        static Texture2D Dot(int size, Color fill, Color ring)
        {
            float r = size / 2f;
            return Make(size, size, (x, y) =>
            {
                float d = new Vector2(x + 0.5f - r, y + 0.5f - r).magnitude;
                Color c = d < r - 4f ? fill : Color.Lerp(fill, ring, Mathf.Clamp01(d - (r - 4f)));
                if (d > r - 1.5f) c = Color.Lerp(ring, fill, 0.6f);
                c.a = Mathf.Clamp01(r - d + 0.5f);
                return c;
            });
        }

        // Diagonal stripes for points past the cap.
        static Texture2D HatchTexture() =>
            Make(8, 8, (x, y) => ((x + y) % 8) < 4 ? Hex(0x141a29) : Hex(0x1c2436), TextureWrapMode.Repeat);

        // A small padlock for options that still need research.
        static Texture2D LockTexture()
        {
            const int w = 10, h = 12;
            return Make(w, h, (x, y) =>
            {
                float px = x + 0.5f, py = y + 0.5f;             // y up from the bottom
                bool body = px >= 1 && px <= 9 && py >= 0.5f && py <= 6.5f;
                float d = new Vector2(px - 5, py - 7f).magnitude;
                bool shackle = py >= 6.5f && d >= 2f && d <= 3.4f;
                Color c = Ink3;
                c.a = body || shackle ? 1 : 0;
                return c;
            });
        }

        #endregion
    }
}
