using System.Windows;
using System.Windows.Controls;
using System.Net.Http;
using System;
using System.Threading.Tasks;
using OpenAI;
using OpenAI.Chat;
using OpenAI.Models;
using System.Windows.Input;
using System.Collections.Generic;

namespace WpfApp1
{
    public partial class MainWindow : Window
    {
        private OpenAIClient api;

        public MainWindow()
        {
            InitializeComponent();
            string curDir = System.IO.Directory.GetCurrentDirectory();
            string configPath = System.IO.Path.Combine(curDir, "config.txt");
            if (System.IO.File.Exists(configPath))
            {
                string configText = System.IO.File.ReadAllText(configPath);
                string[] lines = configText.Split('\n');
                foreach (string line in lines)
                {
                    string[] keyValue = line.Split('=');
                    if (keyValue.Length == 2 && keyValue[0] == "key")
                    {
                        string apikey = keyValue[1].Trim();
                        api = new OpenAIClient(apikey);
                        break;
                    }
                }
            }
        }
        private async void TextBox1_KeyUp(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter && Keyboard.Modifiers == ModifierKeys.None)
            {
                e.Handled = true;
                TextBox1.Text = TextBox1.Text.Remove(TextBox1.Text.Length - 1);
                SendMessage();
            }
        }

        private async void SendMessage()
        {
            TextBox textBox = FindName("TextBox") as TextBox;
            if (TextBox1.Text != "")
            {
                TextBox.Text = TextBox.Text + "You: " + TextBox1.Text + "\n\n";
                textBox.ScrollToEnd();
                var chatPrompts = new List<ChatPrompt>
                {
                    new ChatPrompt("user", TextBox1.Text),
                };
                TextBox1.Text = "";
                var chatRequest = new ChatRequest(chatPrompts, Model.GPT3_5_Turbo);
                TextBox.Text = TextBox.Text + "gpt-3.5-turbo: ";
                await api.ChatEndpoint.StreamCompletionAsync(chatRequest, result =>
                {
                    Console.WriteLine(result.FirstChoice);
                    TextBox.Text = TextBox.Text + result.FirstChoice;

                    textBox.ScrollToEnd();

                });
                TextBox.Text = TextBox.Text + "\n\n";
            }
            else
            {
                var models = await api.ModelsEndpoint.GetModelsAsync();

                foreach (var model in models)
                {
                    TextBox.Text = model.ToString();
                }
            }
        }
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            SendMessage();
        }
    }
}