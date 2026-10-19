// אריזת קבצי הקוד מהתיקייה הנוכחית לקובץ אחד
public class Bundler
{
    // תיקיות שלא נכללות ב-bundle (בלי תלות באותיות גדולות/קטנות)
    private static readonly string[] ExcludedFolders =
        ["bin", "obj", "debug", "release", ".vs", ".git", "node_modules"];

    private readonly string _baseDir = Directory.GetCurrentDirectory();

    public void Run(BundleOptions options)
    {
        var extensions = Languages.GetExtensions(options.Languages);
        if (extensions.Count == 0)
        {
            Console.WriteLine("Error: no valid language was given.");
            Console.WriteLine($"Valid languages: {Languages.ValidList}");
            return;
        }

        var files = CollectFiles(extensions, options.Output);
        if (files.Count == 0)
        {
            Console.WriteLine("Error: no code files were found in this folder for the selected languages.");
            return;
        }

        files = SortFiles(files, options.Sort);

        if (Directory.Exists(options.Output.FullName))
        {
            Console.WriteLine($"Error: '{options.Output.FullName}' is a folder. Please give a file name, e.g. result.txt");
            return;
        }

        try
        {
            WriteBundle(files, options);
            Console.WriteLine($"created with {files.Count} files");
        }
        catch (DirectoryNotFoundException)
        {
            Console.WriteLine("Error: the folder does not exist.");
        }
        catch (UnauthorizedAccessException)
        {
            Console.WriteLine($"Error: no permission to write to: {options.Output.FullName}");
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
    private List<string> CollectFiles(HashSet<string> extensions, FileInfo output)
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

    private void WriteBundle(List<string> files, BundleOptions options)
    {
        using var writer = new StreamWriter(options.Output.FullName);

        if (!string.IsNullOrWhiteSpace(options.Author))
        {
            writer.WriteLine($"// Author: {options.Author}");
        }

        foreach (var file in files)
        {
            if (options.Note)
            {
                var relativePath = Path.GetRelativePath(_baseDir, file);
                writer.WriteLine(Languages.ToComment(file, $"Source: {relativePath}"));
            }

            var lines = File.ReadAllLines(file);

            if (options.RemoveEmptyLines)
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
}
