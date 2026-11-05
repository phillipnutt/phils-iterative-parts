using System;
using System.Collections.Generic;
using System.Linq;
using KSP.UI.Screens;
using UnityEngine;

namespace PIP
{
    [KSPAddon(KSPAddon.Startup.SpaceCentre, false)]
    public class PipWindowKsc : PipWindow { }

    [KSPAddon(KSPAddon.Startup.EditorAny, false)]
    public class PipWindowEditor : PipWindow { }

    // The program window. Programs on the left; on the right, the new-program
    // screen (what you're building on the left half, the part and its points on
    // the right half, cost and Start along the bottom), a program's record, or
    // the look picker. Everything is drawn from the part type's choices,
    // categories and rows, so every type uses the same window.
    public abstract class PipWindow : MonoBehaviour
    {
        const string LockId = "PIP.Window";
        const string IconPath = "PIP/Icons/toolbar";
        const float W = 1100, H = 720, TitleH = 46, RailW = 240, CfgW = 400, FootH = 76, Pad = 20;

        static readonly string[] SuggestedNames =
        {
            "Kestrel", "Harrier", "Comet", "Tempest", "Zephyr", "Corsair", "Sabre", "Talon", "Ember", "Aurora",
            "Halberd", "Lancer", "Mistral", "Pioneer", "Sirocco", "Valkyrie", "Peregrine", "Gale", "Vortex", "Osprey",
            "Albatross", "Bolide", "Cirrus", "Dervish", "Equinox", "Falcon", "Gannet", "Heron", "Ibis", "Jaeger",
        };

        // The tile symbol and short name of each type.
        static readonly Dictionary<PartType, string[]> Short = new Dictionary<PartType, string[]>
        {
            { PartTypes.Liquid, new[] { "Lq", "Liquid" } }, { PartTypes.Solid, new[] { "So", "Solid" } },
            { PartTypes.Monoprop, new[] { "Mp", "Monoprop" } }, { PartTypes.Nuclear, new[] { "Nu", "Nuclear" } },
            { PartTypes.Electric, new[] { "El", "Electric" } }, { PartTypes.Jet, new[] { "Jt", "Jet" } },
            { PartTypes.Rcs, new[] { "Rc", "RCS" } }, { PartTypes.Battery, new[] { "Bt", "Batteries" } },
            { PartTypes.Wheel, new[] { "Rw", "Wheels" } }, { PartTypes.Shield, new[] { "Hs", "Shields" } },
            { PartTypes.Solar, new[] { "Sp", "Solar" } }, { PartTypes.Antenna, new[] { "An", "Antennas" } },
            { PartTypes.Probe, new[] { "Pc", "Probes" } }, { PartTypes.Tank, new[] { "Tk", "Tanks" } },
            { PartTypes.Rtg, new[] { "Rt", "RTGs" } }, { PartTypes.Intake, new[] { "In", "Intakes" } },
        };
        static string Sym(PartType t) => Short.TryGetValue(t, out string[] s) ? s[0] : t.Id.Substring(0, 2);
        static string ShortName(PartType t) => Short.TryGetValue(t, out string[] s) ? s[1] : t.Title;

        // Kept across scene changes so a half-designed program isn't lost.
        static string newName;
        static PartType newType = PartTypes.Liquid;
        static readonly Dictionary<string, string[]> models = new Dictionary<string, string[]>();
        static readonly Dictionary<string, int[]> designs = new Dictionary<string, int[]>();
        static readonly Dictionary<string, string> previewVariants = new Dictionary<string, string>();
        static readonly Dictionary<string, string> looks = new Dictionary<string, string>();       // "" = the standard look
        static readonly Dictionary<string, string> sideLooks = new Dictionary<string, string>();
        static int previewMk = 4;
        static string selectedId;
        static bool creating = true;
        static Rect window = new Rect(-1, -1, W, H);

        // The look picker: closed, or open for the inline or side-mount look.
        static bool pickerOpen, pickerSide;
        static string search = "";
        static int filter;    // 0 all, 1 same fuel, 2 gimbals

        ApplicationLauncherButton button;
        bool visible, wasVisible, heroShown, retiredOpen;
        Vector2 listScroll, cfgScroll, pointsScroll, pickerScroll;
        float listHeight, cfgHeight, pointsHeight;
        string message;
        bool retireArmed;
        PartPreview preview;
        Texture logo;
        readonly Dictionary<string, bool> gimbalCache = new Dictionary<string, bool>();
        static readonly Dictionary<string, string> modNames = new Dictionary<string, string>();

        void Start()
        {
            if (window.x < 0) window = new Rect(Screen.width / 2f - W / 2, Screen.height / 2f - H / 2, W, H);
            if (string.IsNullOrEmpty(newName)) newName = SuggestName();
            preview = new PartPreview();
            GameEvents.onGUIApplicationLauncherReady.Add(AddButton);
            if (ApplicationLauncher.Ready) AddButton();
        }

        void OnDestroy()
        {
            GameEvents.onGUIApplicationLauncherReady.Remove(AddButton);
            if (button != null && ApplicationLauncher.Instance != null) ApplicationLauncher.Instance.RemoveModApplication(button);
            InputLockManager.RemoveControlLock(LockId);
            preview?.Dispose();
        }

        void AddButton()
        {
            if (button != null || ApplicationLauncher.Instance == null || !PipSettings.AnyOn) return;
            Texture icon = GameDatabase.Instance.GetTexture(IconPath, false) ?? new Texture2D(38, 38);
            button = ApplicationLauncher.Instance.AddModApplication(
                () => visible = true, () => visible = false, () => { }, () => { }, () => { }, () => { },
                ApplicationLauncher.AppScenes.SPACECENTER | ApplicationLauncher.AppScenes.VAB | ApplicationLauncher.AppScenes.SPH,
                icon);
        }

        void Close()
        {
            if (button != null) button.SetFalse();
            else visible = false;
        }

        static string SuggestName(string after = null)
        {
            var used = PipScenario.Instance?.Programs.Select(p => p.Name).ToList() ?? new List<string>();
            int start = after == null ? 0 : Array.IndexOf(SuggestedNames, after) + 1;
            for (int i = 0; i < SuggestedNames.Length; i++)
            {
                string n = SuggestedNames[(start + i) % SuggestedNames.Length];
                if (!used.Contains(n) && n != after) return n;
            }
            return "Program " + (used.Count + 1);
        }

        static string[] ModelOf(PartType t)
        {
            if (!models.TryGetValue(t.Id, out string[] m) || m.Length != t.Choices.Length) models[t.Id] = m = t.DefaultModel;
            return m;
        }

        static int[] PointsOf(PartType t)
        {
            if (!designs.TryGetValue(t.Id, out int[] p) || p.Length != t.Categories.Length) designs[t.Id] = p = t.DefaultPoints;
            return p;
        }

        // The toolbar button follows the game's settings: none while every PIP
        // type is off (a game started before PIP), back when one is switched on.
        void Update()
        {
            if (button == null)
            {
                if (ApplicationLauncher.Ready && PipScenario.Instance != null && PipSettings.AnyOn) AddButton();
            }
            else if (!PipSettings.AnyOn && ApplicationLauncher.Instance != null)
            {
                ApplicationLauncher.Instance.RemoveModApplication(button);
                button = null;
                visible = false;
            }
        }

        void LateUpdate()
        {
            if (visible && PipScenario.Instance != null) preview?.Tick(heroShown);
        }

        void OnGUI()
        {
            // Opening on a type that can't be built yet: show one that can instead.
            if (visible && !wasVisible && PipScenario.Instance != null && !TechUnlocks.Opened(newType))
                newType = PartTypes.All.FirstOrDefault(t => PipSettings.On(t) && TechUnlocks.Opened(t)) ?? newType;
            wasVisible = visible;
            if (!visible || PipScenario.Instance == null)
            {
                InputLockManager.RemoveControlLock(LockId);
                return;
            }
            PipSkin.Init();
            GUI.skin = PipSkin.Skin;
            if (Event.current.type == EventType.Layout) heroShown = false;
            window = GUI.Window(GetInstanceID(), window, DrawWindow, GUIContent.none, PipSkin.Window);
            window.x = Mathf.Clamp(window.x, -W + 120, Screen.width - 120);
            window.y = Mathf.Clamp(window.y, 0, Screen.height - 60);

            var mouse = new Vector2(Input.mousePosition.x, Screen.height - Input.mousePosition.y);
            if (window.Contains(mouse))
                InputLockManager.SetControlLock(ControlTypes.ALLBUTCAMERAS | ControlTypes.EDITOR_LOCK, LockId);
            else InputLockManager.RemoveControlLock(LockId);
        }

        void DrawWindow(int id)
        {
            PipScenario scenario = PipScenario.Instance;
            DrawTitleBar();
            DrawRail(scenario);

            var main = new Rect(RailW + 1, TitleH + 1, W - RailW - 2, H - TitleH - 2);
            GUI.BeginGroup(main);
            var area = new Rect(0, 0, main.width, main.height);
            Program selected = scenario.Find(selectedId);
            if (pickerOpen && pickerFor != null && !creating && selected != null && selected.Id == pickerFor)
                DrawPicker(area, selected.Type, selected.Model, selected);
            else if (creating || selected == null)
            {
                if (pickerOpen && pickerFor == null) DrawPicker(area, newType, ModelOf(newType));
                else DrawNew(scenario, area);
            }
            else DrawProgram(scenario, selected, area);
            GUI.EndGroup();

            DrawTooltip();
            GUI.DragWindow(new Rect(0, 0, W - 56, TitleH));
        }

        #region Frame

        void DrawTitleBar()
        {
            PipSkin.Draw(new Rect(1, 1, W - 2, TitleH), PipSkin.TitleBox);
            PipSkin.Fill(new Rect(1, TitleH / 2, W - 2, TitleH / 2), PipSkin.Panel2);
            PipSkin.Fill(new Rect(0, TitleH, W, 1), PipSkin.Line);

            if (logo == null) logo = GameDatabase.Instance.GetTexture(IconPath, false);
            if (logo != null && Event.current.type == EventType.Repaint) GUI.DrawTexture(new Rect(14, 11, 24, 24), logo, ScaleMode.ScaleToFit);
            GUI.Label(new Rect(46, 0, 60, TitleH), "PIP", PipSkin.Title);
            GUI.Label(new Rect(46 + PipSkin.Width(PipSkin.Title, "PIP") + 10, 0, 200, TitleH), "Phil's Iterative Parts", PipSkin.TitleSub);

            string funds = PipScenario.Free ? "Programs are free in this game"
                : Funding.Instance != null ? "√ " + Funding.Instance.Funds.ToString("N0") : null;
            if (funds != null)
            {
                float fw = PipSkin.Width(PipSkin.Pill, funds) + 24;
                PipSkin.Draw(new Rect(W - 50 - fw, 10, fw, 26), PipSkin.Pill, funds);
            }
            if (GUI.Button(new Rect(W - 42, 8, 30, 30), new GUIContent("×", "Close"), PipSkin.Close)) Close();
        }

        void DrawRail(PipScenario scenario)
        {
            float top = TitleH + 1, h = H - TitleH - 2;
            PipSkin.Draw(new Rect(1, top, RailW + 12, h), PipSkin.RailBox);
            PipSkin.Fill(new Rect(RailW, top, 14, h), PipSkin.Panel);
            PipSkin.Fill(new Rect(RailW, top, 1, h), PipSkin.Line);

            if (GUI.Button(new Rect(12, top + 12, RailW - 24, 40), "+   New program", creating ? PipSkin.NewProgramOn : PipSkin.NewProgram))
            {
                creating = true;
                message = null;
            }

            var view = new Rect(0, top + 62, RailW, h - 64);
            listScroll = GUI.BeginScrollView(view, listScroll, new Rect(0, 0, RailW - 14, Mathf.Max(listHeight, view.height - 1)));
            float y = 0, x = 8, w = RailW - 26;
            foreach (PartType t in PartTypes.All)
            {
                List<Program> ofType = scenario.Active.Where(p => p.Type == t).ToList();
                if (ofType.Count == 0) continue;
                GUI.Label(new Rect(x + 8, y + 8, w, 18), t.Title.ToUpperInvariant(), PipSkin.Group);
                y += 30;
                foreach (Program p in ofType)
                {
                    ProgramRow(new Rect(x, y, w, 54), p);
                    y += 56;
                }
            }
            List<Program> retired = scenario.Programs.Where(p => p.Retired).ToList();
            if (retired.Count > 0)
            {
                y += 6;
                if (GUI.Button(new Rect(x, y, w, 26), "", PipSkin.Row)) retiredOpen = !retiredOpen;
                GUI.Label(new Rect(x + 8, y, w - 16, 26), "RETIRED", PipSkin.Group);
                GUI.Label(new Rect(x + 8, y, w - 16, 26), $"{retired.Count}  {(retiredOpen ? "▾" : "›")}", PipSkin.SectionRight);
                y += 28;
                if (retiredOpen)
                    foreach (Program p in retired)
                    {
                        GUI.Label(new Rect(x + 8, y, w - 16, 22), p.DisplayName(), PipSkin.Meta);
                        y += 22;
                    }
            }
            if (scenario.Programs.Count == 0)
            {
                GUI.Label(new Rect(x + 8, y + 8, w - 16, 60), "No programs yet. Start one with New program.", PipSkin.Small);
                y += 70;
            }
            if (Event.current.type == EventType.Repaint) listHeight = y + 10;
            GUI.EndScrollView();
        }

        void ProgramRow(Rect r, Program p)
        {
            bool on = !creating && selectedId == p.Id;
            if (GUI.Button(r, GUIContent.none, on ? PipSkin.RowOn : PipSkin.Row))
            {
                creating = false;
                selectedId = p.Id;
                retireArmed = false;
                message = null;
            }
            if (on) PipSkin.Fill(new Rect(r.x - 8, r.y + 11, 3, 32), PipSkin.Amber);
            PipSkin.Draw(new Rect(r.x + 8, r.y + 11, 30, 30), PipSkin.SymBox, Sym(p.Type));
            string mk = "Mk " + p.Mk;
            float mw = PipSkin.Width(PipSkin.MkBadge, mk) + 14;
            PipSkin.Draw(new Rect(r.xMax - 8 - mw, r.y + 10, mw, 20), p.Mk > 0 ? PipSkin.MkBadge : PipSkin.MkBadgeZero, mk);
            GUI.Label(new Rect(r.x + 48, r.y + 7, r.width - 60 - mw, 20), p.Title, PipSkin.Name);
            GUI.Label(new Rect(r.x + 48, r.y + 26, r.width - 56, 16), p.Label, PipSkin.Meta);
            var bar = new Rect(r.x + 48, r.y + 46, r.width - 56, 3);
            PipSkin.Fill(bar, PipSkin.Panel3);
            bar.width *= XpShare(p);
            PipSkin.Fill(bar, PipSkin.Amber);
        }

        // How far a program is from its current Mk to the next (1 when fully developed).
        static float XpShare(Program p)
        {
            if (p.Mk >= Progress.MaxMk) return 1;
            int lo = Progress.XpForMk(p.Mk), hi = Progress.XpForMk(p.Mk + 1);
            return hi > lo ? Mathf.Clamp01((p.Xp - lo) / (float)(hi - lo)) : 1;
        }

        void DrawTooltip()
        {
            string tip = GUI.tooltip;
            if (string.IsNullOrEmpty(tip) || Event.current.type != EventType.Repaint) return;
            var content = new GUIContent(tip);
            float w = Mathf.Min(340, PipSkin.Tooltip.CalcSize(content).x + 2);
            float h = PipSkin.Tooltip.CalcHeight(content, w);
            Vector2 m = Event.current.mousePosition;
            var r = new Rect(m.x + 14, m.y + 20, w, h);
            if (r.xMax > W - 6) r.x = m.x - w - 8;
            if (r.yMax > H - 6) r.y = m.y - h - 8;
            PipSkin.Tooltip.Draw(r, content, false, false, false, false);
        }

        #endregion

        #region New program

        void DrawNew(PipScenario scenario, Rect area)
        {
            if (!PipSettings.On(newType)) newType = PartTypes.All.FirstOrDefault(PipSettings.On) ?? PartTypes.Liquid;
            PartType type = newType;
            string[] model = ModelOf(type);
            int[] pts = PointsOf(type);
            bool valid = type.Valid(model);
            // The size, for types that have sizes: part of the design (DESIGN.md section 14).
            string variant = valid ? SizeOf(type, model) : null;
            bool side = PartType.IsSide(variant);
            // Gimbal, a free pick for some engines (DESIGN.md 18.4).
            string gimbal = valid ? GimbalOf(type, model) : "";
            bool unlocked = valid && TechUnlocks.Available(type, model, variant) &&
                            (!type.HasGimbalPick || TechUnlocks.Available(type, PartType.GimbalOptions.First(o => o.Code == gimbal)));

            bool Used(int i) => valid && type.CategoryUsed(model, i);
            int cap = TechUnlocks.Cap(type);
            for (int i = 0; i < pts.Length; i++) pts[i] = Used(i) ? Math.Min(pts[i], cap) : 0;

            DrawConfig(new Rect(0, 0, CfgW, area.height - FootH), type, model);
            PipSkin.Fill(new Rect(CfgW, 0, 1, area.height - FootH), PipSkin.Line);

            Stats s0 = null, sN = null, s10 = null;
            FailureLevel level = PipSettings.Failures;
            var right = new Rect(CfgW + 1 + Pad, 18, area.width - CfgW - 1 - 2 * Pad, area.height - FootH - 18);
            float y = right.y;

            // The part, turning: the side-mount look for a side-mount program.
            string look = !type.HasLooks ? null
                : side ? SideLookOf(type, model)
                : LookOf(type, model) is string l && l != "" ? l : PartFactory.StandardSource(type, model);
            string sizeNote = variant != null && type.Variants(model)?.Length + (type.SideVariants(model)?.Length ?? 0) > 1
                ? "  ·  " + type.VariantLabel(model, variant) : "";
            Hero(new Rect(right.x, y, right.width, 196), look ?? (valid ? type.ChassisPart(model) : null), type,
                $"{(string.IsNullOrWhiteSpace(newName) ? "Unnamed" : newName.Trim())} {Progress.MkName(previewMk)}" + sizeNote +
                (look != null ? "  ·  " + Comparison.ShortName(Looks.Title(look)) + " look" : ""));
            y += 196 + 12;

            // Which Mk to preview.
            GUI.Label(new Rect(right.x, y, 84, 24), "PREVIEW AT", PipSkin.StripLabel);
            previewMk = Mathf.Clamp(previewMk, Progress.MinMk, Progress.MaxMk);
            int count = Progress.MaxMk - Progress.MinMk + 1;
            float sw = (right.width - 84 - 3 * (count - 1)) / count;
            for (int mk = Progress.MinMk; mk <= Progress.MaxMk; mk++)
            {
                var r = new Rect(right.x + 84 + (mk - Progress.MinMk) * (sw + 3), y, sw, 24);
                GUIStyle st = mk == previewMk ? PipSkin.MkStripOn : mk < previewMk ? PipSkin.MkStripPast : PipSkin.MkStrip;
                if (GUI.Button(r, new GUIContent("Mk " + mk, "Preview " + Progress.MkName(mk)), st)) previewMk = mk;
            }
            y += 24 + 10;

            if (valid)
            {
                int deg = PartType.GimbalDegrees(gimbal);
                s0 = type.Compute(model, pts, Progress.MinMk, level, variant, null, deg);
                sN = type.Compute(model, pts, previewMk, level, variant, null, deg);
                s10 = type.Compute(model, pts, Progress.MaxMk, level, variant, null, deg);
                DrawPoints(new Rect(right.x, y, right.width, right.yMax - y - 8), type, model, pts, cap, Used, -1, s0, sN, s10);
            }

            DrawFooter(scenario, new Rect(0, area.height - FootH, area.width, FootH), type, model, variant, gimbal, pts, valid, unlocked, s0, sN, s10);
        }

        // The gimbal picked for a new program of this type: one that's researched,
        // and 0° when the model or look can't gimbal.
        static readonly Dictionary<string, string> gimbals = new Dictionary<string, string>();
        static string GimbalOf(PartType type, string[] model)
        {
            if (!type.HasGimbalPick) return "";
            if (!CanGimbalNow(type, model)) return "0";
            if (!gimbals.TryGetValue(type.Id, out string g) || PartType.GimbalOptions.All(o => o.Code != g)) gimbals[type.Id] = g = "5";
            if (!TechUnlocks.Available(type, PartType.GimbalOptions.First(o => o.Code == g)))
                g = PartType.GimbalOptions.Where(o => TechUnlocks.Available(type, o)).Select(o => o.Code).LastOrDefault() ?? "0";
            return g;
        }

        static bool CanGimbalNow(PartType type, string[] model) =>
            type.CanGimbal(model) && (!type.HasLooks || Looks.Gimbals(type, LookOf(type, model), model));

        // Every size a new program of this type can have: in-line, then side-mount
        // when a side-mount look is installed. Null for types without sizes.
        static Option[] SizeChoices(PartType type, string[] model)
        {
            if (!type.Valid(model)) return null;
            Option[] inline = type.Variants(model);
            if (inline == null) return null;
            Option[] sides = type.HasSideMount && type.HasLooks && Looks.List(type, true, model).Count > 0 ? type.SideVariants(model) : null;
            return sides == null ? inline : inline.Concat(sides).ToArray();
        }

        // The size picked for a new program of this type (kept while switching types).
        static string SizeOf(PartType type, string[] model)
        {
            Option[] all = SizeChoices(type, model);
            if (all == null) return null;
            if (!previewVariants.TryGetValue(type.Id, out string v) || !all.Any(o => o.Code == v))
                previewVariants[type.Id] = v = type.DefaultVariant(model);
            // A size that isn't researched yet (the 1.25 m probe core at the start of a
            // career) moves to the nearest one that is, smaller first, of the same mount.
            if (SizeLocked(type, type.VariantOption(model, v)) != null)
            {
                bool side = PartType.IsSide(v);
                Option[] mount = all.Where(o => PartType.IsSide(o.Code) == side).ToArray();
                int at = Math.Max(0, Array.FindIndex(mount, o => o.Code == v));
                Option open = mount.Take(at).Reverse().Concat(mount.Skip(at + 1)).FirstOrDefault(o => SizeLocked(type, o) == null);
                if (open != null) previewVariants[type.Id] = v = open.Code;
            }
            return v;
        }

        // Why a size can't be picked yet, or null.
        static string SizeLocked(PartType type, Option o) =>
            o == null || TechUnlocks.Available(type, o) ? null : "Research " + TechUnlocks.Title(TechUnlocks.Opened(type) ? o.Tech : type.CapTechs[0]);

        // The size rows of the design: mount (in-line or side-mount), then size, then a
        // tank's length. Each is fixed for the program's life.
        float DrawSize(float x, float y, float w, PartType type, string[] model, Option[] all)
        {
            string current = SizeOf(type, model);
            Option[] sides = all.Where(o => PartType.IsSide(o.Code)).ToArray(), inline = all.Where(o => !PartType.IsSide(o.Code)).ToArray();
            bool side = PartType.IsSide(current);
            void SizeRow(string label, string[] labels, int selected, Func<int, string> locked, Action<int> pick)
            {
                float rowY = y;
                int picked = Segments(x + 64, ref y, w - 64, labels, selected, locked, out bool shortened);
                GUI.Label(new Rect(x, rowY, 62, 34), label + (shortened ? ", m" : ""), PipSkin.SegLabel);
                if (picked >= 0) { pick(picked); message = null; }
            }

            if (sides.Length > 0)
                SizeRow("Mount", new[] { "In-line", "Side-mount" }, side ? 1 : 0, i => null, i =>
                {
                    if ((i == 1) == side) return;
                    previewVariants[type.Id] = i == 1 ? sides[0].Code : type.DefaultVariant(model);
                });
            current = SizeOf(type, model);
            side = PartType.IsSide(current);
            Option[] list = side ? sides : inline;
            if (list.Length <= 1) return y;

            // "1.25 m long": a row for the diameter, one for the length.
            string[][] split = list.Select(v => SplitVariant(v.Label)).ToArray();
            if (split.All(s => s != null) && split.Select(s => s[0]).Distinct().Count() > 1 && split.Select(s => s[1]).Distinct().Count() > 1)
            {
                string[] diameters = split.Select(s => s[0]).Distinct().ToArray(), lengths = split.Select(s => s[1]).Distinct().ToArray();
                int Find(string a, string b) => Array.FindIndex(split, s => s[0] == a && s[1] == b);
                int cur = Math.Max(0, Array.FindIndex(list, v => v.Code == current));
                string size = split[cur][0], length = split[cur][1];
                SizeRow("Size", diameters, Array.IndexOf(diameters, size), i => SizeLocked(type, list[Array.FindIndex(split, s => s[0] == diameters[i])]), i =>
                {
                    int k = Find(diameters[i], length);
                    if (k < 0) k = Array.FindIndex(split, s => s[0] == diameters[i]);
                    previewVariants[type.Id] = list[k].Code;
                });
                cur = Math.Max(0, Array.FindIndex(list, v => v.Code == SizeOf(type, model)));
                size = split[cur][0];
                SizeRow("Length", lengths.Select(l => char.ToUpperInvariant(l[0]) + l.Substring(1)).ToArray(), Array.IndexOf(lengths, split[cur][1]),
                    i => Find(size, lengths[i]) < 0 ? "Not made at " + size : SizeLocked(type, list[Find(size, lengths[i])]),
                    i => previewVariants[type.Id] = list[Find(size, lengths[i])].Code);
                // What each length holds at this diameter, as the tank comes.
                if (type == PartTypes.Tank)
                {
                    string holds = string.Join("  ·  ", lengths.Where(l => Find(size, l) >= 0)
                        .Select(l => $"{l} {FuelTanks.Units(model, list[Find(size, l)].Code):N0}"));
                    var content = new GUIContent($"Holds: {holds} units of {FuelTanks.Contents(model)}");
                    float hh = PipSkin.Hint.CalcHeight(content, w - 64);
                    GUI.Label(new Rect(x + 64, y - 2, w - 64, hh), content, PipSkin.Hint);
                    y += hh + 6;
                }
            }
            else
                SizeRow(type.VariantName == "Class" ? "Class" : "Size", list.Select(v => v.Label).ToArray(), Array.FindIndex(list, v => v.Code == current),
                    i => SizeLocked(type, list[i]), i => previewVariants[type.Id] = list[i].Code);
            return y;
        }

        void DrawConfig(Rect area, PartType type, string[] model)
        {
            cfgScroll = GUI.BeginScrollView(area, cfgScroll, new Rect(0, 0, area.width - 14, Mathf.Max(cfgHeight, area.height - 1)));
            float x = Pad, w = area.width - 2 * Pad, y = 18;

            // Name
            GUI.Label(new Rect(x, y, w, 16), "PROGRAM NAME", PipSkin.Section);
            y += 22;
            newName = GUI.TextField(new Rect(x, y, w - 52, 44), newName ?? "", 30, PipSkin.NameField);
            if (GUI.Button(new Rect(x + w - 44, y, 44, 44), new GUIContent("", "Suggest another name"), PipSkin.Dice)) newName = SuggestName(newName);
            if (Event.current.type == EventType.Repaint) GUI.DrawTexture(new Rect(x + w - 33, y + 11, 22, 22), PipSkin.DiceIcon);
            y += 44 + 20;

            // Type
            PartType[] engines = PartTypes.Engines.Where(PipSettings.On).ToArray(), parts = PartTypes.Parts.Where(PipSettings.On).ToArray();
            GUI.Label(new Rect(x, y, w, 16), "TYPE", PipSkin.Section);
            GUI.Label(new Rect(x, y, w, 16), $"{Count(engines.Length, "engine")}  ·  {Count(parts.Length, "part")}", PipSkin.SectionRight);
            y += 22;
            const int cols = 6;
            const float gap = 5, th = 50;
            float tw = (w - gap * (cols - 1)) / cols;
            foreach (PartType[] group in new[] { engines, parts })
            {
                if (group.Length == 0) continue;
                for (int i = 0; i < group.Length; i++)
                {
                    var r = new Rect(x + (i % cols) * (tw + gap), y + (i / cols) * (th + gap), tw, th);
                    PartType t = group[i];
                    bool on = t == newType;
                    // Not researched yet: dimmed with a padlock, still there to look at.
                    bool locked = !TechUnlocks.Opened(t);
                    string tip = locked ? $"{t.Title}: research {TechUnlocks.Title(t.CapTechs[0])} first" : t.Title;
                    if (GUI.Button(r, new GUIContent("", tip), on ? PipSkin.TileOn : locked ? PipSkin.TileLocked : PipSkin.Tile) && !on)
                    {
                        newType = t;
                        message = null;
                        pickerOpen = false;
                    }
                    GUI.Label(new Rect(r.x, r.y + 7, r.width, 22), Sym(t), on ? PipSkin.TileSymOn : locked ? PipSkin.TileSymLocked : PipSkin.TileSym);
                    GUI.Label(new Rect(r.x, r.y + 30, r.width, 14), ShortName(t), locked ? PipSkin.TileLabelLocked : PipSkin.TileLabel);
                    if (locked && Event.current.type == EventType.Repaint) GUI.DrawTexture(new Rect(r.xMax - 13, r.y + 5, 8, 10), PipSkin.Lock);
                }
                y += ((group.Length + cols - 1) / cols) * (th + gap) + 6;
            }
            y += 12;

            // Design: the type's choices, then its size.
            Option[] sizes = SizeChoices(type, model);
            bool hasSize = sizes != null && sizes.Length > 1;
            if (type.Choices.Length > 0 || hasSize)
            {
                GUI.Label(new Rect(x, y, w, 16), "DESIGN", PipSkin.Section);
                y += 22;
                for (int c = 0; c < type.Choices.Length; c++)
                {
                    Option[] options = type.Choices[c].Options;
                    string[] labels = options.Select(o => o.Label).ToArray();
                    float rowY = y;
                    int choice = c;
                    int picked = Segments(x + 64, ref y, w - 64, labels, Array.FindIndex(options, o => o.Code == model[c]),
                        i => LockedWhy(type, model, choice, options[i]), out bool shortened);
                    GUI.Label(new Rect(x, rowY, 62, 34), type.Choices[c].Name + (shortened ? ", m" : ""), PipSkin.SegLabel);
                    if (picked >= 0)
                    {
                        model[c] = options[picked].Code;
                        message = null;
                    }
                }
                if (hasSize) y = DrawSize(x, y, w, type, model, sizes);
                // Gimbal: a free pick, opened by research; 0° when the model or look can't.
                if (type.HasGimbalPick && type.Valid(model))
                {
                    bool can = CanGimbalNow(type, model);
                    string current = GimbalOf(type, model);
                    Option[] go = PartType.GimbalOptions;
                    float rowY = y;
                    int picked = Segments(x + 64, ref y, w - 64, go.Select(o => o.Label).ToArray(), Array.FindIndex(go, o => o.Code == current),
                        i => i > 0 && !can ? (type.CanGimbal(model) ? "This look has a fixed nozzle" : "Can't gimbal")
                            : TechUnlocks.Available(type, go[i]) ? null : "Research " + TechUnlocks.Title(go[i].Tech), out _);
                    GUI.Label(new Rect(x, rowY, 62, 34), "Gimbal", PipSkin.SegLabel);
                    if (picked >= 0) { gimbals[type.Id] = go[picked].Code; message = null; }
                }
                y += 12;
            }

            // Look: the in-line look, or the side-mount look for a side-mount program.
            if (type.HasLooks)
            {
                bool sideNow = PartType.IsSide(SizeOf(type, model));
                List<Looks.Look> all = Looks.List(type, sideNow, model);
                string standard = sideNow ? Looks.DefaultSide(type, model) : PartFactory.StandardSource(type, model);
                int count = all.Count(l => l.Source != standard) + 1;
                GUI.Label(new Rect(x, y, w, 16), "LOOK", PipSkin.Section);
                GUI.Label(new Rect(x, y, w, 16), Count(count, "look") + " to pick from", PipSkin.SectionRight);
                y += 22;
                if (sideNow)
                {
                    string sideLook = SideLookOf(type, model);
                    if (LookCard(new Rect(x, y, w, 72), sideLook, ModOf(sideLook) + (sideLook == standard ? "  ·  default" : ""))) OpenPicker(true);
                }
                else
                {
                    string current = LookOf(type, model);
                    string source = current == "" ? standard : current;
                    string sub = ModOf(source) + (current == "" ? "  ·  standard" : "");
                    if (type == PartTypes.Liquid) sub += "  ·  burns " + Fmt.Lower(FuelName(all.Find(l => l.Source == source)?.Fuel ?? model[0]));
                    if (type.HasGimbalPick && !Looks.Gimbals(type, current, model)) sub += "  ·  fixed nozzle";
                    if (LookCard(new Rect(x, y, w, 72), source, sub)) OpenPicker(false);
                }
                y += 72 + 8;
                string hint = "Fixed for the program's life. It changes the model, never the numbers." +
                              (type.HasSideMount && !(sizes ?? new Option[0]).Any(o => PartType.IsSide(o.Code))
                                  ? " No side-mount look is installed, so there's no side-mount version." : "");
                float hh = PipSkin.Hint.CalcHeight(new GUIContent(hint), w);
                GUI.Label(new Rect(x, y, w, hh), hint, PipSkin.Hint);
                y += hh + 18;
            }
            if (Event.current.type == EventType.Repaint) cfgHeight = y;
            GUI.EndScrollView();
        }

        static string Count(int n, string noun) => n + " " + noun + (n == 1 ? "" : "s");

        // Why an option can't be picked: research still to do, or (with the rest of
        // the design as it is) a model whose part needs a mod that isn't installed.
        static string LockedWhy(PartType type, string[] model, int choice, Option o)
        {
            if (!TechUnlocks.Available(type, o))
                return "Research " + TechUnlocks.Title(TechUnlocks.Opened(type) ? o.Tech : type.CapTechs[0]);
            var m = (string[])model.Clone();
            m[choice] = o.Code;
            return type.Valid(m) && !PartFactory.Made(type, m) ? "Needs " + PartFactory.Needs(type, m) : null;
        }

        // "1.25 m long" -> { "1.25 m", "long" }; null when the label isn't a size and something.
        static string[] SplitVariant(string label)
        {
            int i = label.IndexOf(" m ", StringComparison.Ordinal);
            return i < 0 ? null : new[] { label.Substring(0, i + 2), label.Substring(i + 3) };
        }

        bool LookCard(Rect r, string source, string sub)
        {
            bool clicked = GUI.Button(r, GUIContent.none, PipSkin.LookCard);
            Thumb(new Rect(r.x + 8, r.y + 8, 56, 56), source);
            GUI.Label(new Rect(r.x + 76, r.y + 14, r.width - 110, 22), Looks.Title(source), PipSkin.CardTitle);
            GUI.Label(new Rect(r.x + 76, r.y + 36, r.width - 110, 18), sub, PipSkin.CardSub);
            GUI.Label(new Rect(r.xMax - 34, r.y, 26, r.height), "›", PipSkin.Chevron);
            return clicked;
        }

        void Thumb(Rect r, string partName)
        {
            PipSkin.Draw(r, PipSkin.ThumbBox);
            Texture tex = preview.Thumb(partName);
            // Inset past the rounded corners; the picture's background is the box's own color.
            if (tex != null && Event.current.type == EventType.Repaint) GUI.DrawTexture(new Rect(r.x + 3, r.y + 3, r.width - 6, r.height - 6), tex, ScaleMode.ScaleToFit, false);
        }

        // The big picture: the part turning, with a caption.
        void Hero(Rect r, string partName, PartType type, string caption)
        {
            PipSkin.Draw(r, PipSkin.HeroBox);
            if (Event.current.type == EventType.Repaint) heroShown = true;
            preview.Want(partName);
            Texture tex = preview.Turntable;
            if (tex != null && Event.current.type == EventType.Repaint)
                GUI.DrawTexture(new Rect(r.x + 4, r.y + 4, r.width - 8, r.height - 8), tex, ScaleMode.ScaleToFit, false);
            else GUI.Label(r, Sym(type), PipSkin.HeroSym);
            if (caption != null) GUI.Label(new Rect(r.x + 12, r.yMax - 28, r.width - 24, 22), caption, PipSkin.Caption);
        }

        void DrawPoints(Rect area, PartType type, string[] model, int[] pts, int cap, Func<int, bool> used, int fixedGimbal,
            Stats s0, Stats sN, Stats s10)
        {
            Row[] rows = type.Rows(model);
            pointsScroll = GUI.BeginScrollView(area, pointsScroll, new Rect(0, 0, area.width - (pointsHeight > area.height ? 14 : 0), Mathf.Max(pointsHeight, area.height - 1)));
            float w = area.width - (pointsHeight > area.height ? 14 : 0);
            const float LabelW = 104, StepW = 72, ValueW = 104, Gap = 10;
            float sliderW = w - LabelW - StepW - ValueW - 3 * Gap;
            float y = 0;
            var seen = new HashSet<int>();
            foreach (Row row in rows)
            {
                bool main = row.Category >= 0 && seen.Add(row.Category);
                string v0 = row.Format(s0[row.Key]), vN = row.Format(sN[row.Key]), v10 = row.Format(s10[row.Key]);
                if (!main)
                {
                    GUI.Label(new Rect(12, y, LabelW + sliderW, 22), row.Label.Trim(), PipSkin.SubLabel);
                    GUI.Label(new Rect(LabelW + Gap, y, sliderW + Gap + StepW, 22), new GUIContent(v0 + "  →  " + v10, "Mk. 1 → Mk. 5"), PipSkin.ValueSmall);
                    GUI.Label(new Rect(w - ValueW, y, ValueW, 22), vN, PipSkin.SubValue);
                    y += 24;
                    continue;
                }

                int i = row.Category;
                bool on = used(i);
                if (y > 0) PipSkin.Fill(new Rect(0, y, w, 1), PipSkin.Line);
                string cat = type.Categories[i].Label;
                GUI.Label(new Rect(0, y + 5, LabelW, 20), cat, on ? PipSkin.PointLabel : PipSkin.PointLabelOff);
                // A caption that wraps ("Output in Kerbin's sunlight") makes its row taller rather than clip.
                float rowH = 48;
                if (!string.Equals(row.Label.Trim(), cat, StringComparison.OrdinalIgnoreCase))
                {
                    var caption = new GUIContent(row.Label.Trim(), row.Label.Trim());
                    float ch = PipSkin.RowCaption.CalcHeight(caption, LabelW);
                    GUI.Label(new Rect(0, y + 24, LabelW, ch), caption, PipSkin.RowCaption);
                    rowH = Mathf.Max(rowH, 24 + ch + 4);
                }

                var sr = new Rect(LabelW + Gap, y + 16, sliderW, 16);
                PointsSlider(sr, pts, i, cap, on);
                var step = new Rect(sr.xMax + Gap, y + 10, StepW, 28);
                PipSkin.Draw(step, PipSkin.Stepper);
                bool was = GUI.enabled;
                GUI.enabled = was && on;
                int by = Event.current.shift ? 1 : 5;
                if (GUI.Button(new Rect(step.x, step.y, 22, step.height), new GUIContent("−", "Fewer points (Shift: 1)"), PipSkin.StepButton)) pts[i] = Math.Max(0, pts[i] - by);
                if (GUI.Button(new Rect(step.xMax - 22, step.y, 22, step.height), new GUIContent("+", "More points (Shift: 1)"), PipSkin.StepButton)) pts[i] = Math.Min(cap, pts[i] + by);
                GUI.enabled = was;
                GUI.Label(new Rect(step.x + 20, step.y, step.width - 40, step.height), pts[i].ToString(), PipSkin.StepValue);

                var val = new Rect(w - ValueW, y + 4, ValueW, 24);
                if (on)
                {
                    // A long value steps down a size; the Mk. 1 → Mk. 5 line may reach further left.
                    GUIStyle vs = PipSkin.Width(PipSkin.Value, vN) > ValueW + 28 ? PipSkin.ValueTight : PipSkin.Value;
                    GUI.Label(new Rect(val.x - 30, val.y, ValueW + 30, val.height), new GUIContent(vN, vN), vs);
                    string range = v0 + "  →  " + v10;
                    var rangeContent = new GUIContent(range, "Mk. 1 → Mk. 5");
                    float rw = PipSkin.Width(PipSkin.ValueSmall, range);
                    float rx = Mathf.Min(val.x - 30, w - rw - 2);
                    GUI.Label(new Rect(rx, y + 28, w - rx, 16), rangeContent, PipSkin.ValueSmall);
                }
                else GUI.Label(new Rect(val.x - 30, y, ValueW + 30, 48), i == fixedGimbal ? "fixed nozzle" : "does nothing here", PipSkin.ValueSmall);
                y += rowH;
            }
            y += 4;
            var note = new GUIContent($"Points are capped at {cap} for now; research raises the cap.  − and + change 5, or 1 with Shift.");
            float nh = PipSkin.Small.CalcHeight(note, w);
            GUI.Label(new Rect(0, y, w, nh), note, PipSkin.Small);
            y += nh + 6;
            if (Event.current.type == EventType.Repaint) pointsHeight = y;
            GUI.EndScrollView();
        }

        // A slider from 0 to 100 points, filled to the value, striped past the cap.
        void PointsSlider(Rect r, int[] pts, int i, int cap, bool on)
        {
            const float Knob = 16;
            float left = r.x + Knob / 2, width = r.width - Knob;
            var track = new Rect(left, r.y + 4, width, 8);
            float capX = left + width * cap / 100f;
            PipSkin.Bar(track, PipSkin.Panel3);
            if (cap < 100) PipSkin.Tiled(new Rect(capX, track.y, track.xMax - capX, track.height), PipSkin.Hatch);
            if (!on) return;
            PipSkin.Bar(new Rect(left, track.y, width * pts[i] / 100f, track.height), PipSkin.Amber);
            if (cap < 100) PipSkin.Fill(new Rect(capX - 0.5f, track.y - 3, 1, track.height + 6), PipSkin.Ink3);
            float v = GUI.HorizontalSlider(r, pts[i], 0, 100, PipSkin.SliderTrack, PipSkin.SliderThumb);
            int n = Mathf.Clamp(Mathf.RoundToInt(v), 0, cap);
            if (n != pts[i]) pts[i] = n;
        }

        void DrawFooter(PipScenario scenario, Rect r, PartType type, string[] model, string variant, string gimbal, int[] pts, bool valid, bool unlocked, Stats s0, Stats sN, Stats s10)
        {
            PipSkin.Draw(new Rect(0, r.y, r.width, r.height), PipSkin.FootBox);
            PipSkin.Fill(new Rect(0, r.y, r.width, 30), PipSkin.Panel2);
            PipSkin.Fill(new Rect(0, r.y, 14, r.height), PipSkin.Panel2);
            PipSkin.Fill(new Rect(0, r.y, r.width, 1), PipSkin.Line);

            float x = Pad, y = r.y + 14;
            int total = pts.Sum();
            double cost = PipScenario.Cost(type, model, pts);
            Stat(ref x, y, "POINTS", total.ToString(), 62, PipSkin.Ink);
            bool short_ = !PipScenario.Free && Funding.Instance != null && Funding.Instance.Funds < cost;
            Stat(ref x, y, "PROGRAM COST", PipScenario.Free ? "Free" : "√ " + cost.ToString("N0"), 118, short_ ? PipSkin.Bad : PipSkin.Ink,
                PipScenario.Free ? "Programs are free in this game mode." : $"About √ {(PipScenario.Cost(type, model, pts.Select((v, i) => i == 0 ? v + 10 : v).ToArray()) - cost):N0} more for 10 more points.");
            if (s0 != null)
                Stat(ref x, y, "BUILD PRICE, EACH", $"√ {s0.BuildPrice:N0}–{s10.BuildPrice:N0}", 150, PipSkin.Ink, $"Each {type.Noun} costs this to build: Mk. 0 to Mk. 10.");

            float goW = 168;
            var text = new Rect(x, r.y + 8, r.width - x - goW - 2 * Pad, r.height - 16);
            GUIStyle style = PipSkin.Compare;
            string line, tip = null;
            if (!string.IsNullOrEmpty(message)) { line = message; style = PipSkin.CompareBad; }
            else if (!valid) { line = "That combination isn't made. Pick another."; style = PipSkin.CompareWarn; }
            else if (!unlocked) { line = $"Locked. Research {TechUnlocks.Missing(type, model, variant)}."; style = PipSkin.CompareWarn; }
            else if (!PartFactory.Made(type, model)) { line = $"This model needs {PartFactory.Needs(type, model)}, which isn't installed."; style = PipSkin.CompareWarn; }
            else
            {
                string summary = Comparison.Summary(newName, type, model, pts, PipSettings.Failures);
                line = Comparison.Short(type, model, sN, previewMk) ?? summary;
                tip = (Comparison.Line(type, model, sN, previewMk) is string full ? full + "\n\n" : "") + summary;
            }
            GUI.Label(text, new GUIContent(line, tip), style);

            // Gray, and does nothing, while the design can't be started; the line beside it says why.
            bool ready = valid && unlocked && PartFactory.Made(type, model);
            var go = new Rect(r.width - Pad - goW, r.y + 15, goW, 46);
            if (!ready) GUI.Label(go, new GUIContent("Start program  →", line), PipSkin.GoOff);
            else if (GUI.Button(go, "Start program  →", PipSkin.Go))
            {
                Program p = scenario.Create(newName, type, model, (int[])pts.Clone(),
                    type.HasLooks ? LookOf(type, model) : "", type.HasLooks && type.HasSideMount ? SideLookOf(type, model) ?? "" : "", variant, gimbal, out string error);
                if (p == null) message = error;
                else
                {
                    message = null;
                    creating = false;
                    selectedId = p.Id;
                    newName = SuggestName();
                    ScreenMessages.PostScreenMessage($"Program {p.Title} started. {p.DisplayName()} is in the parts list.", 6f, ScreenMessageStyle.UPPER_CENTER);
                }
            }
        }

        static void Stat(ref float x, float y, string key, string value, float w, Color color, string tip = null)
        {
            GUI.Label(new Rect(x, y, w + 20, 14), new GUIContent(key, tip), PipSkin.FootKey);
            Color was = PipSkin.FootValue.normal.textColor;
            PipSkin.FootValue.normal.textColor = color;
            GUI.Label(new Rect(x, y + 16, w + 20, 30), new GUIContent(value, tip), PipSkin.FootValue);
            PipSkin.FootValue.normal.textColor = was;
            x += Mathf.Max(w, PipSkin.Width(PipSkin.FootValue, value)) + 18;
        }

        // A row of buttons that read as one control; wraps onto more lines when it
        // doesn't fit, after first dropping a trailing " m" from every label.
        // lockedWhy(i) returns the reason an option can't be picked, or null.
        int Segments(float x, ref float y, float width, string[] labels, int selected, Func<int, string> lockedWhy, out bool shortened)
        {
            const float Hgt = 34, Inner = 3, Between = 2, LockW = 14;
            float avail = width - 2 * Inner;
            string[] reasons = labels.Select((_, i) => lockedWhy(i)).ToArray();
            float Size(string l, int i) => PipSkin.Width(PipSkin.SegOn, l) + 16 + (reasons[i] != null ? LockW : 0);
            float Total(string[] ls) => ls.Select(Size).Sum() + Between * (ls.Length - 1);

            shortened = false;
            if (Total(labels) > avail && labels.Any(l => l.EndsWith(" m")))
            {
                labels = labels.Select(l => l.EndsWith(" m") ? l.Substring(0, l.Length - 2) : l).ToArray();
                shortened = true;
            }

            // Fill lines left to right; when it takes more than one, even them out
            // so no option sits alone on a line of its own.
            List<List<int>> Split(int most)
            {
                var result = new List<List<int>>();
                var line = new List<int>();
                float used = 0;
                for (int i = 0; i < labels.Length; i++)
                {
                    float s = Size(labels[i], i);
                    if (line.Count > 0 && (used + s > avail || line.Count >= most)) { result.Add(line); line = new List<int>(); used = 0; }
                    line.Add(i);
                    used += s + Between;
                }
                result.Add(line);
                return result;
            }
            List<List<int>> lines = Split(int.MaxValue);
            if (lines.Count > 1)
            {
                List<List<int>> even = Split((labels.Length + lines.Count - 1) / lines.Count);
                if (even.Count == lines.Count) lines = even;
            }

            int clicked = -1;
            foreach (List<int> ln in lines)
            {
                PipSkin.Draw(new Rect(x, y, width, Hgt), PipSkin.SegBox);
                float sum = ln.Sum(i => Size(labels[i], i)) + Between * (ln.Count - 1);
                float extra = Mathf.Max(0, (avail - sum) / ln.Count), bx = x + Inner;
                foreach (int i in ln)
                {
                    float bw = Size(labels[i], i) + extra;
                    var br = new Rect(bx, y + Inner, bw, Hgt - 2 * Inner);
                    if (reasons[i] != null)
                    {
                        float tw = PipSkin.Width(PipSkin.SegLocked, labels[i]);
                        float sx = br.center.x - (tw + LockW) / 2;
                        if (Event.current.type == EventType.Repaint) GUI.DrawTexture(new Rect(sx, br.center.y - 6, 10, 12), PipSkin.Lock);
                        GUI.Label(new Rect(sx + LockW, br.y, tw + 2, br.height), labels[i], PipSkin.SegLocked);
                        GUI.Label(br, new GUIContent("", reasons[i]));
                    }
                    else if (GUI.Button(br, labels[i], i == selected ? PipSkin.SegOn : PipSkin.Seg)) clicked = i;
                    bx += bw + Between;
                }
                y += Hgt + 6;
            }
            return clicked;
        }

        // The picked look, or "" (standard) when nothing valid is picked.
        static string LookOf(PartType type, string[] model)
        {
            if (!looks.TryGetValue(type.Id, out string look) || look == PartFactory.StandardSource(type, model) || !Looks.Exists(type, false, look, model)) return "";
            return look;
        }

        static string SideLookOf(PartType type, string[] model)
        {
            if (sideLooks.TryGetValue(type.Id, out string look) && Looks.Exists(type, true, look, model)) return look;
            return Looks.DefaultSide(type, model);
        }

        static string FuelName(string code) => PartTypes.Liquid.Choices[0].Find(code)?.Label ?? code;

        // The mod a part comes from, by its folder: "Stock", "Cryo Engines", "ReStock+".
        static string ModOf(string partName)
        {
            if (string.IsNullOrEmpty(partName)) return "";
            if (modNames.TryGetValue(partName, out string name)) return name;
            string url = PartLoader.getPartInfoByName(partName)?.partUrl ?? "";
            string[] parts = url.Split('/');
            string top = parts[0] == "SquadExpansion" && parts.Length > 1 ? parts[1] : parts[0];
            switch (top)
            {
                case "Squad": name = "Stock"; break;
                case "MakingHistory": name = "Making History"; break;
                case "Serenity": name = "Breaking Ground"; break;
                case "ReStock": name = "ReStock"; break;
                case "ReStockPlus": name = "ReStock+"; break;
                default:
                    var sb = new System.Text.StringBuilder();
                    for (int i = 0; i < top.Length; i++)
                    {
                        if (i > 0 && char.IsUpper(top[i]) && char.IsLower(top[i - 1])) sb.Append(' ');
                        sb.Append(top[i]);
                    }
                    name = sb.ToString();
                    break;
            }
            return modNames[partName] = name;
        }

        #endregion

        #region Look picker

        // The look a program wears now, and the one its next version will (it may change).
        static readonly Dictionary<string, string> nextLooks = new Dictionary<string, string>();
        static string pickerFor;   // the program whose next version's look is being picked; null: a new program's

        static string CurrentLookOf(Program p)
        {
            if (p.FixedSize && p.IsSide) return string.IsNullOrEmpty(p.SideLook) ? Looks.DefaultSide(p.Type, p.Model) : p.SideLook;
            return string.IsNullOrEmpty(p.Look) ? PartFactory.StandardSource(p.Type, p.Model) : p.Look;
        }

        static string NextLookOf(Program p) => nextLooks.TryGetValue(p.Id, out string l) ? l : CurrentLookOf(p);

        void OpenPicker(bool side, string forProgram = null)
        {
            pickerFor = forProgram;
            pickerOpen = true;
            pickerSide = side;
            search = "";
            filter = 0;
            pickerScroll = Vector2.zero;
        }

        sealed class Entry
        {
            public string Source, Title, Mod, Fuel;
            public bool Standard;
        }

        void DrawPicker(Rect area, PartType type, string[] model, Program forProgram = null)
        {
            bool side = pickerSide;
            string standard = side ? Looks.DefaultSide(type, model) : PartFactory.StandardSource(type, model);
            string current = forProgram != null ? NextLookOf(forProgram) : side ? SideLookOf(type, model) : (LookOf(type, model) is string l && l != "" ? l : standard);
            List<Looks.Look> all = Looks.List(type, side, model);
            var entries = new List<Entry>();
            if (!string.IsNullOrEmpty(standard))
                entries.Add(new Entry { Source = standard, Title = Looks.Title(standard), Mod = ModOf(standard), Standard = true, Fuel = all.Find(x => x.Source == standard)?.Fuel ?? "" });
            entries.AddRange(all.Where(x => x.Source != standard).Select(x => new Entry { Source = x.Source, Title = x.Title, Mod = ModOf(x.Source), Fuel = x.Fuel }));
            if (type == PartTypes.Liquid)
                entries = entries.OrderBy(e => e.Standard ? 0 : 1).ThenBy(e => e.Fuel == model[0] ? 0 : 1).ToList();

            bool gimbal = type.HasGimbalPick;
            bool Gimbals(string s)
            {
                string key = type.Id + "/" + string.Join("-", model) + "/" + s;
                if (!gimbalCache.TryGetValue(key, out bool g)) gimbalCache[key] = g = Looks.Gimbals(type, s == standard ? "" : s, model);
                return g;
            }

            float x = Pad, w = area.width - 2 * Pad;
            GUI.Label(new Rect(x, 14, w, 30), side ? "Pick a side-mount look" : "Pick a look", PipSkin.PickerTitle);
            if (GUI.Button(new Rect(area.width - Pad - 30, 14, 30, 30), new GUIContent("×", "Back to the program"), PipSkin.Close)) { pickerOpen = false; pickerFor = null; }

            GUI.SetNextControlName("pipSearch");
            var sr = new Rect(x, 54, 300, 34);
            search = GUI.TextField(sr, search ?? "", 40, PipSkin.Search);
            if (string.IsNullOrEmpty(search) && GUI.GetNameOfFocusedControl() != "pipSearch")
                GUI.Label(new Rect(sr.x + 12, sr.y, sr.width, sr.height), $"Search {Count(entries.Count, "look")}", PipSkin.Placeholder);

            var chips = new List<string> { "All" };
            if (!side && type == PartTypes.Liquid) chips.Add("Burns " + Fmt.Lower(FuelName(model[0])));
            if (!side && gimbal) chips.Add("Gimbals");
            float cx = sr.xMax + 12;
            for (int i = 0; i < chips.Count; i++)
            {
                int f = i == 0 ? 0 : chips[i] == "Gimbals" ? 2 : 1;
                float cw = PipSkin.Width(PipSkin.Chip, chips[i]) + 24;
                if (GUI.Button(new Rect(cx, 59, cw, 24), chips[i], filter == f ? PipSkin.ChipOn : PipSkin.Chip)) filter = f;
                cx += cw + 6;
            }

            string q = (search ?? "").Trim();
            List<Entry> shown = entries.Where(e =>
                (q.Length == 0 || e.Title.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0 || e.Mod.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0) &&
                (filter != 1 || e.Fuel == model[0] || (e.Standard && e.Fuel == "")) &&
                (filter != 2 || Gimbals(e.Source))).ToList();

            PipSkin.Fill(new Rect(0, 100, area.width, 1), PipSkin.Line);
            var view = new Rect(0, 101, area.width, area.height - 101 - 44);
            const float RowH = 80;
            pickerScroll = GUI.BeginScrollView(view, pickerScroll, new Rect(0, 0, area.width - 14, Mathf.Max(shown.Count * RowH + 8, view.height - 1)));
            float y = 4;
            foreach (Entry e in shown)
            {
                // Only rows in view ask for a picture.
                bool visibleRow = y + RowH >= pickerScroll.y && y <= pickerScroll.y + view.height;
                var r = new Rect(10, y, area.width - 34, RowH - 4);
                if (visibleRow)
                {
                    if (GUI.Button(r, GUIContent.none, e.Source == current ? PipSkin.LookRowOn : PipSkin.LookRow))
                    {
                        if (forProgram != null) nextLooks[forProgram.Id] = e.Source;
                        else if (side) sideLooks[type.Id] = e.Source;
                        else looks[type.Id] = e.Standard ? "" : e.Source;
                        pickerOpen = false;
                        message = null;
                    }
                    Thumb(new Rect(r.x + 8, r.y + 6, 64, 64), e.Source);
                    GUI.Label(new Rect(r.x + 88, r.y + 16, r.width - 300, 22), e.Title, PipSkin.CardTitle);
                    GUI.Label(new Rect(r.x + 88, r.y + 38, r.width - 300, 18), e.Mod, PipSkin.CardSub);

                    var tags = new List<KeyValuePair<string, GUIStyle>>();
                    if (e.Standard) tags.Add(new KeyValuePair<string, GUIStyle>(side ? "Default" : "Standard", PipSkin.Tag));
                    if (type == PartTypes.Liquid && !side && e.Fuel != "")
                        tags.Add(new KeyValuePair<string, GUIStyle>(FuelName(e.Fuel), e.Fuel == model[0] ? PipSkin.TagGood : PipSkin.TagWarn));
                    if (!side && gimbal && !Gimbals(e.Source)) tags.Add(new KeyValuePair<string, GUIStyle>("Fixed nozzle", PipSkin.TagBad));
                    float tx = r.xMax - 12;
                    foreach (KeyValuePair<string, GUIStyle> t in tags)
                    {
                        float tw = PipSkin.Width(t.Value, t.Key) + 2;
                        tx -= tw;
                        PipSkin.Draw(new Rect(tx, r.y + 27, tw, 20), t.Value, t.Key);
                        tx -= 6;
                    }
                }
                y += RowH;
            }
            if (shown.Count == 0) GUI.Label(new Rect(Pad, 20, area.width - 2 * Pad, 40), "No look matches. Clear the search or pick All.", PipSkin.Empty);
            GUI.EndScrollView();

            PipSkin.Fill(new Rect(0, area.height - 44, area.width, 1), PipSkin.Line);
            string note = side ? "Side-mount looks are scaled to the size you picked."
                : (type.IsEngine ? "Only engines of this size are listed: a look is never scaled. " : "") +
                  (type == PartTypes.Liquid ? "Looks that burn your fuel come first; the flame comes from the look. " : "") +
                  "The look changes the model, never the numbers.";
            GUI.Label(new Rect(Pad, area.height - 40, area.width - 2 * Pad, 36), note, PipSkin.PickerNote);
        }

        #endregion

        #region A program

        void DrawProgram(PipScenario scenario, Program p, Rect area)
        {
            int mk = p.Mk;
            // A side-mount program wears its side look; everything else its look.
            string look = p.FixedSize && p.IsSide && !string.IsNullOrEmpty(p.SideLook) ? p.SideLook
                : string.IsNullOrEmpty(p.Look) ? PartFactory.StandardSource(p.Type, p.Model) : p.Look;
            Hero(new Rect(26, 22, 230, 230), look ?? p.ChassisPart, p.Type, look != null ? Comparison.ShortName(Looks.Title(look)) + " look" : null);

            float x = 282, w = area.width - x - 26, y = 24;
            GUI.Label(new Rect(x, y, w, 18), $"{p.Type.Title}  ·  {p.Label}", PipSkin.Eyebrow);
            y += 20;
            GUI.Label(new Rect(x, y, w, 54), p.Title, PipSkin.Big);
            string badge = "Mk " + mk;
            float nameW = Mathf.Min(w - 100, PipSkin.Width(PipSkin.Big, p.Title));
            PipSkin.Draw(new Rect(x + nameW + 14, y + 12, PipSkin.Width(PipSkin.BigMk, badge) + 22, 34), PipSkin.BigMk, badge);
            y += 66;

            // The experience track: one segment for each Mk. after the first.
            const float SegGap = 4;
            int steps = Progress.MaxMk - Progress.MinMk;
            float segW = (w - SegGap * (steps - 1)) / steps;
            for (int i = Progress.MinMk + 1; i <= Progress.MaxMk; i++)
            {
                var r = new Rect(x + (i - Progress.MinMk - 1) * (segW + SegGap), y, segW, 8);
                PipSkin.Bar(r, PipSkin.Panel3);
                float share = i <= mk ? 1 : i == mk + 1 ? XpShare(p) : 0;
                if (share > 0) PipSkin.Bar(new Rect(r.x, r.y, r.width * share, r.height), PipSkin.Amber);
                GUI.Label(new Rect(r.x, y + 10, r.width, 16), new GUIContent("Mk " + i, $"{Progress.MkName(i)} at {Progress.XpForMk(i)} experience"),
                    i == mk + 1 ? PipSkin.TrackLabelOn : PipSkin.TrackLabel);
            }
            y += 34;
            // At Mk. 5: what comes next (DESIGN.md section 18.1).
            string blocked = PipScenario.NextBlocked(p);
            int cap = TechUnlocks.Cap(p.Type);
            int[] nextPts = p.NextPoints(cap);
            bool nextMax = Enumerable.Range(0, nextPts.Length).All(i => !p.Type.CategoryUsed(p.Model, i) || nextPts[i] >= Pricing.MaxPoints);
            string nextTitle = p.Name + " " + (nextMax ? "Max" : Program.Roman(p.Version + 1));
            string xp = mk < Progress.MaxMk
                ? $"{p.Xp} of {Progress.XpForMk(mk + 1)} experience for {Progress.MkName(mk + 1)}. Each flight is worth {Progress.LaunchXp}, and each recovery {Progress.RecoveryXp}."
                : blocked == null ? $"Fully developed. {nextTitle} is ready: every slider +10, starting from these numbers."
                : $"Fully developed. {blocked}";
            GUI.Label(new Rect(x, y, w, 40), xp, PipSkin.XpLine);
            y += 44;
            // A mod its model came from was removed: say so instead of a silent gap.
            if (PartFactory.Problem(p) is string problem)
            {
                GUI.Label(new Rect(x, y - 6, w, 34), problem, PipSkin.CompareWarn);
                y += 30;
            }

            var chips = new List<string>
            {
                $"<b>{p.Launches}</b> launch{(p.Launches == 1 ? "" : "es")}",
                $"<b>{p.Recoveries}</b> recover{(p.Recoveries == 1 ? "y" : "ies")}",
                $"{p.Total} points  ·  " + (p.FundsPaid > 0 ? $"cost √ {p.FundsPaid:N0}" : "started free"),
            };
            if (p.Type.HasLooks) chips.Add("Look: " + (look != null ? Looks.Title(look) : "standard"));
            if (p.Type.HasGimbalPick) chips.Add("Gimbal " + (p.Type.CanGimbal(p.Model) ? Fmt.Deg(p.GimbalDegrees) : "none"));
            // Programs from before sizes were fixed still make a side-mount part too.
            if (p.Type.HasSideMount && !p.FixedSize) chips.Add(string.IsNullOrEmpty(p.SideLook) ? "No side-mount part" : "Side-mount: " + Looks.Title(p.SideLook));
            float cx = x;
            foreach (string c in chips)
            {
                float cw = PipSkin.Width(PipSkin.ProgChip, c.Replace("<b>", "").Replace("</b>", "")) + 4;
                if (cx + cw > x + w) { cx = x; y += 30; }
                PipSkin.Draw(new Rect(cx, y, cw, 24), PipSkin.ProgChip, c);
                cx += cw + 6;
            }
            // The family's versions so far.
            List<Program> family = scenario.Family(p);
            if (family.Count > 1)
            {
                y += 32;
                string history = "Versions:  " + string.Join("   ·   ", family.Select(v => $"{v.Title} {(v.Type.Categories.Length > 0 ? v.Points.Max() : 0)} pts{(v == p ? " (now)" : "")}"));
                GUI.Label(new Rect(x, y, w, 20), new GUIContent(history, "Each version's highest slider"), PipSkin.Meta);
            }
            y = Mathf.Max(y + 40, 22 + 230 + 24);

            // Stats now and at Mk. 5, in two columns.
            FailureLevel level = PipSettings.Failures;
            Stats now = p.Stats(mk, level), top = p.Stats(Progress.MaxMk, level);
            var lines = new List<KeyValuePair<Row, int>>();
            foreach (Row r in p.Type.Rows(p.Model))
                if (r.Category < 0 || p.Type.CategoryUsed(p.Model, r.Category)) lines.Add(new KeyValuePair<Row, int>(r, r.Category >= 0 ? p.Points[r.Category] : -1));

            float colGap = 28, colW = (area.width - 52 - colGap) / 2;
            int perCol = (lines.Count + 1) / 2;
            for (int c = 0; c < 2; c++)
            {
                float cx0 = 26 + c * (colW + colGap);
                StatRowHead(new Rect(cx0, y, colW, 16));
                float ry = y + 22;
                foreach (KeyValuePair<Row, int> line in lines.Skip(c * perCol).Take(perCol))
                {
                    StatRow(new Rect(cx0, ry, colW, 42), line.Key, line.Value, now[line.Key.Key], top[line.Key.Key]);
                    ry += 42;
                }
            }

            // Footer: the build rule, and retiring.
            float fy = area.height - 46;
            PipSkin.Fill(new Rect(26, fy, area.width - 52, 1), PipSkin.Line);
            // The next version: its look (it may change), its cost, and Start.
            bool ready = blocked == null;
            float textW = area.width - (ready ? 690 : 360);
            GUI.Label(new Rect(26, fy + 6, textW, 34), ready ? $"New builds use {Progress.MkName(mk)}." : $"Parts already built keep the Mk they were built at. New builds use {Progress.MkName(mk)}.", PipSkin.Small);
            if (ready && !retireArmed)
            {
                double nextCost = PipScenario.NextCost(p);
                bool side = p.FixedSize && p.IsSide;
                string nextLook = NextLookOf(p);
                if (p.Type.HasLooks && GUI.Button(new Rect(area.width - 26 - 486, fy + 8, 150, 30),
                        new GUIContent("Look: " + Comparison.ShortName(Looks.Title(nextLook ?? "")), $"The look {nextTitle} wears. Click to pick another."), PipSkin.Retire))
                {
                    OpenPicker(side, p.Id);
                }
                if (GUI.Button(new Rect(area.width - 26 - 326, fy + 4, 326, 38),
                        new GUIContent($"Start {nextTitle}  ·  " + (PipScenario.Free ? "free" : $"√ {nextCost:N0}") + "  →",
                            $"Every slider +10 (cap {cap}). A tenth of a new program's cost. {p.Title} retires; its parts keep flying."), PipSkin.Go))
                {
                    // Unchanged: keep it; back to the standard look: "".
                    string lookArg = nextLook == CurrentLookOf(p) ? null : !side && nextLook == PartFactory.StandardSource(p.Type, p.Model) ? "" : nextLook;
                    Program next = scenario.StartNext(p, lookArg, out string error);
                    if (next == null) message = error;
                    else
                    {
                        selectedId = next.Id;
                        nextLooks.Remove(p.Id);
                        message = null;
                        ScreenMessages.PostScreenMessage($"{next.Title} started. {next.DisplayName()} is in the parts list.", 6f, ScreenMessageStyle.UPPER_CENTER);
                    }
                }
                if (!string.IsNullOrEmpty(message)) GUI.Label(new Rect(26, fy - 26, area.width - 52, 22), message, PipSkin.CompareBad);
            }
            else if (!retireArmed)
            {
                if (GUI.Button(new Rect(area.width - 26 - 150, fy + 8, 150, 30), "Retire program…", PipSkin.Retire)) retireArmed = true;
            }
            else
            {
                GUI.Label(new Rect(area.width - 26 - 520, fy + 4, 300, 38), $"Retire {p.Title}? It leaves the parts list. Parts already flying are unaffected. No refund.", PipSkin.Confirm);
                if (GUI.Button(new Rect(area.width - 26 - 210, fy + 8, 96, 30), "Keep it", PipSkin.ConfirmButton)) retireArmed = false;
                if (GUI.Button(new Rect(area.width - 26 - 106, fy + 8, 106, 30), "Retire it", PipSkin.ConfirmDanger))
                {
                    scenario.Retire(p);
                    creating = true;
                    retireArmed = false;
                }
            }
        }

        static void StatRowHead(Rect r)
        {
            const float LabelW = 132, NowW = 88, TopW = 74, Gap = 10;
            GUI.Label(new Rect(r.x + LabelW + Gap, r.y, r.width - LabelW - NowW - TopW - 3 * Gap, r.height),
                new GUIContent("PROGRESS", "How close it is now to Mk. 5"), PipSkin.StatHead);
            GUI.Label(new Rect(r.xMax - TopW - Gap - NowW, r.y, NowW, r.height), "NOW", PipSkin.StatHeadRight);
            GUI.Label(new Rect(r.xMax - TopW, r.y, TopW, r.height), "MK 5", PipSkin.StatHeadRight);
        }

        static void StatRow(Rect r, Row row, int points, double now, double top)
        {
            const float LabelW = 132, NowW = 88, TopW = 74, Gap = 10;
            PipSkin.Fill(new Rect(r.x, r.y, r.width, 1), PipSkin.Line);
            string label = row.Label.Trim();
            if (points >= 0)
            {
                GUI.Label(new Rect(r.x, r.y + 4, LabelW, 20), new GUIContent(label, label), PipSkin.StatLabel);
                GUI.Label(new Rect(r.x, r.y + 23, LabelW, 16), $"{points} points", PipSkin.StatSub);
            }
            else GUI.Label(new Rect(r.x + (points == -1 ? 10 : 0), r.y, LabelW, r.height), new GUIContent(label, label), points == -1 ? PipSkin.SubLabel : PipSkin.StatLabel);

            // How close "now" is to Mk. 10, whichever way the number improves.
            double lo = Math.Min(Math.Abs(now), Math.Abs(top)), hi = Math.Max(Math.Abs(now), Math.Abs(top));
            float share = hi <= 0 ? 0 : (float)(lo / hi);     // nothing at all (a fixed nozzle): no bar
            var bar = new Rect(r.x + LabelW + Gap, r.y + r.height / 2 - 3, r.width - LabelW - NowW - TopW - 3 * Gap, 6);
            if (points != -2)   // the build price rises a little with Mk: not progress
            {
                PipSkin.Bar(bar, PipSkin.Panel3);
                PipSkin.Bar(new Rect(bar.x, bar.y, bar.width * share, bar.height), points == -1 ? PipSkin.Ink3 : PipSkin.Amber);
            }

            GUI.Label(new Rect(r.xMax - TopW - Gap - NowW, r.y, NowW, r.height), row.Format(now), points == -1 ? PipSkin.SubValue : PipSkin.StatNow);
            GUI.Label(new Rect(r.xMax - TopW, r.y, TopW, r.height), row.Format(top), PipSkin.StatTop);
        }

        #endregion
    }
}
