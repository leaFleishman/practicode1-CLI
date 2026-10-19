// הערכים שהמשתמש בחר בפקודת bundle
public record BundleOptions(
    FileInfo Output,
    string[] Languages,
    bool Note,
    string Sort,
    bool RemoveEmptyLines,
    string? Author);
