namespace Shantiw.Data.Schema
{
    public static class DbSchemaConsole
    {
        public static bool Check(string[] args)
        {
            switch (args.Length)
            {
                case 0:
                    Help();
                    return false;
                case 1:
                    if (args[0] == "?" || args[0].Equals("help", StringComparison.CurrentCultureIgnoreCase))
                    {
                        Help();
                    }
                    else
                    {
                        Console.WriteLine();
                        Console.WriteLine("The syntax of the command is incorrect.");
                    }
                    return false;
                case 2:
                    if (File.Exists(args[1]))
                    {
                        Console.WriteLine();
                        Console.WriteLine("The file already exists.");
                        return false;
                    }
                    args[0] = Unquote(args[0]);
                    args[1] = Unquote(args[1]);
                    return true;
                default:
                    Console.WriteLine();
                    Console.WriteLine("Too many arguments.");
                    return false;
            }
        }

        private static string Unquote(string value) // defensive programming
        {
            if (value.StartsWith('"') && value.EndsWith('"') ||
                value.StartsWith('\'') && value.EndsWith('\''))
            {
                return value[1..^1];
            }
            return value;
        }

        public static void WriteXmlSchema(DbSchemaExtractor extractor, string fileName)
        {
            Console.WriteLine();

            Console.WriteLine("Executing, please wait...");

            extractor.WriteXmlSchema(fileName);

            Console.WriteLine();

            Console.WriteLine("The command has been successfully executed.");
        }

        private static void Help()
        {
            Console.WriteLine();
            Console.WriteLine("Extracts the schema from the specified database.");
            Console.WriteLine();

            Console.WriteLine(System.AppDomain.CurrentDomain.FriendlyName + " {\"connection string\"} {file name}");
            Console.WriteLine();

            Console.WriteLine("connection string  The connection string used to open the connection.");
            Console.WriteLine("file name          The file name (including the path) to which to write.");
        }
    }
}
