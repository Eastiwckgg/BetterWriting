using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

namespace BetterWriting
{
    sealed class Hooks : Native
    {
        static readonly IntPtr Magic = (IntPtr)0x42575254;

        const int VK_BACK = 0x08, VK_TAB = 0x09, VK_RETURN = 0x0D, VK_SHIFT = 0x10, VK_CONTROL = 0x11,
                  VK_MENU = 0x12, VK_CAPITAL = 0x14, VK_SPACE = 0x20, VK_LWIN = 0x5B, VK_RWIN = 0x5C;

        readonly Engine engine;
        readonly HookProc kbProc, mouseProc;
        readonly WinEventProc foregroundProc;
        readonly byte[] keyState = new byte[256];
        readonly char[] chars = new char[8];
        readonly INPUT[] inputs = new INPUT[256];
        readonly StringBuilder sb = new StringBuilder(1024);
        readonly uint selfPid = (uint)System.Diagnostics.Process.GetCurrentProcess().Id;

        IntPtr kbHook, mouseHook, fgHook;
        IntPtr lastFg;
        uint fgThread;
        bool fgExcluded, fgIsDialog, spaceHeld, afterDeadKey;
        ContextReader reader;
        Config baseCfg;
        bool fgIsSettings;
        EventWaitHandle demoFocus;

        public string ForegroundProcess { get; private set; }

        public Hooks(Engine engine)
        {
            this.engine = engine;
            baseCfg = engine.Cfg;
            kbProc = KeyboardProc;
            mouseProc = MouseProc;
            foregroundProc = OnForegroundEvent;
        }

        public bool Installed { get { return kbHook != IntPtr.Zero; } }

        public void Install()
        {
            if (Installed) return;
            IntPtr mod = GetModuleHandle(null);
            kbHook = SetWindowsHookEx(WH_KEYBOARD_LL, kbProc, mod, 0);
            mouseHook = SetWindowsHookEx(WH_MOUSE_LL, mouseProc, mod, 0);
            fgHook = SetWinEventHook(EVENT_SYSTEM_FOREGROUND, EVENT_SYSTEM_FOREGROUND, IntPtr.Zero, foregroundProc, 0, 0, 0);
            lastFg = IntPtr.Zero;
            if (baseCfg.LayoutFix) Dictionaries.Preload();
        }

        public void SetConfig(Config cfg)
        {
            baseCfg = cfg;
            engine.Cfg = cfg;
            lastFg = IntPtr.Zero;
            if (cfg.LayoutFix && Installed) Dictionaries.Preload();
        }

        public void Uninstall()
        {
            if (kbHook != IntPtr.Zero) UnhookWindowsHookEx(kbHook);
            if (mouseHook != IntPtr.Zero) UnhookWindowsHookEx(mouseHook);
            if (fgHook != IntPtr.Zero) UnhookWinEvent(fgHook);
            kbHook = mouseHook = fgHook = IntPtr.Zero;
        }

        public void Invalidate() { lastFg = IntPtr.Zero; }

        bool ActiveHere(IntPtr fg)
        {
            if (fgIsSettings && (demoFocus == null || !demoFocus.WaitOne(0))) return false;
            return !fgExcluded && !(engine.Cfg.SkipFullscreen && IsFullscreen(fg));
        }

        bool ReadsAfterCursorMove { get { return engine.Cfg.Capitalize && engine.Cfg.CapitalizeAfterClick; } }

        void RequestContext(bool allowStartCap)
        {
            if (reader == null)
                reader = new ContextReader(SynchronizationContext.Current ?? new SynchronizationContext(), OnContext);
            reader.Request(engine.Version, allowStartCap);
        }

        void OnContext(int version, bool allowStartCap, CaretContext ctx)
        {
            if (version != engine.Version) return;
            if (ctx.Known || ctx.Password) engine.SetContext(ctx.Before, ctx.AtStart, ctx.Password, allowStartCap);
        }

        IntPtr MouseProc(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0)
            {
                int msg = (int)wParam;
                if (msg != WM_MOUSEMOVE)
                {
                    try { HandleMouse(msg); }
                    catch { }
                }
            }
            return CallNextHookEx(IntPtr.Zero, nCode, wParam, lParam);
        }

        void HandleMouse(int msg)
        {
            if (msg == WM_LBUTTONDOWN || msg == WM_RBUTTONDOWN || msg == WM_MBUTTONDOWN)
            {
                engine.Reset(engine.Cfg.CapitalizeAfterClick);
            }
            else if (msg == WM_LBUTTONUP)
            {
                IntPtr fg = GetForegroundWindow();
                if (fg != lastFg) OnForegroundChanged(fg, engine.Cfg.CapitalizeAfterClick);
                else if (ReadsAfterCursorMove && ActiveHere(fg)) RequestContext(true);
            }
        }

        void OnForegroundEvent(IntPtr hook, uint ev, IntPtr hwnd, int idObject, int idChild, uint thread, uint time)
        {
            try
            {
                IntPtr fg = GetForegroundWindow();
                if (fg != lastFg) OnForegroundChanged(fg, engine.Cfg.CapitalizeOnWindowSwitch);
            }
            catch { }
        }

        void OnForegroundChanged(IntPtr fg, bool allowStartCap)
        {
            lastFg = fg;
            uint pid;
            fgThread = GetWindowThreadProcessId(fg, out pid);
            string name = ProcessName(pid);
            if (pid != selfPid) ForegroundProcess = name;
            fgExcluded = pid == selfPid || (name != null && baseCfg.IsExcluded(name));
            engine.Cfg = baseCfg.ForApp(name);
            engine.BothLayouts = HasLayout(Lang.Ru) && HasLayout(Lang.En);
            fgIsSettings = !fgExcluded && string.Equals(name, "BetterWriting", StringComparison.OrdinalIgnoreCase);
            if (fgIsSettings && demoFocus == null)
            {
                EventWaitHandle h;
                if (EventWaitHandle.TryOpenExisting(Program.DemoEventName, out h)) demoFocus = h;
            }

            sb.Clear();
            GetClassName(fg, sb, 256);
            fgIsDialog = sb.ToString() == "#32770";
            bool cap = allowStartCap && !fgIsDialog;
            engine.Reset(cap);
            if (cap && engine.Cfg.Capitalize && ActiveHere(fg)) RequestContext(true);
        }

        IntPtr KeyboardProc(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0)
            {
                try
                {
                    if (HandleKey((int)wParam, lParam)) return (IntPtr)1;
                }
                catch { }
            }
            return CallNextHookEx(IntPtr.Zero, nCode, wParam, lParam);
        }

        bool HandleKey(int msg, IntPtr lParam)
        {
            int vk = Marshal.ReadInt32(lParam, 0);
            int scan = Marshal.ReadInt32(lParam, 4);
            int flags = Marshal.ReadInt32(lParam, 8);
            IntPtr extra = Marshal.ReadIntPtr(lParam, 16);

            if ((flags & LLKHF_INJECTED) != 0 && (extra == Magic || engine.Cfg.IgnoreInjected)) return false;

            bool down = msg == WM_KEYDOWN || msg == WM_SYSKEYDOWN;
            if (vk == VK_SPACE && !down) { spaceHeld = false; return false; }
            if (!down) return false;

            IntPtr fg = GetForegroundWindow();
            if (fg != lastFg) OnForegroundChanged(fg, engine.Cfg.CapitalizeOnWindowSwitch);

            if (!ActiveHere(fg))
            {
                engine.Reset(false);
                return false;
            }

            if (IsIgnoredKey(vk)) return false;

            IntPtr hkl = GetKeyboardLayout(fgThread);
            bool capsLock = (GetKeyState(VK_CAPITAL) & 1) != 0;
            engine.Lang = LangOf(hkl);
            engine.CapsLock = capsLock;

            bool ctrl = Pressed(VK_CONTROL), alt = Pressed(VK_MENU), shift = Pressed(VK_SHIFT);
            bool win = Pressed(VK_LWIN) || Pressed(VK_RWIN);
            bool altGr = ctrl && alt;

            if (vk == VK_RETURN)
            {
                var fix = engine.OnEnter();
                engine.Reset(engine.Cfg.CapitalizeAfterEnter);
                if (!fix.Replace) return false;
                Send(fix, VK_RETURN);
                return true;
            }
            if (win)
            {
                if (vk != VK_SPACE) engine.Reset(false);
                return false;
            }
            if ((ctrl || alt) && !altGr)
            {
                engine.Reset(false);
                if (ctrl && ReadsAfterCursorMove) RequestContext(true);
                return false;
            }
            if (vk == VK_BACK)
            {
                bool unseen = engine.BufferEmpty;
                var undo = engine.OnBackspace();
                if (undo.Replace)
                {
                    Send(undo, 0);
                    return true;
                }
                if (unseen && ReadsAfterCursorMove) RequestContext(true);
                return false;
            }
            if (vk == VK_TAB || (vk >= 0x21 && vk <= 0x28))
            {
                engine.Reset(false);
                if (ReadsAfterCursorMove) RequestContext(true);
                return false;
            }

            bool repeat = false;
            if (vk == VK_SPACE) { repeat = spaceHeld; spaceHeld = true; }

            Array.Clear(keyState, 0, 256);
            if (shift) keyState[VK_SHIFT] = 0x80;
            if (ctrl) keyState[VK_CONTROL] = 0x80;
            if (alt) keyState[VK_MENU] = 0x80;
            if (capsLock) keyState[VK_CAPITAL] = 1;

            int n = ToUnicodeEx((uint)vk, (uint)scan, keyState, chars, chars.Length, 4, hkl);
            if (n < 0) { afterDeadKey = true; return false; }
            bool wasDead = afterDeadKey;
            afterDeadKey = false;
            if (n == 0 || chars[0] < ' ') return false;
            if (n > 1)
            {
                for (int i = 0; i < n; i++) engine.OnChar(chars[i], 0, true, false);
                return false;
            }

            char c = chars[0];
            bool capAllowed = char.IsLetter(c) && char.IsLower(c) && !wasDead && !IsPasswordFocus();

            var act = engine.OnChar(c, (long)GetTickCount64(), repeat, capAllowed);
            if (!act.Replace) return false;
            Send(act, 0);
            return true;
        }

        static Lang LangOf(IntPtr hkl)
        {
            int primary = (int)((long)hkl & 0x3FF);
            if (primary == 0x19 || primary == 0x22 || primary == 0x23) return Lang.Ru;
            if (primary == 0x09) return Lang.En;
            return Lang.Other;
        }

        void SwitchLayout(Lang lang)
        {
            IntPtr hkl = FindLayout(lang);
            if (hkl != IntPtr.Zero) PostMessage(lastFg, WM_INPUTLANGCHANGEREQUEST, IntPtr.Zero, hkl);
        }

        static bool HasLayout(Lang lang) { return FindLayout(lang) != IntPtr.Zero; }

        static IntPtr FindLayout(Lang lang)
        {
            var list = new IntPtr[32];
            int count = GetKeyboardLayoutList(list.Length, list);
            for (int i = 0; i < count; i++)
                if (LangOf(list[i]) == lang) return list[i];
            return IntPtr.Zero;
        }

        string ProcessName(uint pid)
        {
            IntPtr h = OpenProcess(PROCESS_QUERY_LIMITED_INFORMATION, false, pid);
            if (h == IntPtr.Zero) return null;
            try
            {
                sb.Clear();
                int size = sb.Capacity;
                if (!QueryFullProcessImageName(h, 0, sb, ref size)) return null;
                return Path.GetFileNameWithoutExtension(sb.ToString());
            }
            finally { CloseHandle(h); }
        }

        static bool IsFullscreen(IntPtr fg)
        {
            if (fg == IntPtr.Zero) return false;
            if ((GetWindowLong(fg, GWL_STYLE) & WS_CAPTION) == WS_CAPTION) return false;
            RECT r;
            if (!GetWindowRect(fg, out r)) return false;
            var mi = new MONITORINFO { cbSize = Marshal.SizeOf(typeof(MONITORINFO)) };
            if (!GetMonitorInfo(MonitorFromWindow(fg, MONITOR_DEFAULTTONEAREST), ref mi)) return false;
            var m = mi.rcMonitor;
            if (r.Left > m.Left || r.Top > m.Top || r.Right < m.Right || r.Bottom < m.Bottom) return false;
            var csb = new StringBuilder(32);
            GetClassName(fg, csb, 32);
            string cls = csb.ToString();
            return cls != "Progman" && cls != "WorkerW";
        }

        bool IsPasswordFocus()
        {
            var gti = new GUITHREADINFO { cbSize = Marshal.SizeOf(typeof(GUITHREADINFO)) };
            if (!GetGUIThreadInfo(fgThread, ref gti) || gti.hwndFocus == IntPtr.Zero) return false;
            if ((GetWindowLong(gti.hwndFocus, GWL_STYLE) & ES_PASSWORD) == 0) return false;
            var csb = new StringBuilder(64);
            GetClassName(gti.hwndFocus, csb, 64);
            return csb.ToString().IndexOf("edit", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        void Send(EngineAction act, int vkAfter)
        {
            int k = 0;
            for (int i = 0; i < act.Backspaces && k < inputs.Length - 8; i++)
            {
                inputs[k++] = Key(VK_BACK, '\0', 0);
                inputs[k++] = Key(VK_BACK, '\0', KEYEVENTF_KEYUP);
            }
            foreach (char ch in act.Text)
            {
                if (k >= inputs.Length - 8) break;
                if (ch == '\n')
                {
                    inputs[k++] = Key(VK_SHIFT, '\0', 0);
                    inputs[k++] = Key(VK_RETURN, '\0', 0);
                    inputs[k++] = Key(VK_RETURN, '\0', KEYEVENTF_KEYUP);
                    inputs[k++] = Key(VK_SHIFT, '\0', KEYEVENTF_KEYUP);
                    continue;
                }
                inputs[k++] = Key(0, ch, KEYEVENTF_UNICODE);
                inputs[k++] = Key(0, ch, KEYEVENTF_UNICODE | KEYEVENTF_KEYUP);
            }
            if (act.ToggleCapsLock)
            {
                inputs[k++] = Key(VK_CAPITAL, '\0', 0);
                inputs[k++] = Key(VK_CAPITAL, '\0', KEYEVENTF_KEYUP);
            }
            if (vkAfter != 0)
            {
                inputs[k++] = Key(vkAfter, '\0', 0);
                inputs[k++] = Key(vkAfter, '\0', KEYEVENTF_KEYUP);
            }
            SendInput((uint)k, inputs, Marshal.SizeOf(typeof(INPUT)));
            if (act.SwitchLayout != Lang.Other) SwitchLayout(act.SwitchLayout);
        }

        static INPUT Key(int vk, char ch, uint flags)
        {
            var input = new INPUT { type = INPUT_KEYBOARD };
            input.u.ki = new KEYBDINPUT { wVk = (ushort)vk, wScan = ch, dwFlags = flags, dwExtraInfo = Magic };
            return input;
        }

        static bool Pressed(int vk) { return (GetAsyncKeyState(vk) & 0x8000) != 0; }

        static bool IsIgnoredKey(int vk)
        {
            return (vk >= 0x10 && vk <= 0x14)
                || (vk >= 0x15 && vk <= 0x1F)
                || vk == 0x2C || vk == 0x2D || vk == 0x2E
                || vk == VK_LWIN || vk == VK_RWIN || vk == 0x5D || vk == 0x5F
                || (vk >= 0x70 && vk <= 0x87)
                || vk == 0x90 || vk == 0x91
                || (vk >= 0xA0 && vk <= 0xB7);
        }
    }
}
