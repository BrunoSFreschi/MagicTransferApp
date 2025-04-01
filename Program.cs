using Newtonsoft.Json;
using System.Data.SqlClient;
using System.Xml;

class Program
{
    static void Main()
    {
        string server = "localhost";
        string database = "PRUDENCIO-";
        string username = "aggersa";
        string password = "1800Dz10";
        string trustedConnection = "yes";

        string connStr = $"Server={server};Database={database};User Id={username};Password={password};";

        SqlConnection conn = null;
        SqlCommand cmd = null;

        try
        {
            // Conectar ao banco de dados
            conn = new SqlConnection(connStr);
            conn.Open();

            // Dicionário para armazenar tabelas e colunas
            Dictionary<string, dynamic> dbStructure = new Dictionary<string, dynamic>();

            // Query para listar as tabelas
            cmd = new SqlCommand("SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_TYPE = 'BASE TABLE'", conn);
            SqlDataReader reader = cmd.ExecuteReader();

            List<string> tables = new List<string>();
            while (reader.Read())
            {
                tables.Add(reader["TABLE_NAME"].ToString());
            }
            reader.Close();

            // Percorre cada tabela para obter suas colunas, tipos de dados e chaves estrangeiras
            foreach (string tableName in tables)
            {
                // Obtém os nomes das colunas e tipos de dados
                cmd = new SqlCommand("SELECT COLUMN_NAME, DATA_TYPE FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = @tableName", conn);
                cmd.Parameters.AddWithValue("@tableName", tableName);
                reader = cmd.ExecuteReader();

                List<dynamic> columns = new List<dynamic>();
                while (reader.Read())
                {
                    columns.Add(new
                    {
                        name = reader["COLUMN_NAME"].ToString(),
                        type = reader["DATA_TYPE"].ToString()
                    });
                }
                reader.Close();

                // Obtém as chaves estrangeiras corretamente no SQL Server
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

                List<dynamic> foreignKeys = new List<dynamic>();
                while (reader.Read())
                {
                    foreignKeys.Add(new
                    {
                        column = reader["COLUMN_NAME"].ToString(),
                        constraint = reader["CONSTRAINT_NAME"].ToString(),
                        references_table = reader["referenced_table"].ToString(),
                        references_column = reader["referenced_column"].ToString()
                    });
                }
                reader.Close();

                // Adiciona a estrutura da tabela ao dicionário
                dbStructure.Add(tableName, new
                {
                    columns = columns,
                    foreign_keys = foreignKeys
                });
            }

            // Salvar em um arquivo JSON
            string json = JsonConvert.SerializeObject(dbStructure, Newtonsoft.Json.Formatting.Indented);
            //                                                          ↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑
            File.WriteAllText("db_structure.json", json);

            Console.WriteLine("Estrutura do banco salva em 'db_structure.json'.");
        }
        catch (Exception e)
        {
            Console.WriteLine($"Erro: {e.Message}");
        }
        finally
        {
            // Fechar conexão para evitar vazamento de recursos
            if (cmd != null) cmd.Dispose();
            if (conn != null) conn.Close();
        }
    }
}