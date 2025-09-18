using Shantiw.Data.Schema;

switch (args.Length)
{
    case 0:
        Help();
        break;
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
        break;
    case 3:
        if (File.Exists(args[2]))
        {
            Console.WriteLine();
            Console.WriteLine("The file already exists.");
            break;
        }
        Console.WriteLine();
        Console.WriteLine("Generating, please wait...");

        SchemaGenerator generator = new(args[0], args[1]);
        generator.WriteXmlSchema(args[2]);

        Console.WriteLine();
        Console.WriteLine("The command has been successfully executed.");
        break;
    default:
        Console.WriteLine();
        Console.WriteLine("Too many or too few arguments.");
        break;
}
static void Help()
{
    Console.WriteLine();
    Console.WriteLine("Generates the schema from the specified database schema.");
    Console.WriteLine();

    Console.WriteLine(AppDomain.CurrentDomain.FriendlyName + " {database schema file name} {mapping file name} {schema file name}");
    Console.WriteLine();

    Console.WriteLine("database schema file name    The file extracted from the specified database.");
    Console.WriteLine("mapping file name            The file provides mapping config.");
    Console.WriteLine("schema file name             The file name (including the path) to which to write.");
}