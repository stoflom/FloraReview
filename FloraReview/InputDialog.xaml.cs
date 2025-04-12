using System.Windows;
using System.Windows.Controls;

namespace FloraReview
{
    public partial class InputDialog : Window
    {
        public string Comment { get; private set; }

        public InputDialog(string prompt)
        {
            InitializeComponent();
            PromptTextBlock.Text = prompt;
            OkButton.IsDefault = true;
            CancelButton.IsCancel = true;
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            Comment = CommentTextBox.Text.Trim();
            if (string.IsNullOrWhiteSpace(Comment))
            {
                MessageBox.Show("Please enter a comment.");
                return;
            }

            DialogResult = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}