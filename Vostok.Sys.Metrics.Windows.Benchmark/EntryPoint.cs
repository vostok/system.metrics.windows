﻿using BenchmarkDotNet.Running;

namespace Vostok.Sys.Metrics.Windows.Benchmark
{
    internal static class EntryPoint
    {
        private static void Main(string[] args)
        {
            
            BenchmarkRunner.Run<LogicalDiskMeterBechmark>();
        }
    }
}