using Shantiw.Data.Schema;

// "Data Source=LOCALHOST\SQLEXPRESS;Initial Catalog=Northwind;User ID=sa;Password=123456;Encrypt=False"
if (DbSchemaConsole.Check(args))
{
    DbSchemaExtractor extractor = SqlSchemaExtractorFactory.Create(args[0]);
    DbSchemaConsole.WriteXmlSchema(extractor, args[1]);
}
