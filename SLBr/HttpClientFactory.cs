using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;

namespace SLBr
{
    public static class HttpClientFactory
    {
        public static bool IsHappyEyeballsEnabled = true;

        private static async ValueTask<Stream> ConnectCallback(SocketsHttpConnectionContext Context, CancellationToken CancellationToken)
        {
            DnsEndPoint EndPoint = Context.DnsEndPoint;
            if (IsHappyEyeballsEnabled)
            {
                IPAddress[] ResolvedAddresses = await Dns.GetHostAddressesAsync(EndPoint.Host, CancellationToken).ConfigureAwait(false);
                if (ResolvedAddresses == null || ResolvedAddresses.Length == 0)
                    throw new Exception($"Host {EndPoint.Host} resolved to no IPs.");

                Socket? _CustomSocket = await HappyEyeballs.ParallelTask(HappyEyeballs.SortInterleaved(ResolvedAddresses), EndPoint.Port, TimeSpan.FromMilliseconds(HappyEyeballs.ConnectionAttemptDelay), CancellationToken).ConfigureAwait(false);
                return new NetworkStream(_CustomSocket, true);
            }
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

        public static HttpClient Create(SocketsHttpHandler SocketsHandler, TimeSpan? _Timeout = null)
        {
            //SocketsHandler.EnableMultipleHttp3Connections = false;
            //SocketsHandler.EnableMultipleHttp2Connections = false;
            SocketsHandler.ConnectCallback = ConnectCallback;
            HttpClient Client = new(SocketsHandler)
            {
                DefaultVersionPolicy = HttpVersionPolicy.RequestVersionOrHigher
            };
            if (_Timeout.HasValue)
                Client.Timeout = _Timeout.Value;
            return Client;
        }
    }

    //https://slugcat.systems/post/24-06-16-ipv6-is-hard-happy-eyeballs-dotnet-httpclient/
    //https://24days.in/umbraco-cms/2025/timeout-mystery/
    //https://datatracker.ietf.org/doc/html/rfc8305
    //TODO: Deprecate on .NET 11.
    //Alternatively, support Happy Eyeballs v3, racing QUIC (UDP) against TCP, involving QUIC-IPv6, QUIC-IPv4, TCP-IPv6, TCP-IPv4
    //Happy Eyeballs v3 has yet to be implemented in .NET 11 so far.
    //https://datatracker.ietf.org/doc/draft-ietf-happy-happyeyeballs-v3/
    //https://daniel.haxx.se/blog/2025/08/04/even-happier-eyeballs/
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

                if (CompletedTask is Task<Socket> socketTask && socketTask.IsCompletedSuccessfully)
                {
                    SuccessTask = socketTask;
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
