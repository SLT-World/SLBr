/*Copyright © SLT Softwares. All rights reserved.
Use of this source code is governed by a GNU license that can be found in the LICENSE file.*/

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace SLBr
{
    public unsafe class DomainList : IDisposable
    {
        private UnmanagedNode* Root;
        private bool _Disposed;
        public readonly FastHashSet<string> AllDomains = [with(StringComparer.Ordinal)];

        private readonly List<IntPtr> ArenaChunks = [];
        private byte* CurrentChunkPtr;
        private nint RemainingChunkBytes;
        private const nint ChunkSize = 4 * 1024 * 1024;

        public DomainList()
        {
            AllocateNewArenaChunk();
            Root = Allocate<UnmanagedNode>();
        }

        public void Add(string Domain)
        {
            if (string.IsNullOrWhiteSpace(Domain) || !AllDomains.Add(Domain)) return;
            ReadOnlySpan<char> Span = Domain.AsSpan().Trim().TrimStart('.');

            UnmanagedNode* Node = Root;
            int End = Span.Length;

            while (End > 0)
            {
                int Dot = Span.Slice(0, End).LastIndexOf('.');
                ReadOnlySpan<char> label = Dot == -1 ? Span.Slice(0, End) : Span.Slice(Dot + 1, End - Dot - 1);
                End = Dot == -1 ? 0 : Dot;
                Node = GetOrCreateChildNative(Node, label);
            }

            Node->IsEnd = true;
        }

        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        public bool Has(string Domain)
        {
            if (string.IsNullOrEmpty(Domain)) return false;

            ReadOnlySpan<char> Span = Domain.AsSpan().TrimEnd('.');
            UnmanagedNode* Node = Root;
            int End = Span.Length;

            while (End > 0)
            {
                if (Node->IsEnd)
                    return true;

                int Dot = Span[..End].LastIndexOf('.');
                ReadOnlySpan<char> label = Dot == -1 ? Span[..End] : Span.Slice(Dot + 1, End - Dot - 1);
                End = Dot == -1 ? 0 : Dot;

                Node = TryGetChildNative(Node, label);
                if (Node == null)
                    return false;
            }

            return Node->IsEnd;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private UnmanagedNode* TryGetChildNative(UnmanagedNode* Parent, ReadOnlySpan<char> Label)
        {
            int Count = Parent->ChildCount;
            if (Count == 0)
                return null;
            if (Parent->HasHashTable != 0)
            {
                uint Hash = CalculateHash(Label);
                uint Mask = (uint)Parent->ChildCapacity - 1;
                uint Index = Hash & Mask;

                ChildLink* Table = Parent->Children;
                while (Table[Index].Label != null)
                {
                    ReadOnlySpan<char> CurrentLabelSpan = new(Table[Index].Label, Table[Index].LabelLength);
                    if (Label.Equals(CurrentLabelSpan, StringComparison.Ordinal))
                        return Table[Index].TargetNode;
                    Index = (Index + 1) & Mask;
                }
                return null;
            }

            ChildLink* Links = Parent->Children;
            for (int i = 0; i < Count; i++)
            {
                var Link = Links[i];
                ReadOnlySpan<char> CurrentLabelSpan = new(Link.Label, Link.LabelLength);
                if (Label.Equals(CurrentLabelSpan, StringComparison.Ordinal))
                    return Link.TargetNode;
            }
            return null;
        }

        private UnmanagedNode* GetOrCreateChildNative(UnmanagedNode* Parent, ReadOnlySpan<char> Label)
        {
            UnmanagedNode* Existing = TryGetChildNative(Parent, Label);
            if (Existing != null) return Existing;
            UnmanagedNode* NewNode = Allocate<UnmanagedNode>();
            int LabelBytes = Label.Length * sizeof(char);
            char* NativeLabelBuffer = (char*)AllocateRaw((nuint)LabelBytes);
            fixed (char* LabelPtr = Label)
            {
                Buffer.MemoryCopy(LabelPtr, NativeLabelBuffer, LabelBytes, LabelBytes);
            }

            int CurrentCount = Parent->ChildCount;
            if (Parent->HasHashTable != 0)
            {
                if (CurrentCount >= Parent->ChildCapacity * 3 / 4)
                    ResizeNativeHashTable(Parent);
                InsertIntoHashTable(Parent->Children, (uint)Parent->ChildCapacity, NativeLabelBuffer, Label.Length, NewNode);
                Parent->ChildCount++;
            }
            else if (CurrentCount < 4)
            {
                if (CurrentCount == 0)
                {
                    Parent->Children = AllocateArray<ChildLink>(4);
                    Parent->ChildCapacity = 4;
                }
                Parent->Children[CurrentCount].Label = NativeLabelBuffer;
                Parent->Children[CurrentCount].LabelLength = Label.Length;
                Parent->Children[CurrentCount].TargetNode = NewNode;
                Parent->ChildCount++;
            }
            else
                UpgradeToHashTable(Parent, NativeLabelBuffer, Label.Length, NewNode);
            return NewNode;
        }

        private void UpgradeToHashTable(UnmanagedNode* Parent, char* NewLabel, int NewLabelLen, UnmanagedNode* NewNode)
        {
            int OldEntries = Parent->ChildCount;
            int NewCapacity = 16;
            ChildLink* NewTable = AllocateArray<ChildLink>((nuint)NewCapacity);
            ChildLink* OldTable = Parent->Children;
            for (int i = 0; i < OldEntries; i++)
            {
                var Enry = OldTable[i];
                InsertIntoHashTable(NewTable, (uint)NewCapacity, Enry.Label, Enry.LabelLength, Enry.TargetNode);
            }
            InsertIntoHashTable(NewTable, (uint)NewCapacity, NewLabel, NewLabelLen, NewNode);

            Parent->Children = NewTable;
            Parent->ChildCapacity = NewCapacity;
            Parent->ChildCount = OldEntries + 1;
            Parent->HasHashTable = 1;
        }

        private void ResizeNativeHashTable(UnmanagedNode* Parent)
        {
            int OldCapacity = Parent->ChildCapacity;
            int NewCapacity = OldCapacity * 2;
            ChildLink* NewTable = AllocateArray<ChildLink>((nuint)NewCapacity);
            ChildLink* OldTable = Parent->Children;
            uint Mask = (uint)OldCapacity - 1;

            for (int i = 0; i < OldCapacity; i++)
            {
                var Entry = OldTable[i];
                if (Entry.Label != null)
                    InsertIntoHashTable(NewTable, (uint)NewCapacity, Entry.Label, Entry.LabelLength, Entry.TargetNode);
            }

            Parent->Children = NewTable;
            Parent->ChildCapacity = NewCapacity;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void InsertIntoHashTable(ChildLink* Table, uint Capacity, char* Label, int Length, UnmanagedNode* Target)
        {
            uint Hash = CalculateHash(new ReadOnlySpan<char>(Label, Length));
            uint Mask = Capacity - 1;
            uint Index = Hash & Mask;

            while (Table[Index].Label != null)
            {
                Index = (Index + 1) & Mask;
            }

            Table[Index].Label = Label;
            Table[Index].LabelLength = Length;
            Table[Index].TargetNode = Target;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private uint CalculateHash(ReadOnlySpan<char> Label)
        {
            uint Hash = 2166136261;
            for (int i = 0; i < Label.Length; i++)
            {
                Hash ^= Label[i];
                Hash *= 16777619;
            }
            return Hash;
        }

        private void AllocateNewArenaChunk()
        {
            IntPtr Chunk = (IntPtr)NativeMemory.AllocZeroed((nuint)ChunkSize);
            ArenaChunks.Add(Chunk);
            CurrentChunkPtr = (byte*)Chunk;
            RemainingChunkBytes = ChunkSize;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private T* Allocate<T>() where T : unmanaged =>
            (T*)AllocateRaw((nuint)sizeof(T));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private T* AllocateArray<T>(nuint Count) where T : unmanaged =>
            (T*)AllocateRaw(Count * (nuint)sizeof(T));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private byte* AllocateRaw(nuint Bytes)
        {
            Bytes = (Bytes + 7) & ~(nuint)7;
            if (RemainingChunkBytes < (nint)Bytes)
                AllocateNewArenaChunk();
            byte* Result = CurrentChunkPtr;
            CurrentChunkPtr += Bytes;
            RemainingChunkBytes -= (nint)Bytes;
            return Result;
        }

        public bool Contains(string Domain) => AllDomains.Contains(Domain);

        public void Clear()
        {
            AllDomains.Clear();
            foreach (IntPtr Chunk in ArenaChunks)
                NativeMemory.Free((void*)Chunk);
            ArenaChunks.Clear();
            AllocateNewArenaChunk();
            Root = Allocate<UnmanagedNode>();
        }

        public void Dispose()
        {
            if (_Disposed) return;
            foreach (IntPtr Chunk in ArenaChunks)
                NativeMemory.Free((void*)Chunk);
            ArenaChunks.Clear();
            Root = null;
            _Disposed = true;
            GC.SuppressFinalize(this);
        }

        ~DomainList() => Dispose();

        [StructLayout(LayoutKind.Sequential)]
        struct UnmanagedNode
        {
            public bool IsEnd;
            //public bool Wildcard;
            public byte HasHashTable;
            public int ChildCount;
            public int ChildCapacity;
            public ChildLink* Children;
        }

        [StructLayout(LayoutKind.Sequential)]
        struct ChildLink
        {
            public char* Label;
            public int LabelLength;
            public UnmanagedNode* TargetNode;
        }
    }
}