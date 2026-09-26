namespace Caro.Server.WinForms;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();
        Directory.SetCurrentDirectory(AppContext.BaseDirectory);
        Application.Run(new ServerForm());
    }
}
