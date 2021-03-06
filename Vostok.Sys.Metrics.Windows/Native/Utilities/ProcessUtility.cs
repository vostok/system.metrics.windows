﻿using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using Vostok.Sys.Metrics.Windows.Native.Constants;
using Vostok.Sys.Metrics.Windows.Native.Flags;
using Vostok.Sys.Metrics.Windows.Native.Libraries;

namespace Vostok.Sys.Metrics.Windows.Native.Utilities
{
    internal static class ProcessUtility
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="accessRights">Desired access rights for process handle</param>
        /// <param name="pid">Process ID</param>
        /// <returns>Handle to process or <see cref="IntPtr.Zero"/> if process doesn't exists</returns>
        /// <exception cref="Win32Exception">Unexpected WinApi error</exception>
        public static IntPtr OpenProcessHandle(ProcessAccessRights accessRights, int pid)
        {
            var handle = IntPtr.Zero;
            try
            {
                handle = Kernel32.OpenProcess(accessRights, false, pid);
                if (handle == IntPtr.Zero)
                {
                    var errorCode = Marshal.GetLastWin32Error();
                    if (errorCode == (int) ErrorCodes.ERROR_INVALID_PARAMETER)
                        return IntPtr.Zero; // ERROR_INVALID_PARAMETER means that process doesn't exist. Don't throw exception here, because this error is expected and shouldn't crash ctor's
                    Win32ExceptionUtility.Throw(errorCode);
                }
            }
            catch
            {
                Kernel32.CloseHandle(handle);
                throw;
            }

            return handle;
        }

        public static bool Is64BitProcess(int pid)
        {
            if (!Environment.Is64BitOperatingSystem)
                return false;

            var handle = OpenLimitedQueryInformationProcessHandle(pid);

            try
            {
                if (!Kernel32.IsWow64Process(handle, out var result))
                    Win32ExceptionUtility.Throw();
                return !result;
            }
            finally
            {
                Kernel32.CloseHandle(handle);
            }
        }

        // PROCESS_QUERY_LIMITED_INFORMATION is not supported on XP and Server 2003, but .NET Framework 4.6.1 (target fw for lib) also is not supported on these OS'es

        public static IntPtr OpenLimitedQueryInformationProcessHandle(int? pid) =>
            pid == null || pid == CurrentProcessId
                ? CurrentProcessHandle
                : OpenProcessHandle(ProcessAccessRights.PROCESS_QUERY_LIMITED_INFORMATION, pid.Value);

        public static void EnsureProcessIsRunning(IntPtr handle, int pid)
        {
            if (handle == IntPtr.Zero)
                ThrowOnNonExistentProcess(pid);
            
            if (!Kernel32.GetExitCodeProcess(handle, out var exitCode))
                Win32ExceptionUtility.Throw();

            if (exitCode != ProcessExitCodes.STILL_ALIVE)
                ThrowOnProcessExit(pid);
        }

        public static void EnsureProcessIsRunning(int pid)
        {
            var handle = IntPtr.Zero;
            try
            {
                handle = OpenLimitedQueryInformationProcessHandle(pid);
                EnsureProcessIsRunning(handle, pid);
            }
            finally
            {
                Kernel32.CloseHandle(handle);
            }
        }

        private static void ThrowOnNonExistentProcess(int pid)
            => throw new InvalidOperationException(
                $"Process with pid {pid} doesn't exists, so the metrics are not available.");

        private static void ThrowOnProcessExit(int pid)
            => throw new InvalidOperationException(
                $"Process with pid {pid} has exited, so the metrics are not available.");


        public static readonly IntPtr CurrentProcessHandle;

        public static readonly int CurrentProcessId;

        static ProcessUtility()
        {
            CurrentProcessHandle = Kernel32.GetCurrentProcess();
            CurrentProcessId = Kernel32.GetProcessId(CurrentProcessHandle);
        }
    }
}