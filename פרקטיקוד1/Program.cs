using System.CommandLine;

public class Program
{
    public static int Main(string[] args)
    {
        // ---------- אפשרויות פקודת bundle ----------

        var outputOption = new Option<FileInfo>("--output", "-o")
        {
            Description = "File path and name",
            Required = true
        };

        var languageOption = new Option<string[]>("--language", "-l")
        {
            Description = "Programming languages to include (or 'all')",
            Required = true,
            AllowMultipleArgumentsPerToken = true
        };

        var noteOption = new Option<bool>("--note", "-n")
        {
            Description = "Write the source file path as a comment"
        };

        var sortOption = new Option<string>("--sort", "-s")
        {
            Description = "Sort files by 'name' or 'type'",
            DefaultValueFactory = _ => "name"
        };
        sortOption.AcceptOnlyFromAmong("name", "type");

        var removeEmptyLinesOption = new Option<bool>("--remove-empty-lines", "-r")
        {
            Description = "Remove empty lines from the source code"
        };

        var authorOption = new Option<string>("--author", "-a")
        {
            Description = "Name of the bundle file creator"
        };

        // ---------- פקודת bundle ----------

        var bundleCommand = new Command("bundle", "bundle codes file to single file");
        bundleCommand.Options.Add(outputOption);
        bundleCommand.Options.Add(languageOption);
        bundleCommand.Options.Add(noteOption);
        bundleCommand.Options.Add(sortOption);
        bundleCommand.Options.Add(removeEmptyLinesOption);
        bundleCommand.Options.Add(authorOption);

        bundleCommand.SetAction(parseResult =>
        {
            RunBundle(
                parseResult.GetValue(outputOption)!,
                parseResult.GetValue(languageOption)!,
                parseResult.GetValue(noteOption),
                parseResult.GetValue(sortOption)!,
                parseResult.GetValue(removeEmptyLinesOption),
                parseResult.GetValue(authorOption));
        });

        // ---------- פקודת create-rsp ----------

        var createRspCommand = new Command("create-rsp", "Create a response file for the bundle command");
        createRspCommand.SetAction(_ => RunCreateRsp());

        // ---------- פקודת השורש ----------

        var rootCommand = new RootCommand("root command for file bundle");
        rootCommand.Subcommands.Add(bundleCommand);
        rootCommand.Subcommands.Add(createRspCommand);

        return rootCommand.Parse(args).Invoke();
    }

    // =====================================================================
    // אריזת קבצי הקוד מהתיקייה הנוכחית לקובץ אחד
    // =====================================================================

    // תיקיות שלא נכללות ב-bundle (בלי תלות באותיות גדולות/קטנות)
    private static readonly string[] ExcludedFolders =
        ["bin", "obj", "debug", "release", ".vs", ".git", "node_modules"];

    private static readonly string _baseDir = Directory.GetCurrentDirectory();

    private static void RunBundle(FileInfo output, string[] languages, bool note, string sort, bool removeEmptyLines, string? author)
    {
        var extensions = GetExtensions(languages);
        if (extensions.Count == 0)
        {
            Console.WriteLine("Error: no valid language was given.");
            Console.WriteLine($"Valid languages: {ValidList}");
            return;
        }

        var files = CollectFiles(extensions, output);
        if (files.Count == 0)
        {
            Console.WriteLine("Error: no code files were found in this folder for the selected languages.");
            return;
        }

        files = SortFiles(files, sort);

        if (Directory.Exists(output.FullName))
        {
            Console.WriteLine($"Error: '{output.FullName}' is a folder. Please give a file name, e.g. result.txt");
            return;
        }

        try
        {
            WriteBundle(files, output, note, removeEmptyLines, author);
            Console.WriteLine($"created with {files.Count} files");
        }
        catch (DirectoryNotFoundException)
        {
            Console.WriteLine("Error: the folder does not exist.");
        }
        catch (UnauthorizedAccessException)
        {
            Console.WriteLine($"Error: no permission to write to: {output.FullName}");
        }
        catch (PathTooLongException)
        {
            Console.WriteLine("Error: the path is too long");
        }
        catch (IOException ex)
        {
            Console.WriteLine($"Error: could not save the file: {ex.Message}");
        }
    }

    // קבצי הקוד מהתיקייה הנוכחית, בלי תיקיות מוחרגות ובלי קובץ הפלט עצמו
    private static List<string> CollectFiles(HashSet<string> extensions, FileInfo output)
    {
        return Directory.GetFiles(_baseDir, "*.*", SearchOption.AllDirectories)
            .Where(f => extensions.Contains(Path.GetExtension(f).ToLower()))
            .Where(f => !ExcludedFolders.Any(folder =>
                f.Contains($"{Path.DirectorySeparatorChar}{folder}{Path.DirectorySeparatorChar}",
                           StringComparison.OrdinalIgnoreCase)))
            .Where(f => !string.Equals(Path.GetFullPath(f), output.FullName, StringComparison.OrdinalIgnoreCase))
            .ToList();
    }

    // מיון לפי שם הקובץ (ברירת מחדל) או לפי סוג הקוד
    private static List<string> SortFiles(List<string> files, string sort)
    {
        if (sort == "type")
        {
            return files
                .OrderBy(f => Path.GetExtension(f).ToLower())
                .ThenBy(f => Path.GetFileName(f))
                .ToList();
        }

        return files
            .OrderBy(f => Path.GetFileName(f))
            .ToList();
    }

    private static void WriteBundle(List<string> files, FileInfo output, bool note, bool removeEmptyLines, string? author)
    {
        using var writer = new StreamWriter(output.FullName);

        if (!string.IsNullOrWhiteSpace(author))
        {
            writer.WriteLine($"// Author: {author}");
        }

        foreach (var file in files)
        {
            if (note)
            {
                var relativePath = Path.GetRelativePath(_baseDir, file);
                writer.WriteLine(ToComment(file, $"Source: {relativePath}"));
            }

            var lines = File.ReadAllLines(file);

            if (removeEmptyLines)
            {
                lines = lines.Where(line => !string.IsNullOrWhiteSpace(line)).ToArray();
            }

            foreach (var line in lines)
            {
                writer.WriteLine(line);
            }
            writer.WriteLine();
        }
    }

    // =====================================================================
    // יצירת קובץ תגובה (response file) עם פקודת bundle מוכנה
    // =====================================================================

    private static void RunCreateRsp()
    {
        var args = new List<string> { "bundle" };

        var output = AskRequired("Output file name or path");
        args.Add($"--output \"{output}\"");

        var languages = AskLanguages();
        args.Add($"--language {string.Join(" ", languages)}");

        if (AskYesNo("Add source file as a comment?"))
            args.Add("--note");

        var sort = AskChoice("Sort by", ["name", "type"], "name");
        if (sort == "type")
            args.Add("--sort type");

        if (AskYesNo("Remove empty lines?"))
            args.Add("--remove-empty-lines");

        var author = AskOptional("Author name");
        if (author != null)
            args.Add($"--author \"{author}\"");

        var rspName = AskOptional("Response file name", "Enter for 'bundle.rsp'") ?? "bundle.rsp";
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

            var invalid = languages.Where(l => !IsValid(l)).ToList();

            if (languages.Length > 0 && invalid.Count == 0)
                return languages;

            if (languages.Length == 0)
                Console.WriteLine("Language is required.");
            else
                Console.WriteLine($"Unknown language: {string.Join(", ", invalid)}");
            Console.WriteLine($"Valid languages: {ValidList}");
        }
    }

    // =====================================================================
    // שאלות למשתמש שחוזרות עד שמתקבלת תשובה חוקית
    // =====================================================================

    // שאלה עם תשובת חובה (לא ריקה)
    private static string AskRequired(string question)
    {
        Console.Write($"{question}: ");
        var answer = Console.ReadLine();
        while (string.IsNullOrWhiteSpace(answer))
        {
            Console.Write($"This field is required. {question}: ");
            answer = Console.ReadLine();
        }
        return answer.Trim();
    }

    // שאלה אופציונלית - מחזירה null אם המשתמש לחץ Enter
    private static string? AskOptional(string question, string hint = "Enter to skip")
    {
        Console.Write($"{question} ({hint}): ");
        var answer = Console.ReadLine();
        return string.IsNullOrWhiteSpace(answer) ? null : answer.Trim();
    }

    // שואלת שאלת כן/לא עד שמתקבלת תשובה חוקית
    private static bool AskYesNo(string question)
    {
        while (true)
        {
            Console.Write($"{question} (y/n): ");
            var answer = Console.ReadLine()?.Trim().ToLower();
            if (answer == "y" || answer == "yes") return true;
            if (answer == "n" || answer == "no") return false;
            Console.WriteLine("Please answer y or n.");
        }
    }

    // שואלת שאלה עם רשימת תשובות אפשריות, Enter מחזיר את ברירת המחדל
    private static string AskChoice(string question, string[] choices, string defaultValue)
    {
        while (true)
        {
            Console.Write($"{question} ({string.Join("/", choices)}, Enter for {defaultValue}): ");
            var answer = Console.ReadLine()?.Trim().ToLower();
            if (string.IsNullOrEmpty(answer)) return defaultValue;
            if (choices.Contains(answer)) return answer;
            Console.WriteLine($"Invalid choice. Please enter one of: {string.Join(", ", choices)}");
        }
    }

    // =====================================================================
    // כל מה שקשור לשפות התכנות: סיומות, בדיקת תקינות ותחביר הערות
    // =====================================================================

    private static readonly Dictionary<string, string[]> Extensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ["csharp"] = [".cs"],
        ["java"] = [".java"],
        ["python"] = [".py"],
        ["javascript"] = [".js"],
        ["typescript"] = [".ts"],
        ["html"] = [".html", ".htm"],
        ["css"] = [".css"],
        ["c"] = [".c", ".h"],
        ["cpp"] = [".cpp", ".hpp"],
        ["sql"] = [".sql"]
    };

    // רשימת השפות החוקיות להצגה למשתמש
    private static string ValidList => $"{string.Join(", ", Extensions.Keys)}, all";

    private static bool IsAll(string language) =>
        language.Equals("all", StringComparison.OrdinalIgnoreCase);

    private static bool IsValid(string language) =>
        IsAll(language) || Extensions.ContainsKey(language);

    // אוסף הסיומות המותרות לפי השפות שנבחרו
    private static HashSet<string> GetExtensions(string[] languages)
    {
        if (languages.Any(IsAll))
            return Extensions.Values.SelectMany(e => e).ToHashSet();

        var extensions = new HashSet<string>();
        foreach (var lang in languages)
        {
            if (Extensions.TryGetValue(lang, out var exts))
                extensions.UnionWith(exts);
            else
                Console.WriteLine($"Unknown language: {lang}");
        }
        return extensions;
    }

    // מחזירה הערה בתחביר המתאים לשפה של הקובץ
    private static string ToComment(string file, string text) => Path.GetExtension(file).ToLower() switch
    {
        ".py" => $"# {text}",
        ".html" or ".htm" => $"<!-- {text} -->",
        ".css" => $"/* {text} */",
        ".sql" => $"-- {text}",
        _ => $"// {text}"   // C#, Java, JavaScript, TypeScript, C, C++
    };
}
