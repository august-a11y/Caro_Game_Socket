using Caro.Client.WinForms;
using Caro.Client.WinForms.Core;

namespace WinFormsApp1
{
    internal static class Program
    {
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            var logger = ClientLogger.Shared;
            logger.Info("Client application starting.");
            Application.ThreadException += (_, args) =>
                logger.Error("Unhandled UI exception.", args.Exception);
            AppDomain.CurrentDomain.UnhandledException += (_, args) =>
                logger.Error("Unhandled application exception.", args.ExceptionObject as Exception);
            TaskScheduler.UnobservedTaskException += (_, args) =>
            {
                logger.Error("Unobserved task exception.", args.Exception);
                args.SetObserved();
            };
            // To customize application configuration such as set high DPI settings or default font,
            // see https://aka.ms/applicationconfiguration.
            ApplicationConfiguration.Initialize();
            var main = new FMain();
            var session = new Caro.Client.WinForms.Features.Session.SessionPresenter(main);
            main.Shown += async (_, _) =>
            {
                // Keep the main window hidden until a session is authenticated.
                main.Hide();
                while (!main.IsDisposed && !session.LastConnectionCancelled &&
                    !await session.ConnectAtStartupAsync())
                {
                    // ConnectionDialog is shown again after a failed attempt.
                }

                if (!main.IsDisposed && !session.LastConnectionCancelled)
                    main.Show();
                else if (!main.IsDisposed)
                    main.Close();
            };
            try { Application.Run(main); }
            finally
            {
                logger.Info("Client application stopped.");
                using var flushTimeout = new CancellationTokenSource(TimeSpan.FromSeconds(2));
                try { logger.FlushAsync(flushTimeout.Token).GetAwaiter().GetResult(); }
                catch (OperationCanceledException) { }
            }
        }
    }
}
