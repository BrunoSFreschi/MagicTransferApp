using Microsoft.Data.SqlClient;

string connectionString = "Server=localhost;Database=HeroisDB;User Id=sa;Password=1800Dz10;TrustServerCertificate=True;";

List<DatabaseSchema> databases = new List<DatabaseSchema>();

using (SqlConnection connection = new SqlConnection(connectionString))
{
    try
    {
        connection.Open();

        List<string> databaseNames = GetDatabaseNames(connection);

        foreach (string dbName in databaseNames)
        {
            if (dbName == "master" || dbName == "tempdb" || dbName == "model" || dbName == "msdb")
                continue;

            Console.WriteLine($"Processing: {dbName}");

            DatabaseSchema dbSchema = new DatabaseSchema
            {
                DataBaseName = dbName,
                Tables = new List<TableSchema>()
            };

            string dbConnectionString = $"Database={dbName}, {connectionString}";

            using (SqlConnection sqlConnection = new SqlConnection(dbConnectionString))
            {
                sqlConnection.Open();

                List<TableSchema> tableSchemas = GetTable(sqlConnection);


                foreach (TableSchema table in tableSchemas)
                {
                    table.Columns = GetColumns(sqlConnection, table.TableName);

                    Console.WriteLine(table.Columns);

                    dbSchema.Tables.Add(table);
                }
            }

            databases.Add(dbSchema);
        }

        Console.WriteLine("Conexão bem-sucedida");
    }
    catch (Exception ex)
    {
        Console.WriteLine(ex.Message);
    }
}

List<ColumnSchema> GetColumns(SqlConnection sqlConnection, string tableName)
{
    List<ColumnSchema> columns = new List<ColumnSchema>();

    string query = @"
            SELECT 
                c.name AS ColumnName,
                tp.name AS DataType,
                c.max_length AS MaxLength,
                c.precision AS Precision,
                c.scale AS Scale,
                c.is_nullable AS IsNullable,
                c.is_identity AS IsIdentity
            FROM 
                sys.columns c
            INNER JOIN 
                sys.types tp ON c.user_type_id = tp.user_type_id
            WHERE 
                c.object_id = OBJECT_ID(@TableName)
            ORDER BY 
                c.column_id";

    using (SqlCommand command = new SqlCommand(query, sqlConnection))
    {
        command.Parameters.AddWithValue("@TableName", tableName);

        using (SqlDataReader reader = command.ExecuteReader())
        {
            while (reader.Read())
            {
                columns.Add(new ColumnSchema
                {
                    ColumnName = reader["ColumnName"].ToString(),
                    DataType = reader["DataType"].ToString(),
                    MaxLength = Convert.ToInt32(reader["MaxLength"]),
                    Precision = Convert.ToInt32(reader["Precision"]),
                    Scale = Convert.ToInt32(reader["Scale"]),
                    IsNullable = Convert.ToBoolean(reader["IsNullable"]),
                    IsIdentity = Convert.ToBoolean(reader["IsIdentity"])
                });
                ;
                Console.WriteLine(reader["ColumnName"].ToString());
            }
        }
    }

    return columns;
}

List<TableSchema> GetTable(SqlConnection sqlConnection)
{
    List<TableSchema> tableSchemas = new List<TableSchema>();

    string query = @"
            SELECT 
                s.name AS SchemaName, 
                t.name AS TableName
            FROM 
                sys.tables t
            INNER JOIN 
                sys.schemas s ON t.schema_id = s.schema_id
            ORDER BY 
                s.name, t.name";

    using (SqlCommand command = new SqlCommand(query, sqlConnection))
    using (SqlDataReader reader = command.ExecuteReader())
    {
        while (reader.Read())
        {
            tableSchemas.Add(new TableSchema
            {
                SchemaName = reader["SchemaName"].ToString(),
                TableName = reader["TableName"].ToString()
            });
        }
    }

    return tableSchemas;
}

List<string> GetDatabaseNames(SqlConnection connection)
{
    List<string> databases = new List<string>();

    string query = "SELECT name FROM sys.databases WHERE state = 0";

    using (SqlCommand command = new SqlCommand(query, connection))
    using (SqlDataReader reader = command.ExecuteReader())
    {
        while (reader.Read())
        {
            databases.Add(reader["name"].ToString());
        }
    }

    return databases;
}