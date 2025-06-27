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
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace PROG_POE_Final
{
    // Logic for ChatBubble.xaml
    public partial class ChatBubble : UserControl
    {
        public ChatBubble()
        {
            InitializeComponent();
            UpdateBubble();
        }

            // Message property
            public static readonly DependencyProperty MessageProperty =
                DependencyProperty.Register("Message", typeof(string), typeof(ChatBubble), new PropertyMetadata(string.Empty, OnMessageChanged));

            public string Message
            {
                get { return (string)GetValue(MessageProperty); }
                set { SetValue(MessageProperty, value); }
            }

            private static void OnMessageChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
            {
                var bubble = d as ChatBubble;
                bubble.MessageText.Text = (string)e.NewValue;
            }

            // IsUser property
            public static readonly DependencyProperty IsUserProperty =
                DependencyProperty.Register("IsUser", typeof(bool), typeof(ChatBubble), new PropertyMetadata(false, OnIsUserChanged));

            public bool IsUser
            {
                get { return (bool)GetValue(IsUserProperty); }
                set { SetValue(IsUserProperty, value); }
            }

            private static void OnIsUserChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
            {
                var bubble = d as ChatBubble;
                bubble.UpdateBubble();
            }

            // Update visuals based on whether it's user or bot
            private void UpdateBubble()
            {
                if (IsUser)
                {
                    this.HorizontalAlignment = HorizontalAlignment.Right;

                    // User message (right)
                    BubbleBorder.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#3E39C2"));
                    MessageText.Foreground = Brushes.White;
                    BotTrail.Visibility = Visibility.Collapsed;
                    UserTrail.Visibility = Visibility.Visible;
                }
                else
                {

                    this.HorizontalAlignment = HorizontalAlignment.Left;


                    // Bot message (left)
                    BubbleBorder.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E5E5EA"));
                    MessageText.Foreground = Brushes.Black;
                    BotTrail.Visibility = Visibility.Visible;
                    UserTrail.Visibility = Visibility.Collapsed;
                }
            }
        }
    }
