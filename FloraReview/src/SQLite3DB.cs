using System.Text;
using System.Data;
using System.Data.Common;
using System.Data.SQLite;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Diagnostics;


namespace SQLite3DB
{
    //this class assumes we are working with the SQLite database schema schema.sql
    public class SQLite3db : IDisposable
    {
        private SQLiteConnection? connection;
        private const string tableName = "descriptions";
        private readonly string? dbPath = string.Empty;
        private readonly Dictionary<string, string?>? inputData;

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
                    Debug.WriteLine($"Error opening database: {ex.Message}");
                    throw; // Re-throw the exception for higher-level handling
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
                Debug.WriteLine($"Error opening database: {ex.Message}");
                throw; // Re-throw the exception for higher-level handling
            }
        }

        private static string[]? FixTextTitles(string? textTitle)
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

        private static string[]? FixStatuss(string? textStatus)
        {
            string[]? fixStatuss = Array.Empty<string>();
            if (textStatus != null && textStatus.Length > 0)
            {
                fixStatuss = textStatus.Split(',');
                // Trim spaces from the textStatus
                fixStatuss = fixStatuss.Select(item => item.Trim()).ToArray();
            }
            return fixStatuss;
        }

        public void ExportFullTable(string filePath)
        {
            if (connection != null && connection.State == ConnectionState.Open)
            {
                try
                {
                    using SQLiteCommand cmd = new($"SELECT * from {tableName} ", connection);
                    using SQLiteDataReader reader = cmd.ExecuteReader();
                    using StreamWriter writer = new(filePath);
                    WriteDataToFile(reader, writer);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error exporting data: {ex.Message}");
                    throw; // Re-throw the exception for higher-level handling
                }
            }
            else
            {
                Debug.WriteLine("Error: database connection is not open.");
            }
        }

        private static void WriteDataToFile(SQLiteDataReader reader, StreamWriter writer)
        {
            // Write Header Row
            ReadOnlyCollection<DbColumn> schema = reader.GetColumnSchema();
            StringBuilder header = new();
            for (int i = 0; i < schema.Count; i++)
            {
                header.Append(schema[i].ColumnName);
                if (i < schema.Count - 1)
                {
                    header.Append('\t');
                }
            }
            writer.WriteLine(header.ToString());

            // Write Data Rows
            while (reader.Read())
            {
                StringBuilder stringBuilder = new();
                for (int i = 0; i < reader.FieldCount; i++)
                {
                    stringBuilder.Append(reader[i].ToString());
                    if (i < reader.FieldCount - 1)
                        stringBuilder.Append('\t');
                }
                writer.WriteLine(stringBuilder.ToString());
            }
        }


        public async Task<int> UpdateTable(Dictionary<string, string?> inputdata)
        {
            if (connection != null && connection.State == ConnectionState.Open && inputdata != null)
            {
                try
                {
                    string update = $"UPDATE {tableName} SET {ConstructUpdate(inputdata)}";
                    using SQLiteCommand cmd = new(update, connection);
                    AddUpdateParameters(cmd, inputdata);
                    int rowsAffected = await cmd.ExecuteNonQueryAsync();  
                    return rowsAffected;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error updating data: {ex.Message}");
                    throw; // Re-throw the exception for higher-level handling  
                }
            }
            else
            {
                Debug.WriteLine("Error: database connection/query broken.");
            }

            return 0;
        }

        private static string ConstructUpdate(Dictionary<string, string?>? inputdata)
        {
            if (inputdata == null)
            {
                throw new ArgumentNullException(nameof(inputdata), "Input data cannot be null.");
            }
            StringBuilder updateBuilder = new();
            foreach (var item in inputdata)
            {
                if (item.Key == "rowid")
                {
                    continue; // Skip the textTitle field
                }
                updateBuilder.Append($"{item.Key} = @{item.Key}, ");
            }
            if (updateBuilder.Length > 0)
            {
                updateBuilder.Remove(updateBuilder.Length - 2, 2); // Remove the last comma and space
            }
            if (inputdata.TryGetValue("rowid", out string? rowid))
            {
                updateBuilder.Append($" WHERE rowid = {@rowid}");
            }
            else
            {
                throw new ArgumentException("rowid is required for the update operation.");
            }
            return updateBuilder.ToString();
        }

        private static void AddUpdateParameters(SQLiteCommand cmd, Dictionary<string, string?> inputdata)
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

        public async Task<DataTable> GetAllQueryRows()
        {
            DataTable datatable = new();
            string query = $"SELECT rowid,* FROM {tableName} {ConstructQueryWhere()}";
            if (connection != null && connection.State == ConnectionState.Open)
            {
                try
                {
                    using SQLiteDataAdapter adapter = new(query, connection);
                    AddParameters(adapter.SelectCommand);
                    await Task.Run(() => adapter.Fill(datatable));
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error retrieving data: {ex.Message}");
                    throw; // Re-throw the exception for higher-level handling
                }
            }
            else
            {
                Debug.WriteLine("Error: database connection/query broken.");
            }
            return datatable;
        }

        public async Task<int> GetQueryRowCount()
        {
            string query = $"SELECT COUNT(*) FROM {tableName} {ConstructQueryWhere()}";
            if (connection != null && connection.State == ConnectionState.Open)
            {
                using (SQLiteCommand cmd = new(query, connection))
                {
                    AddParameters(cmd);
                    int totalRows = 0;
                    object? result = await Task.Run(() => cmd.ExecuteScalar());
                    if (result != null && int.TryParse(result.ToString(), out totalRows))
                    {
                        return totalRows;
                    }
                    else
                    {
                        Debug.WriteLine($"Query did not return a valid number: {result}");
                    }
                }
            }
            else
            {
                Debug.WriteLine("Error: database connection/query broken.");
            }
            return 0;
        }

        public void ExportQueryRows(string filePath)
        {
            string query = $"SELECT rowid,* FROM {tableName} {ConstructQueryWhere()}";
            if (connection != null && connection.State == ConnectionState.Open)
            {
                try
                {
                    using SQLiteCommand cmd = new(query, connection);
                    AddParameters(cmd);
                    {
                        using SQLiteDataReader reader = cmd.ExecuteReader();
                        using StreamWriter writer = new(filePath);
                        WriteDataToFile(reader, writer);
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error exporting data: {ex.Message}");
                    throw; // Re-throw the exception for higher-level handling
                }
            }
            else
            {
                Debug.WriteLine("Error: database connection/query broken.");
            }

        }


        public DataTable GetQueryPageRows(int pageSize, int pageIndex)
        {
            DataTable dataTable = new();
            if (connection != null && connection.State == ConnectionState.Open)
            {
                try
                {
                    string query = $"SELECT rowid,* FROM {tableName} {ConstructQueryWhere()}"
                        + $" LIMIT {pageSize} OFFSET {pageIndex * pageSize}";
                    using (SQLiteDataAdapter adapter = new(query, connection))
                    {
                        AddParameters(adapter.SelectCommand);
                        adapter.Fill(dataTable);
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error loading page data: {ex.Message}");
                    throw; // Re-throw the exception for higher-level handling
                }
            }
            else
            {
                Debug.WriteLine("Error: database connection/query broken.");
            }
            return dataTable;
        }


        private string ConstructQueryWhere()
        {
            string? textTitles = string.Empty;
            string? queryName = string.Empty;
            string? textStatus = string.Empty;
            StringBuilder queryWhereBuilder = new();
            if (inputData != null && inputData.TryGetValue("textTitle", out textTitles) && !string.IsNullOrEmpty(textTitles))
            {
                if (textTitles != null && textTitles.Length > 0) // Ensure textTitles is not null
                {
                    string[]? textTitlesArray = FixTextTitles(textTitles);
                    if (textTitlesArray != null) // Add null check for textTitlesArray
                    {
                        string placeholders = string.Join(",", System.Linq.Enumerable.Repeat("?", textTitlesArray.Length));
                        queryWhereBuilder.Append($"TextTitle IN ({placeholders})");
                    }
                }
            }
            if (inputData != null && inputData.TryGetValue("status", out textStatus) && !string.IsNullOrEmpty(textStatus))
            {
                if (textStatus != null && textStatus.Length > 0) // Ensure textStatus is not null
                {
                    string[]? textStatussArray = FixStatuss(textStatus);
                    if (textStatussArray != null) // Add null check for textTitlesArray
                    {
                        string placeholders = string.Join(",", System.Linq.Enumerable.Repeat("?", textStatussArray.Length));
                        queryWhereBuilder.Append($" AND Status IN ({placeholders})");
                    }
                }
            }
            if (inputData != null && inputData.TryGetValue("queryName", out queryName) && !string.IsNullOrEmpty(queryName))
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
            string[]? textTitlesArray = FixTextTitles(inputData["textTitle"]);
            if (textTitlesArray != null && textTitlesArray.Length > 0)
            {
                for (int i = 0; i < textTitlesArray.Length; i++)
                {
                    cmd.Parameters.AddWithValue($"@p{i}", textTitlesArray[i]);
                }
            }
            string[]? textStatussArray = FixStatuss(inputData["status"]);
            if (textStatussArray != null && textStatussArray.Length > 0)
            {
                for (int i = 0; i < textStatussArray.Length; i++)
                {
                    cmd.Parameters.AddWithValue($"@q{i}", textStatussArray[i]);
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
            GC.SuppressFinalize(this); // Ensure finalizer is suppressed
        }



    }
}
