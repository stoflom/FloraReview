using System.Text;
using System.Data;
using System.Data.Common;
using System.Data.SQLite;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;


namespace SQLite3DB
{
    //this class assumes we are working with the SQLite database schema schema.sql
    public class SQLite3db
    {
        private SQLiteConnection? connection;
        private const string tableName = "descriptions";
        private readonly string? dbPath = string.Empty;
        private readonly Dictionary<string, string>? inputData;

        public SQLite3db(string? adbPath)
        {
            if (!string.IsNullOrEmpty(adbPath) && adbPath.Length > 0)
                try
                {
                    dbPath = adbPath ?? string.Empty;
                    connection = new SQLiteConnection($"Data Source={dbPath};Version=3;");
                    connection.Open();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error opening database: {ex.Message}");
                }
        }

        public SQLite3db(Dictionary<string, string?> inputdata)
        {
            if (inputdata == null || !inputdata.TryGetValue("dbPath", out dbPath) || string.IsNullOrEmpty(dbPath))
            {
                throw new ArgumentException("Invalid database path provided.");
            }
            inputData = inputdata;
            try
            {
                connection = new SQLiteConnection($"Data Source={dbPath};Version=3;");
                connection.Open();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error opening database: {ex.Message}");
            }
        }

        private string[]? fixTextTitles(string? textTitle)
        {
            string[]? FixTextTitles = Array.Empty<string>();
            if (textTitle != null && textTitle.Length > 0)
            {
                FixTextTitles = textTitle.Split(',');
                // Trim spaces from the text titles
                FixTextTitles = FixTextTitles.Select(item => item.Trim()).ToArray();
            }
            return FixTextTitles;
        }

        public void ExportFullTable(string filePath)
        {
            if (connection != null && connection.State == ConnectionState.Open)
            {
                try
                {
                    using SQLiteCommand cmd = new(@"SELECT * from {tableName} ", connection);
                    using SQLiteDataReader reader = cmd.ExecuteReader();
                    using StreamWriter writer = new(filePath);
                    {
                        // Write Header Row
                        ReadOnlyCollection<DbColumn> schema = reader.GetColumnSchema();
                        // Write each field in the row, separated by tabs
                        StringBuilder header = new();
                        for (int i = 0; i < schema.Count; i++)
                        {
                            header.Append(schema[i].ColumnName);
                            if (i < schema.Count - 1)
                            {
                                header.Append("\t");
                            }

                        }
                        writer.WriteLine(header.ToString());
                    }
                    {
                        //Write Data Rows
                        while (reader.Read())
                        {
                            // Write each field in the row, separated by tabs
                            StringBuilder stringBuilder = new();
                            for (int i = 0; i < reader.FieldCount; i++)
                            {
                                stringBuilder.Append(reader[i].ToString());
                                if (i < reader.FieldCount - 1)
                                    stringBuilder.Append("\t");
                            }
                            writer.WriteLine(stringBuilder.ToString());
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error exporting data: {ex.Message}");
                }
            }
            else
            {
                MessageBox.Show("Error: database connection is not open.");
            }
        }

        public int UpdateTable(Dictionary<string, string?> inputdata)
        {
            if (connection != null && connection.State == ConnectionState.Open && inputdata != null)
            { 
                try
                {
                    string update = @"UPDATE {tableName} SET {ConstructUpdate(inputdata)}";
                    using SQLiteCommand cmd = new(update, connection);
                    AddUpdateParameters(cmd, inputdata);
                    int rowsAffected = cmd.ExecuteNonQuery();
                    return rowsAffected;
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error updating data: {ex.Message}");
                }
            }
            else
            {
                MessageBox.Show("Error: database connection/query broken.");
            }   

            return 0;
        }

        private string ConstructUpdate(Dictionary<string, string>? inputdata)
        {
            if (inputdata == null)
            {
                throw new ArgumentNullException(nameof(inputdata), "Input data cannot be null.");
            }
            StringBuilder updateBuilder = new();
            foreach (var item in inputdata)
            {
                if (item.Key != "textTitle")
                {
                    updateBuilder.Append($"{item.Key} = @{item.Key}, ");
                }
            }
            if (updateBuilder.Length > 0)
            {
                updateBuilder.Remove(updateBuilder.Length - 2, 2); // Remove the last comma and space
            }
            return updateBuilder.ToString();
        }

        private void AddUpdateParameters(SQLiteCommand cmd, Dictionary<string, string?> inputdata)
        {
            if (inputdata == null)
            {
                throw new ArgumentNullException(nameof(inputdata), "Input data cannot be null.");
            }
            foreach (var item in inputdata)
            {
                if (item.Key != "textTitle")
                {
                    cmd.Parameters.AddWithValue($"@{item.Key}", item.Value);
                }
            }
        }

        public DataTable GetAllQueryRows()
        {
            DataTable datatable = new();
            string query = @"SELECT * FROM {tableName} WHERE {ConstructQueryWhere()}";
            if (connection != null && connection.State == ConnectionState.Open)
            {
                try
                {
                    using SQLiteDataAdapter adapter = new(query, connection);
                    AddParameters(adapter.SelectCommand);
                    adapter.Fill(datatable);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error retrieving data: {ex.Message}");
                }
            }
            else
            {
                MessageBox.Show("Error: database connection/query broken.");
            }
            return datatable;
        }

        public int GetQueryRowCount()
        {
            string query = @"SELECT COUNT(*) FROM {tableName} WHERE {ConstructQueryWhere()}";
            if (connection != null && connection.State == ConnectionState.Open)
            {
                using (SQLiteCommand cmd = new(query, connection))
                {
                    AddParameters(cmd);
                    int totalRows = 0;
                    var result = cmd.ExecuteScalar();
                    if (result != null && int.TryParse(result.ToString(), out totalRows))
                    {
                        return totalRows;
                    }
                    else
                    {
                        MessageBox.Show($"Query did not return a valid number: {result}");
                    }
                }
            }
            else
            {
                MessageBox.Show("Error: database connection/query broken.");
            }
            return 0;
        }

        public void ExportQueryRows(string filePath)
        {
            string query = @"SELECT * FROM {tableName} WHERE {ConstructQueryWhere()}";
            if (connection != null && connection.State == ConnectionState.Open)
            {
                try
                {
                    using SQLiteCommand cmd = new(query, connection);
                    AddParameters(cmd);
                    {
                        using SQLiteDataReader reader = cmd.ExecuteReader();
                        using StreamWriter writer = new(filePath);
                        {
                            // Write Header Row
                            ReadOnlyCollection<DbColumn> schema = reader.GetColumnSchema();
                            // Write each field in the row, separated by tabs
                            StringBuilder header = new();
                            for (int i = 0; i < schema.Count; i++)
                            {
                                header.Append(schema[i].ColumnName);
                                if (i < schema.Count - 1)
                                {
                                    header.Append("\t");
                                }

                            }
                            writer.WriteLine(header.ToString());

                            //Write Data Rows
                            while (reader.Read())
                            {
                                // Write each field in the row, separated by tabs
                                StringBuilder stringBuilder = new();
                                for (int i = 0; i < reader.FieldCount; i++)
                                {
                                    stringBuilder.Append(reader[i].ToString());
                                    if (i < reader.FieldCount - 1)
                                        stringBuilder.Append("\t");
                                }
                                writer.WriteLine(stringBuilder.ToString());
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error exporting data: {ex.Message}");
                }
            }
            else
            {
                MessageBox.Show("Error: database connection/query broken.");
            }

        }


        public DataTable GetQueryPageRows(int pageSize, int pageIndex)
        {
            DataTable dataTable = new();
            if (connection != null && connection.State == ConnectionState.Open)
            {
                try
                {
                    string query = @"SELECT * FROM {tableName} WHERE {ConstructQueryWhere()}"
                        + $" LIMIT {pageSize} OFFSET {pageIndex * pageSize}";
                    using (SQLiteDataAdapter adapter = new(query, connection))
                    {
                        AddParameters(adapter.SelectCommand);
                        adapter.Fill(dataTable);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error loading page data: {ex.Message}");
                }
            }
            else
            {
                MessageBox.Show("Error: database connection/query broken.");
            }
            return dataTable;
        }


        private string ConstructQueryWhere()
        {
            string? textTitles = string.Empty;
            string? queryName = string.Empty;
            StringBuilder queryWhereBuilder = new();
            if (inputData != null && !inputData.TryGetValue("textTitles", out textTitles) && string.IsNullOrEmpty(textTitles))
            {

                if (textTitles != null && textTitles.Length > 0)
                {
                    string placeholders = string.Join(",", System.Linq.Enumerable.Repeat("?", textTitles.Length));
                    queryWhereBuilder.Append($"TextTitle IN ({placeholders})");
                }
            }
            if (inputData != null && !inputData.TryGetValue("queryName", out queryName) && string.IsNullOrEmpty(queryName))
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
        private void AddParameters(SQLiteCommand cmd)
        {
            if (inputData == null)
            {
                throw new ArgumentNullException(nameof(inputData), "Input data cannot be null.");
            }
            string[]? textTitlesArray = fixTextTitles(inputData["textTitles"]);
            if (textTitlesArray != null && textTitlesArray.Length > 0)
            {
                for (int i = 0; i < textTitlesArray.Length; i++)
                {
                    cmd.Parameters.AddWithValue($"@p{i}", textTitlesArray[i]);
                }
            }
            string? queryName = inputData["queryName"];
            if (!string.IsNullOrEmpty(queryName))
            {
                cmd.Parameters.AddWithValue("@queryName", $"%{queryName}%");
            }

        }

        public void Dispose()
        {
            if (connection != null && connection.State == ConnectionState.Open)
            {
                connection.Close();
                connection.Dispose();
            }
        }

        ~SQLite3db()
        {
            Dispose();
        }

    }
}
