using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text.Json;

namespace StudentChat;

public class ChatServer
{
    private readonly int _port;
    private readonly List<ClientHandler> _clients = new();
    private readonly List<ChatMessage> _history = new();
    private readonly object _clientsLock = new();
    private readonly string _historyPath;
    private TcpListener? _listener;
    private volatile bool _running;

    public ChatServer(int port)
    {
        _port = port;

        string folder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "StudentChat");
        _historyPath = Path.Combine(folder, $"history_{port}.jsonl");
        LoadHistory();
    }

    public void Start()
    {
        if (_running)
            return;

        _listener = new TcpListener(IPAddress.Any, _port);
        _listener.Start();
        _running = true;

        new Thread(AcceptLoop) { IsBackground = true }.Start();
    }

    private void AcceptLoop()
    {
        while (_running)
        {
            try
            {
                TcpClient socket = _listener!.AcceptTcpClient();
                if (!_running)
                {
                    socket.Close();
                    break;
                }

                var handler = new ClientHandler(this, socket);

                new Thread(handler.Run) { IsBackground = true }.Start();
            }
            catch (SocketException) when (!_running)
            {
                break;
            }
            catch (ObjectDisposedException) when (!_running)
            {
                break;
            }
            catch (SocketException)
            {
            }
        }
    }

    public void Stop()
    {
        if (!_running)
            return;

        _running = false;
        _listener?.Stop();

        List<ClientHandler> connected;
        lock (_clientsLock)
        {
            connected = _clients.ToList();
        }
        foreach (ClientHandler client in connected)
            client.Close();
    }

    public bool TryRegister(ClientHandler client, string nick)
    {
        lock (_clientsLock)
        {
            if (!_running || _clients.Any(other =>
                string.Equals(other.Nick, nick, StringComparison.OrdinalIgnoreCase)))
                return false;

            if (!client.Send(new ChatMessage("login_ok", "Сервер", "Вход выполнен", DateTime.UtcNow)))
                return false;

            foreach (ChatMessage oldMessage in _history)
            {
                if (!client.Send(oldMessage with { Type = "history" }))
                    return false;
            }

            _clients.Add(client);
            return true;
        }
    }

    public void Unregister(ClientHandler client)
    {
        bool wasConnected;
        lock (_clientsLock)
        {
            wasConnected = _clients.Remove(client);
        }

        if (wasConnected && _running)
        {
            Broadcast(new ChatMessage("system", "Сервер", $"{client.Nick} вышел из чата", DateTime.UtcNow));
            BroadcastUsers();
        }
    }

    public void SendPublic(ClientHandler sender, string text)
    {
        var message = new ChatMessage("text", sender.Nick, text, DateTime.UtcNow);
        lock (_clientsLock)
        {
            _history.Add(message);
            if (_history.Count > 20)
                _history.RemoveAt(0);

            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(_historyPath)!);
                File.AppendAllText(_historyPath, message.ToJson() + Environment.NewLine);
            }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }

            foreach (ClientHandler client in _clients)
            {
                if (!client.Send(message))
                    client.Close();
            }
        }
    }

    public void SendPrivate(ClientHandler sender, string to, string text)
    {
        lock (_clientsLock)
        {
            ClientHandler? receiver = _clients.FirstOrDefault(client =>
                string.Equals(client.Nick, to, StringComparison.OrdinalIgnoreCase));

            if (receiver == null)
            {
                sender.Send(new ChatMessage("error", "Сервер", "Получатель не в сети", DateTime.UtcNow));
                return;
            }

            var message = new ChatMessage("private", sender.Nick, text, DateTime.UtcNow, receiver.Nick);

            if (!sender.Send(message)) sender.Close();
            if (receiver != sender && !receiver.Send(message)) receiver.Close();
        }
    }

    public void Broadcast(ChatMessage message)
    {
        lock (_clientsLock)
        {
            foreach (ClientHandler client in _clients)
            {
                if (!client.Send(message))
                    client.Close();
            }
        }
    }

    public void BroadcastUsers()
    {
        lock (_clientsLock)
        {
            List<string> nicks = _clients
                .Select(client => client.Nick)
                .OrderBy(nick => nick, StringComparer.OrdinalIgnoreCase)
                .ToList();

            var message = new ChatMessage("users", "Сервер", "", DateTime.UtcNow, Users: nicks);
            foreach (ClientHandler client in _clients)
            {
                if (!client.Send(message))
                    client.Close();
            }
        }
    }

    private void LoadHistory()
    {
        if (!File.Exists(_historyPath))
            return;

        try
        {
            foreach (string line in File.ReadLines(_historyPath))
            {
                try
                {
                    ChatMessage? message = ChatMessage.FromJson(line);
                    if (message?.Type != "text") continue;
                    _history.Add(message);
                    if (_history.Count > 20) _history.RemoveAt(0);
                }
                catch (JsonException) {  }
            }
        }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }
    }
}
