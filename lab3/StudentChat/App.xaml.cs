using System.Windows;

namespace StudentChat;

public partial class App : Application
{

    public ChatServer? LocalServer { get; private set; }

    public App()
    {
        ShutdownMode = ShutdownMode.OnMainWindowClose;
    }

    public void StartLocalServer(int port)
    {
        if (LocalServer != null)
            throw new InvalidOperationException("Сервер уже запущен в этом окне.");

        var server = new ChatServer(port);
        server.Start();
        LocalServer = server;
    }

    protected override void OnExit(ExitEventArgs e)
    {
        LocalServer?.Stop();
        base.OnExit(e);
    }
}
