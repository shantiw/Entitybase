namespace Shantiw.Data.Schema
{
    public static class SqlSchemaExtractorFactory
    {
        public static DbSchemaExtractor Create(string connectionString)
        {
            return new SqlSchemaExtractor(connectionString);
        }
    }
}
