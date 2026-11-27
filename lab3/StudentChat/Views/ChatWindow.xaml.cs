using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;

namespace StudentChat;

public partial class ChatWindow : Window
{
    private readonly ChatClient _client;
    private readonly ObservableCollection<ChatLine> _messages = new();
    private readonly ObservableCollection<string> _users = new();
    private bool _sending;

    public ChatWindow(ChatClient client)
    {
        InitializeComponent();
        _client = client;

        NickText.Text = $"Вы вошли как: {_client.Nick}";
        MessagesList.ItemsSource = _messages;
        UsersList.ItemsSource = _users;

        _client.MessageReceived += OnMessageReceived;
        _client.Disconnected += OnDisconnected;
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        _client.StartReceiving();
        MessageInput.Focus();
    }

    private void OnMessageReceived(ChatMessage message)
    {

        Dispatcher.BeginInvoke(new Action(() => ShowMessage(message)));
    }

    private void OnDisconnected()
    {
        Dispatcher.BeginInvoke(new Action(() =>
        {
            AddLine("Соединение с сервером потеряно.", isSystem: true);
            SendButton.IsEnabled = false;
            MessageInput.IsEnabled = false;
            _users.Clear();
        }));
    }

    private void ShowMessage(ChatMessage message)
    {
        string time = message.Time.ToLocalTime().ToString("HH:mm");

        switch (message.Type)
        {
            case "text":
                AddLine($"[{time}] {message.Nick}: {message.Text}");
                break;
            case "history":
                AddLine($"[{time}] (история) {message.Nick}: {message.Text}");
                break;
            case "private":
                AddLine($"[{time}] [ЛИЧНОЕ] {message.Nick} → {message.To}: {message.Text}", isPrivate: true);
                break;
            case "system":
                AddLine($"[{time}] {message.Text}", isSystem: true);
                break;
            case "error":
                AddLine($"Ошибка: {message.Text}", isSystem: true);
                break;
            case "users":
                string? selected = UsersList.SelectedItem as string;
                _users.Clear();
                foreach (string nick in message.Users ?? new List<string>())
                    _users.Add(nick);

                if (selected != null && _users.Contains(selected))
                    UsersList.SelectedItem = selected;
                break;
        }
    }

    private void AddLine(string text, bool isPrivate = false, bool isSystem = false)
    {
        var line = new ChatLine
        {
            Display = text,
            IsPrivate = isPrivate,
            IsSystem = isSystem
        };

        _messages.Add(line);
        MessagesList.ScrollIntoView(line);
    }

    private async Task SendMessageAsync()
    {
        string text = MessageInput.Text.Trim();
        if (text.Length == 0 || _sending || !_client.IsConnected)
            return;

        string? to = UsersList.SelectedItem as string;
        if (string.Equals(to, _client.Nick, StringComparison.OrdinalIgnoreCase))
        {
            AddLine("Выберите другого участника или нажмите «Писать всем».", isSystem: true);
            return;
        }

        _sending = true;
        SendButton.IsEnabled = false;
        try
        {
            await _client.SendAsync(text, to);
            MessageInput.Clear();
            MessageInput.Focus();
        }
        catch (Exception ex)
        {
            AddLine($"Не удалось отправить сообщение: {ex.Message}", isSystem: true);
        }
        finally
        {
            _sending = false;
            SendButton.IsEnabled = _client.IsConnected;
        }
    }

    private async void SendButton_Click(object sender, RoutedEventArgs e)
    {
        await SendMessageAsync();
    }

    private async void MessageInput_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            e.Handled = true;
            await SendMessageAsync();
        }
    }

    private void EveryoneButton_Click(object sender, RoutedEventArgs e)
    {
        UsersList.SelectedItem = null;
        MessageInput.Focus();
    }

    private void UsersList_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        string? selected = UsersList.SelectedItem as string;
        RecipientText.Text = selected == null ? "Сообщение всем" : $"Личное сообщение для: {selected}";
    }

    private void Window_Closed(object? sender, EventArgs e)
    {
        _client.MessageReceived -= OnMessageReceived;
        _client.Disconnected -= OnDisconnected;
        _client.Close();
    }
}
