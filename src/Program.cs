using System;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;
using Microsoft.Win32;

namespace BetterWriting
{
    static class Program
    {
        const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
        const string AppName = "BetterWriting";
        public const string ReloadEventName = @"Local\BetterWriting.Reload";
        public const string QuitEventName = @"Local\BetterWriting.Quit";
        public const string DemoEventName = @"Local\BetterWriting.DemoFocus";
        public const string CloseSettingsEventName = @"Local\BetterWriting.CloseSettings";

        public static readonly string ExePath = Process.GetCurrentProcess().MainModule.FileName;
        public static readonly string ConfigPath = ResolveConfigPath();

        static string ResolveConfigPath()
        {
            const string FileName = "BetterWriting.ini";
            string appData = Environment.GetEnvironmentVariable("APPDATA");
            if (string.IsNullOrEmpty(appData)) appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string shared = Path.Combine(appData, "BetterWriting", FileName);
            Directory.CreateDirectory(Path.GetDirectoryName(shared));
            if (File.Exists(shared)) return shared;

            string local = Path.Combine(Path.GetDirectoryName(ExePath), FileName);
            string old = AutostartExe();
            string oldIni = old == null ? null : Path.Combine(Path.GetDirectoryName(old), FileName);
            try
            {
                if (File.Exists(local)) File.Move(local, shared);
                else if (oldIni != null && File.Exists(oldIni)) File.Copy(oldIni, shared);
            }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
            return shared;
        }

        static string AutostartExe()
        {
            try
            {
                using (var key = Registry.CurrentUser.OpenSubKey(RunKey))
                {
                    string val = key == null ? null : key.GetValue(AppName) as string;
                    if (val == null) return null;
                    string old = val.Trim();
                    if (old.StartsWith("\""))
                    {
                        int end = old.IndexOf('"', 1);
                        return end > 0 ? old.Substring(1, end - 1) : old.Substring(1);
                    }
                    int args = old.IndexOf(" --", StringComparison.Ordinal);
                    return args > 0 ? old.Substring(0, args) : old;
                }
            }
            catch (System.Security.SecurityException) { return null; }
            catch (ArgumentException) { return null; }
        }

        static bool IsSameOrNewer(string exe)
        {
            try
            {
                return new Version(FileVersionInfo.GetVersionInfo(exe).FileVersion) >= new Version(FileVersionInfo.GetVersionInfo(ExePath).FileVersion);
            }
            catch (ArgumentException) { return true; }
            catch (FormatException) { return true; }
        }

        [STAThread]
        static int Main(string[] args)
        {
            string arg = args.Length > 0 ? args[0].ToLowerInvariant() : "";
            if (arg == "--autostart-on") { SetAutostart(true); return 0; }
            if (arg == "--autostart-off") { SetAutostart(false); return 0; }
            if (arg == "--settings") return SettingsApp.Run();

            bool created;
            using (var mutex = new Mutex(true, @"Local\BetterWriting.SingleInstance", out created))
            {
                if (!created) return SettingsApp.Run();
                RunTray(arg != "--background");
                GC.KeepAlive(mutex);
            }
            return 0;
        }

        static void RunTray(bool showSettings)
        {
            RepairAutostart();
            using (new TrayApp())
            {
                if (showSettings) OpenSettings();
                Application.Run();
            }
        }

        public static void OpenSettings()
        {
            Process.Start(ExePath, "--settings");
        }

        public static bool IsMainRunning()
        {
            EventWaitHandle h;
            if (!EventWaitHandle.TryOpenExisting(ReloadEventName, out h)) return false;
            h.Dispose();
            return true;
        }

        public static void Signal(string name)
        {
            EventWaitHandle h;
            if (!EventWaitHandle.TryOpenExisting(name, out h)) return;
            using (h) h.Set();
        }

        public static bool IsAutostart()
        {
            using (var key = Registry.CurrentUser.OpenSubKey(RunKey))
            {
                var val = key == null ? null : key.GetValue(AppName) as string;
                return val != null && val.IndexOf(ExePath, StringComparison.OrdinalIgnoreCase) >= 0;
            }
        }

        public static string Theme = "dark";

        public static bool IsDark()
        {
            return Theme == "light" ? false : Theme == "system" ? IsSystemDark() : true;
        }

        public static Color Rgb(int hex)
        {
            return Color.FromArgb((hex >> 16) & 0xFF, (hex >> 8) & 0xFF, hex & 0xFF);
        }

        public static bool IsSystemDark()
        {
            using (var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize"))
            {
                object v = key == null ? null : key.GetValue("AppsUseLightTheme");
                return v is int && (int)v == 0;
            }
        }

        static void RepairAutostart()
        {
            try
            {
                string old = AutostartExe();
                if (old == null || string.Equals(old, ExePath, StringComparison.OrdinalIgnoreCase)) return;
                if (File.Exists(old) && IsSameOrNewer(old)) return;
                SetAutostart(true);
            }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
            catch (System.Security.SecurityException) { }
        }

        public static void SetAutostart(bool on)
        {
            using (var key = Registry.CurrentUser.CreateSubKey(RunKey))
            {
                if (on) key.SetValue(AppName, "\"" + ExePath + "\" --background");
                else if (key.GetValue(AppName) != null) key.DeleteValue(AppName);
            }
        }
    }

    sealed class TrayMenuRenderer : ToolStripProfessionalRenderer
    {
        readonly Color back, hover, text, line, accent;

        public TrayMenuRenderer(bool dark)
        {
            RoundedEdges = false;
            back = dark ? Program.Rgb(0x2C2C2C) : Program.Rgb(0xF9F9F9);
            hover = dark ? Program.Rgb(0x3E3E3E) : Program.Rgb(0xE8E8E8);
            text = dark ? Program.Rgb(0xF2F2F2) : Program.Rgb(0x1F1F1F);
            line = dark ? Program.Rgb(0x444444) : Program.Rgb(0xDEDEDE);
            accent = dark ? Program.Rgb(0xF2F2F2) : Program.Rgb(0x1F1F1F);
        }

        protected override void OnRenderToolStripBackground(ToolStripRenderEventArgs e)
        {
            using (var b = new SolidBrush(back)) e.Graphics.FillRectangle(b, e.AffectedBounds);
        }

        protected override void OnRenderToolStripBorder(ToolStripRenderEventArgs e)
        {
            using (var p = new Pen(line)) e.Graphics.DrawRectangle(p, 0, 0, e.ToolStrip.Width - 1, e.ToolStrip.Height - 1);
        }

        protected override void OnRenderImageMargin(ToolStripRenderEventArgs e) { }

        protected override void OnRenderMenuItemBackground(ToolStripItemRenderEventArgs e)
        {
            if (!e.Item.Selected) return;
            using (var b = new SolidBrush(hover)) e.Graphics.FillRectangle(b, 3, 1, e.Item.Width - 6, e.Item.Height - 2);
        }

        protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
        {
            e.TextColor = text;
            base.OnRenderItemText(e);
        }

        protected override void OnRenderSeparator(ToolStripSeparatorRenderEventArgs e)
        {
            int y = e.Item.Height / 2;
            using (var p = new Pen(line)) e.Graphics.DrawLine(p, 8, y, e.Item.Width - 8, y);
        }

        protected override void OnRenderItemCheck(ToolStripItemImageRenderEventArgs e)
        {
            var r = e.ImageRectangle;
            var old = e.Graphics.SmoothingMode;
            e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            using (var p = new Pen(accent, 2f))
                e.Graphics.DrawLines(p, new[]
                {
                    new PointF(r.Left + r.Width * 0.2f, r.Top + r.Height * 0.52f),
                    new PointF(r.Left + r.Width * 0.42f, r.Top + r.Height * 0.74f),
                    new PointF(r.Left + r.Width * 0.82f, r.Top + r.Height * 0.28f),
                });
            e.Graphics.SmoothingMode = old;
        }
    }

    sealed class HotkeyWindow : NativeWindow, IDisposable
    {
        readonly Action[] actions;
        readonly string[] current;

        public HotkeyWindow(params Action[] actions)
        {
            this.actions = actions;
            current = new string[actions.Length];
            CreateHandle(new CreateParams { Parent = (IntPtr)(-3) });
        }

        public void Apply(params string[] texts)
        {
            for (int id = 0; id < actions.Length; id++)
                if ((texts[id] ?? "") != (current[id] ?? "")) Native.UnregisterHotKey(Handle, id + 1);
            for (int id = 0; id < actions.Length; id++)
            {
                string text = texts[id] ?? "";
                if (text == (current[id] ?? "")) continue;
                current[id] = text;
                uint mods, vk;
                if (Hotkey.TryParse(text, out mods, out vk))
                    Native.RegisterHotKey(Handle, id + 1, mods | Hotkey.ModNoRepeat, vk);
            }
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == Native.WM_HOTKEY)
            {
                int id = (int)m.WParam - 1;
                if (id >= 0 && id < actions.Length) actions[id]();
            }
            base.WndProc(ref m);
        }

        public void Dispose()
        {
            for (int id = 0; id < actions.Length; id++) Native.UnregisterHotKey(Handle, id + 1);
            DestroyHandle();
        }
    }

    sealed class TrayApp : IDisposable
    {
        readonly NotifyIcon tray = new NotifyIcon();
        readonly HotkeyWindow hotkey;
        readonly System.Windows.Forms.Timer watchdog = new System.Windows.Forms.Timer { Interval = 10000 };
        readonly System.Windows.Forms.Timer stateCheck = new System.Windows.Forms.Timer { Interval = 90 };
        readonly System.Windows.Forms.Timer layoutSave = new System.Windows.Forms.Timer { Interval = 2000 };
        WindowTools tools;
        readonly Icon iconOn = LoadIcon("app.ico"), iconOff = LoadIcon("tray-off.ico");
        readonly Engine engine;
        readonly Hooks hooks;
        Config cfg;
        readonly ToolStripMenuItem miSettings, miEnabled, miExclude, miExit;
        readonly ToolStripMenuItem miAwake, miAwake1, miAwake3, miAwakeForever, miAwakeScreen, miAwakeOff;
        readonly System.Windows.Forms.Timer awakeCheck = new System.Windows.Forms.Timer { Interval = 30000 };
        readonly EventWaitHandle reloadEvent, quitEvent;
        readonly RegisteredWaitHandle reloadWait, quitWait;

        public TrayApp()
        {
            cfg = Config.Load(Program.ConfigPath);
            Loc.Apply(cfg);
            Program.Theme = cfg.Theme;
            engine = new Engine(cfg);
            hooks = new Hooks(engine);

            var menu = new ContextMenuStrip();
            miSettings = new ToolStripMenuItem("", null, delegate { Program.OpenSettings(); });
            miSettings.Font = new Font(miSettings.Font, FontStyle.Bold);
            miEnabled = new ToolStripMenuItem("", null, delegate { SetEnabled(!cfg.Enabled); });
            miExclude = new ToolStripMenuItem("", null, delegate { ExcludeCurrent(); });
            miAwake1 = new ToolStripMenuItem("", null, delegate { StartAwake(1); });
            miAwake3 = new ToolStripMenuItem("", null, delegate { StartAwake(3); });
            miAwakeForever = new ToolStripMenuItem("", null, delegate { StartAwake(0); });
            miAwakeScreen = new ToolStripMenuItem("", null, delegate
            {
                cfg.KeepAwakeScreen = !cfg.KeepAwakeScreen;
                SaveAwake();
            });
            miAwakeOff = new ToolStripMenuItem("", null, delegate { StopAwake(); });
            miAwake = new ToolStripMenuItem("");
            miAwake.DropDownItems.AddRange(new ToolStripItem[]
            {
                miAwake1, miAwake3, miAwakeForever, new ToolStripSeparator(), miAwakeScreen, miAwakeOff,
            });
            menu.Items.AddRange(new ToolStripItem[]
            {
                miSettings,
                new ToolStripSeparator(),
                miEnabled, miExclude,
                new ToolStripSeparator(),
                miAwake,
                new ToolStripSeparator(),
                miExit = new ToolStripMenuItem("", null, delegate
                {
                    Program.Signal(Program.CloseSettingsEventName);
                    Application.Exit();
                }),
            });
            menu.Opening += delegate
            {
                menu.Renderer = new TrayMenuRenderer(Program.IsDark());
                miAwake.DropDown.Renderer = menu.Renderer;
                RefreshMenu();
            };

            tray.ContextMenuStrip = menu;
            tray.MouseClick += (s, e) => { if (e.Button == MouseButtons.Left) Program.OpenSettings(); };
            ApplyEnabled();
            tray.Visible = true;

            var ui = SynchronizationContext.Current;
            tools = new WindowTools(ui);
            tools.IndicatorSize = cfg.IndicatorSize;
            hotkey = new HotkeyWindow(TogglePause, delegate { tools.TogglePinForeground(); }, PasteAsPlainText, ToggleAwake);
            awakeCheck.Tick += delegate { ApplyAwake(); };
            ApplyAwake();
            ApplyHotkeys();
            watchdog.Tick += delegate { if (cfg.Enabled) hooks.Watch(); };
            watchdog.Start();

            stateCheck.Tick += delegate
            {
                stateCheck.Stop();
                hooks.CheckIndicator();
            };
            hooks.StateMayChange += delegate
            {
                stateCheck.Stop();
                stateCheck.Start();
            };
            hooks.IndicatorText += text => tools.ShowIndicator(text);
            layoutSave.Tick += delegate
            {
                layoutSave.Stop();
                cfg.Save(Program.ConfigPath);
            };
            hooks.LayoutsChanged += delegate
            {
                layoutSave.Stop();
                layoutSave.Start();
            };
            hooks.MouseButtonDown += (x, y) => tools.OnMouseDown(x, y);
            hooks.RightButtonUp += (x, y) => { if (cfg.PinOnTitleBar) tools.OnRightButtonUp(x, y); };

            reloadEvent = new EventWaitHandle(false, EventResetMode.AutoReset, Program.ReloadEventName);
            quitEvent = new EventWaitHandle(false, EventResetMode.AutoReset, Program.QuitEventName);
            reloadWait = ThreadPool.RegisterWaitForSingleObject(reloadEvent,
                delegate { ui.Post(delegate { Reload(); }, null); }, null, -1, false);
            quitWait = ThreadPool.RegisterWaitForSingleObject(quitEvent,
                delegate { ui.Post(delegate { Application.Exit(); }, null); }, null, -1, true);

            var trim = new System.Windows.Forms.Timer { Interval = 5000 };
            trim.Tick += delegate { trim.Stop(); trim.Dispose(); TrimMemory(); };
            trim.Start();
        }

        void SetEnabled(bool on)
        {
            cfg.Enabled = on;
            cfg.Save(Program.ConfigPath);
            ApplyEnabled();
        }

        void ApplyHotkeys()
        {
            hotkey.Apply(cfg.PauseHotkey, cfg.PinHotkey, cfg.PlainPasteHotkey, cfg.KeepAwakeHotkey);
        }

        void StartAwake(int hours)
        {
            cfg.KeepAwakeHours = hours;
            cfg.StartKeepAwake(DateTime.Now);
            SaveAwake();
        }

        void StopAwake()
        {
            cfg.KeepAwakeUntil = "";
            SaveAwake();
        }

        void SaveAwake()
        {
            cfg.Save(Program.ConfigPath);
            ApplyAwake();
        }

        void ToggleAwake()
        {
            const string Moon = "\uE708";
            if (cfg.KeepAwakeActive(DateTime.Now))
            {
                StopAwake();
                tools.ShowNotice(Moon, Loc.T("Can sleep", "Можно засыпать"));
                return;
            }
            StartAwake(cfg.KeepAwakeHours);
            tools.ShowNotice(Moon, Loc.T("Stay awake: ", "Не засыпать: ") + AwakeLeft());
        }

        string AwakeLeft()
        {
            DateTime until;
            if (!DateTime.TryParse(cfg.KeepAwakeUntil, CultureInfo.InvariantCulture, DateTimeStyles.None, out until))
                return Loc.T("until turned off", "пока не выключу");
            int minutes = Math.Max(1, (int)Math.Ceiling((until - DateTime.Now).TotalMinutes));
            return minutes >= 60
                ? (minutes + 30) / 60 + Loc.T(" h", " ч")
                : minutes + Loc.T(" min", " мин");
        }

        void ApplyAwake()
        {
            bool on = cfg.KeepAwakeActive(DateTime.Now);
            if (!on && cfg.KeepAwakeUntil.Length > 0)
            {
                cfg.KeepAwakeUntil = "";
                cfg.Save(Program.ConfigPath);
            }
            uint flags = Native.ES_CONTINUOUS;
            if (on) flags |= Native.ES_SYSTEM_REQUIRED | (cfg.KeepAwakeScreen ? Native.ES_DISPLAY_REQUIRED : 0);
            Native.SetThreadExecutionState(flags);
            awakeCheck.Enabled = on && cfg.KeepAwakeUntil != "forever";
            UpdateTrayText();
        }

        void UpdateTrayText()
        {
            string state = cfg.Enabled ? Loc.T("BetterWriting: on", "BetterWriting: работает") : Loc.T("BetterWriting: paused", "BetterWriting: на паузе");
            tray.Text = cfg.KeepAwakeActive(DateTime.Now) ? state + Loc.T(", staying awake", ", не засыпает") : state;
        }

        void PasteAsPlainText()
        {
            string text;
            try
            {
                if (!Clipboard.ContainsText()) return;
                text = Clipboard.GetText();
                Clipboard.SetDataObject(text, true, 5, 30);
            }
            catch (System.Runtime.InteropServices.ExternalException) { return; }
            hooks.SendPaste();
            var after = new System.Windows.Forms.Timer { Interval = 120 };
            after.Tick += delegate
            {
                after.Stop();
                after.Dispose();
                if (cfg.Enabled) hooks.AfterExternalEdit();
            };
            after.Start();
        }

        void TogglePause()
        {
            SetEnabled(!cfg.Enabled);
            tray.ShowBalloonTip(1500, "BetterWriting",
                cfg.Enabled ? Loc.T("On", "Работает") : Loc.T("Paused", "На паузе"), ToolTipIcon.None);
        }

        void ApplyEnabled()
        {
            bool on = cfg.Enabled;
            if (on) hooks.Install(); else hooks.Uninstall();
            tray.Icon = on ? iconOn : iconOff;
            UpdateTrayText();
            TrimMemory();
        }

        void ExcludeCurrent()
        {
            string name = hooks.ForegroundProcess;
            if (string.IsNullOrEmpty(name)) return;
            cfg.ExcludeApps.Add(name);
            cfg.Save(Program.ConfigPath);
            Reload();
        }

        void Reload()
        {
            try { cfg = Config.Load(Program.ConfigPath); }
            catch (Exception ex) { MessageBox.Show(Loc.T("Could not read the settings:\n", "Не удалось прочитать настройки:\n") + ex.Message, "BetterWriting"); }
            Loc.Apply(cfg);
            Program.Theme = cfg.Theme;
            hooks.SetConfig(cfg);
            engine.Reset(false);
            tools.IndicatorSize = cfg.IndicatorSize;
            ApplyHotkeys();
            ApplyEnabled();
            ApplyAwake();
        }

        void RefreshMenu()
        {
            miSettings.Text = Loc.T("Settings", "Настройки");
            miEnabled.Text = Loc.T("Enabled", "Включено");
            miExit.Text = Loc.T("Exit", "Выход");
            miEnabled.Checked = cfg.Enabled;
            miEnabled.ShortcutKeyDisplayString = cfg.PauseHotkey;
            string name = hooks.ForegroundProcess;
            miExclude.Visible = !string.IsNullOrEmpty(name) && !cfg.IsExcluded(name);
            miExclude.Text = Loc.T("Don't work in “" + name + "”", "Не работать в «" + name + "»");
            bool awake = cfg.KeepAwakeActive(DateTime.Now);
            miAwake.Text = awake ? Loc.T("Stay awake: ", "Не засыпать: ") + AwakeLeft() : Loc.T("Stay awake", "Не засыпать");
            miAwake.Checked = awake;
            miAwake1.Text = Loc.T("1 hour", "1 час");
            miAwake3.Text = Loc.T("3 hours", "3 часа");
            miAwakeForever.Text = Loc.T("Until I turn it off", "Пока не выключу");
            miAwakeScreen.Text = Loc.T("Keep the screen on", "Не гасить экран");
            miAwakeOff.Text = Loc.T("Turn off", "Выключить");
            miAwake1.Checked = awake && cfg.KeepAwakeHours == 1;
            miAwake3.Checked = awake && cfg.KeepAwakeHours == 3;
            miAwakeForever.Checked = awake && cfg.KeepAwakeHours != 1 && cfg.KeepAwakeHours != 3;
            miAwakeScreen.Checked = cfg.KeepAwakeScreen;
            miAwakeOff.Visible = awake;
        }

        static Icon LoadIcon(string name)
        {
            using (var s = Assembly.GetExecutingAssembly().GetManifestResourceStream("BetterWriting." + name))
                return new Icon(s, SystemInformation.SmallIconSize);
        }

        static void TrimMemory()
        {
            Native.SetProcessWorkingSetSize(Native.GetCurrentProcess(), (IntPtr)(-1), (IntPtr)(-1));
        }

        public void Dispose()
        {
            reloadWait.Unregister(null);
            quitWait.Unregister(null);
            reloadEvent.Dispose();
            quitEvent.Dispose();
            watchdog.Dispose();
            awakeCheck.Dispose();
            Native.SetThreadExecutionState(Native.ES_CONTINUOUS);
            stateCheck.Dispose();
            if (layoutSave.Enabled) cfg.Save(Program.ConfigPath);
            layoutSave.Dispose();
            hotkey.Dispose();
            hooks.Uninstall();
            tray.Visible = false;
            tray.Dispose();
        }
    }
}
