/*Copyright © SLT Softwares. All rights reserved.
Use of this source code is governed by a GNU license that can be found in the LICENSE file.*/

using System.Buffers.Binary;
using System.Runtime.CompilerServices;
using System.Windows;

namespace SLBr.Protobuf
{
    //https://protobuf.dev/programming-guides/encoding/
    public static class SafeBrowsingV5Parser
    {
        public unsafe ref struct SearchHashesResponse
        {
            private fixed byte _HashesBuffer[52];
            private byte _Count;

            public readonly int Count => _Count;

            public readonly ReadOnlySpan<FullHash> FullHashes
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get { fixed (byte* p = _HashesBuffer) return new(p, _Count); }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void AddFullHash(in FullHash Hash)
            {
                if (_Count < 4)
                {
                    fixed (byte* p = _HashesBuffer)
                    {
                        Span<FullHash> Span = new(p, 4);
                        Span[_Count++] = Hash;
                    }
                }
            }
        }

        public unsafe struct FullHash
        {
            private fixed byte _Hash[32];
            private fixed int _ThreatTypes[4];
            private byte _ThreatCount;
            private byte _HasHash;

            public readonly bool IsComplete => _HasHash != 0 && _ThreatCount > 0;

            public readonly ReadOnlySpan<byte> HashBytes
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get { fixed (byte* p = _Hash) return new ReadOnlySpan<byte>(p, 32); }
            }

            public readonly ReadOnlySpan<int> ThreatTypes
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get { fixed (int* p = _ThreatTypes) return new ReadOnlySpan<int>(p, _ThreatCount); }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void SetHash(ReadOnlySpan<byte> Hash)
            {
                int CopyLength = Math.Min(Hash.Length, 32);
                fixed (byte* d = _Hash)
                {
                    fixed (byte* s = Hash) Unsafe.CopyBlockUnaligned(d, s, (uint)CopyLength);
                }
                _HasHash = 1;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void AddThreatType(int Threat)
            {
                if (_ThreatCount < 4)
                {
                    fixed (int* p = _ThreatTypes)
                    {
                        p[_ThreatCount++] = Threat;
                    }
                }
            }
        }

        public static SearchHashesResponse ParseResponse(ReadOnlySpan<byte> Buffer)
        {
            SearchHashesResponse Result = new();
            try
            {
                if (Buffer.Length >= 5 && Buffer[0] == 0x00)
                {
                    int MessageLength = BinaryPrimitives.ReadInt32BigEndian(Buffer.Slice(1, 4));
                    if (Buffer.Length >= 5 + MessageLength)
                        Buffer = Buffer.Slice(5, MessageLength);
                }

                ProtobufReader Reader = new(Buffer);
                while (!Reader.IsConsumed)
                {
                    if (!Reader.TryReadTag(out int FieldNumber, out int WireType))
                        break;
                    if (FieldNumber == 1 && WireType == 2)
                    {
                        Result.AddFullHash(ParseFullHash(Reader.ReadLengthDelimited()));
                        //if (Result.Count >= 4)
                        break;
                    }
                    else
                        Reader.SkipField(WireType);
                }
            }
#if DEBUG
            catch (Exception Ex)
            {
                MessageBox.Show($"Parser failure: {Ex.Message}");
            }
#else
            catch { }
#endif
            return Result;
        }

        private static FullHash ParseFullHash(ReadOnlySpan<byte> Buffer)
        {
            FullHash _FullHash = new();
            ProtobufReader Reader = new(Buffer);
            while (!Reader.IsConsumed)
            {
                if (!Reader.TryReadTag(out int FieldNumber, out int WireType))
                    break;
                if (WireType == 2 && FieldNumber == 1)
                    _FullHash.SetHash(Reader.ReadLengthDelimited());
                else if (WireType == 2 && FieldNumber == 2)
                {
                    ReadOnlySpan<byte> DetailPayload = Reader.ReadLengthDelimited();
                    ProtobufReader DetailReader = new(DetailPayload);
                    while (!DetailReader.IsConsumed)
                    {
                        if (!DetailReader.TryReadTag(out int DetailFieldNumber, out int DetailWireType))
                            break;
                        if (DetailWireType == 0 && DetailFieldNumber == 1)
                            _FullHash.AddThreatType(DetailReader.ReadVarint());
                        else
                            DetailReader.SkipField(DetailWireType);
                    }
                }
                else
                    Reader.SkipField(WireType);
                if (_FullHash.IsComplete)
                    break;
            }
            return _FullHash;
        }
    }
}
