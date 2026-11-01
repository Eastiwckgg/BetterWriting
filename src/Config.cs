using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace BetterWriting
{
    sealed class Config
    {
        public static readonly string[] Features =
        {
            "Capitalize", "DoubleSpacePeriod", "AutoSpace", "AutoHyphen", "SmartQuotes",
            "CapsLockFix", "Typos", "EnglishI", "LayoutFix", "Snippets"
        };

        public bool Enabled = true;
        public string UiLanguage = "en";
        public bool Capitalize = true;
        public bool DoubleSpacePeriod = true;
        public int DoubleSpaceIntervalMs = 600;
        public bool AutoSpace = true;
        public bool AutoHyphen = true;
        public bool SmartQuotes = true;
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
                cfg.Save(path);
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
                else cfg.SetValue(key, val);
            }
            return cfg;
        }

        void SetValue(string key, string val)
        {
            switch (key.ToLowerInvariant())
            {
                case "enabled": Enabled = Bool(val); break;
                case "language": UiLanguage = val.ToLowerInvariant() == "ru" ? "ru" : "en"; break;
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

        public void Save(string path)
        {
            var sb = new StringBuilder();
            sb.AppendLine("; BetterWriting settings. It is easier to change them in the settings window (click the tray icon).");
            sb.AppendLine("; 1 = on, 0 = off.");
            sb.AppendLine();
            sb.AppendLine("[Main]");
            Line(sb, "Working or paused", "Enabled", Enabled);
            Line(sb, "Interface language: en or ru", "Language", UiLanguage);
            Line(sb, "Start sentences with a capital letter", "Capitalize", Capitalize);
            Line(sb, "Double space -> period and space", "DoubleSpacePeriod", DoubleSpacePeriod);
            Line(sb, "Max milliseconds between the two spaces", "DoubleSpaceIntervalMs", DoubleSpaceIntervalMs.ToString());
            Line(sb, "Spaces around punctuation: \"hello ,world\" -> \"hello, world\"", "AutoSpace", AutoSpace);
            Line(sb, "Russian hyphens: \"кое как\" -> \"кое-как\"", "AutoHyphen", AutoHyphen);
            Line(sb, "Russian quotes «» in the Russian layout", "SmartQuotes", SmartQuotes);
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
            Line(sb, "Programs to leave alone (.exe name; * at the end matches anything)", "ExcludeApps", Join(ExcludeApps));
            Line(sb, "Abbreviations not followed by a capital (without the last period)", "Abbreviations", Join(Abbreviations));

            sb.AppendLine();
            sb.AppendLine("; Per-app rules: program = features turned off in it.");
            sb.AppendLine("; Feature names: " + string.Join(", ", Features));
            sb.AppendLine("[Apps]");
            foreach (var r in AppRules) sb.AppendLine(r.Key + " = " + Join(r.Value));

            sb.AppendLine();
            sb.AppendLine("; Text shortcuts: shortcut = text. Write a line break as \\n.");
            sb.AppendLine("[Snippets]");
            foreach (var s in SnippetList) sb.AppendLine(s.Key + " = " + Escape(s.Value));

            File.WriteAllText(path, sb.ToString(), new UTF8Encoding(true));
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
