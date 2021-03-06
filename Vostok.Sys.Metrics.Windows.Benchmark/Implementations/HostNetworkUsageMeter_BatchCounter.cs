﻿using System;
using System.Collections.Generic;
using Vostok.Sys.Metrics.PerfCounters;
using Vostok.Sys.Metrics.Windows.Meters.Network;
using Vostok.Sys.Metrics.Windows.Native.Utilities;

namespace Vostok.Sys.Metrics.Windows.Benchmark.Implementations
{
    
    public class HostNetworkUsageMeter_BatchCounter : IDisposable
    {
        private readonly IPerformanceCounterFactory counterFactory;
        private readonly Dictionary<string, IPerformanceCounter<InterfaceUsageMetrics>> counters = new Dictionary<string, IPerformanceCounter<InterfaceUsageMetrics>>();
        private readonly List<string> toRemove = new List<string>();
        private readonly List<InterfaceUsageMetrics> interfaceMetrics = new List<InterfaceUsageMetrics>();

        public HostNetworkUsageMeter_BatchCounter()
            : this(PerformanceCounterFactory.Default)
        {
        }

        private HostNetworkUsageMeter_BatchCounter(IPerformanceCounterFactory counterFactory)
        {
            this.counterFactory = counterFactory;
        }

        public NetworkUsageMetrics GetNetworkMetrics()
        {
            var interfaces = ActiveNetworkInterfaceCache.Instance.GetUpInterfaces();
            
            interfaceMetrics.Clear();
            foreach (var networkInterface in interfaces)
            {
                try
                {
                    var metrics = ObtainCounterForInterface(networkInterface).Observe();
                    metrics.Interface = networkInterface;
                    interfaceMetrics.Add(metrics);

                }
                catch(InvalidOperationException){ }
            }

            toRemove.Clear();
            foreach (var interfaceName in counters.Keys)
                if (!interfaces.Contains(interfaceName))
                    toRemove.Add(interfaceName);

            foreach (var interfaceName in toRemove)
            {
                counters[interfaceName].Dispose();
                counters.Remove(interfaceName);
            }
            
            return new NetworkUsageMetrics(interfaceMetrics.ToArray());
        }

        private IPerformanceCounter<InterfaceUsageMetrics> ObtainCounterForInterface(string networkInterface)
        {
            if (counters.TryGetValue(networkInterface, out var counter))
                return counter;
            return counters[networkInterface] = CreateCounterForInterface(networkInterface);
        }

        private IPerformanceCounter<InterfaceUsageMetrics> CreateCounterForInterface(string networkInterface)
        {
            return counterFactory
                .Create<InterfaceUsageMetrics>()
                .AddCounter("Network Interface", "Bytes Received/sec",
                    (c, v) => c.Result.ReceivedBytesPerSecond = (long) v)
                .AddCounter("Network Interface", "Bytes Sent/sec",
                    (c, v) => c.Result.SentBytesPerSecond = (long) v)
                .AddCounter("Network Interface", "Current Bandwidth",
                    (c, v) => c.Result.SentBytesPerSecond = (long) v)
                .Build(networkInterface);
        }

        public void Dispose()
        {
            foreach (var counter in counters.Values)
                counter.Dispose();
        }
    }
    
}