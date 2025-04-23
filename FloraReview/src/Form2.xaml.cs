
using System.Data;
using SQLite3DB;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Input;
using System.Windows.Controls;
using Microsoft.Win32; // For SaveFileDialog

namespace FloraReview
{
    public partial class Form2 : Window
    {
        private readonly Dictionary<string, string?> inputData;
        private readonly string? User;
        private int rowCount = 0;
        private int pageIndex = 0;
        private readonly int pageSize = 50;
        private int totalPages = 0;
        private readonly SQLite3db? db;

        public Form2(Dictionary<string, string?> inputdata)
        {
            InitializeComponent();

            if (inputdata == null || !inputdata.TryGetValue("user", out User) || string.IsNullOrEmpty(User))
            {
                throw new ArgumentException("Invalid user name provided.");
            }
            inputData = inputdata;
            db = new SQLite3db(inputData);
            Refresh();
        }


        public async void Refresh()
        {
            try
            {
                if (db == null)
                {
                    throw new InvalidOperationException("Database connection is not initialized.");
                }
                Dispatcher.Invoke(() => Mouse.OverrideCursor = Cursors.Wait);
                rowCount = await db.GetQueryRowCount();
                Dispatcher.Invoke(() => Mouse.OverrideCursor = null);

                totalPages = (int)Math.Ceiling((double)rowCount / pageSize);
                rowCountLabel.Content = $"Rows Returned: {rowCount}";
                LoadPageData();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error refreshing data: {ex.Message}");
            }
        }

        private async void LoadPageData()
        {
            try
            {
                if (db == null)
                {
                    throw new InvalidOperationException("Database connection is not initialized.");
                }
                Dispatcher.Invoke(() => Mouse.OverrideCursor = Cursors.Wait);
                // Use Task.Run to run the database operation on a background thread
                DataTable dataTable = await Task.Run(() => db.GetQueryPageRows(pageSize, pageIndex));
                Dispatcher.Invoke(() => Mouse.OverrideCursor = null);

                if (dataTable != null)
                {
                    dataGrid.ItemsSource = dataTable.DefaultView;
                }
                else
                {
                    MessageBox.Show("No data available to display.");
                }

                pageNumberLabel.Content = $"Page {pageIndex + 1} of {totalPages} pages";
                SetPageButtons();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading page data: {ex.Message}");
            }
        }
        public void SetPageButtons()
        {
            NextPageButton.IsEnabled = pageIndex + 1 < totalPages;
            PreviousPageButton.IsEnabled = pageIndex > 0;
            ExportButton.IsEnabled = dataGrid.Items.Count > 0;
        }

        private void ExportQuery_Click(object sender, RoutedEventArgs e)
        {
            if (db == null)
            {
                MessageBox.Show("Database connection is not initialized.");
                return; // Exit early if db is null
            }

            if (MessageBox.Show($"{rowCount} rows will be exported to a csv file", "Export?", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                SaveFileDialog saveFileDialog = new()
                {
                    Filter = "CSV Files (*.csv)|*.csv",
                    Title = "Export Data to CSV",
                    FileName = "ExportedData.csv"
                };

                if (saveFileDialog.ShowDialog() == true)
                {
                    string filePath = saveFileDialog.FileName;
                    Dispatcher.Invoke(() => Mouse.OverrideCursor = Cursors.Wait);
                    db.ExportQueryRows(filePath);
                    Dispatcher.Invoke(() => Mouse.OverrideCursor = null);
                }
            }
        }


        private static async Task<int> ExportSelectedRows(DataTable dataTable)
        {
            if (MessageBox.Show($"{dataTable.Rows.Count} will be exported to a csv file", "Export?", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
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
                        string filePath = saveFileDialog.FileName;
                        int i = 0;
                        using StreamWriter writer = new(filePath);
                        i = await WriteRows(dataTable, writer);

                        MessageBox.Show($"{i} rows exported to {filePath}");
                        return i;
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error exporting data: {ex.Message}");
                    return 0;
                }
            }
            return 0;
        }

        private static async Task<int> WriteRows(DataTable? dataTable, StreamWriter? writer)
        {
            if (dataTable == null || writer == null)
            {
                throw new ArgumentNullException("DataTable or StreamWriter cannot be null.");
            }

            int rowCount = 0;

            // Use Task.Run to perform the operation asynchronously
            await Task.Run(() =>
            {
                // Write Header Row using LINQ
                string[] columnNames = dataTable.Columns.Cast<DataColumn>()
                    .Select(static x => x.ColumnName)
                    .ToArray();
                writer.WriteLine(string.Join("\t", columnNames));

                // Write Data Rows
                foreach (DataRow row in dataTable.Rows)
                {
                    string[] fields = row.ItemArray
                        .Select(x => x?.ToString() ?? string.Empty)
                        .ToArray();
                    writer.WriteLine(string.Join("\t", fields));
                    rowCount++;
                }
            });

            return rowCount;
        }


        private void PreviousPage_Click(object sender, RoutedEventArgs e)
        {
            if (pageIndex > 0)
            {
                pageIndex--;
                LoadPageData();
            }
        }

        private void NextPage_Click(object sender, RoutedEventArgs e)
        {
            if (pageIndex + 1 < totalPages)
            {
                pageIndex++;
                LoadPageData();
            }
        }


        private void Refresh_Click(object sender, RoutedEventArgs e)
        {
            Refresh();
        }
        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.MainWindow.Show();
            this.Close();
        }

        private void DataGrid_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            //This will first select the clicked row, so only 1 row is selected
            ReviewSelectedRows_Click(sender, e);
        }

        private void DataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ReviewButton.IsEnabled = ExportButton.IsEnabled = exportMenuItem.IsEnabled = reviewMenuItem.IsEnabled = dataGrid.SelectedItems.Count > 0;
        }

        private void SelectAll_Click(object sender, RoutedEventArgs e)
        {
            dataGrid.SelectAll();
        }

        private void ReviewSelectedRows_Click(object sender, RoutedEventArgs e)
        {
            if (dataGrid.SelectedItems.Count > 0)
            {
                List<DataRow> selectedRows = new();
                foreach (var item in dataGrid.SelectedItems)
                {
                    if (item is DataRowView dataRowView)
                    {
                        selectedRows.Add(dataRowView.Row);
                    }
                }

                if (selectedRows.Count > 0)
                {
                    Form3 form3 = new(db, selectedRows, User);
                    form3.Show();
                }
                else
                {
                    MessageBox.Show("No valid rows to review.");
                }
            }
            else
            {
                MessageBox.Show("Please select at least one row to review.");
            }
        }

        private async void ExportSelectedRows_Click(object sender, RoutedEventArgs e)
        {
            Dispatcher.Invoke(() => Mouse.OverrideCursor = Cursors.Wait);
            if (dataGrid.SelectedItems.Count > 0)
            {
                DataTable dataTable = new DataTable();
                if (dataGrid.ItemsSource is DataView dataView && dataView.Table != null)
                {
                    dataTable = dataView.Table.Clone(); // Clone the structure of the original DataTable
                }
                else
                {
                    MessageBox.Show("No valid rows to export.");
                    return; // Exit the method if the data source is invalid
                }

                foreach (var item in dataGrid.SelectedItems)
                {
                    if (item is DataRowView dataRowView)
                    {
                        dataTable.ImportRow(dataRowView.Row); // Import the DataRow into the new DataTable
                    }
                }

                if (dataTable.Rows.Count > 0)
                {
                    await ExportSelectedRows(dataTable); // Add 'await' to ensure the method is awaited
                }
                else
                {
                    MessageBox.Show("No valid rows to export.");
                }
            }
            else
            {
                MessageBox.Show("Please select at least one row to export.");
            }
            Dispatcher.Invoke(() => Mouse.OverrideCursor = null);
        }
        private void Window_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if ( e.Key == System.Windows.Input.Key.Right)
            {
                // Call NextPage_Click when Right Arrow is pressed
                NextPage_Click(sender, e);
            }
            else if ( e.Key == System.Windows.Input.Key.Left )
            {
                // Call PreviousPage_Click when Left Arrow is pressed
                PreviousPage_Click(sender, e);
            }
        }

    }
}


