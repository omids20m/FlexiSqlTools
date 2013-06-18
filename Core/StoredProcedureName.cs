namespace FlexiSqlTools.Core
{
    public class StoredProcedureName
    {
        public string SchemaName { get; set; }
        public string Prefix { get; set; }
        public string TableName { get; set; }
        public string Postfix { get; set; }
        public string Name { get; set; }

        public override string ToString()
        {
            return SchemaName + "." + Prefix + TableName + Name + Postfix;
        }
    }
}