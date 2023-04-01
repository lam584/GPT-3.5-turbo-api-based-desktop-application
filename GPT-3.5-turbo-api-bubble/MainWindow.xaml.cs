using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using OpenAI;
using OpenAI.Chat;
using OpenAI.Models;

namespace WpfApp1
{
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        private string apiKey;

        public event PropertyChangedEventHandler PropertyChanged;

        private ObservableCollection<ChatMessage> _chatMessages = new ObservableCollection<ChatMessage>();

        public static readonly DependencyProperty ChatMessagesProperty = DependencyProperty.Register("ChatMessages", typeof(ObservableCollection<ChatMessage>), typeof(MainWindow), new PropertyMetadata(new ObservableCollection<ChatMessage>()));

        public ObservableCollection<ChatMessage> ChatMessages
        {
            get { return (ObservableCollection<ChatMessage>)GetValue(ChatMessagesProperty); }
            set { SetValue(ChatMessagesProperty, value); }
        }

        public MainWindow()
        {
            InitializeComponent();
            ChatListBox.ItemsSource = ChatMessages;
            string currentDirectory = Directory.GetCurrentDirectory();
            string configFile = Path.Combine(currentDirectory, "config.txt");
            if (File.Exists(configFile))
            {
                string[] lines = File.ReadAllLines(configFile);
                foreach (string line in lines)
                {
                    if (line.StartsWith("key="))
                    {
                        apiKey = line.Substring(4);
                        break;
                    }
                }
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            SendMessage();
        }

        private async void TextBox1_KeyUp(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter && Keyboard.Modifiers == ModifierKeys.None)
            {
                SendMessage();
                e.Handled = true;
            }
        }

        private async void SendMessage()
        {
            if (TextBox1.Text != "")
            {
                ChatMessage lastMessage;
                if (ChatMessages.Count == 0)
                {
                    lastMessage = null;
                }
                else
                {
                    lastMessage = ChatMessages[ChatMessages.Count - 1];
                }

                double itemWidth = GetMessageWidth(TextBox1.Text);
                if (TextBox1.Text.Length < 8)
                {
                    itemWidth = 60;
                }
                ChatMessages.Add(new ChatMessage { Text = TextBox1.Text, Background = System.Windows.Media.Brushes.LightGray, FontWeight = FontWeights.Bold, ItemWidth = itemWidth });
                if (apiKey == null)
                {
                    MessageBox.Show("Unable to find API key in config file.");
                    return;
                }
                var api = new OpenAIClient(apiKey);
                var chatPrompts = new System.Collections.Generic.List<ChatPrompt>
        {
            new ChatPrompt("user", TextBox1.Text),
        };
                TextBox1.Text = "";
                var chatRequest = new ChatRequest(chatPrompts, Model.GPT3_5_Turbo);
                string response = "";
                await api.ChatEndpoint.StreamCompletionAsync(chatRequest, result =>
                {
                    response += result.FirstChoice;
                });
                if (lastMessage != null && GetMessageWidth(lastMessage.Text) > GetMessageWidth(response))
                {
                    lastMessage.ItemWidth = GetMessageWidth(response);
                }
                if (GetMessageWidth(response) > GetMessageWidth(response))
                {
                    ChatMessages.Add(new ChatMessage { Text = "gpt-3.5-turbo: ", Background = System.Windows.Media.Brushes.LightBlue, FontWeight = FontWeights.Bold, ItemWidth = GetMessageWidth(response) });
                    while (response != "")
                    {
                        int length = GetLengthThatFitsInWidth(response);
                        string substr = response.Substring(0, length);
                        ChatMessages.Add(new ChatMessage { Text = substr, Background = System.Windows.Media.Brushes.LightBlue, ItemWidth = GetMessageWidth(response) });
                        response = response.Substring(length).Trim();
                    }
                }
                else
                {
                    ChatMessages.Add(new ChatMessage { Text = "gpt-3.5-turbo: " + response, Background = System.Windows.Media.Brushes.LightBlue, FontWeight = FontWeights.Bold, ItemWidth = GetMessageWidth(response) });
                }
            }
        }


        public class ChatMessage
        {
            public string Text { get; set; }
            public System.Windows.Media.Brush Background { get; set; }
            public System.Windows.FontWeight FontWeight { get; set; }
            public double ItemWidth { get; set; }
        }

        private double GetMessageWidth(string text)
        {
            var textBlock = new TextBlock { Text = text, TextWrapping = TextWrapping.Wrap };
            textBlock.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            return textBlock.DesiredSize.Width;
        }

        private int GetLengthThatFitsInWidth(string text)
        {
            int length = text.Length;
            var textBlock = new TextBlock { TextWrapping = TextWrapping.Wrap };
            while (true)
            {
                textBlock.Text = text.Substring(0, length);
                textBlock.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
                if (textBlock.DesiredSize.Width <= ChatListBox.ActualWidth * 0.9)
                {
                    return length;
                }
                length--;
            }
        }
    }
}

