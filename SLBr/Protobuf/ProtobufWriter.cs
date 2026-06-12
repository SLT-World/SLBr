/*Copyright © SLT Softwares. All rights reserved.
Use of this source code is governed by a GNU license that can be found in the LICENSE file.*/

using System.Runtime.CompilerServices;
using System.Text;

namespace SLBr.Protobuf
{
    public ref struct ProtobufWriter
    {
        private readonly Span<byte> Buffer;
        public int Position;

        public ProtobufWriter(Span<byte> _Buffer)
        {
            Buffer = _Buffer;
            Position = 0;
        }

        public ReadOnlySpan<byte> WrittenSpan => Buffer[..Position];

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteTag(int FieldNumber, int WireType)
        {
            WriteVarint((uint)((FieldNumber << 3) | WireType));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteVarint(uint Value)
        {
            while (Value >= 0x80)
            {
                Buffer[Position++] = (byte)((Value & 0x7F) | 0x80);
                Value >>= 7;
            }
            Buffer[Position++] = (byte)Value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteVarint(ulong Value)
        {
            while (Value >= 0x80)
            {
                Buffer[Position++] = (byte)((Value & 0x7F) | 0x80);
                Value >>= 7;
            }
            Buffer[Position++] = (byte)Value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteString(int FieldNumber, string Value)
        {
            if (string.IsNullOrEmpty(Value))
                return;

            int ByteCount = Encoding.UTF8.GetByteCount(Value);

            WriteTag(FieldNumber, 2);
            WriteVarint((uint)ByteCount);

            Encoding.UTF8.GetBytes(Value, Buffer.Slice(Position, ByteCount));
            Position += ByteCount;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteBytes(int FieldNumber, scoped ReadOnlySpan<byte> Value)
        {
            if (Value.IsEmpty)
                return;

            WriteTag(FieldNumber, 2);
            WriteVarint((uint)Value.Length);

            Value.CopyTo(Buffer[Position..]);
            Position += Value.Length;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteInt64(int FieldNumber, long Value)
        {
            WriteTag(FieldNumber, 0);
            WriteVarint((ulong)Value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteBool(int FieldNumber, bool Value)
        {
            WriteTag(FieldNumber, 0);
            Buffer[Position++] = (byte)(Value ? 1 : 0);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteEnum(int FieldNumber, int Value)
        {
            WriteTag(FieldNumber, 0);
            WriteVarint((uint)Value);
        }
    }
}
