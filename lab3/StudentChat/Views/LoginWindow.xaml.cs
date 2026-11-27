using System.Net.Sockets;
using System.Windows;
using System.Windows.Media;

namespace StudentChat;

public partial class LoginWindow : Window
{
    public LoginWindow()
    {
        InitializeComponent();
    }

    private bool TryGetPort(out int port)
    {
        if (!int.TryParse(PortBox.Text, out port) || port < 1 || port > 65535)
        {
            ErrorText.Text = "Порт должен быть числом от 1 до 65535.";
            return false;
        }
        return true;
    }

    private void StartServerButton_Click(object sender, RoutedEventArgs e)
    {
        ErrorText.Text = "";
        if (!TryGetPort(out int port)) return;

        try
        {
            ((App)Application.Current).StartLocalServer(port);
            StartServerButton.IsEnabled = false;
            PortBox.IsEnabled = false;
            ServerStatusText.Text = $"Сервер работает на порту {port}. Теперь можно войти в чат.";
            ServerStatusText.Foreground = Brushes.DarkGreen;
            ServerBox.Text = "127.0.0.1";
        }
        catch (SocketException)
        {
            ErrorText.Text = "Не удалось запустить сервер: возможно, этот порт уже занят.";
        }
        catch (Exception ex)
        {
            ErrorText.Text = "Ошибка запуска сервера: " + ex.Message;
        }
    }

    private async void ConnectButton_Click(object sender, RoutedEventArgs e)
    {
        string host = ServerBox.Text.Trim();
        string nick = NickBox.Text.Trim();
        ErrorText.Text = "";

        if (string.IsNullOrWhiteSpace(host) || string.IsNullOrWhiteSpace(nick))
        {
            ErrorText.Text = "Укажите адрес сервера и ник.";
            return;
        }

        if (nick.Length > 20 || nick.Any(char.IsControl))
        {
            ErrorText.Text = "Ник должен содержать от 1 до 20 обычных символов.";
            return;
        }

        if (!TryGetPort(out int port)) return;

        ConnectButton.IsEnabled = false;
        ConnectButton.Content = "Подключение...";
        var client = new ChatClient();

        try
        {
            await client.ConnectAsync(host, port, nick);
            var chatWindow = new ChatWindow(client);
            Application.Current.MainWindow = chatWindow;
            chatWindow.Show();
            Close();
        }
        catch (TimeoutException)
        {
            client.Close();
            ErrorText.Text = "Сервер не ответил за 5 секунд. Проверьте адрес и порт.";
        }
        catch (SocketException)
        {
            client.Close();
            ErrorText.Text = "Нет подключения. Проверьте IP, порт и запущен ли сервер.";
        }
        catch (Exception ex)
        {
            client.Close();
            ErrorText.Text = "Не удалось войти: " + ex.Message;
        }
        finally
        {
            ConnectButton.IsEnabled = true;
            ConnectButton.Content = "Войти в чат";
        }
    }
}
