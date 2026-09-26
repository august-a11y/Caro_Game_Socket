using System.Runtime.InteropServices;
using CaroGame.Server;
using var shutdown = new CancellationTokenSource();

Console.CancelKeyPress += (_, eventArgs) =>
{
    eventArgs.Cancel = true;
    shutdown.Cancel();
};
using var terminationSignal = OperatingSystem.IsLinux()
    ? PosixSignalRegistration.Create(PosixSignal.SIGTERM, context =>
    {
        context.Cancel = true;
        shutdown.Cancel();
    })
    : null;

await ServerHost.RunAsync(shutdown.Token);
