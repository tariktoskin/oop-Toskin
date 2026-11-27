using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;

namespace StudentChat;

public class ChatClient
{
    private TcpClient? _tcpClient;
    private StreamReader? _reader;
    private StreamWriter? _writer;
    private readonly SemaphoreSlim _sendLock = new(1, 1);
    private volatile bool _closed = true;
    private bool _readingStarted;

    public string Nick { get; private set; } = "";
    public bool IsConnected => !_closed;
    public event Action<ChatMessage>? MessageReceived;
    public event Action? Disconnected;

    public async Task ConnectAsync(string host, int port, string nick)
    {
        _closed = false;
        _tcpClient = new TcpClient();

        try
        {

            await _tcpClient.ConnectAsync(host, port).WaitAsync(TimeSpan.FromSeconds(5));

            NetworkStream stream = _tcpClient.GetStream();
            _reader = new StreamReader(stream, new UTF8Encoding(false));
            _writer = new StreamWriter(stream, new UTF8Encoding(false)) { AutoFlush = true };

            var login = new ChatMessage("login", nick, "", DateTime.UtcNow);
            await _writer.WriteLineAsync(login.ToJson());

            string? line = await _reader.ReadLineAsync().WaitAsync(TimeSpan.FromSeconds(5));
            if (line == null)
                throw new IOException("Сервер закрыл соединение");

            ChatMessage? answer = ChatMessage.FromJson(line);
            if (answer?.Type == "login_error")
                throw new InvalidOperationException(answer.Text);

            if (answer?.Type != "login_ok")
                throw new IOException("Сервер вернул неизвестный ответ");

            Nick = nick;
        }
        catch
        {
            Close();
            throw;
        }
    }

    public void StartReceiving()
    {
        if (_reader == null || _readingStarted)
            return;

        _readingStarted = true;
        _ = Task.Run(ReadLoop);
    }

    private void ReadLoop()
    {
        try
        {
            while (!_closed)
            {

                string? line = _reader!.ReadLine();
                if (line == null)
                    break;

                try
                {
                    ChatMessage? message = ChatMessage.FromJson(line);
                    if (message != null)
                        MessageReceived?.Invoke(message);
                }
                catch (JsonException)
                {

                }
            }
        }
        catch (IOException)
        {

        }
        catch (ObjectDisposedException)
        {

        }
        finally
        {
            bool connectionLost = !_closed;
            Close();
            if (connectionLost)
                Disconnected?.Invoke();
        }
    }

    public async Task SendAsync(string text, string? to)
    {
        if (_closed || _writer == null)
            throw new IOException("Нет подключения к серверу");

        var message = new ChatMessage(to == null ? "text" : "private", Nick, text, DateTime.UtcNow, to);

        await _sendLock.WaitAsync();
        try
        {
            if (_closed)
                throw new IOException("Соединение закрыто");

            await _writer.WriteLineAsync(message.ToJson());
        }
        finally
        {
            _sendLock.Release();
        }
    }

    public void Close()
    {
        _closed = true;
        _tcpClient?.Close();
    }
}
