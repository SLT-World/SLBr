using System.Buffers;
using System.Collections.Concurrent;
using System.Runtime.InteropServices;

namespace SLBr.Handlers
{
    public partial class AdBlockHandler
    {
        public readonly DomainList Domains = new();

        public FastHashSet<string> Whitelist = [];

        private ConcurrentDictionary<string, bool> HostCache = [];

        public AdBlockHandler(Saving _Saving)
        {
            int AllowListCount = _Saving.GetInt("Count", -1);
            if (AllowListCount != -1)
            {
                for (int i = 0; i < AllowListCount; i++)
                    Whitelist.Add(_Saving.Get($"{i}"));
            }
            else
            {
                Whitelist.Add("ecosia.org");
                Whitelist.Add("youtube.com");
            }
        }

        public bool ShouldBlockRequest(string Url, string FocusedUrl, ResourceRequestType _Type)
        {
            if (Domains.AllDomains.Count == 0)
                return false;
            if (Whitelist.Contains(Utils.FastHost(FocusedUrl.Length == 0 ? Url : FocusedUrl)))
                return false;
            if (_Type == ResourceRequestType.Ping)
                return true;
            if (IsPossiblyAd(_Type))
            {
                if (IsHostBlocked(Utils.FastHost(Url)))
                    return true;
            }
            return false;
        }

        private bool IsHostBlocked(string Host)
        {
            if (HostCache.TryGetValue(Host, out bool IsBlocked))
                return IsBlocked;
            if (Domains.Has(Host))
            {
                HostCache[Host] = true;
                return true;
            }
            HostCache[Host] = false;
            return false;
        }

        private static bool IsPossiblyAd(ResourceRequestType _ResourceType) =>
             _ResourceType is ResourceRequestType.XMLHTTPRequest or ResourceRequestType.Media or ResourceRequestType.Script or ResourceRequestType.SubFrame or ResourceRequestType.Image;

        //https://adblockplus.org/filter-cheatsheet
        public unsafe void ParseAdd(string Data)
        {
            if (string.IsNullOrEmpty(Data)) return;

            //SearchValues<char> _SearchValues = SearchValues.Create(" /?#!:@[]*&^%$#@!");
            int ProcessorCount = Environment.ProcessorCount;
            int TotalLength = Data.Length;
            int ChunkTarget = TotalLength / ProcessorCount;
            GCHandle Handle = GCHandle.Alloc(Data, GCHandleType.Pinned);

            try
            {
                IntPtr BaseAddress = Handle.AddrOfPinnedObject();
                Parallel.For(0, ProcessorCount, () => new List<string>(4096), (ChunkIndex, State, LocalList) =>
                {
                    char* BasePointer = (char*)BaseAddress;

                    int StartOffset = ChunkIndex * ChunkTarget;
                    int EndOffset = (ChunkIndex == ProcessorCount - 1) ? TotalLength : (ChunkIndex + 1) * ChunkTarget;
                    if (ChunkIndex > 0)
                    {
                        while (StartOffset < TotalLength && BasePointer[StartOffset - 1] != '\n')
                        {
                            StartOffset++;
                        }
                    }
                    if (ChunkIndex < ProcessorCount - 1)
                    {
                        while (EndOffset < TotalLength && BasePointer[EndOffset - 1] != '\n')
                        {
                            EndOffset++;
                        }
                    }

                    int CurrentPosition = StartOffset;
                    while (CurrentPosition < EndOffset)
                    {
                        int LineEnd = CurrentPosition;
                        while (LineEnd < EndOffset && BasePointer[LineEnd] != '\r' && BasePointer[LineEnd] != '\n')
                        {
                            LineEnd++;
                        }

                        int LineLength = LineEnd - CurrentPosition;
                        if (LineLength > 0)
                        {
                            ReadOnlySpan<char> Span = new ReadOnlySpan<char>(BasePointer + CurrentPosition, LineLength).Trim();
                            if (!Span.IsEmpty && Span[0] is not '#' and not '!' and not '[' and not '@')
                            {
                                ReadOnlySpan<char> Domain = [];
                                if (Span.StartsWith("||"))
                                {
                                    Span = Span[2..];
                                    int CaretIndex = Span.IndexOf('^');
                                    if (CaretIndex != -1)
                                        Domain = Span[..CaretIndex];
                                }
                                else if (Span.StartsWith("0.0.0.0 "))
                                {
                                    Span = Span[8..].TrimStart(' ');
                                    int SpaceIndex = Span.IndexOf(' ');
                                    Domain = SpaceIndex != -1 ? Span[..SpaceIndex] : Span;
                                }
                                else if (Span.StartsWith("127.0.0.1 "))
                                {
                                    Span = Span[10..].TrimStart(' ');
                                    int SpaceIndex = Span.IndexOf(' ');
                                    Domain = SpaceIndex != -1 ? Span[..SpaceIndex] : Span;
                                }
                                /*else
                                {
                                    if (Span.IndexOfAny(_SearchValues) == -1)
                                        Domain = Span;
                                }*/

                                if (!Domain.IsEmpty)
                                    LocalList.Add(Domain.ToString());
                            }
                        }

                        CurrentPosition = LineEnd;
                        while (CurrentPosition < EndOffset && (BasePointer[CurrentPosition] == '\r' || BasePointer[CurrentPosition] == '\n'))
                        {
                            CurrentPosition++;
                        }
                    }
                    return LocalList;
                },
                (FinalLocalList) =>
                {
                    lock (Domains)
                    {
                        foreach (string Domain in FinalLocalList)
                        {
                            Domains.Add(Domain);
                        }
                    }
                });
            }
            finally
            {
                if (Handle.IsAllocated)
                {
                    Handle.Free();
                }
            }
            //TODO: Support keywords.
            /*if (!Sanitized.StartsWith("@@") && Sanitized.Contains("##"))
            {
                string SelectorPart = Sanitized.Split("##", 2)[1];
                if (SelectorPart.Contains('[') && SelectorPart.Contains("*="))
                {
                    Match _Match = AttributeSelectorRegex().Match(SelectorPart);
                    if (_Match.Success)
                    {
                        string Keyword = _Match.Groups[1].Value.ToLower();
                        if (!string.IsNullOrEmpty(Keyword))
                            Keywords.Add(Keyword);
                    }
                }
            }*/
        }

        /*[GeneratedRegex(@"\*=""([^""]+)""")]
        private static partial Regex AttributeSelectorRegex();*/
    }
}
