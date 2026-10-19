using System.CommandLine;

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
    var options = new BundleOptions(
        parseResult.GetValue(outputOption)!,
        parseResult.GetValue(languageOption)!,
        parseResult.GetValue(noteOption),
        parseResult.GetValue(sortOption)!,
        parseResult.GetValue(removeEmptyLinesOption),
        parseResult.GetValue(authorOption));

    new Bundler().Run(options);
});

// ---------- פקודת create-rsp ----------

var createRspCommand = new Command("create-rsp", "Create a response file for the bundle command");
createRspCommand.SetAction(_ => new RspCreator().Run());

// ---------- פקודת השורש ----------

var rootCommand = new RootCommand("root command for file bundle");
rootCommand.Subcommands.Add(bundleCommand);
rootCommand.Subcommands.Add(createRspCommand);

return rootCommand.Parse(args).Invoke();
