using System.Diagnostics;
using System.IO.Compression;

namespace Updater
{
    internal class Program
    {
        /*static void DrawProgressBar(int Percent)
        {
            int Filled = Percent * 50 / 100;
            Console.Write($"\r[{new string('█', Filled) + new string('-', 50 - Filled)}] {Percent}%");
        }*/
        /*static void DrawProgressBar(int percent)
        {
            // Clamp to 0–100 to avoid out of range errors
            percent = Math.Max(0, Math.Min(100, percent));

            const int barLength = 50; // length of the bar
            int filled = (percent * barLength) / 100;
            string bar = new string('█', filled) + new string('-', barLength - filled);

            Console.Write($"\r[{bar}] {percent}%");
        }*/

        static async Task<int> Main()
        {
            string ApplicationDirectory = AppDomain.CurrentDomain.BaseDirectory;
            string TemporaryZip = Path.Combine(Path.GetTempPath(), "SLBrUpdate.zip");

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

                /*using (HttpClient Client = new HttpClient())
                {
                    using (HttpResponseMessage Response = await Client.GetAsync("https://github.com/SLT-World/SLBr/releases/latest/download/SLBr.zip", HttpCompletionOption.ResponseHeadersRead))
                    {
                        Response.EnsureSuccessStatusCode();

                        long? TotalBytes = Response.Content.Headers.ContentLength;

                        using (Stream? _Stream = await Response.Content.ReadAsStreamAsync())
                        {
                            using (FileStream _FileStream = new FileStream(TemporaryZip, FileMode.Create, FileAccess.Write, FileShare.None))
                            {
                                byte[] Buffer = new byte[81920];
                                long TotalRead = 0;
                                int Read;
                                int LastProgress = -1;
                                Read = await _Stream.ReadAsync(Buffer, 0, Buffer.Length);
                                while (Read > 0)
                                {
                                    await _FileStream.WriteAsync(Buffer, 0, Read);
                                    TotalRead += Read;
                                    if (TotalBytes.HasValue)
                                    {
                                        int Progress = (int)(TotalRead * 100L / TotalBytes.Value);
                                        if (Progress != LastProgress)
                                        {
                                            LastProgress = Progress;
                                            DrawProgressBar(Progress);
                                        }
                                    }
                                }
                            }
                        }
                        Console.WriteLine();
                    }
                }*/

                using (HttpClient client = new HttpClient())
                using (HttpResponseMessage? Response = await client.GetAsync("https://github.com/SLT-World/SLBr/releases/latest/download/SLBr.zip"))
                {
                    Response.EnsureSuccessStatusCode();
                    using (FileStream _FileStream = new FileStream(TemporaryZip, FileMode.Create))
                    {
                        await Response.Content.CopyToAsync(_FileStream);
                    }
                }

                Console.WriteLine("Extracting update...");
                string ExtractDirectory = Path.Combine(Path.GetTempPath(), "SLBrUpdate_Extract");
                if (Directory.Exists(ExtractDirectory))
                    Directory.Delete(ExtractDirectory, true);
                ZipFile.ExtractToDirectory(TemporaryZip, ExtractDirectory);
                foreach (string _File in Directory.GetFiles(ExtractDirectory, "*", SearchOption.AllDirectories))
                {
                    string Destination = Path.Combine(ApplicationDirectory, _File.Substring(ExtractDirectory.Length + 1));
                    Directory.CreateDirectory(Path.GetDirectoryName(Destination)!);
                    File.Copy(_File, Destination, true);
                }

                Console.WriteLine("Restarting SLBr...");
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
            }
        }
    }
}
