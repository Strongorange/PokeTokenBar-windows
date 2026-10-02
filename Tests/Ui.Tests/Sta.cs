namespace PokeTokenBar.Ui.Tests;

/// <summary>
/// WPF controls have thread affinity and require STA. xUnit runs on the MTA
/// thread pool, so layout tests pump their arrangement through a dedicated
/// STA thread and rethrow anything it captures.
/// </summary>
internal static class Sta
{
    public static void Run(Action action)
    {
        Exception? captured = null;
        var thread = new Thread(() =>
        {
            try
            {
                action();
            }
            catch (Exception e)
            {
                captured = e;
            }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();
        if (captured is not null)
            throw captured;
    }
}
