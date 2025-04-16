﻿
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
using System.Collections;
using System.Text.RegularExpressions;
using SQLite3DB;

namespace FloraReview
{
    public partial class Form3 : Window
    {
        // Constants for statuses
        private enum Status
        {
            Open,
            Close
        }

        private const string StatusOpen = "OPEN";
        private const string StatusClose = "CLOSE";

        // Data from database (ReadOnly)
        private readonly string? User;

        private string? modifiedText;

        // Local variables updated when the database is updated
        private bool modified = false;

        private readonly List<DataRow>? selectedRows;
        private int currentIndex = 0;
        private DataRow? currentRow;
        private string? currentRowId;
        private string? currentComment;
        private string? currentStatus;
        private SQLite3db? db;


        private FindDialog? findDialog;
        private List<TextPointer> matchPositions = new();
        private int currentMatchIndex = -1;




        public Form3(SQLite3db? adb, List<DataRow> selectedRows, string? aUser)
        {
            InitializeComponent();
            db = adb;
            User = aUser;
            if (selectedRows != null && selectedRows.Count > 0)
            {
                this.selectedRows = selectedRows;
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
            if (currentIndex < 0 || selectedRows == null || currentIndex >= selectedRows.Count)
            {
                return;
            }
            currentRow = selectedRows[currentIndex];
            currentRowId = currentRow["rowid"]?.ToString();
            SelectedRowLabel.Content = $"{currentIndex + 1} of {selectedRows.Count}";
            rowIdTextBlock.Text = $"Row ID: {currentRow["rowid"]}";
            guidTextBlock.Text = $"GUID: {currentRow["Id"]}";
            textTypeTextBlock.Text = $"Text Type: {currentRow["TextTitle"]}";
            scientificNameTextBlock.Text = $"Scientific Name: {currentRow["CalcFullName"]}";
            currentStatus = string.IsNullOrEmpty(currentRow["Status"]?.ToString()?.ToLower()) ? "open" : currentRow["Status"]?.ToString();
            currentComment = string.IsNullOrEmpty(currentRow["Comment"]?.ToString()) ? string.Empty : currentRow["Comment"]?.ToString();
            logTextBox.Text = FixComment(currentComment);
            originalTextBox.Text = currentRow["CoalescedText"].ToString();


            bool EnableDiscard =false;

            if (string.IsNullOrEmpty(currentRow["ApprovedText"]?.ToString()))
                
            {
                modifiedText = currentRow["FinalText"]?.ToString() ?? string.Empty;
                InfoLabel.Content = "Loaded AI reviewed text.";
                EnableDiscard = false; 
            }
            else
            {
                modifiedText = currentRow["ApprovedText"]?.ToString() ?? string.Empty;
                InfoLabel.Content = "Loaded saved text.";
                EnableDiscard = true;
            }
            UpdateModifiedRichTextBox(modifiedText);

            WordDiff2.DiffFunction(currentRow["CoalescedText"].ToString(), modifiedText, diffRichTextBox);

            modified = false;
            SetStateControls();
            DiscardButton.IsEnabled = EnableDiscard; // Enable or disable the button based on the condition
        }

        private void SetStateControls()
        {
            if (selectedRows != null && currentRow != null)
            {
                 bool isClosed = currentRow["Status"]?.ToString()?.ToUpper() == StatusClose;
                modifiedRichTextBox.IsEnabled = !isClosed;
                StatusLabel.Content = isClosed ? StatusClose : StatusOpen;
                ReOpenButton.IsEnabled = isClosed;
                ApproveButton.IsEnabled = !isClosed;
                RevertButton.IsEnabled = !isClosed;
                SaveButton.IsEnabled = !isClosed && modified;
                UndoButton.IsEnabled = !isClosed && modified;
                DiscardButton.IsEnabled = !isClosed && modified;
                BackButton.IsEnabled = currentIndex > 0;
                ForwardButton.IsEnabled = currentIndex < selectedRows.Count - 1;
               
            }
        }

        private void UpdateModifiedRichTextBox(string? text)
        {
            modifiedRichTextBox.Document.Blocks.Clear();
            modifiedRichTextBox.Document.Blocks.Add(new Paragraph(new Run(text)));
            WordDiff2.DiffFunction(originalTextBox.Text, text, diffRichTextBox);
        }

        private static string? FixComment(string? comment)
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
            if (selectedRows != null && currentIndex < selectedRows.Count - 1)
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

        private async void ReOpen_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("Do you want to re-open approved text?", "Re-open", MessageBoxButton.YesNo) == MessageBoxResult.No)
            {
                return;
            }
            await SetStatus(StatusOpen); 
        }

        private async void Approve_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("Do you want to approve the text?", "Approve", MessageBoxButton.YesNo) == MessageBoxResult.No)
            {
                return;
            }
            await SetStatus(StatusClose);
        }


        private void Discard_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("Do you want to load AI reviewed text?", "Discard", MessageBoxButton.YesNo) == MessageBoxResult.No)
            {
                return;
            }
            if (currentRow != null)
            {
                modifiedText = currentRow["FinalText"]?.ToString() ?? string.Empty;
                InfoLabel.Content = "Loaded AI reviewed text.";
                UpdateModifiedRichTextBox(modifiedText);
                modified = true;
                SetStateControls();
                DiscardButton.IsEnabled = false; // Disable the button after discarding
            }
        }

        private async Task<int> SetStatus(string newStatus)
        {
            int result = 0;
            try
            {
                string updatedText = GetCleanedText();
                string statusLabel = newStatus.Trim().ToLower() == "open" ? StatusOpen : StatusClose;
                Dictionary<string, string?> updates = new();

                string userComment = AskForComment();
                currentComment += $"{statusLabel} {DateTime.Now} {User}: {userComment}";

                updates["ApprovedText"] = updatedText;
                updates["Reviewer"] = User;
                updates["Status"] = newStatus;
                updates["Comment"] = currentComment;
                updates["rowid"] = currentRowId;

                if (db != null)
                {
                    result = await Task.Run(() => db.UpdateTable(updates)); // Wrap synchronous method in Task.Run
                    if (result > 0)
                    {
                        InfoLabel.Content = $"Status updated to {statusLabel}.";

                        logTextBox.Text = FixComment(currentComment);
                        currentStatus = newStatus;
                        modified = false;

                        UpdateCurrentRow();
                        SetStateControls();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error updating status: {ex.Message}");
            }
            return result;
        }

        private async void Save_Click(object sender, RoutedEventArgs e)
        {
            modified = await SaveData() != 1; // Await the Task<int> returned by SaveData
            SetStateControls();
        }


        private void Refresh_Click(object sender, RoutedEventArgs e)
        {
            TextRange textRange = new(modifiedRichTextBox.Document.ContentStart, modifiedRichTextBox.Document.ContentEnd);
            WordDiff2.DiffFunction(originalTextBox.Text, textRange.Text, diffRichTextBox);
        }

        private async Task<int> SaveData()
        {
            int result = 0;
            try
            {
                string updatedText = GetCleanedText();
                string statusLabel = currentStatus?.Trim().ToLower() == "open" ? StatusOpen : StatusClose;
                Dictionary<string, string?> updates = new();

                string userComment = AskForComment();
                currentComment += $"{statusLabel} {DateTime.Now} {User}: {userComment}";

                updates["ApprovedText"] = updatedText;
                updates["Reviewer"] = User;
                updates["Status"] = currentStatus;
                updates["Comment"] = currentComment;
                updates["rowid"] = currentRowId;

                if (db != null)
                {
                    result = await Task.Run(() => db.UpdateTable(updates)); // Wrap synchronous method in Task.Run
                    if (result > 0)
                    {
                        InfoLabel.Content = $"Data saved.";

                        logTextBox.Text = FixComment(currentComment);
                        modified = false;

                        UpdateCurrentRow();
                        SetStateControls();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving: {ex.Message}");
            }
            return result;
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
                Task.Run(async () =>
                {
                    modified = await SaveData() != 1; // Await the Task<int> returned by SaveData
                    Dispatcher.Invoke(SetStateControls); // Ensure UI updates are done on the UI thread
                });
                ClearHighlights(modifiedRichTextBox);
            }
        }

        private static string AskForComment()
        {
            InputDialog inputDialog = new("Enter your comment:");
            return inputDialog.ShowDialog() == true ? inputDialog.Comment : string.Empty;
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
            if (currentRow != null)
            {
                currentRow["Status"] = currentStatus;
                currentRow["ApprovedText"] = GetCleanedText();
                currentRow["Reviewer"] = User;
                currentRow["Comment"] = currentComment;
            }
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

        private void spellCheckBox_Click(object sender, RoutedEventArgs e)
        {
            modifiedRichTextBox.SpellCheck.IsEnabled = spellCheckBox.IsChecked == true;
        }
        private static string GetSelectedText(RichTextBox richTextBox)
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


        private static void ApplyHighlight(TextPointer start, TextPointer end, Brush color)
        {
            TextRange highlightRange = new TextRange(start, end);
            highlightRange.ApplyPropertyValue(TextElement.BackgroundProperty, color);
        }


        private static TextPointer GetTextPointerAtOffset(TextPointer start, int offset)
        {
            TextPointer? current = start;
            int count = 0;

            while (current != null && count < offset)
            {
                if (current.GetPointerContext(LogicalDirection.Forward) == TextPointerContext.Text)
                {
                    int textLength = current.GetTextRunLength(LogicalDirection.Forward);
                    if (count + textLength > offset)
                    {
                        return current.GetPositionAtOffset(offset - count)!; 
                    }
                    count += textLength;
                }
                current = current.GetPositionAtOffset(1, LogicalDirection.Forward);
            }
            return current!; 
        }
        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            saveRow();
        }


    }
}




