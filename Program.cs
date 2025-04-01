using Newtonsoft.Json;
using System.Data.SqlClient;
using System.Diagnostics;

class Program
{
    [Obsolete]
    static void Main()
    {
        // Configuração inicial
        string server = "localhost";
        string database = "PRUDENCIO-";
        string username = "aggersa";
        string password = "1800Dz10";
        string connStr = $"Server={server};Database={database};User Id={username};Password={password};";

        // Inicializa o cronômetro para medir o tempo total
        Stopwatch totalTimer = Stopwatch.StartNew();
        Console.WriteLine("Iniciando análise da estrutura do banco de dados...");
        Console.WriteLine();

        SqlConnection? conn = null;
        SqlCommand? cmd = null;

        try
        {
            // Etapa 1: Conectar ao banco de dados
            LogStep(1, 3, "Conectando ao banco de dados...");
            var connectTimer = Stopwatch.StartNew();

            conn = new SqlConnection(connStr);
            conn.Open();

            connectTimer.Stop();
            LogSuccess($"Conexão estabelecida com sucesso. Tempo: {connectTimer.Elapsed.TotalSeconds:0.00}s");
            Console.WriteLine();

            // Dicionário para armazenar tabelas e colunas
            Dictionary<string, dynamic> dbStructure = [];

            // Etapa 2: Listar tabelas
            LogStep(2, 3, "Obtendo lista de tabelas...");
            var tablesTimer = Stopwatch.StartNew();

            cmd = new SqlCommand("SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_TYPE = 'BASE TABLE'", conn);
            SqlDataReader reader = cmd.ExecuteReader();

            List<string> tables = [];
            while (reader.Read())
            {
                tables.Add(reader["TABLE_NAME"].ToString() ?? "");
            }
            reader.Close();

            tablesTimer.Stop();
            LogSuccess($"Encontradas {tables.Count} tabelas. Tempo: {tablesTimer.Elapsed.TotalSeconds:0.00}s");
            Console.WriteLine();

            // Etapa 3: Processar cada tabela
            LogStep(3, 3, $"Processando {tables.Count} tabelas...");
            Console.WriteLine();

            int processedTables = 0;
            var processTimer = Stopwatch.StartNew();
            var tableTimer = new Stopwatch();

            foreach (string tableName in tables)
            {
                tableTimer.Restart();
                processedTables++;

                LogInfo($"Processando tabela {processedTables}/{tables.Count}: {tableName}");
                Console.WriteLine($"- Obtendo colunas e tipos de dados...");

                // Obtém os nomes das colunas e tipos de dados
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

                Console.WriteLine($"  > Encontradas {columns.Count} colunas");
                Console.WriteLine($"- Buscando chaves estrangeiras...");

                // Obtém as chaves estrangeiras
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

                Console.WriteLine($"  > Encontradas {foreign_Keys.Count} chaves estrangeiras");

                // Adiciona a estrutura da tabela ao dicionário
                dbStructure.Add(tableName, new
                {
                    columns,
                    foreign_Keys
                });

                tableTimer.Stop();
                LogInfo($"Tabela {tableName} processada. Tempo: {tableTimer.Elapsed.TotalSeconds:0.00}s");

                // Estimativa de tempo restante
                if (processedTables < tables.Count)
                {
                    double avgTimePerTable = processTimer.Elapsed.TotalSeconds / processedTables;
                    double estimatedRemaining = avgTimePerTable * (tables.Count - processedTables);
                    Console.WriteLine($"- Estimativa: faltam ~{estimatedRemaining:0.00}s para conclusão");
                }

                Console.WriteLine();
            }

            processTimer.Stop();
            LogSuccess($"Todas as tabelas processadas. Tempo total: {processTimer.Elapsed.TotalSeconds:0.00}s");
            Console.WriteLine();

            // Salvar em um arquivo JSON
            Console.WriteLine("Gerando arquivo JSON...");
            var jsonTimer = Stopwatch.StartNew();

            string json = JsonConvert.SerializeObject(dbStructure, Newtonsoft.Json.Formatting.Indented);
            File.WriteAllText("db_structure.json", json);

            jsonTimer.Stop();
            LogSuccess($"Arquivo 'db_structure.json' gerado com sucesso. Tempo: {jsonTimer.Elapsed.TotalSeconds:0.00}s");
            Console.WriteLine();

            totalTimer.Stop();
            LogSuccess($"Processo concluído com sucesso! Tempo total: {totalTimer.Elapsed.TotalSeconds:0.00}s");
        }
        catch (Exception e)
        {
            totalTimer.Stop();
            LogError($"Erro: {e.Message}");
            LogError($"Tempo decorrido antes do erro: {totalTimer.Elapsed.TotalSeconds:0.00}s");
        }
        finally
        {
            // Fechar conexão para evitar vazamento de recursos
            cmd?.Dispose();
            conn?.Close();
        }
    }

    // Métodos auxiliares para formatação de logs
    private static void LogStep(int currentStep, int totalSteps, string message)
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine($"[ETAPA {currentStep}/{totalSteps}] {message}");
        Console.ResetColor();
    }

    private static void LogSuccess(string message)
    {
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"[SUCESSO] {message}");
        Console.ResetColor();
    }

    private static void LogInfo(string message)
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine($"[INFO] {message}");
        Console.ResetColor();
    }

    private static void LogError(string message)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"[ERRO] {message}");
        Console.ResetColor();
    }
}