using System;
using System.Diagnostics;
using System.Drawing;
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
        public static readonly string ConfigPath = Path.Combine(Path.GetDirectoryName(ExePath), "BetterWriting.ini");

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

        public static void SetAutostart(bool on)
        {
            using (var key = Registry.CurrentUser.CreateSubKey(RunKey))
            {
                if (on) key.SetValue(AppName, "\"" + ExePath + "\" --background");
                else if (key.GetValue(AppName) != null) key.DeleteValue(AppName);
            }
        }
    }

    sealed class TrayApp : IDisposable
    {
        readonly NotifyIcon tray = new NotifyIcon();
        readonly Icon iconOn = LoadIcon("app.ico"), iconOff = LoadIcon("tray-off.ico");
        readonly Engine engine;
        readonly Hooks hooks;
        Config cfg;
        readonly ToolStripMenuItem miSettings, miEnabled, miExclude, miExit;
        readonly EventWaitHandle reloadEvent, quitEvent;
        readonly RegisteredWaitHandle reloadWait, quitWait;

        public TrayApp()
        {
            cfg = Config.Load(Program.ConfigPath);
            Loc.Apply(cfg);
            engine = new Engine(cfg);
            hooks = new Hooks(engine);

            var menu = new ContextMenuStrip();
            miSettings = new ToolStripMenuItem("", null, delegate { Program.OpenSettings(); });
            miSettings.Font = new Font(miSettings.Font, FontStyle.Bold);
            miEnabled = new ToolStripMenuItem("", null, delegate { SetEnabled(!cfg.Enabled); });
            miExclude = new ToolStripMenuItem("", null, delegate { ExcludeCurrent(); });
            menu.Items.AddRange(new ToolStripItem[]
            {
                miSettings,
                new ToolStripSeparator(),
                miEnabled, miExclude,
                new ToolStripSeparator(),
                miExit = new ToolStripMenuItem("", null, delegate
                {
                    Program.Signal(Program.CloseSettingsEventName);
                    Application.Exit();
                }),
            });
            menu.Opening += delegate { RefreshMenu(); };

            tray.ContextMenuStrip = menu;
            tray.MouseClick += (s, e) => { if (e.Button == MouseButtons.Left) Program.OpenSettings(); };
            ApplyEnabled();
            tray.Visible = true;

            var ui = SynchronizationContext.Current;
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

        void ApplyEnabled()
        {
            bool on = cfg.Enabled;
            if (on) hooks.Install(); else hooks.Uninstall();
            tray.Icon = on ? iconOn : iconOff;
            tray.Text = on ? Loc.T("BetterWriting: on", "BetterWriting: работает") : Loc.T("BetterWriting: paused", "BetterWriting: на паузе");
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
            hooks.SetConfig(cfg);
            engine.Reset(false);
            ApplyEnabled();
        }

        void RefreshMenu()
        {
            miSettings.Text = Loc.T("Settings", "Настройки");
            miEnabled.Text = Loc.T("Enabled", "Включено");
            miExit.Text = Loc.T("Exit", "Выход");
            miEnabled.Checked = cfg.Enabled;
            string name = hooks.ForegroundProcess;
            miExclude.Visible = !string.IsNullOrEmpty(name) && !cfg.IsExcluded(name);
            miExclude.Text = Loc.T("Don't work in “" + name + "”", "Не работать в «" + name + "»");
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
            hooks.Uninstall();
            tray.Visible = false;
            tray.Dispose();
        }
    }
}
