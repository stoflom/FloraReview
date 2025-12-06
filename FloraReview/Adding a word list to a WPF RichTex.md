# Adding a word list to a WPF `RichTextBox`

Adding a word list to a WPF `RichTextBox` for lexical analysis or spell-checking can be achieved by customizing the control and integrating a dictionary or word list. Below is a concise, formatted guide with example code.

## Step 1: Create a Word List

Create a list of words that you want to use for lexical analysis or spell-checking. This can be stored in a text file or directly in code.

```csharp
List<string> wordList = new List<string> { "example", "word", "list", "for", "richtextbox" };
```

## Step 2: Customize the `RichTextBox`

Extend the `RichTextBox` to include a method for checking words against your word list. The example below highlights words not found in the list.

```csharp
public class CustomRichTextBox : RichTextBox
{
    private List<string> _wordList;

    public CustomRichTextBox()
    {
        _wordList = new List<string> { "example", "word", "list", "for", "richtextbox" };
    }

    public void CheckWords()
    {
        TextRange textRange = new TextRange(this.Document.ContentStart, this.Document.ContentEnd);
        string[] words = textRange.Text.Split(new char[] { ' ', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

        foreach (string word in words)
        {
            if (!_wordList.Contains(word))
            {
                HighlightWord(word);
            }
        }
    }

    private void HighlightWord(string word)
    {
        TextRange textRange = new TextRange(this.Document.ContentStart, this.Document.ContentEnd);
        int index = textRange.Text.IndexOf(word);

        while (index != -1)
        {
            TextPointer start = GetTextPositionAtOffset(this.Document.ContentStart, index);
            TextPointer end = GetTextPositionAtOffset(start, word.Length);
            TextRange wordRange = new TextRange(start, end);

            wordRange.ApplyPropertyValue(TextElement.BackgroundProperty, Brushes.Yellow);

            index = textRange.Text.IndexOf(word, index + word.Length);
        }
    }

    private TextPointer GetTextPositionAtOffset(TextPointer start, int offset)
    {
        TextPointer current = start;
        int count = 0;

        while (current != null && count < offset)
        {
            if (current.GetPointerContext(LogicalDirection.Forward) == TextPointerContext.Text)
            {
                int runLength = current.GetTextRunLength(LogicalDirection.Forward);
                if (count + runLength > offset)
                {
                    return current.GetPositionAtOffset(offset - count);
                }
                count += runLength;
            }
            current = current.GetNextContextPosition(LogicalDirection.Forward);
        }

        return current;
    }
}
```

## Step 3: Use the `CustomRichTextBox` in XAML

Add the custom control to your window XAML and a button to trigger the check.

```xml
<Window x:Class="YourNamespace.MainWindow"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        Title="MainWindow" Height="350" Width="525">
    <Grid>
        <local:CustomRichTextBox x:Name="customRichTextBox" VerticalAlignment="Stretch" HorizontalAlignment="Stretch"/>
        <Button Content="Check Words" VerticalAlignment="Bottom" Click="CheckWords_Click"/>
    </Grid>
</Window>
```

## Step 4: Handle the Button Click Event

Trigger the word check from your code-behind.

```csharp
private void CheckWords_Click(object sender, RoutedEventArgs e)
{
    customRichTextBox.CheckWords();
}
```

## Notes

- The example highlights words that are not in the word list by changing their background to yellow. You can customize `HighlightWord` to change color, underline, or add adorner-based squiggles.
- For larger dictionaries or better performance consider using a `HashSet<string>` for lookups and running checks on a background thread.