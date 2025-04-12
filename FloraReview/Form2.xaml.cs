using System;
using System.Collections.ObjectModel;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using System.Data.SQLite;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using FloraReview;
using Microsoft.Win32; // For SaveFileDialog

namespace FloraReview
{
    public partial class Form2 : Window
    {
        private readonly string? dbPath;
        private readonly string? User;

        private int rowCount = 0;
        private int pageIndex = 0;
        private readonly int pageSize = 50;
        private int totalPages = 0;

        private readonly string queryFrom = $"rowid, Id, CalcFullName, TextTitle, CoalescedText, FinalText, Reviewer, Status, Comment, ApprovedText FROM descriptions ";
        private string queryWhere;
        private readonly string? queryName;
        private readonly string[] textTitles;

        public Form2(Dictionary<string, string?> inputData)
        {
            InitializeComponent();

            if (inputData == null || !inputData.TryGetValue("dbPath", out dbPath) || string.IsNullOrEmpty(dbPath))
            {
                throw new ArgumentException("Invalid database path provided.");
            }
            if (inputData == null || !inputData.TryGetValue("user", out User) || string.IsNullOrEmpty(User))
            {
                throw new ArgumentException("Invalid user name provided.");
            }
            if (inputData == null || !inputData.TryGetValue("queryName", out queryName) || string.IsNullOrEmpty(queryName))
            {
                queryName = string.Empty;
            }
         
            if (inputData == null || 
                !inputData.TryGetValue("textTitle", out string? textTitle) ||
                string.IsNullOrEmpty(textTitle) ||
                textTitle.Length < 1)
            {
                this.textTitles = Array.Empty<string>();
            }
            else 
            {
                this.textTitles = getTextTitles(textTitle);
            }
           
              
            this.queryWhere = ConstructQueryWhere();
            Refresh();
        }

       
        public void Refresh()
        {
            CalculateTotalPages();
            LoadPageData();
        }
        private string[] getTextTitles(string textTitle)
        {
            string[] FixTextTitles = textTitle.Split(',');
            // Trim spaces from the text titles
            FixTextTitles = FixTextTitles.Select(item => item.Trim()).ToArray();
            return FixTextTitles;
        }

        private string ConstructQueryWhere()
        {
            StringBuilder queryWhereBuilder = new();
            if (textTitles.Length > 0)
            {
                string placeholders = string.Join(",", System.Linq.Enumerable.Repeat("?", textTitles.Length));
                queryWhereBuilder.Append($"TextTitle IN ({placeholders})");
            }
            if (!string.IsNullOrEmpty(queryName))
            {
                if (queryWhereBuilder.Length > 0)
                {
                    queryWhereBuilder.Append(" AND ");
                }
                queryWhereBuilder.Append($"CalcFullName LIKE @queryName");
            }
            if (queryWhereBuilder.Length > 0)
            {
                queryWhereBuilder.Insert(0, "WHERE ");
            }
            return queryWhereBuilder.ToString();
        }

        private void LoadPageData()
        {
            try
            {
                using (SQLiteConnection conn = new($"Data Source={dbPath};Version=3;"))
                {
                    conn.Open();
                    string query = $"SELECT {queryFrom} {queryWhere} LIMIT {pageSize} OFFSET {pageIndex * pageSize}";
                    using (SQLiteDataAdapter adapter = new(query, conn))
                    {
                        AddParameters(adapter.SelectCommand);
                        DataTable dataTable = new();
                        adapter.Fill(dataTable);
                        dataGrid.ItemsSource = dataTable.DefaultView;
                        pageNumberLabel.Content = $"Page {pageIndex + 1} of {totalPages} pages";
                        SetPageButtons();
                    }
                }
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
        }


        private void CalculateTotalPages()
        {
            try
            {
                using (SQLiteConnection conn = new($"Data Source={dbPath};Version=3;"))
                {
                    conn.Open();

                    string query = $"SELECT COUNT(*) FROM descriptions {queryWhere}";

                    using (SQLiteCommand cmd = new(query, conn))
                    {
                        AddParameters(cmd);
                        var result = cmd.ExecuteScalar();
                        if (result != null && int.TryParse(result.ToString(), out int totalRows))
                        {
                            rowCount = totalRows;
                            totalPages = (int)Math.Ceiling((double)totalRows / pageSize);

                            rowCountLabel.Content = $"Rows Returned: {rowCount}";
                        }
                        else
                        {
                            MessageBox.Show("Query did not return a valid number.");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error calculating total pages: {ex.Message}");
            }
        }

        private void AddParameters(SQLiteCommand cmd)
        {
            if (textTitles.Length > 0)
            {
                for (int i = 0; i < textTitles.Length; i++)
                {
                    cmd.Parameters.AddWithValue($"@p{i}", textTitles[i]);
                }
            }

            if (!string.IsNullOrEmpty(queryName))
            {
                cmd.Parameters.AddWithValue("@queryName", $"%{queryName}%");
            }
        }

        private void ExportCsv_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                using (SQLiteConnection conn = new($"Data Source={dbPath};Version=3;"))
                {

                    string query = $"SELECT {queryFrom} {queryWhere}";
                    using (SQLiteDataAdapter adapter = new(query, conn))
                    {
                        AddParameters(adapter.SelectCommand);
                        DataTable dataTable = new();
                        adapter.Fill(dataTable);
                        exportRows(dataTable);
                    }

                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading page data: {ex.Message}");
            }
        }


        private int exportRows(DataTable dataTable)
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
                        {
                            // Write Header Row usimg LINQ
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
                                i++;
                            }
                        }

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

        private void dataGrid_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            Review_Click(sender, e);
        }

        private void dataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            exportMenuItem.IsEnabled = reviewMenuItem.IsEnabled = dataGrid.SelectedItems.Count > 0;
        }

        private void SelectAll_Click(object sender, RoutedEventArgs e)
        {
            dataGrid.SelectAll();
        }

        private void Review_Click(object sender, RoutedEventArgs e)
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
                    Form3 form3 = new(dbPath, selectedRows, User);
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

        private void Export_Click(object sender, RoutedEventArgs e)
        {
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
                    exportRows(dataTable);
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

        }
    }
}


