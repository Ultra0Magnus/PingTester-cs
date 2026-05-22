using Avalonia;
using Avalonia.Win32;
using System;

namespace PingTester;

sealed class Program
{
    [STAThread]
    public static void Main(string[] args) => BuildAvaloniaApp()
        .StartWithClassicDesktopLifetime(args);

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            // Fallback rendu logiciel si ANGLE/WGL indisponible (évite la fenêtre transparente)
            .With(new Win32PlatformOptions
            {
                RenderingMode = [Win32RenderingMode.AngleEgl, Win32RenderingMode.Wgl, Win32RenderingMode.Software]
            })
            .WithInterFont()
            .LogToTrace();
}
