internal class ColumnSchema
{
    public string ColumnName { get; set; }
    public string DataType { get; set; }
    public int MaxLength { get; set; }
    public int Precision { get; set; }
    public int Scale { get; set; }
    public bool IsNullable { get; set; }
    public bool IsIdentity { get; set; }
}