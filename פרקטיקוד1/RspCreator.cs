// יצירת קובץ תגובה (response file) עם פקודת bundle מוכנה
public class RspCreator
{
    public void Run()
    {
        var args = new List<string> { "bundle" };

        var output = ConsoleHelper.AskRequired("Output file name or path");
        args.Add($"--output \"{output}\"");

        var languages = AskLanguages();
        args.Add($"--language {string.Join(" ", languages)}");

        if (ConsoleHelper.AskYesNo("Add source file as a comment?"))
            args.Add("--note");

        var sort = ConsoleHelper.AskChoice("Sort by", ["name", "type"], "name");
        if (sort == "type")
            args.Add("--sort type");

        if (ConsoleHelper.AskYesNo("Remove empty lines?"))
            args.Add("--remove-empty-lines");

        var author = ConsoleHelper.AskOptional("Author name");
        if (author != null)
            args.Add($"--author \"{author}\"");

        var rspName = ConsoleHelper.AskOptional("Response file name", "Enter for 'bundle.rsp'") ?? "bundle.rsp";
        if (!rspName.EndsWith(".rsp"))
            rspName += ".rsp";

        try
        {
            File.WriteAllText(rspName, string.Join(" ", args));
            Console.WriteLine($"Response file created: {Path.GetFullPath(rspName)}");
            Console.WriteLine($"Run it with: fib @{rspName}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: could not create the file ({ex.Message}).");
        }
    }

    // שפות - כל שפה חייבת להיות במילון או all
    private static string[] AskLanguages()
    {
        while (true)
        {
            Console.Write("Languages (separated by spaces, or 'all'): ");
            var languages = (Console.ReadLine() ?? "")
                .Split(' ', StringSplitOptions.RemoveEmptyEntries);

            var invalid = languages.Where(l => !Languages.IsValid(l)).ToList();

            if (languages.Length > 0 && invalid.Count == 0)
                return languages;

            if (languages.Length == 0)
                Console.WriteLine("Language is required.");
            else
                Console.WriteLine($"Unknown language: {string.Join(", ", invalid)}");
            Console.WriteLine($"Valid languages: {Languages.ValidList}");
        }
    }
}
