
using FloraReview;
using Microsoft.Win32;
using System;
using System.Data;
using System.Data.SQLite;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Text.RegularExpressions;

namespace FloraReview
{
    public partial class Form3 : Window
    {
        // Constants for statuses
        private const string StatusOpen = "open";
        private const string StatusClose = "close";

        // Data from database (ReadOnly)
        private readonly string? dbPath;
        private readonly string? User;

        private string? modifiedText;

        // Local variables updated when the database is updated
        private bool modified = false;

        private readonly List<DataRow> selectedRows;
        private int currentIndex = 0;
        private DataRow currentRow;
        private string? currentRowId;
        private string? currentComment;
        private string? currentStatus;


        private FindDialog? findDialog;
        private List<TextPointer> matchPositions = new();
        private int currentMatchIndex = -1;




        public Form3(string? dbPath, List<DataRow> selectedRows, string? User)
        {
            InitializeComponent();

            if (selectedRows != null && selectedRows.Count > 0)
            {
                this.selectedRows = selectedRows;
                this.dbPath = dbPath;
                this.User = User;

                LoadCurrentRow();
            }
            else
            {
                MessageBox.Show("No rows to edit.");
                this.Close();
                return;
            }
        }

        private void LoadCurrentRow()
        {
            if (currentIndex < 0 || currentIndex >= selectedRows.Count)
            {
                return;
            }

            currentRow = selectedRows[currentIndex];
            currentRowId = currentRow["rowid"]?.ToString();
            SelectedRowLabel.Content = $"{currentIndex + 1} of {selectedRows.Count}";

            rowIdTextBlock.Text = $"Row ID: {currentRow}";
            guidTextBlock.Text = $"GUID: {currentRow["Id"]}";
            textTypeTextBlock.Text = $"Text Type: {currentRow["TextTitle"]}";
            scientificNameTextBlock.Text = $"Scientific Name: {currentRow["CalcFullName"]}";
            currentStatus = string.IsNullOrEmpty(currentRow["Status"]?.ToString()) ? "open" : currentRow["Status"]?.ToString();
            currentComment = string.IsNullOrEmpty(currentRow["Comment"]?.ToString()) ? string.Empty : currentRow["Comment"]?.ToString();
            logTextBox.Text = FixComment(currentComment);
            originalTextBox.Text = currentRow["CoalescedText"].ToString();

            if (string.IsNullOrEmpty(currentRow["ApprovedText"]?.ToString()))
            {
                modifiedText = currentRow["FinalText"]?.ToString() ?? string.Empty;
                InfoLabel.Content = "Loaded AI reviewed text.";
            }
            else
            {
                modifiedText = currentRow["ApprovedText"]?.ToString() ?? string.Empty;
                InfoLabel.Content = "Loaded saved text.";
            }
            UpdateModifiedRichTextBox(modifiedText);

            WordDiff2.DiffFunction(currentRow["CoalescedText"].ToString(), modifiedText, diffRichTextBox);

            modified = false;
            SetStateControls();
        }

        private void SetStateControls()
        {
            string status = currentRow["Status"]?.ToString() ?? "open";
            bool isClosed = status == "close";

            modifiedRichTextBox.IsEnabled = !isClosed;
            StatusLabel.Content = isClosed ? "CLOSE" : "OPEN";
            ReOpenButton.IsEnabled = isClosed;
            ApproveButton.IsEnabled = !isClosed;
            RevertButton.IsEnabled = !isClosed;
            SaveButton.IsEnabled = !isClosed && modified;
            UndoButton.IsEnabled = !isClosed && modified;
            BackButton.IsEnabled = currentIndex > 0;
            ForwardButton.IsEnabled = currentIndex < selectedRows.Count - 1;
        }

        private void UpdateModifiedRichTextBox(string? text)
        {
            modifiedRichTextBox.Document.Blocks.Clear();
            modifiedRichTextBox.Document.Blocks.Add(new Paragraph(new Run(text)));
            WordDiff2.DiffFunction(originalTextBox.Text, text, diffRichTextBox);
        }

        private string? FixComment(string? comment)
        {
            if (string.IsNullOrEmpty(comment))
            {
                return string.Empty;
            }
            string[] parts = comment.Split(new[] { "ǁ" }, StringSplitOptions.RemoveEmptyEntries);
            return string.Join(Environment.NewLine, parts.Prepend("Comments: "));
        }

        private void Back_Click(object sender, RoutedEventArgs e)
        {
            saveRow();
            if (currentIndex > 0)
            {
                currentIndex--;
                LoadCurrentRow();
            }
        }

        private void Forward_Click(object sender, RoutedEventArgs e)
        {
            saveRow();
            if (currentIndex < selectedRows.Count - 1)
            {
                currentIndex++;
                LoadCurrentRow();
            }
        }

        private void Revert_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("Do you want to revert to the original text?", "Revert", MessageBoxButton.YesNo) == MessageBoxResult.No)
            {
                return;
            }
            UpdateModifiedRichTextBox(originalTextBox.Text);
            InfoLabel.Content = "Reverted to original text.";
        }

        private void Undo_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("Do you want to undo changes you made?", "Undo", MessageBoxButton.YesNo) == MessageBoxResult.No)
            {
                return;
            }
            UpdateModifiedRichTextBox(modifiedText);
            InfoLabel.Content = "Local changes reverted.";
        }

        private void ReOpen_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("Do you want to re-open approved text?", "Re-open", MessageBoxButton.YesNo) == MessageBoxResult.No)
            {
                return;
            }
            SetStatus(StatusOpen, "OPEN");
        }

        private void Approve_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("Do you want to approve the text?", "Approve", MessageBoxButton.YesNo) == MessageBoxResult.No)
            {
                return;
            }
            SetStatus(StatusClose, "CLOSED");
        }


        private void Discard_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("Do you want to load AI reviewed text?", "Discard", MessageBoxButton.YesNo) == MessageBoxResult.No)
            {
                return;
            }
            modifiedText = currentRow["FinalText"]?.ToString() ?? string.Empty;
            InfoLabel.Content = "Loaded AI reviewed text.";
            UpdateModifiedRichTextBox(modifiedText);
            modified = true;
            SetStateControls();
        }

        private void SetStatus(string newStatus, string statusLabel)
        {
            try
            {
                string updatedText = GetCleanedText();

                using (SQLiteConnection conn = new($"Data Source={dbPath};Version=3;"))
                {
                    conn.Open();
                    string query = $"UPDATE descriptions SET ApprovedText = @modifiedText, Reviewer = @User, Status = @newStatus WHERE rowid = @rowId";
                    using SQLiteCommand cmd = new(query, conn);
                    cmd.Parameters.AddWithValue("@modifiedText", updatedText);
                    cmd.Parameters.AddWithValue("@User", User);
                    cmd.Parameters.AddWithValue("@newStatus", newStatus);
                    cmd.Parameters.AddWithValue("@rowId", currentRowId);
                    cmd.ExecuteNonQuery();

                    string userComment = AskForComment();
                    currentComment = AppendComment($"{statusLabel} {DateTime.Now} {User}: {userComment}", conn);
                    logTextBox.Text = FixComment(currentComment);
                    currentStatus = newStatus;
                    modified = false;

                    UpdateCurrentRow();
                    SetStateControls();
                }
                InfoLabel.Content = "Changes saved successfully.";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error updating status: {ex.Message}");
            }
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            modified = !SaveData();
            SetStateControls();
        }


        private void Refresh_Click(object sender, RoutedEventArgs e)
        {
            TextRange textRange = new(modifiedRichTextBox.Document.ContentStart, modifiedRichTextBox.Document.ContentEnd);
            WordDiff2.DiffFunction(originalTextBox.Text, textRange.Text, diffRichTextBox);
        }

        private bool SaveData()
        {
            try
            {
                string updatedText = GetCleanedText();

                using (SQLiteConnection conn = new($"Data Source={dbPath};Version=3;"))
                {
                    conn.Open();
                    string query = $"UPDATE descriptions SET ApprovedText = @modifiedText, Reviewer = @User, Status = @currentStatus WHERE rowid = @rowId";
                    using SQLiteCommand cmd = new(query, conn);
                    cmd.Parameters.AddWithValue("@modifiedText", updatedText);
                    cmd.Parameters.AddWithValue("@User", User);
                    cmd.Parameters.AddWithValue("@currentStatus", currentStatus);
                    cmd.Parameters.AddWithValue("@rowId", currentRowId);
                    cmd.ExecuteNonQuery();

                    currentComment = AppendComment($"SAVED {DateTime.Now} {User}", conn);
                    logTextBox.Text = FixComment(currentComment);
                    modified = false;
                    UpdateCurrentRow();
                }
                InfoLabel.Content = "Changes saved successfully.";
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving changes: {ex.Message}");
                return false;
            }
        }

        
        private string GetCleanedText()
        {
            TextRange textRange = new(modifiedRichTextBox.Document.ContentStart, modifiedRichTextBox.Document.ContentEnd);
            string cleanedText = Regex.Replace(textRange.Text, @"[\s]+", " ").Trim();
            UpdateModifiedRichTextBox(cleanedText);
            return cleanedText;
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
             this.Close();
        }

        private void saveRow()
        {
            if (modified && MessageBox.Show("Do you want to save first?", "Save Data?", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                modified = !SaveData();
                SetStateControls();
                ClearHighlights(modifiedRichTextBox);
            }
        }               

        private string AskForComment()
        {
            InputDialog inputDialog = new("Enter your comment:");
            return inputDialog.ShowDialog() == true ? inputDialog.Comment : string.Empty;
        }

        private string AppendComment(string newComment, SQLiteConnection conn)
        {
            string oldComment = string.IsNullOrEmpty(currentComment) ? string.Empty : currentComment + "ǁ";
            string query = $"UPDATE descriptions SET Comment = @fullComment WHERE rowid = @rowId";
            using SQLiteCommand cmd = new(query, conn);
            cmd.Parameters.AddWithValue("@fullComment", oldComment + newComment);
            cmd.Parameters.AddWithValue("@rowId", currentRowId);
            cmd.ExecuteNonQuery();
            return oldComment + newComment;
        }

        private void modifiedRichTextBox_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (Keyboard.Modifiers == ModifierKeys.Control)
            {
                double newFontSize = modifiedRichTextBox.FontSize + (e.Delta > 0 ? 1 : -1);
                if (newFontSize >= 8 && newFontSize <= 72)
                {
                    modifiedRichTextBox.FontSize = newFontSize;
                }
            }
        }

        private void modifiedRichTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            modified = true;
            SetStateControls();
        }

        private void UpdateCurrentRow()
        {
            currentRow["Status"] = currentStatus;
            currentRow["ApprovedText"] = GetCleanedText();
            currentRow["Reviewer"] = User;
            currentRow["FinalText"] = GetCleanedText();
            currentRow["Comment"] = currentComment;
        }

        private void IncreaseFontSize_Click(object sender, RoutedEventArgs e)
        {
            if (modifiedRichTextBox != null)
            {
                double newFontSize = modifiedRichTextBox.FontSize + 1;
                if (newFontSize <= 72)
                {
                    modifiedRichTextBox.FontSize = newFontSize;
                }
            }
        }

        private void DecreaseFontSize_Click(object sender, RoutedEventArgs e)
        {
            if (modifiedRichTextBox != null)
            {
                double newFontSize = modifiedRichTextBox.FontSize - 1;
                if (newFontSize >= 8)
                {
                    modifiedRichTextBox.FontSize = newFontSize;
                }
            }
        }

        private void FindCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            string selectedText = GetSelectedText(modifiedRichTextBox);

            if (findDialog == null)
            {
                findDialog = new FindDialog(selectedText);
                findDialog.Owner = this;

                // Subscribe to FindDialog events
                findDialog.FindRequested += (searchText) => HighlightAllMatches(modifiedRichTextBox, searchText);
                findDialog.NextRequested += (searchText) => HighlightNextMatch(modifiedRichTextBox, searchText);
                findDialog.PreviousRequested += (searchText) => HighlightPreviousMatch(modifiedRichTextBox, searchText);
                findDialog.ClearRequested += () => ClearHighlights(modifiedRichTextBox);

                findDialog.TextChanged += () => ClearHighlights(modifiedRichTextBox);

                findDialog.Closed += (s, args) => findDialog = null;
                findDialog.Show();
            }
            else
            {
                findDialog.Activate();
            }
        }


        private string GetSelectedText(RichTextBox richTextBox)
        {
            TextRange selection = richTextBox.Selection;
            return !selection.IsEmpty ? selection.Text : string.Empty;
        }


        private void HighlightNextMatch(RichTextBox richTextBox, string searchText)
        {
            if (matchPositions.Count == 0) return;

            currentMatchIndex = (currentMatchIndex + 1) % matchPositions.Count; // Cycle to the next match
            MoveToMatch(richTextBox, currentMatchIndex);
        }



        private void HighlightPreviousMatch(RichTextBox richTextBox, string searchText)
        {
            if (matchPositions.Count == 0) return;

            currentMatchIndex = (currentMatchIndex - 1 + matchPositions.Count) % matchPositions.Count; // Cycle to the previous match
            MoveToMatch(richTextBox, currentMatchIndex);
        }



        private void MoveToMatch(RichTextBox richTextBox, int matchIndex)
        {
            if (matchIndex < 0 || matchIndex >= matchPositions.Count) return;

            TextPointer start = matchPositions[matchIndex];
            richTextBox.CaretPosition = start;
            richTextBox.Focus();
        }



        private void HighlightAllMatches(RichTextBox richTextBox, string searchText)
        {
            bool currentModified = modified;
            if (string.IsNullOrEmpty(searchText)) return;

            ClearHighlights(richTextBox);
            matchPositions.Clear();
            currentMatchIndex = -1;

            TextPointer currentPointer = richTextBox.Document.ContentStart;
            while (currentPointer != null)
            {
                TextRange searchRange = new TextRange(currentPointer, richTextBox.Document.ContentEnd);
                int index = searchRange.Text.IndexOf(searchText, StringComparison.OrdinalIgnoreCase);
                if (index >= 0)
                {
                    TextPointer start = GetTextPointerAtOffset(currentPointer, index);
                    TextPointer end = GetTextPointerAtOffset(start, searchText.Length);
                    ApplyHighlight(start, end, Brushes.Yellow);

                    matchPositions.Add(start); // Store the starting position of the match
                    currentPointer = end;
                }
                else
                {
                    break;
                }
            }

            // Move the cursor to the first match if matches are found
            if (matchPositions.Count > 0)
            {
                currentMatchIndex = 0;
                MoveToMatch(richTextBox, currentMatchIndex);
            }
            modified = currentModified;
        }

        private void ClearHighlights(RichTextBox richTextBox)
        {
            bool currentModified = modified;
            TextRange textRange = new TextRange(richTextBox.Document.ContentStart, richTextBox.Document.ContentEnd);
            textRange.ApplyPropertyValue(TextElement.BackgroundProperty, null);
            modified = currentModified;
        }


        private void ApplyHighlight(TextPointer start, TextPointer end, Brush color)
        {
            TextRange highlightRange = new TextRange(start, end);
            highlightRange.ApplyPropertyValue(TextElement.BackgroundProperty, color);
        }


        private TextPointer GetTextPointerAtOffset(TextPointer start, int offset)
        {
            TextPointer current = start;
            int count = 0;

            while (current != null && count < offset)
            {
                if (current.GetPointerContext(LogicalDirection.Forward) == TextPointerContext.Text)
                {
                    int textLength = current.GetTextRunLength(LogicalDirection.Forward);
                    if (count + textLength > offset)
                    {
                        return current.GetPositionAtOffset(offset - count);
                    }
                    count += textLength;
                }
                current = current.GetPositionAtOffset(1, LogicalDirection.Forward);
            }
            return current;
        }
        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            saveRow();
        }
    }
}




