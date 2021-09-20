// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Arc.Collections;

#pragma warning disable SA1401 // Fields should be private
#pragma warning disable SA1009

namespace Benchmark
{
    public static class BenchmarkHelper
    {
        public static void Shuffle<T>(Random r, T[] array)
        {
            var n = array.Length;
            while (n > 1)
            {
                n--;
                var m = r.Next(n + 1);
                (array[m], array[n]) = (array[n], array[m]);
            }
        }

        public static System.Collections.Generic.IEnumerable<int> GetUniqueRandomNumbers(Random r, int start, int end, int count)
        {
            var work = new int[end - start + 1];
            for (int n = start, i = 0; n <= end; n++, i++)
            {
                work[i] = n;
            }

            for (int resultPos = 0; resultPos < count; resultPos++)
            {
                int nextResultPos = r.Next(resultPos, work.Length);
                (work[resultPos], work[nextResultPos]) = (work[nextResultPos], work[resultPos]);
            }

            return work.Take(count);
        }

        public static System.Collections.Generic.IEnumerable<int> GetRandomNumbers(Random r, int start, int end, int count)
        {
            for (var n = 0; n < count; n++)
            {
                yield return r.Next(start, end);
            }
        }
    }
}
