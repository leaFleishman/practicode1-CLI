// כל מה שקשור לשפות התכנות: סיומות, בדיקת תקינות ותחביר הערות
public static class Languages
{
    public static readonly Dictionary<string, string[]> Extensions = new(StringComparer.OrdinalIgnoreCase)
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
    public static string ValidList => $"{string.Join(", ", Extensions.Keys)}, all";

    public static bool IsAll(string language) =>
        language.Equals("all", StringComparison.OrdinalIgnoreCase);

    public static bool IsValid(string language) =>
        IsAll(language) || Extensions.ContainsKey(language);

    // אוסף הסיומות המותרות לפי השפות שנבחרו
    public static HashSet<string> GetExtensions(string[] languages)
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
    public static string ToComment(string file, string text) => Path.GetExtension(file).ToLower() switch
    {
        ".py" => $"# {text}",
        ".html" or ".htm" => $"<!-- {text} -->",
        ".css" => $"/* {text} */",
        ".sql" => $"-- {text}",
        _ => $"// {text}"   // C#, Java, JavaScript, TypeScript, C, C++
    };
}
