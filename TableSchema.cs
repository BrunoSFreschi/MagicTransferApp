internal class TableSchema
{
    public string SchemaName { get; set; }
    public string TableName { get; set; }
    public List<ColumnSchema> Columns { get; set; }
}