using System.Diagnostics;
using System.IO.Compression;
using System.Runtime.InteropServices;

namespace Updater
{
    internal class Program
    {
        [DllImport("kernel32.dll", SetLastError = true)]
        static extern IntPtr GetStdHandle(int nStdHandle);

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool GetConsoleMode(IntPtr hConsoleHandle, out int lpMode);

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool SetConsoleMode(IntPtr hConsoleHandle, int dwMode);

        const int STD_INPUT_HANDLE = -10;
        const int ENABLE_QUICK_EDIT_MODE = 0x0040;
        const int ENABLE_INSERT_MODE = 0x0020;

        static void DisableQuickEdit()
        {
            IntPtr ConsoleHandle = GetStdHandle(STD_INPUT_HANDLE);
            if (GetConsoleMode(ConsoleHandle, out int Mode))
            {
                Mode &= ~ENABLE_QUICK_EDIT_MODE;
                Mode &= ~ENABLE_INSERT_MODE;
                SetConsoleMode(ConsoleHandle, Mode);
            }
        }

        const int BarLength = 50;
        static void DrawProgressBar(int Percent, long BytesRead, long TotalBytes)
        {
            Percent = Math.Max(0, Math.Min(100, Percent));
            int Filled = Percent * BarLength / 100;

            double MBRead = BytesRead / 1024.0 / 1024.0;
            double MBTotal = TotalBytes / 1024.0 / 1024.0;

            Console.Write($"\r[{new string('█', Filled) + new string('-', BarLength - Filled)}] { Percent,3}% ({MBRead:F1}/{MBTotal:F1} MB)");

            /*Percent = Math.Max(0, Math.Min(100, Percent));
            const int BarLength = 50;
            int Filled = (Percent * BarLength) / 100;
            Console.Write($"\r[{new string('█', Filled) + new string('-', BarLength - Filled)}] {Percent}%");*/
        }

        static async Task<int> Main(string[] args)
        {
            DisableQuickEdit();
            string ApplicationDirectory = args.Length > 0 ? args[0] : AppDomain.CurrentDomain.BaseDirectory;
            string TemporaryZip = Path.Combine(Path.GetTempPath(), "SLBrUpdate.zip");
            string ExtractDirectory = Path.Combine(Path.GetTempPath(), "SLBrUpdate_Extract");

            try
            {
                Console.WriteLine("Killing SLBr processes...");
                foreach (Process _Process in Process.GetProcessesByName("SLBr"))
                {
                    try
                    {
                        _Process.Kill();
                        _Process.WaitForExit();
                    }
                    catch { }
                }

                Console.WriteLine("Downloading new update...");

                if (File.Exists(TemporaryZip))
                    File.Delete(TemporaryZip);
                using (HttpClient Client = new HttpClient(new HttpClientHandler
                {
                    AutomaticDecompression = System.Net.DecompressionMethods.All,
                    AllowAutoRedirect = true,
                    MaxConnectionsPerServer = 256
                }))
                {
                    Client.Timeout = TimeSpan.FromMinutes(15);
                    Client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/141.0.0.0 Safari/537.36");

                    using (HttpResponseMessage Response = await Client.GetAsync("https://github.com/SLT-World/SLBr/releases/latest/download/SLBr.zip", HttpCompletionOption.ResponseHeadersRead))
                    {
                        Response.EnsureSuccessStatusCode();

                        long? TotalBytes = Response.Content.Headers.ContentLength;

                        using (Stream? _Stream = await Response.Content.ReadAsStreamAsync())
                        {
                            using (FileStream _FileStream = new FileStream(TemporaryZip, FileMode.Create, FileAccess.Write, FileShare.None))
                            {
                                byte[] Buffer = new byte[1048576];//1024 * 512 * 2
                                long TotalRead = 0;
                                int Read;
                                int LastProgress = -1;
                                while ((Read = await _Stream.ReadAsync(Buffer, 0, Buffer.Length)) > 0)
                                {
                                    await _FileStream.WriteAsync(Buffer, 0, Read);
                                    TotalRead += Read;

                                    if (TotalBytes.HasValue)
                                    {
                                        int Progress = (int)(TotalRead * 100L / TotalBytes.Value);
                                        if (Progress != LastProgress)
                                        {
                                            LastProgress = Progress;
                                            DrawProgressBar(Progress, TotalRead, TotalBytes.Value);
                                        }
                                    }
                                }
                            }
                        }
                        Console.WriteLine();
                    }
                }

                Console.WriteLine("Extracting update...");
                if (Directory.Exists(ExtractDirectory))
                    Directory.Delete(ExtractDirectory, true);
                ZipFile.ExtractToDirectory(TemporaryZip, ExtractDirectory);
                foreach (string _File in Directory.GetFiles(ExtractDirectory, "*", SearchOption.AllDirectories))
                {
                    string Destination = Path.Combine(ApplicationDirectory, _File.Substring(ExtractDirectory.Length + 1));
                    Directory.CreateDirectory(Path.GetDirectoryName(Destination)!);
                    File.Copy(_File, Destination, true);
                }

                Console.WriteLine("Update complete. Restarting SLBr...");
                Process.Start(Path.Combine(ApplicationDirectory, "SLBr.exe"));


                return 0;
            }
            catch (Exception Error)
            {
                Console.WriteLine("Update failed: " + Error.Message);
                return 1;
            }
            finally
            {
                if (File.Exists(TemporaryZip))
                    File.Delete(TemporaryZip);
                if (Directory.Exists(ExtractDirectory))
                    Directory.Delete(ExtractDirectory, true);

                string ExecutablePath = Environment.ProcessPath!;
                if (ExecutablePath.StartsWith(Path.GetTempPath(), StringComparison.OrdinalIgnoreCase))
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = "cmd.exe",
                        Arguments = $"/C timeout 2 & del \"{ExecutablePath}\"",
                        WindowStyle = ProcessWindowStyle.Hidden,
                        CreateNoWindow = true
                    });
                }
            }
        }
    }
}
