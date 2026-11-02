using System;
using System.IO;
using System.Text;

namespace BetterWriting
{
    static class EngineTests
    {
        static int failed, total;

        static int Main()
        {
            Dictionaries.LoadNow();
            var cfg = Fresh();

            Check(cfg, "привет. как дела? норм! ок", "Привет. Как дела? Норм! Ок");
            Check(cfg, "hello world  next", "Hello world. Next");
            Check(cfg, "т.е. так. и т.д. Далее", "Т.е. так. И т.д. Далее");
            Check(cfg, "а. с. пушкин", "А. с. Пушкин");
            Check(cfg, "в 2020 г. было", "В 2020 г. было");
            Check(cfg, "ну... ладно", "Ну... ладно");
            Check(cfg, "строка\nновая", "Строка\nНовая");
            Check(cfg, "\bпривет", "привет");
            Check(cfg, "п\bпривет", "привет");
            Check(cfg, "i said no. then", "I said no. Then");

            Check(cfg, "один  два", "Один. Два");
            Check(cfg, "один ~ два", "Один  два");
            Check(cfg, "конец.  дальше", "Конец.  Дальше");
            Check(cfg, "один  \b", "Один  ");

            Check(cfg, "привет ,как дела", "Привет, как дела");
            Check(cfg, "привет,как", "Привет, как");
            Check(cfg, "конец.начало", "Конец. Начало");
            Check(cfg, "привет!как дела", "Привет! Как дела");
            Check(cfg, "это site.com тут", "Это site.com тут");
            Check(cfg, "т.е.так", "Т.е.так");
            Check(cfg, "число 3,5 и 10:30", "Число 3,5 и 10:30");
            Check(cfg, "смайл :)", "Смайл :)");
            Check(cfg, "ссылка example.org/a?b,c", "Ссылка example.org/a?b,c");
            Check(cfg, "привет,к\b", "Привет,к");

            Check(cfg, "он сказал \"привет\" и ушёл", "Он сказал «привет» и ушёл");
            Check(cfg, "\"а \"б\" в\"", "«А „б“ в»");
            Check(cfg, "\"\b", "\"");
            CheckLang(cfg, Lang.En, "he said \"hi\"", "He said \"hi\"");

            CheckCaps(cfg, "пРИВЕТ ", "Привет ");
            CheckCaps(cfg, "ПРИВЕТ ", "ПРИВЕТ ");
            CheckCapsMid(cfg, "Привет ", "пРИВЕТ ", "Привет привет ");
            CheckCapsMid(cfg, "Конец. ", "пРИВЕТ ", "Конец. Привет ");

            Check(cfg, "слово(пояснение)", "Слово (пояснение)");
            Check(cfg, "слово (пояснение)", "Слово (пояснение)");
            Check(cfg, "жаль((", "Жаль ((");
            Check(cfg, "смайл :(", "Смайл :(");
            Check(cfg, "ссылка site.com/a(b)", "Ссылка site.com/a(b)");
            Check(cfg, "слово(\b", "Слово(");
            Check(cfg, "ну извене.", "Ну извини.");

            Check(cfg, "вообщем, да", "В общем, да");
            Check(cfg, "ну извени.", "Ну извини.");
            Check(cfg, "teh end", "The end");
            Check(cfg, "ok i think", "Ok I think");
            Check(cfg, "ok i'm fine", "Ok I'm fine");
            Check(cfg, "ok it is", "Ok it is");
            Check(cfg, "ну вообщем \b", "Ну вообщем ");

            Check(cfg, "кое как сделал", "Кое-как сделал");
            Check(cfg, "кое с кем поговорил", "Кое с кем поговорил");
            Check(cfg, "когда нибудь приеду", "Когда-нибудь приеду");
            Check(cfg, "либо да, либо нет", "Либо да, либо нет");
            Check(cfg, "всё таки пришёл", "Всё-таки пришёл");
            Check(cfg, "он таки пришёл", "Он таки пришёл");
            Check(cfg, "точь в точь как", "Точь-в-точь как");
            Check(cfg, "по моему, да", "По-моему, да");
            Check(cfg, "по моему совету", "По моему совету");
            Check(cfg, "сделай по другому не выйдет", "Сделай по-другому не выйдет");
            Check(cfg, "кое как \b", "Кое как ");
            Check(cfg, "какието слова", "Какие-то слова");
            Check(cfg, "какойнибудь вариант", "Какой-нибудь вариант");
            Check(cfg, "ктото пришёл", "Кто-то пришёл");
            Check(cfg, "Чтолибо, да", "Что-либо, да");
            Check(cfg, "коекак сделал", "Кое-как сделал");
            Check(cfg, "изза тебя", "Из-за тебя");
            Check(cfg, "помоему да", "По-моему да");
            Check(cfg, "вопервых да", "Во-первых да");
            Check(cfg, "точьвточь как", "Точь-в-точь как");
            Check(cfg, "дайка мне", "Дай-ка мне");
            Check(cfg, "вообще то да", "Вообще-то да");
            Check(cfg, "вообщето да", "Вообще-то да");
            Check(cfg, "какието \b", "Какието ");
            Check(cfg, "какое то", "Какое то");
            Check(cfg, "поиски собака то", "Поиски собака то");
            Check(cfg, "койка кое", "Койка кое");
            Check(cfg, "что нибудь\nок", "Что-нибудь\nОк");
            Check(cfg, "кое как  ок", "Кое-как. Ок");

            CheckLang(cfg, Lang.En, "ghbdtn rfr ltkf", "Привет как дела");
            CheckLang(cfg, Lang.Ru, "руддщ цщкдв", "Hello world");
            CheckLang(cfg, Lang.En, "hello world", "Hello world");
            CheckLang(cfg, Lang.En, "lf ", "Да ");
            CheckLang(cfg, Lang.En, "ghbdtn? rfr", "Привет, как");
            CheckLang(cfg, Lang.En, "ghbdtn \b", "Ghbdtn ");
            CheckLang(cfg, Lang.En, "iphone 15", "Iphone 15");
            CheckLang(cfg, Lang.En, "test@mail.ru ", "Test@mail.ru ");

            Check(cfg, "спс ", "Спасибо ");
            Check(cfg, "ну спс.", "Ну спасибо.");
            Check(cfg, "спс \b", "Спс ");
            var withMail = Fresh();
            withMail.SnippetList[";почта"] = "me@mail.ru";
            withMail.SnippetList["подп"] = "С уважением,\nРуди";
            Check(withMail, "пиши ;почта ", "Пиши me@mail.ru ");
            Check(withMail, "подп\nпс", "С уважением,\nРуди\nПс");
            string iniPath = Path.Combine(Path.GetTempPath(), "bw-snip-" + Guid.NewGuid().ToString("N") + ".ini");
            withMail.Save(iniPath);
            var reloaded = Config.Load(iniPath);
            File.Delete(iniPath);
            Expect(reloaded.SnippetList.ContainsKey(";почта") && reloaded.SnippetList["подп"] == "С уважением,\nРуди", "вставки после сохранения");

            var custom = Fresh();
            custom.CustomTypos["превед"] = "привет";
            custom.CustomTypos["cnj"] = "Сто";
            custom.LayoutIgnore.Add("ghbdtn");
            Check(custom, "ну превед ", "Ну привет ");
            Check(custom, "превед ", "Привет ");
            Check(custom, "ну превед \b", "Ну превед ");
            CheckLang(custom, Lang.En, "ghbdtn ", "Ghbdtn ");
            CheckLang(custom, Lang.En, "ok cnj ", "Ok Сто ");
            CheckLang(cfg, Lang.En, "ok cnj ", "Ok сто ");
            string customPath = Path.Combine(Path.GetTempPath(), "bw-custom-" + Guid.NewGuid().ToString("N") + ".ini");
            custom.PauseHotkey = "";
            custom.Save(customPath);
            var customLoaded = Config.Load(customPath);
            File.Delete(customPath);
            Expect(customLoaded.CustomTypos["превед"] == "привет" && customLoaded.LayoutIgnore.Contains("ghbdtn") && customLoaded.PauseHotkey == "", "custom corrections survive saving");

            uint mods, vk;
            Expect(Hotkey.TryParse("Ctrl+Alt+B", out mods, out vk) && mods == (Hotkey.ModControl | Hotkey.ModAlt) && vk == 0x42, "parse Ctrl+Alt+B");
            Expect(Hotkey.TryParse("Win+F12", out mods, out vk) && mods == Hotkey.ModWin && vk == 0x7B, "parse Win+F12");
            Expect(!Hotkey.TryParse("B", out mods, out vk) && !Hotkey.TryParse("Shift+B", out mods, out vk) && !Hotkey.TryParse("", out mods, out vk), "reject hotkeys without Ctrl, Alt or Win");
            Expect(Hotkey.Format(true, true, false, false, 0x42) == "Ctrl+Alt+B" && Hotkey.Format(false, false, true, false, 0x42) == null && Hotkey.Format(true, false, false, false, 0x20) == null, "format hotkeys");

            var defaults = Fresh();
            Expect(!defaults.AutoBrackets && !defaults.CapitalizeInBrackets && defaults.BracketExit == "hold" && defaults.IndicatorSize == "small" && !defaults.LayoutPerApp && defaults.Theme == "dark", "new defaults");

            var twoSpaces = Fresh();
            twoSpaces.AutoBrackets = true;
            twoSpaces.CapitalizeInBrackets = true;
            twoSpaces.BracketExit = "double";
            Check(twoSpaces, "(привет  ", "(Привет)");
            Check(twoSpaces, "(привет  и", "(Привет)и");
            Check(twoSpaces, "слово (привет) и", "Слово (Привет) и");
            Check(twoSpaces, "слово [да  ", "Слово [Да]");
            Check(twoSpaces, "слово {да}", "Слово {Да}");
            Check(twoSpaces, "(а (б  в  ", "(А (Б)в)");
            Check(twoSpaces, "(\bа", "А");
            Check(twoSpaces, "ну \"да  ", "Ну «да»");
            CheckLang(twoSpaces, Lang.En, "say \"hi  ", "Say \"hi\"");
            Check(twoSpaces, "ссылка a.com/b(c", "Ссылка a.com/b(c");
            Check(twoSpaces, "\"(\", \"{\" и", "«()», «{}» и");
            Check(twoSpaces, "жаль((", "Жаль (()");

            var holdExit = Fresh();
            holdExit.AutoBrackets = true;
            holdExit.BracketExit = "hold";
            Check(holdExit, "ну (да ␣␣␣", "Ну (да)");
            Check(holdExit, "ну (да ␣␣␣ и", "Ну (да) и");
            Check(holdExit, "ну (да  и", "Ну (да. И)");
            Check(holdExit, "ну (да ^␣␣ и", "Ну (да) и");
            Check(holdExit, "ну (да ^и", "Ну (да)и");
            Check(holdExit, "ну да ^и", "Ну да и");
            Check(holdExit, "ну (^и", "Ну (и)");
            Check(holdExit, "ну да ␣␣", "Ну да   ");

            var noPairs = Fresh();
            Check(noPairs, "\"(\", \"{\" и", "«(», «{» и");
            Check(noPairs, "\"а \"б\" в\"", "«А „б“ в»");
            Check(noPairs, "слово (да)", "Слово (да)");
            var vars = Fresh();
            vars.SnippetList["имя"] = "{date:yyyy}, {clipboard}, {нет}";
            var varEngine = new Engine(vars) { Now = () => new DateTime(2026, 11, 2), ClipboardText = () => "буфер" };
            varEngine.Reset(false);
            Verify(varEngine, vars, "", "имя ", "2026, буфер, {нет} ", "snippet variables");

            var awake = Fresh();
            var clock = new DateTime(2026, 11, 2, 12, 0, 0);
            Expect(!awake.KeepAwakeActive(clock), "stay awake is off by default");
            awake.KeepAwakeHours = 3;
            awake.StartKeepAwake(clock);
            Expect(awake.KeepAwakeActive(clock.AddHours(2.9)) && !awake.KeepAwakeActive(clock.AddHours(3.1)), "stay awake ends on time");
            awake.KeepAwakeHours = 0;
            awake.StartKeepAwake(clock);
            Expect(awake.KeepAwakeActive(clock.AddYears(1)), "stay awake until turned off");
            string awakePath = Path.Combine(Path.GetTempPath(), "bw-awake-" + Guid.NewGuid().ToString("N") + ".ini");
            awake.KeepAwakeHotkey = "Ctrl+Alt+K";
            awake.Save(awakePath);
            var awakeLoaded = Config.Load(awakePath);
            File.Delete(awakePath);
            Expect(awakeLoaded.KeepAwakeUntil == "forever" && awakeLoaded.KeepAwakeHotkey == "Ctrl+Alt+K" && awakeLoaded.KeepAwakeHours == 0, "stay awake survives saving");
            string oldIni = Path.Combine(Path.GetTempPath(), "bw-old-" + Guid.NewGuid().ToString("N") + ".ini");
            File.WriteAllText(oldIni, "[Main]\nCapitalize=0\nAutoBrackets=1\nLayoutPerApp=1\nBracketExit=tab\n[Variables]\nenabled = 0\n", Encoding.UTF8);
            var oldLoaded = Config.Load(oldIni);
            Expect(!oldLoaded.Capitalize && oldLoaded.Enabled && oldLoaded.Snippets && !oldLoaded.AutoBrackets && !oldLoaded.LayoutPerApp && oldLoaded.BracketExit == "hold", "an older settings file is upgraded");
            Expect(File.Exists(oldIni + ".v1.bak") && File.ReadAllText(oldIni).Contains("SettingsVersion=" + Config.CurrentVersion), "the upgrade keeps a backup and marks the version");
            File.Delete(oldIni + ".v1.bak");
            var upgraded = Config.Load(oldIni);
            upgraded.AutoBrackets = true;
            upgraded.Save(oldIni);
            Expect(Config.Load(oldIni).AutoBrackets && !File.Exists(oldIni + ".v1.bak"), "an upgraded file is not upgraded again");
            File.Delete(oldIni);
            string newerIni = Path.Combine(Path.GetTempPath(), "bw-new-" + Guid.NewGuid().ToString("N") + ".ini");
            File.WriteAllText(newerIni, "[Main]\nSettingsVersion=99\nCapitalize=0\nFutureOption=1\n", Encoding.UTF8);
            File.WriteAllText(newerIni + ".v1.bak", "old", Encoding.UTF8);
            var newerLoaded = Config.Load(newerIni);
            Expect(!newerLoaded.Capitalize && File.Exists(newerIni + ".v99.bak") && File.ReadAllText(newerIni).Contains("FutureOption") && !File.Exists(newerIni + ".v1.bak"), "a file from a newer version is backed up, left alone, and only the last backup stays");
            File.Delete(newerIni);
            File.Delete(newerIni + ".v99.bak");
            var noEnglish = Fresh();
            noEnglish.LangEnglish = false;
            CheckLang(noEnglish, Lang.En, "hello  world", "hello  world");
            CheckLang(noEnglish, Lang.Ru, "привет  мир", "Привет. Мир");

            CheckContext(cfg, "Привет", true, "ка", "Приветка");
            CheckContext(cfg, "Привет ", true, "как", "Привет как");
            CheckContext(cfg, "Привет. ", true, "как", "Привет. Как");
            CheckContext(cfg, "", true, "как", "Как");
            CheckContext(cfg, "Первая строка\nВторая. ", true, "и", "Первая строка\nВторая. И");
            CheckContext(cfg, "Режим чата\n\uFFFC", false, "привет", "Режим чата\n\uFFFCПривет");
            CheckPassword(cfg);

            var rules = Fresh();
            rules.AppRules["Telegram"] = Config.List("Capitalize, DoubleSpacePeriod");
            Expect(!rules.ForApp("telegram").Capitalize && rules.ForApp("telegram").AutoHyphen, "правило для Telegram");
            Expect(rules.ForApp("chrome").Capitalize, "без правила всё включено");
            Check(rules.ForApp("Telegram"), "привет  мир", "привет  мир");

            Console.WriteLine(failed == 0 ? "OK: all " + total + " tests passed" : "FAILED: " + failed + " of " + total);
            return failed;
        }

        static Config Fresh()
        {
            string ini = Path.Combine(Path.GetTempPath(), "bw-test-" + Guid.NewGuid().ToString("N") + ".ini");
            var cfg = Config.Load(ini);
            File.Delete(ini);
            return cfg;
        }

        static void Expect(bool ok, string label)
        {
            total++;
            if (ok) return;
            failed++;
            Console.WriteLine("FAIL: " + label);
        }

        static void Check(Config cfg, string typed, string expected) { CheckLang(cfg, Lang.Ru, typed, expected); }

        static void CheckLang(Config cfg, Lang lang, string typed, string expected)
        {
            var engine = new Engine(cfg) { Lang = lang };
            engine.Reset(true);
            Verify(engine, cfg, "", typed, expected, typed);
        }

        static void CheckCaps(Config cfg, string typed, string expected)
        {
            var engine = new Engine(cfg) { CapsLock = true };
            engine.Reset(true);
            Verify(engine, cfg, "", typed, expected, "[caps] " + typed);
        }

        static void CheckCapsMid(Config cfg, string before, string typed, string expected)
        {
            var engine = new Engine(cfg);
            engine.SetContext(before, true, false, true);
            engine.CapsLock = true;
            Verify(engine, cfg, before, typed, expected, "[" + before + "][caps] " + typed);
        }

        static void CheckContext(Config cfg, string before, bool atStart, string typed, string expected)
        {
            var engine = new Engine(cfg);
            engine.SetContext(before, atStart, false, true);
            Verify(engine, cfg, before, typed, expected, "[" + before + "]" + typed);
        }

        static void CheckPassword(Config cfg)
        {
            var engine = new Engine(cfg);
            engine.SetContext("", true, true, true);
            Verify(engine, cfg, "", "secret  кое как", "secret  кое как", "password field");
        }

        static void Verify(Engine engine, Config cfg, string initial, string typed, string expected, string label)
        {
            total++;
            var screen = new StringBuilder(initial);
            int caret = initial.Length;
            long now = 1000;
            Lang startLang = engine.Lang;
            foreach (char key in typed)
            {
                char c = key;
                if (engine.Lang != startLang && key != '\b' && key != '\n' && key != '~')
                {
                    string conv = Layouts.Convert(key.ToString(), startLang);
                    if (conv != null) c = conv[0];
                }
                if (c == '~') { now += 5000; continue; }
                if (c == '^')
                {
                    var leave = engine.OnSpaceHeld();
                    Apply(screen, ref caret, leave);
                    continue;
                }
                bool held = c == '␣';
                if (held) c = ' ';
                now += 100;
                EngineAction act;
                if (c == '\b')
                {
                    act = engine.OnBackspace();
                    if (!act.Replace)
                    {
                        if (caret > 0) { screen.Remove(caret - 1, 1); caret--; }
                        continue;
                    }
                }
                else if (c == '\n')
                {
                    act = engine.OnEnter();
                    Apply(screen, ref caret, act);
                    screen.Insert(caret++, '\n');
                    engine.Reset(cfg.CapitalizeAfterEnter);
                    continue;
                }
                else
                {
                    act = engine.OnChar(c, now, held, true);
                    if (!act.Replace) { screen.Insert(caret++, c); continue; }
                }
                Apply(screen, ref caret, act);
            }
            string got = screen.ToString();
            if (got == expected) return;
            failed++;
            Console.WriteLine("FAIL: \"" + Show(label) + "\"\n  expected: \"" + Show(expected) + "\"\n  got:      \"" + Show(got) + "\"");
        }

        static void Apply(StringBuilder screen, ref int caret, EngineAction act)
        {
            if (!act.Replace) return;
            int back = Math.Min(act.Backspaces, caret);
            screen.Remove(caret - back, back);
            caret -= back;
            screen.Insert(caret, act.Text);
            caret += act.Text.Length;
            caret = Math.Max(0, caret - act.CaretLeft);
            caret = Math.Min(screen.Length, caret + act.CaretRight);
            if (act.DeleteRight > 0) screen.Remove(caret, Math.Min(act.DeleteRight, screen.Length - caret));
        }

        static string Show(string s) { return s.Replace("\n", "\\n").Replace("\b", "\\b"); }
    }
}
