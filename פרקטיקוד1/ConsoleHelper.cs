// שאלות למשתמש שחוזרות עד שמתקבלת תשובה חוקית
public static class ConsoleHelper
{
    // שאלה עם תשובת חובה (לא ריקה)
    public static string AskRequired(string question)
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
    public static string? AskOptional(string question, string hint = "Enter to skip")
    {
        Console.Write($"{question} ({hint}): ");
        var answer = Console.ReadLine();
        return string.IsNullOrWhiteSpace(answer) ? null : answer.Trim();
    }

    // שואלת שאלת כן/לא עד שמתקבלת תשובה חוקית
    public static bool AskYesNo(string question)
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
    public static string AskChoice(string question, string[] choices, string defaultValue)
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
}
