using System.Diagnostics;
using System.IO.Compression;
using System.Net;
using System.Net.Sockets;
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

        private static async ValueTask<Stream> ConnectCallback(SocketsHttpConnectionContext Context, CancellationToken CancellationToken)
        {
            DnsEndPoint EndPoint = Context.DnsEndPoint;
            try
            {
                IPAddress[] ResolvedAddresses = await Dns.GetHostAddressesAsync(EndPoint.Host, CancellationToken).ConfigureAwait(false);
                if (ResolvedAddresses == null || ResolvedAddresses.Length == 0)
                    throw new Exception($"Host {EndPoint.Host} resolved to no IPs.");

                Socket? _CustomSocket = await HappyEyeballs.ParallelTask(HappyEyeballs.SortInterleaved(ResolvedAddresses), EndPoint.Port, TimeSpan.FromMilliseconds(HappyEyeballs.ConnectionAttemptDelay), CancellationToken).ConfigureAwait(false);
                return new NetworkStream(_CustomSocket, true);
            }
            catch { }
            Socket _Socket = new(SocketType.Stream, ProtocolType.Tcp) { NoDelay = true };
            try
            {
                await _Socket.ConnectAsync(EndPoint, CancellationToken);
                return new NetworkStream(_Socket, true);
            }
            catch
            {
                _Socket.Dispose();
                throw;
            }
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

                using (HttpClient Client = new(new SocketsHttpHandler
                {
                    AutomaticDecompression = DecompressionMethods.All,
                    EnableMultipleHttp2Connections = true,
                    EnableMultipleHttp3Connections = true,
                    AllowAutoRedirect = true,
                    MaxConnectionsPerServer = 256,
                    PooledConnectionLifetime = TimeSpan.FromMinutes(15),
                    ConnectCallback = ConnectCallback
                })
                {
                    DefaultVersionPolicy = HttpVersionPolicy.RequestVersionOrHigher
                })
                {
                    Client.Timeout = TimeSpan.FromMinutes(15);
                    Client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/141.0.0.0 Safari/537.36");

                    using (HttpResponseMessage Response = await Client.GetAsync("https://github.com/SLT-World/SLBr/releases/latest/download/SLBr.zip", HttpCompletionOption.ResponseHeadersRead))
                    {
                        Response.EnsureSuccessStatusCode();

                        long? TotalBytes = Response.Content.Headers.ContentLength;

                        using (Stream? _Stream = await Response.Content.ReadAsStreamAsync())
                        {
                            using (FileStream _FileStream = new(TemporaryZip, FileMode.Create, FileAccess.Write, FileShare.None))
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

    public static class HappyEyeballs
    {
        public const int ConnectionAttemptDelay = 250;

        private static async Task<Socket> AttemptConnection(IPAddress Address, int Port, CancellationToken Token)
        {
            Socket _Socket = new(Address.AddressFamily, SocketType.Stream, ProtocolType.Tcp)
            {
                NoDelay = true
            };

            try
            {
                await _Socket.ConnectAsync(new IPEndPoint(Address, Port), Token).ConfigureAwait(false);
                return _Socket;
            }
            catch
            {
                _Socket.Dispose();
                throw;
            }
        }

        public static IPAddress[] SortInterleaved(IPAddress[] Addresses)
        {
            int IPv6Count = 0;
            int IPv4Count = 0;
            for (int i = 0; i < Addresses.Length; i++)
            {
                if (Addresses[i].AddressFamily == AddressFamily.InterNetworkV6)
                    IPv6Count++;
                else if (Addresses[i].AddressFamily == AddressFamily.InterNetwork)
                    IPv4Count++;
            }
            var Result = new IPAddress[Addresses.Length];
            int IPv6Idx = 0;
            int IPv4Idx = 1;
            for (int i = 0; i < Addresses.Length; i++)
            {
                IPAddress IP = Addresses[i];
                if (IP.AddressFamily == AddressFamily.InterNetworkV6 && IPv6Idx < Result.Length)
                {
                    Result[IPv6Idx] = IP;
                    IPv6Idx += 2;
                }
                else if (IP.AddressFamily == AddressFamily.InterNetwork && IPv4Idx < Result.Length)
                {
                    Result[IPv4Idx] = IP;
                    IPv4Idx += 2;
                }
            }
            int DestinationIdx = 0;
            for (int i = 0; i < Addresses.Length; i++)
            {
                IPAddress IP = Addresses[i];
                bool AlreadyExists = false;

                for (int j = 0; j < Result.Length; j++)
                {
                    if (Result[j] != null && Result[j].Equals(IP))
                    {
                        AlreadyExists = true;
                        break;
                    }
                }

                if (!AlreadyExists && DestinationIdx < Result.Length)
                {
                    while (DestinationIdx < Result.Length && Result[DestinationIdx] != null)
                    {
                        DestinationIdx++;
                    }
                    if (DestinationIdx < Result.Length)
                        Result[DestinationIdx] = IP;
                }
            }

            return Result;
        }

        internal static async Task<Socket> ParallelTask(IPAddress[] Addresses, int Port, TimeSpan Delay, CancellationToken Token)
        {
            int CandidateCount = Addresses.Length;
            using CancellationTokenSource SuccessCTS = CancellationTokenSource.CreateLinkedTokenSource(Token);

            Task<Socket>[] AllTasks = new Task<Socket>[CandidateCount];
            List<Task> Tasks = [with(CandidateCount + 1)];

            Task<Socket>? SuccessTask = null;
            int LaunchedCount = 0;

            while (SuccessTask == null && (LaunchedCount < CandidateCount || Tasks.Count > 0))
            {
                if (LaunchedCount < CandidateCount)
                {
                    Task<Socket> NewTask = AttemptConnection(Addresses[LaunchedCount], Port, SuccessCTS.Token);
                    AllTasks[LaunchedCount] = NewTask;
                    Tasks.Add(NewTask);
                    LaunchedCount++;
                }

                if (Tasks.Count == 0) break;

                Task CompletedTask;
                if (LaunchedCount < CandidateCount)
                {
                    Task TimeoutTask = Task.Delay(Delay, SuccessCTS.Token);
                    Tasks.Add(TimeoutTask);

                    CompletedTask = await Task.WhenAny(Tasks).ConfigureAwait(false);

                    Tasks.Remove(TimeoutTask);
                    if (CompletedTask == TimeoutTask)
                        continue;
                }
                else
                    CompletedTask = await Task.WhenAny(Tasks).ConfigureAwait(false);

                Tasks.Remove(CompletedTask);

                if (CompletedTask is Task<Socket> SocketTask && SocketTask.IsCompletedSuccessfully)
                {
                    SuccessTask = SocketTask;
                    break;
                }
            }

            Token.ThrowIfCancellationRequested();
            await SuccessCTS.CancelAsync().ConfigureAwait(false);

            if (SuccessTask == null)
            {
                List<Exception> Exceptions = [];
                for (int i = 0; i < LaunchedCount; i++)
                {
                    if (AllTasks[i]?.IsFaulted == true)
                        Exceptions.AddRange(AllTasks[i].Exception!.InnerExceptions);
                }
                throw new AggregateException(Exceptions);
            }

            for (int i = 0; i < LaunchedCount; i++)
            {
                if (AllTasks[i] != SuccessTask && AllTasks[i]?.IsCompletedSuccessfully == true)
                    AllTasks[i].Result.Dispose();
            }
            return SuccessTask.Result;
        }
    }
}
