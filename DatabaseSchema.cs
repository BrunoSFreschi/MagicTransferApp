using System.ComponentModel.DataAnnotations.Schema;

internal class DatabaseSchema
{
    public string DataBaseName { get; set; }
    public List<TableSchema> Tables { get; set; }
}