using System;
using System.Windows;

namespace FloraReview
{
    public partial class FindDialog : Window
    {
        public event Action<string>? FindRequested;
        public event Action<string>? NextRequested;
        public event Action<string>? PreviousRequested;
        public event Action? ClearRequested;
        public event Action? TextChanged;

        public FindDialog(string initialText)
        {
            InitializeComponent();
            SearchTextBox.Text = initialText;
            SearchTextBox.TextChanged += (s, e) => TextChanged?.Invoke();
            SearchTextBox.Focus();
        }

        private void FindButton_Click(object sender, RoutedEventArgs e)
        {
            FindRequested?.Invoke(SearchTextBox.Text);
        }

        private void NextButton_Click(object sender, RoutedEventArgs e)
        {
            NextRequested?.Invoke(SearchTextBox.Text);
        }

        private void PreviousButton_Click(object sender, RoutedEventArgs e)
        {
            PreviousRequested?.Invoke(SearchTextBox.Text);
        }

        private void ClearButton_Click(object sender, RoutedEventArgs e)
        {
            ClearRequested?.Invoke();
        }


        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            ClearRequested?.Invoke();
            Close();
        }
    }
}



