using System.IO;
using System.Text; // For StringBuilder
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using System.Text.Json;
using SQLite3DB;
using System.Data.Common;
using System.Collections.ObjectModel;
using System.Text.RegularExpressions;
using System.Reflection;

namespace FloraReview
{
    public partial class MainWindow : Window
    {
        private readonly string appName = "FloraReview";
        private readonly string appDataPath;
        private Dictionary<string, string?> inputData = [];
        private const string inputDataFileName = "inputData.json";
        private string? version = string.Empty;
        public MainWindow()
        {
            InitializeComponent();

            version = Assembly.GetExecutingAssembly()?.GetName()?.Version?.ToString() ?? string.Empty;
            AppVersionLabel.Content = $"Version: {version}";
            UserTextBox.Text = Environment.UserName;
            inputData["user"] = UserTextBox.Text;
            inputData["dbPath"] = string.Empty;
            inputData["queryName"] = string.Empty;
            inputData["textTitle"] = string.Empty;

            appDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), appName);

            if (!Directory.Exists(appDataPath))
            {
                Directory.CreateDirectory(appDataPath);
            }

            LoadInputData();
        }

        private void LoadInputData()
        {
            string dataFile = Path.Combine(appDataPath, inputDataFileName);
            if (File.Exists(dataFile))
            {
                try
                {
                    string json = File.ReadAllText(dataFile);
                    inputData = JsonSerializer.Deserialize<Dictionary<string, string?>>(json) ?? new Dictionary<string, string?>();
                    if (inputData.TryGetValue("user", out string? user))
                    {
                        UserTextBox.Text = user;
                    }
                    if (inputData.TryGetValue("dbPath", out string? dbPath))
                    {
                        dbPathTextBox.Text = dbPath;
                    }
                    if (inputData.TryGetValue("queryName", out string? queryName))
                    {
                        queryNameTextBox.Text = queryName;
                    }
                    if (inputData.TryGetValue("textTitle", out string? textTitle) && textTitle != null)
                    {
                        {
                            foreach (ListBoxItem item in textTitleListBox.Items)
                            {
                                string content = item.Content.ToString() ?? string.Empty;
                                item.IsSelected = Regex.IsMatch(textTitle, @"\b" + Regex.Escape(content) + @"\b");
                            }
                        }
                    }
                }
                catch (IOException ex)
                {
                    MessageBox.Show($"Error loading input data: {ex.Message}");
                }
            }
        }


        private Boolean SaveInputData()
        {
            string inputDataPath = Path.Combine(appDataPath, inputDataFileName);

            if (!File.Exists(dbPathTextBox.Text))
            {
                MessageBox.Show($"Error with database {dbPathTextBox.Text}");
                return false;
            }
            else if (string.IsNullOrEmpty(UserTextBox.Text))
            {
                MessageBox.Show($"Please provide a user name");
                return false;
            }
            else
            {

                try
                {
                    inputData["dbPath"] = dbPathTextBox.Text;
                    inputData["user"] = UserTextBox.Text;
                    inputData["queryName"] = queryNameTextBox.Text;
                    inputData["textTitle"] = getTextTitles();
                    string json = JsonSerializer.Serialize(inputData);
                    File.WriteAllText(inputDataPath, json);
                    return true;
                }
                catch (IOException ex)
                {
                    MessageBox.Show($"Error saving input data: {ex.Message}");
                    return false;
                }
            }
        }

        private void SelectFile_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new()
            {
                Filter = "SQLite Database (*.db)|*.db"
            };
            if (openFileDialog.ShowDialog() == true)
            {
                dbPathTextBox.Text = openFileDialog.FileName;
            }

            setLoadExportButtons();
        }

        private void LoadData_Click(object sender, RoutedEventArgs e)
        {
            if (SaveInputData())
            {
                Form2 form2 = new(inputData);
                form2.ShowDialog();
            }
        }

        private string? getTextTitles()
        {
            StringBuilder textTitleBuilder = new();
            string textTitle = string.Empty;
            if (textTitleListBox.SelectedItems.Count != 0)
            {

                foreach (ListBoxItem item in textTitleListBox.SelectedItems)
                {
                    textTitleBuilder.Append(item.Content).Append(",");
                }
                textTitle = textTitleBuilder.ToString().TrimEnd(',');
            }
            return textTitle;
        }

        private void QuitApp_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void dbPathTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            inputData["dbPath"] = dbPathTextBox.Text;
            setLoadExportButtons();
        }

        private void User_TextChanged(object sender, TextChangedEventArgs e)
        {
            inputData["user"] = UserTextBox.Text;
            setLoadExportButtons();
        }

        private void setLoadExportButtons()
        {
            string? dbPath;
            string? User;

            if (LoadButton != null && ExportButton != null)
            {
                if (inputData != null && inputData.TryGetValue("dbPath", out dbPath) && !string.IsNullOrEmpty(dbPath)
                && inputData.TryGetValue("user", out User) && !string.IsNullOrEmpty(User) && File.Exists(dbPath))
                {
                    LoadButton.IsEnabled = true;
                    ExportButton.IsEnabled = true;
                    StatusTextLabel.Content = "Define your query and click <Load Data>.";
                }
                else
                {
                    LoadButton.IsEnabled = false;
                    ExportButton.IsEnabled = false;
                    StatusTextLabel.Content = "Please give your name, select a valid database.";
                }
            }
        }



        private async void ExportData_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("This will export the full descriptions table.", "Export Data?", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                StatusTextLabel.Content = "Exporting database, please wait...";
                try
                {
                    SaveFileDialog saveFileDialog = new()
                    {
                        Filter = "CSV Files (*.csv)|*.csv",
                        Title = "Export Data to CSV",
                        FileName = "ExportedData.csv"
                    };

                    if (saveFileDialog.ShowDialog() == true)
                    {
                        if (inputData.TryGetValue("dbPath", out string? connectionString) && !string.IsNullOrEmpty(connectionString))
                        {
                            string filePath = saveFileDialog.FileName;
                            SQLite3db db = new(connectionString);
                            await Task.Run(() => db.ExportFullTable(filePath));
                            db.Dispose();
                            MessageBox.Show($"Data successfully exported to {filePath}");
                        }
                        else
                        {
                            MessageBox.Show("Database path is invalid or not set.");
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error exporting data: {ex.Message}");
                }
                StatusTextLabel.Content = "Define your query and click <Load Data>.";
            }
        }
    }

}
