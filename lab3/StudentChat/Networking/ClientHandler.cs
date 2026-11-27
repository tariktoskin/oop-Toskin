using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;

namespace StudentChat;

public class ClientHandler
{
    private readonly ChatServer _server;
    private readonly TcpClient _tcpClient;
    private readonly object _sendLock = new();
    private StreamWriter? _writer;

    public string Nick { get; private set; } = "";

    public ClientHandler(ChatServer server, TcpClient tcpClient)
    {
        _server = server;
        _tcpClient = tcpClient;
        _tcpClient.SendTimeout = 3000;
    }

    public void Run()
    {
        try
        {
            using (_tcpClient)
            using (NetworkStream stream = _tcpClient.GetStream())
            using (var reader = new StreamReader(stream, new UTF8Encoding(false)))
            using (var writer = new StreamWriter(stream, new UTF8Encoding(false)) { AutoFlush = true })
            {
                _writer = writer;

                string? firstLine = reader.ReadLine();
                if (firstLine == null)
                    return;

                ChatMessage? login;
                try
                {
                    login = ChatMessage.FromJson(firstLine);
                }
                catch (JsonException)
                {
                    SendError("Неверный JSON при входе", "login_error");
                    return;
                }

                if (login?.Type != "login" || string.IsNullOrWhiteSpace(login.Nick))
                {
                    SendError("Введите ник", "login_error");
                    return;
                }

                string nick = login.Nick.Trim();
                if (nick.Length > 20 || nick.Any(char.IsControl))
                {
                    SendError("Ник должен содержать от 1 до 20 обычных символов", "login_error");
                    return;
                }

                Nick = nick;
                if (!_server.TryRegister(this, nick))
                {
                    SendError("Этот ник уже занят", "login_error");
                    return;
                }

                _server.Broadcast(new ChatMessage("system", "Сервер", $"{Nick} вошёл в чат", DateTime.UtcNow));
                _server.BroadcastUsers();

                string? line;
                while ((line = reader.ReadLine()) != null)
                {
                    try
                    {
                        ChatMessage? message = ChatMessage.FromJson(line);
                        if (message != null)
                            HandleMessage(message);
                    }
                    catch (JsonException)
                    {
                        SendError("Сообщение имеет неверный формат JSON");
                    }
                }
            }
        }
        catch (IOException)
        {

        }
        catch (SocketException)
        {

        }
        catch (ObjectDisposedException)
        {

        }
        finally
        {
            _server.Unregister(this);
            Close();
        }
    }

    private void HandleMessage(ChatMessage message)
    {
        if (message.Type != "text" && message.Type != "private")
        {
            SendError("Неизвестный тип сообщения");
            return;
        }

        if (string.IsNullOrWhiteSpace(message.Text) || message.Text.Length > 500)
        {
            SendError("Сообщение должно содержать от 1 до 500 символов");
            return;
        }

        if (message.Type == "text")
        {
            _server.SendPublic(this, message.Text);
        }
        else if (string.IsNullOrWhiteSpace(message.To))
        {
            SendError("Выберите получателя личного сообщения");
        }
        else
        {
            _server.SendPrivate(this, message.To, message.Text);
        }
    }

    private void SendError(string text, string type = "error")
    {
        Send(new ChatMessage(type, "Сервер", text, DateTime.UtcNow));
    }

    public bool Send(ChatMessage message)
    {
        lock (_sendLock)
        {
            try
            {
                if (_writer == null)
                    return false;

                _writer.WriteLine(message.ToJson());
                return true;
            }
            catch (IOException)
            {
                return false;
            }
            catch (ObjectDisposedException)
            {
                return false;
            }
        }
    }

    public void Close()
    {
        _tcpClient.Close();
    }
}
