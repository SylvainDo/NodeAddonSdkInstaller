using System.CommandLine;

namespace NodeAddonSdkInstaller;

public static class Program
{
    public static Task<int> Main(string[] args)
    {
        var targetVersionOption = new Option<string>(
            new string[] { "--target", "-t" },
            "Node.js version");
        var installDirOption = new Option<FileInfo>(
            new string[] { "--installDir", "-i", "--destDir", "-d" },
            "Destination directory");

        var rootCommand = new RootCommand
        {
            targetVersionOption,
            installDirOption
        };
        rootCommand.Description = "Command line tool to install development libraries for building Node.js (Node-API) addons";

        rootCommand.SetHandler(async (string? targetVersion, FileInfo? installDir) =>
        {
            if (targetVersion is null || installDir is null)
            {
                Util.ConsoleWriteColored(ConsoleColor.Red, "Invalid arguments.\n");
                rootCommand.Invoke("-h");
            }
            else
            {
                try
                {
                    await new Setup(targetVersion, installDir.FullName).Execute();
                }
                catch (Exception ex)
                {
                    Util.ConsoleWriteColored(ConsoleColor.Red, "Failed to install sdk: {0}", ex.Message);
                }
            }
        }, targetVersionOption, installDirOption);

        return rootCommand.InvokeAsync(args);
    }
}
