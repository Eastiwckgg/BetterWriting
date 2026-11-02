@echo off
setlocal
cd /d "%~dp0"
set "FW=%WINDIR%\Microsoft.NET\Framework64\v4.0.30319"
if not exist "%FW%\csc.exe" set "FW=%WINDIR%\Microsoft.NET\Framework\v4.0.30319"
set "WPF=%FW%\WPF"
"%FW%\csc.exe" /nologo /target:winexe /optimize+ /codepage:65001 /out:BetterWriting.exe ^
  /win32icon:src\app.ico /win32manifest:src\app.manifest ^
  /r:System.Windows.Forms.dll /r:System.Drawing.dll /r:System.Xaml.dll ^
  /r:"%WPF%\PresentationFramework.dll" /r:"%WPF%\PresentationCore.dll" /r:"%WPF%\WindowsBase.dll" /r:"%WPF%\UIAutomationClient.dll" /r:"%WPF%\UIAutomationTypes.dll" ^
  /resource:src\Settings.xaml,BetterWriting.Settings.xaml ^
  /resource:src\app.ico,BetterWriting.app.ico ^
  /resource:src\tray-off.ico,BetterWriting.tray-off.ico ^
  /resource:src\logo.png,BetterWriting.logo.png ^
  /resource:src\dict\ru.txt.gz,BetterWriting.dict.ru.txt.gz /resource:src\dict\en.txt.gz,BetterWriting.dict.en.txt.gz ^
  /resource:src\hints\hint-brackets.gif,BetterWriting.hint-brackets.gif /resource:src\hints\hint-caps-lock.gif,BetterWriting.hint-caps-lock.gif /resource:src\hints\hint-double-space.gif,BetterWriting.hint-double-space.gif /resource:src\hints\hint-layout.gif,BetterWriting.hint-layout.gif /resource:src\hints\hint-punctuation.gif,BetterWriting.hint-punctuation.gif /resource:src\hints\hint-shortcuts.gif,BetterWriting.hint-shortcuts.gif ^
  src\*.cs || exit /b 1
echo Done: BetterWriting.exe
