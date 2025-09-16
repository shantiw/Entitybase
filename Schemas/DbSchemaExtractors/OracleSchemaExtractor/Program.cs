using Shantiw.Data.Schema;

// 
if (DbSchemaConsole.Check(args))
{
    DbSchemaExtractor extractor = OracleSchemaExtractorFactory.Create(args[0]);
    DbSchemaConsole.WriteXmlSchema(extractor, args[1]);
}
