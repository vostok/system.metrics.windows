﻿using System;
using Vostok.Sys.Metrics.PerfCounters;
using Vostok.Sys.Metrics.Windows.Helpers;

namespace Vostok.Sys.Metrics.Windows.Benchmark.Implementations.PerfCounters
{
    internal class DynamicPerformanceCounter<T> : IPerformanceCounter<T>
        where T : new()
    {
        private const int RetryAttempts = 3;
        private readonly object sync = new object();

        private readonly Func<string, IPerformanceCounter<T>> counterFactory;
        private readonly Func<string> instanceNameProvider;

        private IPerformanceCounter<T> counter;
        private string instanceName;

        public DynamicPerformanceCounter(Func<string, IPerformanceCounter<T>> counterFactory, Func<string> instanceNameProvider)
        {
            this.counterFactory = counterFactory;
            this.instanceNameProvider = instanceNameProvider;
            instanceName = null;
        }

        public void Dispose()
        {
            counter?.Dispose();
        }

        public T Observe()
        {
            for (var i = 0; i < RetryAttempts; ++i)
            {
                try
                {
                    return ObserveInternal();
                }
                catch (InvalidOperationException)
                {
                }
            }

            return Factory.Create<T>();
        }

        private T ObserveInternal()
        {
            var counterInstance = ObtainCounter();

            return counterInstance != null
                ? counterInstance.Observe()
                : Factory.Create<T>();
        }

        private IPerformanceCounter<T> ObtainCounter()
        {
            var newInstanceName = instanceNameProvider();

            var currentCounter = counter;

            if (newInstanceName != instanceName)
            {
                lock (sync)
                {
                    if (counter != currentCounter)
                        return counter;

                    currentCounter?.Dispose();

                    counter = newInstanceName == null ? null : counterFactory(newInstanceName);
                    instanceName = newInstanceName;
                }
            }

            return counter;
        }
    }
}