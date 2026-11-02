using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace BetterWriting
{
    sealed class Config
    {
        public const int CurrentVersion = 2;

        public static readonly string[] Features =
        {
            "Capitalize", "DoubleSpacePeriod", "AutoSpace", "AutoHyphen", "SmartQuotes",
            "AutoBrackets", "CapsLockFix", "Typos", "EnglishI", "LayoutFix", "Snippets"
        };

        public bool Enabled = true;
        public string UiLanguage = "en";
        public string Theme = "dark";
        int fileVersion = 1;
        public bool Capitalize = true;
        public bool DoubleSpacePeriod = true;
        public int DoubleSpaceIntervalMs = 600;
        public bool AutoSpace = true;
        public bool AutoHyphen = true;
        public bool SmartQuotes = true;
        public bool AutoBrackets = false;
        public string BracketExit = "hold";
        public bool CapitalizeInBrackets = false;
        public bool LayoutPerApp = false;
        public bool LayoutIndicator = true;
        public string IndicatorSize = "small";
        public bool PinOnTitleBar = true;
        public string PinHotkey = "";
        public string PlainPasteHotkey = "";
        public string KeepAwakeHotkey = "";
        public string KeepAwakeUntil = "";
        public int KeepAwakeHours = 1;
        public bool KeepAwakeScreen = false;
        public Dictionary<string, string> AppLayouts = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        public bool CapsLockFix = true;
        public bool Typos = true;
        public bool EnglishI = true;
        public bool LayoutFix = true;
        public bool Snippets = true;
        public bool CapitalizeAfterEnter = true;
        public bool CapitalizeOnWindowSwitch = true;
        public bool CapitalizeAfterClick = true;
        public bool CapitalizeAfterEllipsis = false;
        public bool LangRussian = true;
        public bool LangEnglish = true;
        public bool LangOther = false;
        public bool SkipFullscreen = true;
        public bool IgnoreInjected = true;
        public string PauseHotkey = "Ctrl+Alt+B";
        public HashSet<string> LayoutIgnore = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        public Dictionary<string, string> CustomTypos = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        public HashSet<string> ExcludeApps = List(
            "cmd, powershell, pwsh, WindowsTerminal, conhost, OpenConsole, wsl, bash, mintty, Code, Cursor, Windsurf, devenv, " +
            "idea64, pycharm64, webstorm64, rider64, clion64, goland64, studio64, sublime_text, notepad++, KeePass, KeePassXC, " +
            "1Password, Bitwarden, mstsc, vmconnect, vmware, VirtualBoxVM, Ableton*, FL64, reaper, Figma, Photoshop, " +
            "Illustrator, AfterFX, Adobe Premiere Pro, Resolve, blender, krita, obs64");
        public HashSet<string> Abbreviations = List(
            "т.е, т.к, т.н, т.ч, напр, ср, им, ул, г, гг, кв, стр, рис, e.g, i.e, vs, mr, mrs, ms, dr, prof, fig");

        public Dictionary<string, HashSet<string>> AppRules = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
        public Dictionary<string, string> SnippetList = DefaultSnippets();

        public static Config Load(string path)
        {
            var cfg = new Config();
            if (!File.Exists(path))
            {
                try { cfg.Save(path); }
                catch (IOException) { }
                catch (UnauthorizedAccessException) { }
                return cfg;
            }

            string section = "main";
            bool snippetsSeen = false;
            foreach (string raw in File.ReadAllLines(path, Encoding.UTF8))
            {
                string line = raw.Trim();
                if (line.Length == 0 || line == ";" || line.StartsWith("; ") || line[0] == '#') continue;
                if (line[0] == '[')
                {
                    section = line.Trim('[', ']').Trim().ToLowerInvariant();
                    if (section == "snippets" && !snippetsSeen)
                    {
                        snippetsSeen = true;
                        cfg.SnippetList.Clear();
                    }
                    continue;
                }
                int eq = line.IndexOf('=');
                if (eq <= 0) continue;
                string key = line.Substring(0, eq).Trim();
                string val = line.Substring(eq + 1).Trim();

                if (section == "apps") cfg.AppRules[key] = List(val);
                else if (section == "snippets") cfg.SnippetList[key] = Unescape(val);
                else if (section == "corrections") cfg.CustomTypos[key] = Unescape(val);
                else if (section == "layouts") cfg.AppLayouts[key] = val;
                else if (section == "main") cfg.SetValue(key, val);
            }
            cfg.Upgrade(path);
            return cfg;
        }

        void Upgrade(string path)
        {
            if (fileVersion == CurrentVersion) return;
            try
            {
                string backup = path + ".v" + fileVersion + ".bak";
                File.Copy(path, backup, true);
                foreach (string old in Directory.GetFiles(Path.GetDirectoryName(path), Path.GetFileName(path) + ".v*.bak"))
                    if (!string.Equals(old, backup, StringComparison.OrdinalIgnoreCase)) File.Delete(old);
                if (fileVersion > CurrentVersion) return;
                if (fileVersion < 2)
                {
                    AutoBrackets = false;
                    CapitalizeInBrackets = false;
                    LayoutPerApp = false;
                }
                Save(path);
            }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
        }

        void SetValue(string key, string val)
        {
            switch (key.ToLowerInvariant())
            {
                case "enabled": Enabled = Bool(val); break;
                case "language": UiLanguage = val.ToLowerInvariant() == "ru" ? "ru" : "en"; break;
                case "settingsversion": int.TryParse(val, out fileVersion); break;
                case "theme":
                    string theme = val.ToLowerInvariant();
                    Theme = theme == "light" || theme == "system" ? theme : "dark";
                    break;
                case "doublespaceintervalms": int.TryParse(val, out DoubleSpaceIntervalMs); break;
                case "capitalizeafterenter": CapitalizeAfterEnter = Bool(val); break;
                case "capitalizeonwindowswitch": CapitalizeOnWindowSwitch = Bool(val); break;
                case "capitalizeafterclick": CapitalizeAfterClick = Bool(val); break;
                case "capitalizeafterellipsis": CapitalizeAfterEllipsis = Bool(val); break;
                case "langrussian": LangRussian = Bool(val); break;
                case "langenglish": LangEnglish = Bool(val); break;
                case "langother": LangOther = Bool(val); break;
                case "skipfullscreen": SkipFullscreen = Bool(val); break;
                case "ignoreinjected": IgnoreInjected = Bool(val); break;
                case "pausehotkey": PauseHotkey = val; break;
                case "pinhotkey": PinHotkey = val; break;
                case "plainpastehotkey": PlainPasteHotkey = val; break;
                case "keepawakehotkey": KeepAwakeHotkey = val; break;
                case "keepawakeuntil": KeepAwakeUntil = val; break;
                case "keepawakehours": int.TryParse(val, out KeepAwakeHours); break;
                case "keepawakescreen": KeepAwakeScreen = Bool(val); break;
                case "bracketexit": BracketExit = val.ToLowerInvariant() == "double" ? "double" : "hold"; break;
                case "indicatorsize": IndicatorSize = val.ToLowerInvariant(); break;
                case "capitalizeinbrackets": CapitalizeInBrackets = Bool(val); break;
                case "layoutperapp": LayoutPerApp = Bool(val); break;
                case "layoutindicator": LayoutIndicator = Bool(val); break;
                case "pinontitlebar": PinOnTitleBar = Bool(val); break;
                case "layoutignore": LayoutIgnore = List(val); break;
                case "excludeapps": ExcludeApps = List(val); break;
                case "abbreviations": Abbreviations = List(val); break;
                default:
                    foreach (string f in Features)
                        if (f.Equals(key, StringComparison.OrdinalIgnoreCase)) SetFeature(f, Bool(val));
                    break;
            }
        }

        public bool GetFeature(string f)
        {
            switch (f)
            {
                case "Capitalize": return Capitalize;
                case "DoubleSpacePeriod": return DoubleSpacePeriod;
                case "AutoSpace": return AutoSpace;
                case "AutoHyphen": return AutoHyphen;
                case "SmartQuotes": return SmartQuotes;
                case "AutoBrackets": return AutoBrackets;
                case "CapsLockFix": return CapsLockFix;
                case "Typos": return Typos;
                case "EnglishI": return EnglishI;
                case "LayoutFix": return LayoutFix;
                case "Snippets": return Snippets;
            }
            return false;
        }

        public void SetFeature(string f, bool on)
        {
            switch (f)
            {
                case "Capitalize": Capitalize = on; break;
                case "DoubleSpacePeriod": DoubleSpacePeriod = on; break;
                case "AutoSpace": AutoSpace = on; break;
                case "AutoHyphen": AutoHyphen = on; break;
                case "SmartQuotes": SmartQuotes = on; break;
                case "AutoBrackets": AutoBrackets = on; break;
                case "CapsLockFix": CapsLockFix = on; break;
                case "Typos": Typos = on; break;
                case "EnglishI": EnglishI = on; break;
                case "LayoutFix": LayoutFix = on; break;
                case "Snippets": Snippets = on; break;
            }
        }

        public Config ForApp(string processName)
        {
            HashSet<string> off;
            if (processName == null || !AppRules.TryGetValue(processName, out off) || off.Count == 0) return this;
            var copy = (Config)MemberwiseClone();
            foreach (string f in off) copy.SetFeature(f, false);
            return copy;
        }

        public bool IsExcluded(string processName)
        {
            if (ExcludeApps.Contains(processName)) return true;
            foreach (string e in ExcludeApps)
                if (e.EndsWith("*") && processName.StartsWith(e.Substring(0, e.Length - 1), StringComparison.OrdinalIgnoreCase))
                    return true;
            return false;
        }

        public bool LangEnabled(Lang lang)
        {
            return lang == Lang.Ru ? LangRussian : lang == Lang.En ? LangEnglish : LangOther;
        }

        public bool KeepAwakeActive(DateTime now)
        {
            if (KeepAwakeUntil.Length == 0) return false;
            if (KeepAwakeUntil == "forever") return true;
            DateTime until;
            return DateTime.TryParse(KeepAwakeUntil, CultureInfo.InvariantCulture, DateTimeStyles.None, out until) && until > now;
        }

        public void StartKeepAwake(DateTime now)
        {
            KeepAwakeUntil = KeepAwakeHours <= 0 ? "forever" : now.AddHours(KeepAwakeHours).ToString("s", CultureInfo.InvariantCulture);
        }

        public void Save(string path)
        {
            var sb = new StringBuilder();
            sb.AppendLine("; BetterWriting settings. It is easier to change them in the settings window (click the tray icon).");
            sb.AppendLine("; 1 = on, 0 = off.");
            sb.AppendLine();
            sb.AppendLine("[Main]");
            Line(sb, "Version of this file. The app updates it by itself", "SettingsVersion", CurrentVersion.ToString());
            Line(sb, "Working or paused", "Enabled", Enabled);
            Line(sb, "Color theme: dark, light or system", "Theme", Theme);
            Line(sb, "Interface language: en or ru", "Language", UiLanguage);
            Line(sb, "Start sentences with a capital letter", "Capitalize", Capitalize);
            Line(sb, "Double space -> period and space", "DoubleSpacePeriod", DoubleSpacePeriod);
            Line(sb, "Max milliseconds between the two spaces", "DoubleSpaceIntervalMs", DoubleSpaceIntervalMs.ToString());
            Line(sb, "Spaces around punctuation: \"hello ,world\" -> \"hello, world\"", "AutoSpace", AutoSpace);
            Line(sb, "Russian hyphens: \"кое как\" -> \"кое-как\"", "AutoHyphen", AutoHyphen);
            Line(sb, "Russian quotes «» in the Russian layout", "SmartQuotes", SmartQuotes);
            Line(sb, "Close brackets and quotes automatically", "AutoBrackets", AutoBrackets);
            Line(sb, "How to leave an auto-closed bracket: hold (hold Space) or double (two spaces)", "BracketExit", BracketExit);
            Line(sb, "Capital letter right after an opening bracket", "CapitalizeInBrackets", CapitalizeInBrackets);
            Line(sb, "Remember the keyboard layout for each app", "LayoutPerApp", LayoutPerApp);
            Line(sb, "Show the layout and Caps Lock next to the mouse when they change", "LayoutIndicator", LayoutIndicator);
            Line(sb, "Indicator size: small, medium or large", "IndicatorSize", IndicatorSize);
            Line(sb, "Offer a keep-on-top button when you right-click a title bar", "PinOnTitleBar", PinOnTitleBar);
            Line(sb, "Hotkey to keep the active window on top. Empty = no hotkey", "PinHotkey", PinHotkey);
            Line(sb, "Hotkey to paste without formatting. Empty = no hotkey", "PlainPasteHotkey", PlainPasteHotkey);
            Line(sb, "Hotkey to keep the computer awake. Empty = no hotkey", "KeepAwakeHotkey", KeepAwakeHotkey);
            Line(sb, "Keep the computer awake until this time. Empty = off, forever = until turned off", "KeepAwakeUntil", KeepAwakeUntil);
            Line(sb, "How many hours to keep awake when turned on. 0 = until turned off", "KeepAwakeHours", KeepAwakeHours.ToString());
            Line(sb, "Keep the screen on as well", "KeepAwakeScreen", KeepAwakeScreen);
            Line(sb, "Accidental Caps Lock: \"hELLO\" -> \"Hello\"", "CapsLockFix", CapsLockFix);
            Line(sb, "Common typos: \"teh\" -> \"the\"", "Typos", Typos);
            Line(sb, "\"i\" -> \"I\" in English", "EnglishI", EnglishI);
            Line(sb, "Wrong keyboard layout: \"ghbdtn\" -> \"привет\"", "LayoutFix", LayoutFix);
            Line(sb, "Text shortcuts (list in the [Snippets] section)", "Snippets", Snippets);
            Line(sb, "Capital after Enter", "CapitalizeAfterEnter", CapitalizeAfterEnter);
            Line(sb, "Capital after switching to another window", "CapitalizeOnWindowSwitch", CapitalizeOnWindowSwitch);
            Line(sb, "Capital after a mouse click, if the cursor is at the start of a sentence", "CapitalizeAfterClick", CapitalizeAfterClick);
            Line(sb, "Capital after an ellipsis", "CapitalizeAfterEllipsis", CapitalizeAfterEllipsis);
            Line(sb, "Work in the Russian layout (also Ukrainian and Belarusian)", "LangRussian", LangRussian);
            Line(sb, "Work in the English layout", "LangEnglish", LangEnglish);
            Line(sb, "Work in other layouts", "LangOther", LangOther);
            Line(sb, "Do nothing in full-screen apps", "SkipFullscreen", SkipFullscreen);
            Line(sb, "Ignore input typed by other programs", "IgnoreInjected", IgnoreInjected);
            Line(sb, "Hotkey to pause and resume, for example Ctrl+Alt+B. Empty = no hotkey", "PauseHotkey", PauseHotkey);
            Line(sb, "Words the keyboard layout fix never touches", "LayoutIgnore", Join(LayoutIgnore));
            Line(sb, "Programs to leave alone (.exe name; * at the end matches anything)", "ExcludeApps", Join(ExcludeApps));
            Line(sb, "Abbreviations not followed by a capital (without the last period)", "Abbreviations", Join(Abbreviations));

            sb.AppendLine();
            sb.AppendLine("; Per-app rules: program = features turned off in it.");
            sb.AppendLine("; Feature names: " + string.Join(", ", Features));
            sb.AppendLine("[Apps]");
            foreach (var r in AppRules) sb.AppendLine(r.Key + " = " + Join(r.Value));

            sb.AppendLine();
            sb.AppendLine("; Remembered keyboard layouts: program = language id.");
            sb.AppendLine("[Layouts]");
            foreach (var l in AppLayouts) sb.AppendLine(l.Key + " = " + l.Value);

            sb.AppendLine();
            sb.AppendLine("; Your own corrections: wrong = right.");
            sb.AppendLine("[Corrections]");
            foreach (var c in CustomTypos) sb.AppendLine(c.Key + " = " + Escape(c.Value));

            sb.AppendLine();
            sb.AppendLine("; Text shortcuts: shortcut = text. Write a line break as \\n.");
            sb.AppendLine("[Snippets]");
            foreach (var s in SnippetList) sb.AppendLine(s.Key + " = " + Escape(s.Value));

            string temp = path + ".tmp";
            File.WriteAllText(temp, sb.ToString(), new UTF8Encoding(true));
            try
            {
                if (File.Exists(path)) File.Replace(temp, path, null);
                else File.Move(temp, path);
            }
            catch (IOException)
            {
                File.WriteAllText(path, sb.ToString(), new UTF8Encoding(true));
                File.Delete(temp);
            }
        }

        static Dictionary<string, string> DefaultSnippets()
        {
            var d = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            d["brb"] = "be right back";
            d["спс"] = "спасибо";
            return d;
        }

        static void Line(StringBuilder sb, string comment, string key, bool value) { Line(sb, comment, key, value ? "1" : "0"); }

        static void Line(StringBuilder sb, string comment, string key, string value)
        {
            sb.AppendLine();
            sb.AppendLine("; " + comment);
            sb.AppendLine(key + "=" + value);
        }

        static string Join(IEnumerable<string> items) { return string.Join(", ", items.ToArray()); }

        static string Escape(string s) { return s.Replace("\\", "\\\\").Replace("\r\n", "\n").Replace("\n", "\\n"); }

        static string Unescape(string s)
        {
            var sb = new StringBuilder();
            for (int i = 0; i < s.Length; i++)
            {
                if (s[i] == '\\' && i + 1 < s.Length)
                {
                    char n = s[++i];
                    sb.Append(n == 'n' ? '\n' : n);
                }
                else sb.Append(s[i]);
            }
            return sb.ToString();
        }

        static bool Bool(string v) { return v == "1" || v.Equals("true", StringComparison.OrdinalIgnoreCase); }

        public static HashSet<string> List(string v)
        {
            var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (string part in v.Split(','))
            {
                string p = part.Trim();
                if (p.EndsWith(".exe", StringComparison.OrdinalIgnoreCase)) p = p.Substring(0, p.Length - 4);
                if (p.Length > 0) set.Add(p);
            }
            return set;
        }
    }
}
