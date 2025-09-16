using Shantiw.Data.Schema;

// "Server=localhost;Database=northwind;Uid=root;Pwd=123456"
if (DbSchemaConsole.Check(args))
{
    DbSchemaExtractor extractor = MySqlSchemaExtractorFactory.Create(args[0]);
    DbSchemaConsole.WriteXmlSchema(extractor, args[1]);
}
