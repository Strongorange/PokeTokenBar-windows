using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;

namespace PokeTokenBar.Ui;

/// <summary>
/// ScrollViewer 에 가로 휠 스크롤을 붙인다. WPF 은 기본적으로 세로 휠만 먹고,
/// Shift+휠과 MX Master 계열의 틸트 휠(WM_MOUSEHORIZONTALWHEEL, 0x020E)은
/// 무시한다 — 잔디 그리드처럼 가로로만 넘치는 뷰어에 필수.
/// </summary>
internal static class HorizontalScrollSupport
{
    private const int WmMouseHorizontalWheel = 0x020E;
    private const double WheelNotchPixels = 48;

    public static void Attach(ScrollViewer viewer)
    {
        viewer.PreviewMouseWheel += (_, e) =>
        {
            if ((Keyboard.Modifiers & ModifierKeys.Shift) == 0) return;
            viewer.ScrollToHorizontalOffset(viewer.HorizontalOffset - e.Delta / 120.0 * WheelNotchPixels);
            e.Handled = true;
        };

        if (PresentationSource.FromVisual(viewer) is HwndSource source)
            source.AddHook(CreateHook(viewer));
        else
            viewer.Loaded += (_, _) =>
            {
                if (PresentationSource.FromVisual(viewer) is HwndSource late)
                    late.AddHook(CreateHook(viewer));
            };
    }

    private static HwndSourceHook CreateHook(ScrollViewer viewer) => (hwnd, msg, wParam, lParam, ref handled) =>
    {
        if (msg != WmMouseHorizontalWheel) return IntPtr.Zero;
        var delta = (short)((((long)wParam) >> 16) & 0xFFFF);
        // lParam 은 화면 좌표(LOWORD x, HIWORD y, 부호 있음) — 커서가 뷰어 위에
        // 있을 때만 먹인다. 창 전역으로 걸면 다른 카드 위에서도 잔디가 굴러간다.
        var x = (short)(((long)lParam) & 0xFFFF);
        var y = (short)((((long)lParam) >> 16) & 0xFFFF);
        var local = viewer.PointFromScreen(new Point(x, y));
        if (local.X < 0 || local.Y < 0 ||
            local.X > viewer.RenderSize.Width || local.Y > viewer.RenderSize.Height)
            return IntPtr.Zero;
        viewer.ScrollToHorizontalOffset(viewer.HorizontalOffset + delta / 120.0 * WheelNotchPixels);
        handled = true;
        return IntPtr.Zero;
    };
}
