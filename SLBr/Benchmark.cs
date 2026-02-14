/*Copyright © SLT Softwares. All rights reserved.
Use of this source code is governed by a GNU license that can be found in the LICENSE file.*/

using System.Diagnostics;
using System.Text;

namespace SLBr
{
    public class Benchmark
    {
        public class Result
        {
            public string Name;
            public long Time;
            public long Memory;

            public override string ToString() =>
                $"{Name.PadRight(30)} | Time: {Time, 5} ms | Memory: {Memory, 8} bytes";
        }

        private static readonly List<Result> Results = new();

        public static void Clear() => Results.Clear();

        public static void Run(string Name, int Iterations, Action Function)
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            long MemoryBefore = GC.GetTotalMemory(true);
            Stopwatch _Stopwatch = Stopwatch.StartNew();

            for (int i = 0; i < Iterations; i++)
                Function();

            _Stopwatch.Stop();
            long MemoryAfter = GC.GetTotalMemory(false);

            Results.Add(new Result
            {
                Name = Name,
                Time = _Stopwatch.ElapsedMilliseconds,
                Memory = MemoryAfter - MemoryBefore
            });
        }

        public static string Report()
        {
            StringBuilder _String = new StringBuilder();
            _String.AppendLine("Results");
            foreach (Result _Result in Results)
                _String.AppendLine(_Result.ToString());
            return _String.ToString();
        }
    }
}