using Shantiw.Data.Schema;

// "Host=localhost;Username=postgres;Password=123456;Database=dvdrental"
if (DbSchemaConsole.Check(args))
{
    DbSchemaExtractor extractor = PostgreSQLSchemaExtractorFactory.Create(args[0]);
    DbSchemaConsole.WriteXmlSchema(extractor, args[1]);
}
