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
                now += 100;
                EngineAction act;
                if (c == '\b')
                {
                    act = engine.OnBackspace();
                    if (!act.Replace) { if (screen.Length > 0) screen.Length--; continue; }
                }
                else if (c == '\n')
                {
                    act = engine.OnEnter();
                    Apply(screen, act);
                    screen.Append('\n');
                    engine.Reset(cfg.CapitalizeAfterEnter);
                    continue;
                }
                else
                {
                    act = engine.OnChar(c, now, false, true);
                    if (!act.Replace) { screen.Append(c); continue; }
                }
                Apply(screen, act);
            }
            string got = screen.ToString();
            if (got == expected) return;
            failed++;
            Console.WriteLine("FAIL: \"" + Show(label) + "\"\n  expected: \"" + Show(expected) + "\"\n  got:      \"" + Show(got) + "\"");
        }

        static void Apply(StringBuilder screen, EngineAction act)
        {
            if (!act.Replace) return;
            screen.Length -= Math.Min(act.Backspaces, screen.Length);
            screen.Append(act.Text);
        }

        static string Show(string s) { return s.Replace("\n", "\\n").Replace("\b", "\\b"); }
    }
}
