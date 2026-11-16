using System.Runtime.CompilerServices;
using PokeTokenBar.Application;

namespace PokeTokenBar.Ui;

public static class Program
{
    [STAThread]
    public static int Main(string[] args)
    {
        if (args is [UsageScanProcess.Argument, ..]) return UsageScanProcess.RunChild();
        return RunApp();
    }

    // Main 이 App 을 직접 참조하면 스캔 프로세스도 시작할 때 WPF 를 불러옴
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int RunApp()
    {
        var app = new App();
        app.InitializeComponent();
        return app.Run();
    }
}
