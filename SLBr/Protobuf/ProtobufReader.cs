/*Copyright © SLT Softwares. All rights reserved.
Use of this source code is governed by a GNU license that can be found in the LICENSE file.*/

using System.Runtime.CompilerServices;

namespace SLBr.Protobuf
{
    //https://protobuf.dev/programming-guides/encoding/
    //https://kreya.app/blog/protocolbuffers-wire-format/
    public ref struct ProtobufReader
    {
        private readonly ReadOnlySpan<byte> Buffer;
        private int Index;

        public ProtobufReader(ReadOnlySpan<byte> _Buffer)
        {
            Buffer = _Buffer;
            Index = 0;
        }

        public bool IsConsumed => Index >= Buffer.Length;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryReadTag(out int FieldNumber, out int WireType)
        {
            if (Index >= Buffer.Length)
            {
                FieldNumber = 0;
                WireType = 0;
                return false;
            }
            byte Tag = Buffer[Index++];
            FieldNumber = Tag >> 3;
            WireType = Tag & 0x07;
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int ReadVarint()
        {
            int Result = 0;
            int Shift = 0;
            while (Index < Buffer.Length)
            {
                byte Tag = Buffer[Index++];
                Result |= (Tag & 0x7F) << Shift;
                if ((Tag & 0x80) == 0) return Result;
                Shift += 7;
            }
            return Result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ReadOnlySpan<byte> ReadLengthDelimited()
        {
            int Length = ReadVarint();
            if (Index + Length > Buffer.Length)
                Length = Buffer.Length - Index;
            ReadOnlySpan<byte> Slice = Buffer.Slice(Index, Length);
            Index += Length;
            return Slice;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SkipField(int WireType)
        {
            switch (WireType)
            {
                case 0:
                    ReadVarint();
                    break;
                case 1:
                    Index = Math.Min(Index + 8, Buffer.Length);
                    break;
                case 2:
                    Index = Math.Min(Index + ReadVarint(), Buffer.Length);
                    break;
                case 5:
                    Index = Math.Min(Index + 4, Buffer.Length);
                    break;
            }
        }
    }
}
