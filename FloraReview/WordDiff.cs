using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Windows.Media; // Use System.Windows.Media for colors in WPF
using System.Windows.Controls; // Use System.Windows.Controls for WPF RichTextBox
using System.Windows.Documents; // Use System.Windows.Documents for TextRange and TextElement

public class WordDiff2
{
    private const int MatchMax = 5; // Maximum distance of words to search for a match
    private static readonly Regex WordSplitter = new Regex(@"\s+"); // Split on spaces

    private enum Operation
    {
        Same,
        Insert,
        Delete
    }

    public static void DiffFunction(string? originalText, string? modifiedText, RichTextBox richTextBox)
    {
        if (originalText != string.Empty && modifiedText != string.Empty)
        {
            if (richTextBox == null) throw new ArgumentNullException(nameof(richTextBox));

            List<string> originalList = new List<string>(WordSplitter.Split(originalText ?? string.Empty));
            List<string> modifiedList = new List<string>(WordSplitter.Split(modifiedText ?? string.Empty));

            richTextBox.Document.Blocks.Clear(); // Clear the RichTextBox before adding new text

            Operation lastOperation = Operation.Same;

            while (originalList.Count > 0 || modifiedList.Count > 0)
            {
                string? oCurrent = originalList.Count > 0 ? originalList[0] : null;
                string? mCurrent = modifiedList.Count > 0 ? modifiedList[0] : null;

                if (oCurrent == mCurrent)
                {
                    AppendText(richTextBox, oCurrent, Colors.Black, true);
                    originalList.RemoveAt(0);
                    modifiedList.RemoveAt(0);
                    lastOperation = Operation.Same;
                }
                else
                {
                    int matchIndex = FindMatchIndex(modifiedList, oCurrent);
                    if (matchIndex > -1)
                    {
                        InsertWords(richTextBox, modifiedList, matchIndex, Colors.Green, true);
                        lastOperation = Operation.Insert;
                    }
                    else
                    {
                        matchIndex = FindMatchIndex(originalList, mCurrent);
                        if (matchIndex > -1)
                        {
                            DeleteWords(richTextBox, originalList, matchIndex, Colors.Red, true);
                            lastOperation = Operation.Delete;
                        }
                        else
                        {
                            if (oCurrent != null)
                            {
                                AppendText(richTextBox, oCurrent, Colors.Red, lastOperation == Operation.Insert);
                                originalList.RemoveAt(0);
                                lastOperation = Operation.Delete;
                            }
                            if (mCurrent != null)
                            {
                                AppendText(richTextBox, mCurrent, Colors.Green, lastOperation == Operation.Delete);
                                modifiedList.RemoveAt(0);
                                lastOperation = Operation.Insert;
                            }
                        }
                    }
                }
            }
        }
    }

    private static int FindMatchIndex(List<string> list, string? word)
    {
        for (int i = 1; i < MatchMax && i < list.Count; i++)
        {
            if (list[i] == word)
            {
                return i;
            }
        }
        return -1;
    }

    private static void InsertWords(RichTextBox richTextBox, List<string> list, int count, Color color, bool addSpace)
    {
        for (int i = 0; i < count; i++)
        {
            AppendText(richTextBox, list[0], color, addSpace);
            list.RemoveAt(0);
        }
    }

    private static void DeleteWords(RichTextBox richTextBox, List<string> list, int count, Color color, bool addSpace)
    {
        for (int i = 0; i < count; i++)
        {
            AppendText(richTextBox, list[0], color, addSpace);
            list.RemoveAt(0);
        }
    }

    private static void AppendText(RichTextBox richTextBox, string? text, Color color, bool addSpace)
    {
        if (text != null)
        {
            var textRange = new TextRange(richTextBox.Document.ContentEnd, richTextBox.Document.ContentEnd)
            {
                Text = text + (addSpace ? " " : "")
            };
            textRange.ApplyPropertyValue(TextElement.ForegroundProperty, new SolidColorBrush(color));
        }
    }
}







