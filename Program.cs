using System.Data.SqlClient;
using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;

const string ICON_SUCCESS = "✅";
const string ICON_ERROR = "❌";
const string ICON_INFO = "ℹ️";
const string ICON_WARNING = "⚠️";
const string ICON_STEP = "🔵";
const string ICON_TABLE = "📋";
const string ICON_TIME = "⏱️";
const string ICON_FOLDER = "📁";

Console.OutputEncoding = System.Text.Encoding.UTF8;

string server = "localhost";
string database = "HeroisDB";
string username = "sa";
string password = "1800Dz10";
string connStr = $"Server={server};Database={database};User Id={username};Password={password};";

Stopwatch totalTimer = Stopwatch.StartNew();
LogStep(1, 5, $"Iniciando exportação completa do banco de dados '{database}'...");

try
{
    // Create output directory
    string outputDir = Path.Combine(Directory.GetCurrentDirectory(), database);
    Directory.CreateDirectory(outputDir);
    LogSuccess($"{ICON_FOLDER} Diretório de saída criado: {outputDir}");

    LogStep(2, 5, "Conectando ao banco de dados...");
    Stopwatch connectTimer = Stopwatch.StartNew();

    await using SqlConnection conn = new(connStr);
    await conn.OpenAsync();
    connectTimer.Stop();

    LogSuccess($"Conexão estabelecida com sucesso. {ICON_TIME} Tempo: {connectTimer.Elapsed.TotalSeconds:0.00}s");

    // Get database metadata
    LogStep(3, 5, "Obtendo metadados do banco de dados...");
    DatabaseMetadata dbMetadata = await GetDatabaseMetadata(conn, database);

    // Get list of tables
    LogStep(4, 5, "Processando estrutura das tabelas...");
    List<string> tables = await GetTables(conn);
    LogSuccess($"{ICON_TABLE} Encontradas {tables.Count} tabelas.");

    // Process each table
    LogStep(5, 5, "Exportando dados das tabelas...");
    Stopwatch processTimer = Stopwatch.StartNew();
    int processedTables = 0;

    foreach (string tableName in tables)
    {
        processedTables++;
        Stopwatch tableTimer = Stopwatch.StartNew();
        LogInfo($"{ICON_TABLE} Processando tabela {processedTables}/{tables.Count}: {tableName}");

        // Get table structure
        List<ColumnMetadata> columns = await GetColumns(conn, tableName);
        List<ConstraintMetadata> constraints = await GetConstraints(conn, tableName);
        List<ConstraintMetadata> foreignKeys = await GetForeignKeys(conn, tableName);

        // Add to database metadata
        dbMetadata.Tables.Add(new TableMetadata(
            tableName,
            columns,
            constraints.Where(c => c.Type != "FOREIGN KEY").ToList(),
            foreignKeys
        ));

        // Export table data
        List<Dictionary<string, object?>> tableData = await GetTableData(conn, tableName, columns);
        string tableFilePath = Path.Combine(outputDir, $"{tableName}.json");
        await SaveAsJson(tableData, tableFilePath);

        tableTimer.Stop();
        LogSuccess($"{ICON_TABLE} Tabela {tableName} exportada. {ICON_TIME} Tempo: {tableTimer.Elapsed.TotalSeconds:0.00}s");
    }

    // Save database metadata
    string metadataPath = Path.Combine(outputDir, "_database_details.json");
    await SaveAsJson(dbMetadata, metadataPath);

    processTimer.Stop();
    LogSuccess($"{ICON_SUCCESS} Exportação concluída! {ICON_TIME} Tempo total: {processTimer.Elapsed.TotalSeconds:0.00}s");

    totalTimer.Stop();
    LogSuccess($"{ICON_SUCCESS} Processo finalizado com sucesso! {ICON_TIME} Tempo total: {totalTimer.Elapsed.TotalSeconds:0.00}s");
}
catch (Exception e)
{
    totalTimer.Stop();
    LogError($"{ICON_ERROR} Erro: {e.Message}");
    LogError($"{ICON_TIME} Tempo decorrido antes do erro: {totalTimer.Elapsed.TotalSeconds:0.00}s");
}

#region Database Methods
async Task<DatabaseMetadata> GetDatabaseMetadata(SqlConnection conn, string dbName)
{
    await using SqlCommand cmd = new(@"
        SELECT 
            SERVERPROPERTY('ProductVersion') AS Version,
            DATABASEPROPERTYEX(DB_NAME(), 'Collation') AS Collation,
            DATABASEPROPERTYEX(DB_NAME(), 'Recovery') AS RecoveryModel,
            DATABASEPROPERTYEX(DB_NAME(), 'Status') AS Status,
            DATABASEPROPERTYEX(DB_NAME(), 'Updateability') AS Updateability
        ", conn);

    await using SqlDataReader reader = await cmd.ExecuteReaderAsync();
    await reader.ReadAsync();

    return new DatabaseMetadata(
        Name: dbName,
        Version: reader.GetString(0),
        Collation: reader.GetString(1),
        RecoveryModel: reader.GetString(2),
        Status: reader.GetString(3),
        Updateability: reader.GetString(4),
        Tables: new List<TableMetadata>()
    );
}

async Task<List<string>> GetTables(SqlConnection conn)
{
    List<string> tables = new();
    await using SqlCommand cmd = new(
        "SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_TYPE = 'BASE TABLE' ORDER BY TABLE_NAME",
        conn);

    await using SqlDataReader reader = await cmd.ExecuteReaderAsync();
    while (await reader.ReadAsync())
    {
        tables.Add(reader.GetString(0));
    }
    return tables;
}

async Task<List<ColumnMetadata>> GetColumns(SqlConnection conn, string tableName)
{
    List<ColumnMetadata> columns = new();
    await using SqlCommand cmd = new(@"
        SELECT 
            COLUMN_NAME, 
            DATA_TYPE,
            IS_NULLABLE,
            CHARACTER_MAXIMUM_LENGTH,
            NUMERIC_PRECISION,
            NUMERIC_SCALE,
            COLUMN_DEFAULT
        FROM INFORMATION_SCHEMA.COLUMNS 
        WHERE TABLE_NAME = @tableName 
        ORDER BY ORDINAL_POSITION", conn);

    cmd.Parameters.AddWithValue("@tableName", tableName);
    await using SqlDataReader reader = await cmd.ExecuteReaderAsync();

    while (await reader.ReadAsync())
    {
        columns.Add(new ColumnMetadata(
            Name: reader.GetString(0),
            DataType: reader.GetString(1),
            IsNullable: reader.GetString(2) == "YES",
            MaxLength: reader.IsDBNull(3) ? null : reader.GetInt32(3),
            Precision: reader.IsDBNull(4) ? null : reader.GetByte(4),
            Scale: reader.IsDBNull(5) ? null : reader.GetInt32(5),
            DefaultValue: reader.IsDBNull(6) ? null : reader.GetString(6)
        ));
    }
    return columns;
}

async Task<List<ConstraintMetadata>> GetConstraints(SqlConnection conn, string tableName)
{
    List<ConstraintMetadata> constraints = new();

    // Primary Keys
    await using (SqlCommand cmd = new(@"
        SELECT 
            COLUMN_NAME,
            'PRIMARY KEY' AS CONSTRAINT_TYPE
        FROM INFORMATION_SCHEMA.KEY_COLUMN_USAGE
        WHERE OBJECTPROPERTY(OBJECT_ID(CONSTRAINT_SCHEMA + '.' + CONSTRAINT_NAME), 'IsPrimaryKey') = 1
        AND TABLE_NAME = @tableName", conn))
    {
        cmd.Parameters.AddWithValue("@tableName", tableName);
        await using SqlDataReader reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            constraints.Add(new ConstraintMetadata(
                ColumnName: reader.GetString(0),
                Type: reader.GetString(1),
                ReferencedTable: null,
                ReferencedColumn: null
            ));
        }
    }

    // Unique Constraints
    await using (SqlCommand cmd = new(@"
        SELECT 
            COLUMN_NAME,
            'UNIQUE' AS CONSTRAINT_TYPE
        FROM INFORMATION_SCHEMA.KEY_COLUMN_USAGE
        WHERE OBJECTPROPERTY(OBJECT_ID(CONSTRAINT_SCHEMA + '.' + CONSTRAINT_NAME), 'IsUniqueCnst') = 1
        AND TABLE_NAME = @tableName", conn))
    {
        cmd.Parameters.AddWithValue("@tableName", tableName);
        await using SqlDataReader reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            constraints.Add(new ConstraintMetadata(
                ColumnName: reader.GetString(0),
                Type: reader.GetString(1),
                ReferencedTable: null,
                ReferencedColumn: null
            ));
        }
    }

    return constraints;
}

async Task<List<ConstraintMetadata>> GetForeignKeys(SqlConnection conn, string tableName)
{
    List<ConstraintMetadata> foreignKeys = new();
    await using SqlCommand cmd = new(@"
        SELECT 
            kcu.COLUMN_NAME,
            'FOREIGN KEY' AS CONSTRAINT_TYPE,
            kcu2.TABLE_NAME AS REFERENCED_TABLE,
            kcu2.COLUMN_NAME AS REFERENCED_COLUMN
        FROM INFORMATION_SCHEMA.REFERENTIAL_CONSTRAINTS rc
        JOIN INFORMATION_SCHEMA.KEY_COLUMN_USAGE kcu 
            ON rc.CONSTRAINT_NAME = kcu.CONSTRAINT_NAME
        JOIN INFORMATION_SCHEMA.KEY_COLUMN_USAGE kcu2 
            ON rc.UNIQUE_CONSTRAINT_NAME = kcu2.CONSTRAINT_NAME
        WHERE kcu.TABLE_NAME = @tableName", conn);

    cmd.Parameters.AddWithValue("@tableName", tableName);
    await using SqlDataReader reader = await cmd.ExecuteReaderAsync();
    while (await reader.ReadAsync())
    {
        foreignKeys.Add(new ConstraintMetadata(
            ColumnName: reader.GetString(0),
            Type: reader.GetString(1),
            ReferencedTable: reader.GetString(2),
            ReferencedColumn: reader.GetString(3)
        ));
    }
    return foreignKeys;
}

async Task<List<Dictionary<string, object?>>> GetTableData(SqlConnection conn, string tableName, List<ColumnMetadata> columns)
{
    List<Dictionary<string, object?>> tableData = new();
    if (columns.Count == 0) return tableData;

    string columnNames = string.Join(", ", columns.Select(c => $"[{c.Name}]"));
    await using SqlCommand cmd = new($"SELECT {columnNames} FROM [{tableName}]", conn);

    await using SqlDataReader reader = await cmd.ExecuteReaderAsync();
    while (await reader.ReadAsync())
    {
        Dictionary<string, object?> row = new Dictionary<string, object?>();
        for (int i = 0; i < reader.FieldCount; i++)
        {
            string columnName = reader.GetName(i);
            row[columnName] = reader.IsDBNull(i) ? null : reader.GetValue(i);
        }
        tableData.Add(row);
    }
    return tableData;
}
#endregion

#region Utility Methods
async Task SaveAsJson(object data, string filePath)
{
    JsonSerializerOptions options = new JsonSerializerOptions
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter() }
    };

    string json = JsonSerializer.Serialize(data, options);
    await File.WriteAllTextAsync(filePath, json);
}
#endregion

#region Log Methods
static void LogStep(int currentStep, int totalSteps, string message)
{
    Console.ForegroundColor = ConsoleColor.Cyan;
    Console.WriteLine($"{ICON_STEP} [ETAPA {currentStep}/{totalSteps}] {message}");
    Console.ResetColor();
}

static void LogSuccess(string message)
{
    Console.ForegroundColor = ConsoleColor.Green;
    Console.WriteLine($"{ICON_SUCCESS} {message}");
    Console.ResetColor();
}

static void LogInfo(string message)
{
    Console.ForegroundColor = ConsoleColor.Yellow;
    Console.WriteLine($"{ICON_INFO} {message}");
    Console.ResetColor();
}

static void LogWarning(string message)
{
    Console.ForegroundColor = ConsoleColor.DarkYellow;
    Console.WriteLine($"{ICON_WARNING} {message}");
    Console.ResetColor();
}

static void LogError(string message)
{
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine($"{ICON_ERROR} {message}");
    Console.ResetColor();
}
#endregion

#region Data Models
public record DatabaseMetadata(
    string Name,
    string Version,
    string Collation,
    string RecoveryModel,
    string Status,
    string Updateability,
    List<TableMetadata> Tables);

public record TableMetadata(
    string Name,
    List<ColumnMetadata> Columns,
    List<ConstraintMetadata> Constraints,
    List<ConstraintMetadata> ForeignKeys);

public record ColumnMetadata(
    string Name,
    string DataType,
    bool IsNullable,
    int? MaxLength,
    byte? Precision,
    int? Scale,
    string? DefaultValue);

public record ConstraintMetadata(
    string ColumnName,
    string Type,
    string? ReferencedTable,
    string? ReferencedColumn);
#endregion