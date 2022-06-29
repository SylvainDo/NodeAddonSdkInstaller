using Octokit;

namespace NodeAddonSdkInstaller;

internal class Setup
{
    private string m_tempDir;
    private string m_installDir;
    private string m_targetVersion;
    private string m_distBaseUrl;
    private HttpClient m_httpClient;

    public Setup(string targetVersion, string installPath)
    {
        if (File.Exists(installPath) || Directory.Exists(installPath))
            throw new Exception("Destination directory already exists");
        
        m_tempDir = Util.MakeDirTemp();
        m_installDir = installPath;
        m_targetVersion = targetVersion;
        m_distBaseUrl = $"https://nodejs.org/dist/v{targetVersion}/";
        m_httpClient = new HttpClient();
    }

    public async Task Execute()
    {
        Util.ConsoleWriteColored(ConsoleColor.Cyan, "Node headers");
        var headersDir = await DownloadHeadersAsync();

        Util.ConsoleWriteColored(ConsoleColor.Cyan, "\nNode libraries");
        await DownloadLibrariesAsync(headersDir);

        Util.ConsoleWriteColored(ConsoleColor.Cyan, "\nDelay load hook");
        await DownloadDelayLoadHookSourceAsync(headersDir);

        Util.ConsoleWriteColored(ConsoleColor.Cyan, "\nNode addon api");
        var addonApiDir = await DownloadAddonAPIAsync();

        Util.ConsoleWriteColored(ConsoleColor.Cyan, "\nInstall");
        await InstallAsync(headersDir, addonApiDir);

        Directory.Delete(m_tempDir);
    }

    private async Task<string> DownloadHeadersAsync()
    {
        var baseFileName = $"node-v{m_targetVersion}-headers";

        var downloadUri = new Uri(Path.Combine(m_distBaseUrl, $"{baseFileName}.tar.xz"));
        var archiveTarXz = Path.Combine(m_tempDir, $"{baseFileName}.tar.xz");
        var archiveTar = Path.Combine(m_tempDir, $"{baseFileName}.tar");

        Util.ConsoleWriteColored(ConsoleColor.Blue, "Downloading...");
        await Util.DownloadFileAsync(m_httpClient, downloadUri, archiveTarXz);

        Util.ConsoleWriteColored(ConsoleColor.Blue, "Extracting...");
        await Util.ExtractArchiveAsync(archiveTarXz, m_tempDir);
        await Util.ExtractArchiveAsync(archiveTar, m_tempDir);

        File.Delete(archiveTarXz);
        File.Delete(archiveTar);

        return Path.Combine(m_tempDir, $"node-v{m_targetVersion}");
    }

    private async Task<string> DownloadAddonAPIAsync()
    {
        Util.ConsoleWriteColored(ConsoleColor.Blue, "Fetching latest GitHub release...");

        var client = new GitHubClient(new ProductHeaderValue("NodeAddonSdkInstaller"));
        var latestRelease = (await client.Repository.Release.GetAll("nodejs", "node-addon-api"))[0];

        var downloadUri = new Uri($"https://github.com/nodejs/node-addon-api/archive/refs/tags/{latestRelease.TagName}.zip");
        var archiveDir = Path.Combine(m_tempDir, $"node-addon-api-{latestRelease.TagName.Substring(1)}");
        var archiveZip = $"{archiveDir}.zip";

        Util.ConsoleWriteColored(ConsoleColor.Blue, "Downloading...");
        await Util.DownloadFileAsync(m_httpClient, downloadUri, archiveZip);

        Util.ConsoleWriteColored(ConsoleColor.Blue, "Extracting...");
        await Util.ExtractArchiveAsync(archiveZip, m_tempDir);

        File.Delete(archiveZip);

        return archiveDir;
    }

    private async Task DownloadDelayLoadHookSourceAsync(string headersDir)
    {
        Util.ConsoleWriteColored(ConsoleColor.Blue, "Downloading...");

        var srcDir = Path.Combine(headersDir, "src");
        Directory.CreateDirectory(srcDir);

        var srcPath = Path.Combine(srcDir, "win_delay_load_hook.cc");
        var fileUri = new Uri("https://raw.githubusercontent.com/nodejs/node-gyp/master/src/win_delay_load_hook.cc");

        await Util.DownloadFileAsync(m_httpClient, fileUri, srcPath);
    }

    private async Task DownloadLibrariesAsync(string headersDir)
    {
        var downloadForArch = async (string arch) =>
        {
            Util.ConsoleWriteColored(ConsoleColor.Blue, $"Downloading {arch}...");

            var libUri = new Uri($"{m_distBaseUrl}/win-{arch}/node.lib");
            var tempLibPath = Path.Combine(m_tempDir, "node.lib");

            try
            {
                await Util.DownloadFileAsync(m_httpClient, libUri, tempLibPath);
            }
            catch (Exception e)
            {
                Util.ConsoleWriteColored(ConsoleColor.Red, e.Message);
            }

            if (File.Exists(tempLibPath))
            {
                var libDir = Path.Combine(headersDir, $"lib/{arch}");
                Directory.CreateDirectory(libDir);
                File.Move(tempLibPath, Path.Combine(libDir, "node.lib"));
            }
        };

        await downloadForArch("x86");
        await downloadForArch("x64");
    }

    private async Task InstallAsync(string headersDir, string addonApiDir)
    {
        Directory.CreateDirectory(m_installDir);

        Util.ConsoleWriteColored(ConsoleColor.Blue, $"Installing node development files...");
        await Util.CopyDirectory(headersDir, Path.Combine(m_installDir, Path.GetFileName(headersDir)));

        Util.ConsoleWriteColored(ConsoleColor.Blue, $"Installing node addon api development files...");
        await Util.CopyDirectory(addonApiDir, Path.Combine(m_installDir, Path.GetFileName(addonApiDir)));

        Environment.SetEnvironmentVariable("NODE_DEV_DIR", 
            Path.Combine(m_installDir, Path.GetFileName(headersDir)), 
            EnvironmentVariableTarget.User);
        Environment.SetEnvironmentVariable("NODE_ADDON_API_DEV_DIR", 
            Path.Combine(m_installDir, Path.GetFileName(addonApiDir)), 
            EnvironmentVariableTarget.User);
    }
}
