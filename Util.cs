using System.Diagnostics;

namespace NodeAddonSdkInstaller;

internal static class Util
{
    public static void ConsoleWriteColored(ConsoleColor fgColor, string format, params object?[]? arg)
    {
        var _fgColor = Console.ForegroundColor;
        Console.ForegroundColor = fgColor;
        Console.WriteLine(format, arg);
        Console.ForegroundColor = _fgColor;
    }

    public static string MakeDirTemp()
    {
        var path = Path.Combine(Path.GetTempPath(), $"{AppDomain.CurrentDomain.FriendlyName}_{Path.GetRandomFileName()}");
        
        if (File.Exists(path))
            throw new Exception("Temporary directory already exists");

        Directory.CreateDirectory(path);

        if (!Directory.Exists(path)) 
            throw new Exception("Couldn't create temporary directory");

        return path;
    }

    public static async Task DownloadFileAsync(HttpClient client, Uri uri, string fileName)
    {
        var data = await client.GetByteArrayAsync(uri);
        await File.WriteAllBytesAsync(fileName, data);
    }

    public static async Task ExtractArchiveAsync(string path, string outputDir)
    {
        var _7z = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "7z",
                Arguments = $"x {path} -o{outputDir}"
            }
        };
        _7z.Start();
        await _7z.WaitForExitAsync();

        if (_7z.ExitCode != 0)
            throw new Exception("Couldn't extract archive: 7z command error");
    }

    public static async Task CopyDirectory(string src, string dst)
    {
        if (Path.GetPathRoot(src) == Path.GetPathRoot(dst))
            Directory.Move(src, dst);
        else
        {
            var xcopy = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "cmd",
                    Arguments = $"/C xcopy {src} {dst} /E/H/C/I/Q > nul"
                }
            };
            xcopy.Start();
            await xcopy.WaitForExitAsync();

            if (xcopy.ExitCode != 0)
                throw new Exception("Couldn't copy directory: xcopy command error");

            var rmdir = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "cmd",
                    Arguments = $"/C rmdir /S/Q {src} > nul"
                }
            };
            rmdir.Start();
            await rmdir.WaitForExitAsync();

            if (rmdir.ExitCode != 0)
                throw new Exception("Couldn't copy directory: rmdir command error");
        }
    }
}
