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
const string ICON_COLUMN = "📝";
const string ICON_FK = "🔗";
const string ICON_TIME = "⏱️";
const string ICON_JSON = "📄";

Console.OutputEncoding = System.Text.Encoding.UTF8;

string server = "localhost";
string database = "DOUTOR";
string username = "localhost";
string password = "12345";
string connStr = $"Server={server};Database={database};User Id={username};Password={password};";

Stopwatch totalTimer = Stopwatch.StartNew();
LogStep(1, 4, "Iniciando análise da estrutura do banco de dados...");

try
{
    LogStep(2, 4, "Conectando ao banco de dados...");
    var connectTimer = Stopwatch.StartNew();

    await using SqlConnection conn = new(connStr);
    await conn.OpenAsync();
    connectTimer.Stop();

    LogSuccess($"Conexão estabelecida com sucesso. {ICON_TIME} Tempo: {connectTimer.Elapsed.TotalSeconds:0.00}s");

    Dictionary<string, TableStructure> dbStructure = new();

    LogStep(3, 4, "Obtendo lista de tabelas...");
    var tablesTimer = Stopwatch.StartNew();

    // Get tables list - using ExecuteReader with MARS (Multiple Active Result Sets) enabled
    await using SqlCommand tablesCmd = new(
        "SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_TYPE = 'BASE TABLE'",
        conn);

    List<string> tables = new();
    await using (SqlDataReader tablesReader = await tablesCmd.ExecuteReaderAsync())
    {
        while (await tablesReader.ReadAsync())
        {
            tables.Add(tablesReader.GetString(0));
        }
    }

    tablesTimer.Stop();
    LogSuccess($"{ICON_TABLE} Encontradas {tables.Count} tabelas. {ICON_TIME} Tempo: {tablesTimer.Elapsed.TotalSeconds:0.00}s");

    LogStep(4, 4, $"{ICON_TABLE} Processando {tables.Count} tabelas...");

    int processedTables = 0;
    var processTimer = Stopwatch.StartNew();
    var tableTimer = new Stopwatch();

    foreach (string tableName in tables)
    {
        tableTimer.Restart();
        processedTables++;

        LogInfo($"{ICON_TABLE} Processando tabela {processedTables}/{tables.Count}: {tableName}");

        // Get columns - properly dispose the reader when done
        Console.WriteLine($"{ICON_COLUMN} Obtendo colunas e tipos de dados...");
        List<ColumnInfo> columns = new();
        await using (SqlCommand columnsCmd = new(
            "SELECT COLUMN_NAME, DATA_TYPE FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = @tableName",
            conn))
        {
            columnsCmd.Parameters.AddWithValue("@tableName", tableName);
            await using (SqlDataReader columnsReader = await columnsCmd.ExecuteReaderAsync())
            {
                while (await columnsReader.ReadAsync())
                {
                    columns.Add(new ColumnInfo(
                        columnsReader.GetString(0),
                        columnsReader.GetString(1))
                    );
                }
            }
        }

        LogInfo($"  {ICON_COLUMN} Encontradas {columns.Count} colunas");

        // Get foreign keys - properly dispose the reader when done
        Console.WriteLine($"{ICON_FK} Buscando chaves estrangeiras...");
        List<ForeignKeyInfo> foreignKeys = new();
        await using (SqlCommand fkCmd = new(@"
            SELECT 
                kcu.COLUMN_NAME,
                rc.CONSTRAINT_NAME,
                kcu.TABLE_NAME AS referencing_table,
                kcu.COLUMN_NAME AS referencing_column,
                kcu2.TABLE_NAME AS referenced_table,
                kcu2.COLUMN_NAME AS referenced_column
            FROM INFORMATION_SCHEMA.REFERENTIAL_CONSTRAINTS rc
            JOIN INFORMATION_SCHEMA.KEY_COLUMN_USAGE kcu 
                ON rc.CONSTRAINT_NAME = kcu.CONSTRAINT_NAME
            JOIN INFORMATION_SCHEMA.KEY_COLUMN_USAGE kcu2 
                ON rc.UNIQUE_CONSTRAINT_NAME = kcu2.CONSTRAINT_NAME
            WHERE kcu.TABLE_NAME = @tableName", conn))
        {
            fkCmd.Parameters.AddWithValue("@tableName", tableName);
            await using (SqlDataReader fkReader = await fkCmd.ExecuteReaderAsync())
            {
                while (await fkReader.ReadAsync())
                {
                    foreignKeys.Add(new ForeignKeyInfo(
                        fkReader.GetString(0),
                        fkReader.GetString(1),
                        fkReader.GetString(3),
                        fkReader.GetString(4),
                        fkReader.GetString(5))
                    );
                }
            }
        }

        LogInfo($"  {ICON_FK} Encontradas {foreignKeys.Count} chaves estrangeiras");

        dbStructure.Add(tableName, new TableStructure(columns, foreignKeys));

        tableTimer.Stop();
        LogInfo($"{ICON_TABLE} Tabela {tableName} processada. {ICON_TIME} Tempo: {tableTimer.Elapsed.TotalSeconds:0.00}s");

        if (processedTables < tables.Count)
        {
            double avgTimePerTable = processTimer.Elapsed.TotalSeconds / processedTables;
            double estimatedRemaining = avgTimePerTable * (tables.Count - processedTables);
            LogInfo($"{ICON_TIME} Estimativa: faltam ~{estimatedRemaining:0.00}s para conclusão");
        }
    }

    processTimer.Stop();
    LogSuccess($"{ICON_SUCCESS} Todas as tabelas processadas. {ICON_TIME} Tempo total: {processTimer.Elapsed.TotalSeconds:0.00}s");

    Console.WriteLine($"{ICON_JSON} Gerando arquivo JSON...");
    var jsonTimer = Stopwatch.StartNew();

    var jsonOptions = new JsonSerializerOptions
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    string json = JsonSerializer.Serialize(dbStructure, jsonOptions);
    await File.WriteAllTextAsync("db_structure.json", json);

    jsonTimer.Stop();
    LogSuccess($"{ICON_JSON} Arquivo 'db_structure.json' gerado com sucesso. {ICON_TIME} Tempo: {jsonTimer.Elapsed.TotalSeconds:0.00}s");

    totalTimer.Stop();
    LogSuccess($"{ICON_SUCCESS} Processo concluído com sucesso! {ICON_TIME} Tempo total: {totalTimer.Elapsed.TotalSeconds:0.00}s");
}
catch (Exception e)
{
    totalTimer.Stop();
    LogError($"{ICON_ERROR} Erro: {e.Message}");
    LogError($"{ICON_TIME} Tempo decorrido antes do erro: {totalTimer.Elapsed.TotalSeconds:0.00}s");
}

#region Métodos de Log
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


#region Record updateTypes
public record ColumnInfo(string Name, string Type);

public record ForeignKeyInfo(
    string Column,
    string Constraint,
    string ReferencingColumn,
    string ReferencedTable,
    string ReferencedColumn);

public record TableStructure(
    List<ColumnInfo> Columns,
    List<ForeignKeyInfo> ForeignKeys);
#endregion
