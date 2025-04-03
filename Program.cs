using Newtonsoft.Json;
using System.Data.SqlClient;
using System.Diagnostics;

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

    [Obsolete]
    static void Main()
    {

        Console.OutputEncoding = System.Text.Encoding.UTF8;

        string server = "localhost";
        string database = "DOUTOR";
        string username = "aggersa";
        string password = "1800Dz10";
        string connStr = $"Server={server};Database={database};User Id={username};Password={password};";

        Stopwatch totalTimer = Stopwatch.StartNew();
        LogStep(1, 4, "Iniciando análise da estrutura do banco de dados...");

        SqlConnection? conn = null;
        SqlCommand? cmd = null;

        try
        {
            LogStep(2, 4, "Conectando ao banco de dados...");
            var connectTimer = Stopwatch.StartNew();

    await using SqlConnection conn = new(connStr);
    await conn.OpenAsync();
            connectTimer.Stop();
            LogSuccess($"Conexão estabelecida com sucesso. {ICON_TIME} Tempo: {connectTimer.Elapsed.TotalSeconds:0.00}s");

            Dictionary<string, dynamic> dbStructure = [];

            LogStep(3, 4, "Obtendo lista de tabelas...");
            var tablesTimer = Stopwatch.StartNew();

    // Get tables list - using ExecuteReader with MARS (Multiple Active Result Sets) enabled
    await using SqlCommand tablesCmd = new(
        "SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_TYPE = 'BASE TABLE'",
        conn);

    List<string> tables = new();
    await using (SqlDataReader tablesReader = await tablesCmd.ExecuteReaderAsync())
            {
                tables.Add(reader["TABLE_NAME"].ToString() ?? "");
            }
            reader.Close();

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

                Console.WriteLine($"{ICON_COLUMN} Obtendo colunas e tipos de dados...");
                cmd = new SqlCommand("SELECT COLUMN_NAME, DATA_TYPE FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = @tableName", conn);
                cmd.Parameters.AddWithValue("@tableName", tableName);
                reader = cmd.ExecuteReader();

                List<dynamic> columns = [];
                while (reader.Read())
                {
                    columns.Add(new
                    {
                        name = reader["COLUMN_NAME"].ToString(),
                        type = reader["DATA_TYPE"].ToString()
                    });
                }
                reader.Close();

                LogInfo($"  {ICON_COLUMN} Encontradas {columns.Count} colunas");

                Console.WriteLine($"{ICON_FK} Buscando chaves estrangeiras...");
                cmd = new SqlCommand(@"
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
                    WHERE kcu.TABLE_NAME = @tableName", conn);
                cmd.Parameters.AddWithValue("@tableName", tableName);
                reader = cmd.ExecuteReader();

                List<dynamic> foreign_Keys = [];
                while (reader.Read())
                {
                    foreign_Keys.Add(new
                    {
                        column = reader["COLUMN_NAME"].ToString(),
                        constraint = reader["CONSTRAINT_NAME"].ToString(),
                        references_table = reader["referenced_table"].ToString(),
                        references_column = reader["referenced_column"].ToString()
                    });
                }
                reader.Close();

                LogInfo($"  {ICON_FK} Encontradas {foreign_Keys.Count} chaves estrangeiras");


                dbStructure.Add(tableName, new
                {
                    columns,
                    foreign_Keys
                });

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

            string json = JsonConvert.SerializeObject(dbStructure, Newtonsoft.Json.Formatting.Indented);
            File.WriteAllText("db_structure.json", json);

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
        finally
        {
            cmd?.Dispose();
            conn?.Close();
        }
    }

    #region Métodos de Log
    private static void LogStep(int currentStep, int totalSteps, string message)
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine($"{ICON_STEP} [ETAPA {currentStep}/{totalSteps}] {message}");
        Console.ResetColor();
    }

    private static void LogSuccess(string message)
    {
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"{ICON_SUCCESS} {message}");
        Console.ResetColor();
    }

    private static void LogInfo(string message)
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine($"{ICON_INFO} {message}");
        Console.ResetColor();
    }

    private static void LogWarning(string message)
    {
        Console.ForegroundColor = ConsoleColor.DarkYellow;
        Console.WriteLine($"{ICON_WARNING} {message}");
        Console.ResetColor();
    }

    private static void LogError(string message)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"{ICON_ERROR} {message}");
        Console.ResetColor();
    }
    #endregion
}