using Kandra.Desktop.Photino;
using PersonalAccounting.Persistence.Contexts;

namespace PersonalAccounting.Desktop;

/// <summary>
/// The configuration as a desktop app: the web host's registrations (<see cref="PersonalAccountingHost"/>) on loopback
/// Kestrel, shown in a Photino window. Data (SQLite file, blobs, signing key, backups) lives in
/// <c>%LOCALAPPDATA%\PersonalAccounting</c> unless <c>Kandra:DataPath</c> says otherwise. Windows first.
/// </summary>
public static class Program
{
    private static readonly KandraDesktopOptions Options = new()
    {
        AppName = "PersonalAccounting",
        Title = "Personal Accounting",
        IconFile = "personalaccounting.ico",
        Commands =
        {
            KandraDesktopCommands.OpenDataFolder(),
            KandraDesktopCommands.BackUpDatabase<PersonalAccountingDbContext>(),
            KandraDesktopCommands.ResetToFirstRun(),
            KandraDesktopCommand.Separator,
            // License agreement and third-party notices are in the client's main menu (every host), not here.
            KandraDesktopCommands.About(),
        },
    };

    [STAThread] // Photino runs the window's message loop on this thread; Windows needs it STA (so no async Main)
    public static int Main(string[] args)
    {
        KandraDesktopHost? host = null;
        try
        {
            host = KandraDesktopHost.Create(args, Options);
            if (host is null)
                return 0; // already running on this data folder; that window was brought to the front

            host.Builder.AddPersonalAccountingHost();
            var app = host.Builder.Build();
            app.UsePersonalAccountingHostAsync().GetAwaiter().GetResult();
            host.Run(app);
            return 0;
        }
        catch (Exception ex)
        {
            KandraDesktopHost.ReportFatal(Options, ex, host?.DataPaths);
            return 1;
        }
        finally
        {
            host?.Dispose();
        }
    }
}
