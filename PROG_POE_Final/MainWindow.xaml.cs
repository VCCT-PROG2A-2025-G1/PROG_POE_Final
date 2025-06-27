using PROG_POE;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace PROG_POE_Final
{
   
    public partial class MainWindow : Window
    {
        private ChatbotResponses chatbot;

        //Default Constructor
        public MainWindow()
        {
            InitializeComponent();
            Loaded += MainWindow_Loaded;
        }
        //---------------------------------------------------------------------------------------------------------------------------------------------
        //Handles the window loaded event
        //prompts for user name and starts chat
        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            string userName = PromptForUserName();
            chatbot = new ChatbotResponses(userName);
            AddBotMessage($"Hi {userName}! How can I assist you today?");
        }

        //---------------------------------------------------------------------------------------------------------------------------------------------
        // Add a user message in a chat bubble
        private void AddUserMessage(string message)
        {
            var userBubble = new ChatBubble
            {
                Message = message,
                IsUser = true
            };
            ChatPanel.Children.Add(userBubble);
            ScrollToBottom();
        }

        //---------------------------------------------------------------------------------------------------------------------------------------------
        // Add a bot message in a chat bubble
        private void AddBotMessage(string message)
        {
            var botBubble = new ChatBubble
            {
                Message = message,
                IsUser = false
            };
            ChatPanel.Children.Add(botBubble);
            ScrollToBottom();
        }

        //---------------------------------------------------------------------------------------------------------------------------------------------
        // Scrolls to bottom after adding a message
        private void ScrollToBottom()
        {
            ChatScrollViewer.ScrollToEnd();
        }

        //---------------------------------------------------------------------------------------------------------------------------------------------
       // Allows window to be dragged by the border
        private void Border_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if(e.ChangedButton == MouseButton.Left)
            {
                DragMove();
            }
        }

        //---------------------------------------------------------------------------------------------------------------------------------------------
        //Minimizes the window when button is clicked
        private void btnMinimize_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        //---------------------------------------------------------------------------------------------------------------------------------------------
        //Maximizes or restores window's original size
        private void btnMaximise_Click(object sender, RoutedEventArgs e)
        {
            //Maximizes the window when window is normal sized 
            if (WindowState == WindowState.Normal)
            {
                WindowState = WindowState.Maximized;
            }
            else
            {
                WindowState = WindowState.Normal;
            }
        }

        //---------------------------------------------------------------------------------------------------------------------------------------------
        //Closes the application
        private void btnExit_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }


        //---------------------------------------------------------------------------------------------------------------------------------------------
        private void btnTakQuiz_Click(object sender, RoutedEventArgs e)
        {
            string quizStartResponse = chatbot.GetResponse("start quiz");
            AddBotMessage(quizStartResponse);
        }

        //---------------------------------------------------------------------------------------------------------------------------------------------
        private void btnNewChat_Click(object sender, RoutedEventArgs e)
        {
            // Clears the chat panel
            ChatPanel.Children.Clear();

            // Resets chatbot session but keeps the same user
            chatbot.RestartSession();

            // Greet the user again
            AddBotMessage("New chat started. How can I assist you?");
        }

        //---------------------------------------------------------------------------------------------------------------------------------------------
       // Handles the send button click
        async private void btnSend_Click(object sender, RoutedEventArgs e)
        {
            string userInput = InputTextBox.Text.Trim();
            if (string.IsNullOrEmpty(userInput)) 
                return;

            AddUserMessage(userInput); // Show user message bubble
            InputTextBox.Clear();

            // Add 'Thinking...' animation
            var loadingBubble = CreateThinkingAnimation();

            await Task.Delay(2500); // Simulate bot thinking

            // Remove 'Thinking...' bubble
            ChatPanel.Children.Remove(loadingBubble);

            string botReply = chatbot.GetResponse(userInput);  // Get bot response
            AddBotMessage(botReply);
        }

        //---------------------------------------------------------------------------------------------------------------------------------------------
        //Prompts the user to enter their name when the app starts
        private string PromptForUserName()
        {
            var inputDialog = new Window
            {
                Title = "Enter Your Name",
                SizeToContent = SizeToContent.WidthAndHeight,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = this,
                ResizeMode = ResizeMode.NoResize
            };

            var stack = new StackPanel { Margin = new Thickness(10) };
            stack.Children.Add(new TextBlock { Text = "Please enter your name:" });
            var textBox = new TextBox { Width = 200, Margin = new Thickness(0, 5, 0, 5) };
            stack.Children.Add(textBox);

            var btnOk = new Button { Content = "OK", Width = 60, IsDefault = true, IsCancel = false };
            btnOk.Click += (s, e) => inputDialog.DialogResult = true;
            stack.Children.Add(btnOk);

            inputDialog.Content = stack;

            bool? result = inputDialog.ShowDialog();
            if (result == true && !string.IsNullOrWhiteSpace(textBox.Text))
            {
                return textBox.Text.Trim();
            }
            else
            {
                return "Friend";
            }
        }
        //---------------------------------------------------------------------------------------------------------------------------------------------
       // Creates an animation to show that the chatbot is thinking
        private TextBlock CreateThinkingAnimation()
        {
            var thinkingText = new TextBlock
            {
                Text = "thinking",
                Foreground = Brushes.LightGray,
                FontStyle = FontStyles.Italic,
                FontSize = 14,
                Margin = new Thickness(10)
            };

            ChatPanel.Children.Add(thinkingText);
            ScrollToBottom();

            // Create the animation for dots
            var dotsAnimation = new StringAnimationUsingKeyFrames
            {
                RepeatBehavior = RepeatBehavior.Forever,
                Duration = TimeSpan.FromSeconds(1.5)
            };

                dotsAnimation.KeyFrames.Add(new DiscreteStringKeyFrame("thinking.", KeyTime.FromTimeSpan(TimeSpan.FromSeconds(0.3))));
                dotsAnimation.KeyFrames.Add(new DiscreteStringKeyFrame("thinking..", KeyTime.FromTimeSpan(TimeSpan.FromSeconds(0.6))));
                dotsAnimation.KeyFrames.Add(new DiscreteStringKeyFrame("thinking...", KeyTime.FromTimeSpan(TimeSpan.FromSeconds(0.9))));
                dotsAnimation.KeyFrames.Add(new DiscreteStringKeyFrame("thinking", KeyTime.FromTimeSpan(TimeSpan.FromSeconds(1.2))));

                Storyboard.SetTarget(dotsAnimation, thinkingText);
                Storyboard.SetTargetProperty(dotsAnimation, new PropertyPath(TextBlock.TextProperty));

                var storyboard = new Storyboard();
                storyboard.Children.Add(dotsAnimation);
                storyboard.Begin();

                // Return both the text and the storyboard (if needed to stop later)
                return thinkingText;
         }
        //---------------------------------------------------------------------------------------------------------------------------
        // Button for viewing tasks if user does not want to prompt chatbot
        private void btnViewTasks_Click(object sender, RoutedEventArgs e)
        {
            string taskList = chatbot.ViewTasks();
            AddBotMessage(taskList);
        }
    }

}
