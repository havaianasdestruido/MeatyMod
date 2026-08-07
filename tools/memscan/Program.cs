// ============================================================================
// memscan — process memory viewer for the MeatyMod pipeline.
//
// Verifies at runtime that a modded game process actually loaded the mod DLL
// and that the mod's marker string is present in the process heap (proving the
// mod DLL executed). The mod pipeline ("meatymod inject") patches the game exe
// on disk so the mod DLL loads at startup; this tool checks the RUNNING process.
//
// BUILD
//   dotnet build tools\memscan\MemScan.csproj -c Release
//   -> tools\memscan\bin\Release\net10.0\memscan.exe
//
// USAGE
//   memscan <process-name|pid> [--marker <string>] [--find-module <name>]
//     --marker       UTF-16LE (and UTF-8) string to search for in the target's
//                    committed, readable memory. Default: "Oink injected"
//     --find-module  module name (case-insensitive, e.g. "Oink.dll") that must
//                    appear in the target's loaded-module list. Default: "Oink.dll"
//     --max-hits     how many hit addresses to print (with hex context). Default: 5
//
// EXAMPLES
//   memscan DummyTarget
//   memscan 2468 --marker "Oink injected" --find-module Oink.dll
//
// USE AGAINST THE REAL GAME (BloodandBacon.exe)
//   1. Launch the modded game with:
//        mods\launch\launch-oink.bat
//      (restores the original exe, injects Oink, starts the game, then restores
//       the exe when the game exits).
//   2. While the game is RUNNING, scan it:
//        tools\memscan\bin\Release\net10.0\memscan.exe BloodandBacon
//      A PASS looks like:
//        [FOUND MODULE] Oink.dll -> 0x...
//        marker "Oink injected" : FOUND (N hit(s))
//        RESULT: PASS
//   3. Scan BEFORE the game exits: launch-oink.bat restores the exe as soon as
//      the game closes, and the process (module + heap marker) is gone.
//
// EXIT CODES
//   0 = scan completed and all requested checks passed (module + marker found)
//   1 = error (process not found / access denied / scan failed / bad usage)
//   2 = scan completed but a requested check did not pass (module or marker missing)
//
// NOTES
//   - Built AnyCPU; on x64 Windows this runs 64-bit, so it can open both 64-bit
//     and 32-bit targets. The module snapshot requests TH32CS_SNAPMODULE32 so
//     32-bit target modules are listed too.
//   - Needs PROCESS_QUERY_INFORMATION | PROCESS_VM_READ on the target. An
//     elevated shell may be required for protected / other-user processes.
//   - Windows-only tool (uses kernel32 P/Invoke).
// ============================================================================
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

[assembly: System.Runtime.Versioning.SupportedOSPlatform("windows")]

namespace MemScan;

internal static class Program
{
    private const string DefaultMarker = "Oink injected";
    private const string DefaultModule = "Oink.dll";
    private const int DefaultMaxHits = 5;
    private const int ChunkSize = 64 * 1024;
    private const int HexSurround = 16;

    private static int Main(string[] args)
    {
        Options options;
        try
        {
            options = ParseOptions(args);
        }
        catch (ArgumentException ex)
        {
            Console.Error.WriteLine($"memscan: {ex.Message}");
            PrintUsage();
            return 1;
        }

        if (options.ShowHelp || options.Target is null)
        {
            PrintUsage();
            return 1;
        }

        return Run(options);
    }

    private static int Run(Options options)
    {
        Console.WriteLine("============================================================");
        Console.WriteLine(" memscan — MeatyMod process memory viewer");
        Console.WriteLine("============================================================");
        Console.WriteLine($"Marker      : \"{options.Marker}\"");
        Console.WriteLine($"  UTF-16LE  : {FormatBytes(Encoding.Unicode.GetBytes(options.Marker))}");
        Console.WriteLine($"  UTF-8     : {FormatBytes(Encoding.UTF8.GetBytes(options.Marker))}");
        Console.WriteLine($"Find module : {options.FindModule ?? "(none)"}");

        var (pid, processName) = ResolveTarget(options.Target!);
        Console.WriteLine($"Target      : {processName} (PID {pid})");

        var hProcess = NativeMethods.OpenProcess(
            NativeMethods.ProcessQueryInformation | NativeMethods.ProcessVmRead,
            false,
            pid);
        if (hProcess == IntPtr.Zero)
        {
            var err = Marshal.GetLastWin32Error();
            Console.Error.WriteLine($"memscan: OpenProcess(PID {pid}) failed, Win32 error {err} (0x{err:X8}).");
            if (err == 5)
            {
                Console.Error.WriteLine("  Access denied — run the shell as Administrator, or the process may be protected.");
            }

            return 1;
        }

        try
        {
            _ = NativeMethods.IsWow64Process(hProcess, out var wow64);
            Console.WriteLine($"  bitness    : {(wow64 ? "32-bit (WoW64)" : "64-bit")}");

            Console.WriteLine("------------------------------------------------------------");
            Console.WriteLine("Loaded modules:");
            var modules = EnumerateModules(pid);
            foreach (var module in modules)
            {
                Console.WriteLine($"  0x{module.BaseAddress:X16}  {module.Name}");
            }

            Console.WriteLine($"  ({modules.Count} module(s) total)");

            var moduleOk = true;
            if (options.FindModule is not null)
            {
                var match = modules.FirstOrDefault(m =>
                    m.Name.Equals(options.FindModule, StringComparison.OrdinalIgnoreCase) ||
                    Path.GetFileName(m.Path).Equals(options.FindModule, StringComparison.OrdinalIgnoreCase) ||
                    Path.GetFileNameWithoutExtension(m.Path).Equals(options.FindModule, StringComparison.OrdinalIgnoreCase));
                if (match is not null)
                {
                    Console.WriteLine($"  [FOUND MODULE] {match.Name} -> 0x{match.BaseAddress:X16}");
                    moduleOk = true;
                }
                else
                {
                    Console.WriteLine($"  [MISSING MODULE] '{options.FindModule}' is not in the loaded-module list");
                    moduleOk = false;
                }
            }

            Console.WriteLine("------------------------------------------------------------");
            Console.WriteLine("Memory scan (UTF-16LE + UTF-8):");
            var stats = new ScanStats();
            var hits = new List<MemoryHit>();
            var markerUtf16 = Encoding.Unicode.GetBytes(options.Marker);
            var markerUtf8 = Encoding.UTF8.GetBytes(options.Marker);
            ScanAddressSpace(hProcess, markerUtf16, markerUtf8, options.MaxHits, hits, stats);
            Console.WriteLine($"  readable regions scanned : {stats.RegionsScanned:N0}");
            Console.WriteLine($"  bytes read               : {stats.TotalBytesRead:N0}");
            Console.WriteLine($"  marker hits              : {stats.HitCount:N0}");
            foreach (var hit in hits)
            {
                Console.WriteLine($"  0x{hit.Address:X16}  [{hit.Encoding}]  {RenderHexDump(hit.Context)}");
            }

            var markerOk = stats.HitCount > 0;

            Console.WriteLine("------------------------------------------------------------");
            Console.WriteLine("SUMMARY:");
            Console.WriteLine($"  module '{options.FindModule ?? "(none)"}'    : {(moduleOk ? "FOUND" : "NOT FOUND")}");
            Console.WriteLine($"  marker \"{options.Marker}\" : {(markerOk ? $"FOUND ({stats.HitCount:N0} hit(s))" : "NOT FOUND")}");
            var pass = moduleOk && markerOk;
            Console.WriteLine($"RESULT: {(pass ? "PASS" : "FAIL")}");
            return pass ? 0 : 2;
        }
        finally
        {
            _ = NativeMethods.CloseHandle(hProcess);
        }
    }

    private static (uint Pid, string Name) ResolveTarget(string target)
    {
        if (uint.TryParse(target, out var parsedPid))
        {
            try
            {
                return (parsedPid, Process.GetProcessById((int)parsedPid).ProcessName);
            }
            catch (ArgumentException)
            {
                return (parsedPid, "(unknown)");
            }
            catch (Win32Exception)
            {
                return (parsedPid, "(unknown)");
            }
        }

        var procs = Process.GetProcessesByName(target);
        if (procs.Length == 0)
        {
            throw new InvalidOperationException($"memscan: no process named '{target}' is running.");
        }

        if (procs.Length > 1)
        {
            Console.WriteLine($"  (note: {procs.Length} processes named '{target}'; using PID {procs[0].Id})");
        }

        var pid = (uint)procs[0].Id;
        var name = procs[0].ProcessName;
        foreach (var p in procs)
        {
            p.Dispose();
        }

        return (pid, name);
    }

    private static List<ModuleInfo> EnumerateModules(uint pid)
    {
        var modules = new List<ModuleInfo>();
        var snapshot = NativeMethods.CreateToolhelp32Snapshot(
            NativeMethods.Th32csSnapmodule | NativeMethods.Th32csSnapmodule32,
            pid);
        if (snapshot == NativeMethods.InvalidHandleValue)
        {
            var err = Marshal.GetLastWin32Error();
            throw new InvalidOperationException(
                $"CreateToolhelp32Snapshot failed, Win32 error {err} (0x{err:X8}). Access denied? Try an elevated shell.");
        }

        try
        {
            var entry = new NativeMethods.MODULEENTRY32W
            {
                dwSize = (uint)Marshal.SizeOf<NativeMethods.MODULEENTRY32W>(),
            };
            var ok = NativeMethods.Module32FirstW(snapshot, ref entry);
            while (ok)
            {
                modules.Add(new ModuleInfo(
                    (ulong)entry.modBaseAddr.ToInt64(),
                    entry.modBaseSize,
                    entry.szModule,
                    entry.szExePath));
                entry.dwSize = (uint)Marshal.SizeOf<NativeMethods.MODULEENTRY32W>();
                ok = NativeMethods.Module32NextW(snapshot, ref entry);
            }
        }
        finally
        {
            _ = NativeMethods.CloseHandle(snapshot);
        }

        return modules;
    }

    private static void ScanAddressSpace(
        IntPtr hProcess,
        byte[] markerUtf16,
        byte[] markerUtf8,
        int maxDumpHits,
        List<MemoryHit> dumpHits,
        ScanStats stats)
    {
        const ulong maxAddress = 0x7FFFFFFFFFFF;
        var mbiSize = Marshal.SizeOf<NativeMethods.MEMORY_BASIC_INFORMATION>();
        ulong address = 0;

        while (address < maxAddress)
        {
            var mbi = new NativeMethods.MEMORY_BASIC_INFORMATION();
            var ret = NativeMethods.VirtualQueryEx(hProcess, new IntPtr((long)address), ref mbi, new IntPtr(mbiSize));
            if (ret == IntPtr.Zero)
            {
                break;
            }

            var regionSize = (ulong)mbi.RegionSize.ToInt64();
            if (regionSize == 0)
            {
                address += 0x10000;
                continue;
            }

            var protect = mbi.Protect;
            var isCommit = mbi.State == NativeMethods.MemCommit;
            var noAccess = (protect & 0xFF) == NativeMethods.PageNoaccess;
            var guard = (protect & NativeMethods.PageGuard) != 0;
            if (isCommit && !noAccess && !guard)
            {
                stats.RegionsScanned++;
                ReadAndScanRegion(hProcess, (ulong)mbi.BaseAddress.ToInt64(), regionSize, markerUtf16, markerUtf8, maxDumpHits, dumpHits, stats);
            }

            var next = address + regionSize;
            if (next <= address)
            {
                break;
            }

            address = next;
        }
    }

    private static void ReadAndScanRegion(
        IntPtr hProcess,
        ulong regionBase,
        ulong regionSize,
        byte[] markerUtf16,
        byte[] markerUtf8,
        int maxDumpHits,
        List<MemoryHit> dumpHits,
        ScanStats stats)
    {
        var carryLen = Math.Max(markerUtf16.Length, markerUtf8.Length) - 1;
        var carry = new byte[carryLen];
        var carryCount = 0;
        var chunk = new byte[ChunkSize];
        var combined = new byte[ChunkSize + carryLen];
        ulong offset = 0;

        while (offset < regionSize)
        {
            var toRead = (int)Math.Min((ulong)ChunkSize, regionSize - offset);
            var ok = NativeMethods.ReadProcessMemory(
                hProcess,
                new IntPtr((long)(regionBase + offset)),
                chunk,
                new IntPtr(toRead),
                out var bytesReadPtr);
            var bytesRead = bytesReadPtr.ToInt64();
            if (!ok || bytesRead <= 0)
            {
                offset += (ulong)toRead;
                continue;
            }

            if (bytesRead < toRead)
            {
                toRead = (int)bytesRead;
            }

            stats.TotalBytesRead += toRead;

            Buffer.BlockCopy(carry, 0, combined, 0, carryCount);
            Buffer.BlockCopy(chunk, 0, combined, carryCount, toRead);
            var combinedLen = carryCount + toRead;

            ScanCombined(combined, combinedLen, carryCount, regionBase, offset, markerUtf16, "UTF-16LE", maxDumpHits, dumpHits, stats);
            ScanCombined(combined, combinedLen, carryCount, regionBase, offset, markerUtf8, "UTF-8", maxDumpHits, dumpHits, stats);

            var newCarryCount = Math.Min(carryLen, combinedLen);
            if (newCarryCount > 0)
            {
                Buffer.BlockCopy(combined, combinedLen - newCarryCount, carry, 0, newCarryCount);
            }

            carryCount = newCarryCount;
            offset += (ulong)toRead;
        }
    }

    private static void ScanCombined(
        byte[] combined,
        int combinedLen,
        int carryCount,
        ulong regionBase,
        ulong regionOffset,
        byte[] needle,
        string encoding,
        int maxDumpHits,
        List<MemoryHit> dumpHits,
        ScanStats stats)
    {
        var idx = 0;
        while (true)
        {
            idx = IndexOf(combined, needle, idx, combinedLen);
            if (idx < 0)
            {
                break;
            }

            stats.HitCount++;
            if (dumpHits.Count < maxDumpHits)
            {
                var address = regionBase + regionOffset + (ulong)idx - (ulong)carryCount;
                var context = ExtractContext(combined, combinedLen, idx, needle.Length);
                dumpHits.Add(new MemoryHit(address, encoding, context));
            }

            idx += Math.Max(1, needle.Length);
        }
    }

    private static int IndexOf(byte[] haystack, byte[] needle, int start, int end)
    {
        if (needle.Length == 0 || start > end - needle.Length)
        {
            return -1;
        }

        var first = needle[0];
        var maxStart = end - needle.Length;
        for (var i = Array.IndexOf(haystack, first, start, end - start);
             i >= 0 && i <= maxStart;
             i = Array.IndexOf(haystack, first, i + 1, end - i - 1))
        {
            var match = true;
            for (var j = 1; j < needle.Length; j++)
            {
                if (haystack[i + j] != needle[j])
                {
                    match = false;
                    break;
                }
            }

            if (match)
            {
                return i;
            }
        }

        return -1;
    }

    private static byte[] ExtractContext(byte[] combined, int combinedLen, int matchIdx, int matchLen)
    {
        var start = Math.Max(0, matchIdx - HexSurround);
        var end = Math.Min(combinedLen, matchIdx + matchLen + HexSurround);
        var context = new byte[end - start];
        Buffer.BlockCopy(combined, start, context, 0, context.Length);
        return context;
    }

    private static string FormatBytes(byte[] bytes)
    {
        var sb = new StringBuilder(bytes.Length * 3);
        foreach (var b in bytes)
        {
            sb.Append(b.ToString("X2"));
            sb.Append(' ');
        }

        return sb.ToString().TrimEnd();
    }

    private static string RenderHexDump(byte[] data)
    {
        var hex = new StringBuilder(data.Length * 3);
        var ascii = new StringBuilder(data.Length);
        foreach (var b in data)
        {
            hex.Append(b.ToString("X2"));
            hex.Append(' ');
            ascii.Append(b is >= 0x20 and <= 0x7E ? (char)b : '.');
        }

        return $"{hex.ToString().TrimEnd()}  | {ascii}";
    }

    private static Options ParseOptions(string[] args)
    {
        var options = new Options();
        for (var i = 0; i < args.Length; i++)
        {
            var arg = args[i];
            switch (arg)
            {
                case "-h":
                case "--help":
                    options.ShowHelp = true;
                    break;
                case "--marker":
                    options.Marker = NextArg(args, ref i, "--marker");
                    break;
                case "--find-module":
                    options.FindModule = NextArg(args, ref i, "--find-module");
                    break;
                case "--max-hits":
                    options.MaxHits = int.Parse(NextArg(args, ref i, "--max-hits"));
                    break;
                default:
                    if (arg.StartsWith('-'))
                    {
                        Console.Error.WriteLine($"memscan: unknown option '{arg}'.");
                        options.ShowHelp = true;
                    }
                    else if (options.Target is null)
                    {
                        options.Target = arg;
                    }
                    else
                    {
                        Console.Error.WriteLine($"memscan: unexpected extra argument '{arg}'.");
                        options.ShowHelp = true;
                    }

                    break;
            }
        }

        return options;
    }

    private static string NextArg(string[] args, ref int i, string option)
    {
        if (i + 1 >= args.Length)
        {
            throw new ArgumentException($"option '{option}' requires a value");
        }

        i++;
        return args[i];
    }

    private static void PrintUsage()
    {
        Console.WriteLine();
        Console.WriteLine("Usage: memscan <process-name|pid> [--marker <string>] [--find-module <name>]");
        Console.WriteLine();
        Console.WriteLine("Options:");
        Console.WriteLine("  --marker <string>      Search string (UTF-16LE + UTF-8). Default: \"Oink injected\"");
        Console.WriteLine("  --find-module <name>   Module name that must be loaded.  Default: \"Oink.dll\"");
        Console.WriteLine("  --max-hits <n>         Print first N hit addresses with hex context. Default: 5");
        Console.WriteLine("  -h, --help             Show this help");
        Console.WriteLine();
        Console.WriteLine("Examples:");
        Console.WriteLine("  memscan BloodandBacon");
        Console.WriteLine("  memscan 2468 --marker \"Oink injected\" --find-module Oink.dll");
        Console.WriteLine();
        Console.WriteLine("Exit codes: 0 = checks passed, 1 = error, 2 = scan ok but a check failed.");
    }

    private sealed class Options
    {
        public string? Target { get; set; }

        public string Marker { get; set; } = DefaultMarker;

        public string? FindModule { get; set; } = DefaultModule;

        public int MaxHits { get; set; } = DefaultMaxHits;

        public bool ShowHelp { get; set; }
    }

    private sealed class ModuleInfo
    {
        public ModuleInfo(ulong baseAddress, uint size, string name, string path)
        {
            BaseAddress = baseAddress;
            Size = size;
            Name = name;
            Path = path;
        }

        public ulong BaseAddress { get; }

        public uint Size { get; }

        public string Name { get; }

        public string Path { get; }
    }

    private sealed class MemoryHit
    {
        public MemoryHit(ulong address, string encoding, byte[] context)
        {
            Address = address;
            Encoding = encoding;
            Context = context;
        }

        public ulong Address { get; }

        public string Encoding { get; }

        public byte[] Context { get; }
    }

    private sealed class ScanStats
    {
        public long TotalBytesRead { get; set; }

        public long HitCount { get; set; }

        public long RegionsScanned { get; set; }
    }

    internal static class NativeMethods
    {
        internal const uint ProcessQueryInformation = 0x0400;
        internal const uint ProcessVmRead = 0x0010;
        internal const uint Th32csSnapmodule = 0x00000008;
        internal const uint Th32csSnapmodule32 = 0x00000010;
        internal const uint MemCommit = 0x1000;
        internal const uint PageNoaccess = 0x01;
        internal const uint PageGuard = 0x100;
        internal static readonly IntPtr InvalidHandleValue = new(-1);

        [DllImport("kernel32.dll", SetLastError = true)]
        [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        internal static extern IntPtr OpenProcess(uint dwDesiredAccess, bool bInheritHandle, uint dwProcessId);

        [DllImport("kernel32.dll", SetLastError = true)]
        [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        internal static extern bool CloseHandle(IntPtr hObject);

        [DllImport("kernel32.dll", SetLastError = true)]
        [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        internal static extern bool ReadProcessMemory(
            IntPtr hProcess,
            IntPtr lpBaseAddress,
            byte[] lpBuffer,
            IntPtr nSize,
            out IntPtr lpNumberOfBytesRead);

        [DllImport("kernel32.dll", SetLastError = true)]
        [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        internal static extern IntPtr VirtualQueryEx(
            IntPtr hProcess,
            IntPtr lpAddress,
            ref MEMORY_BASIC_INFORMATION lpBuffer,
            IntPtr dwLength);

        [DllImport("kernel32.dll", SetLastError = true)]
        [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        internal static extern IntPtr CreateToolhelp32Snapshot(uint dwFlags, uint th32ProcessID);

        [DllImport("kernel32.dll", SetLastError = true)]
        [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool Module32FirstW(IntPtr hSnapshot, ref MODULEENTRY32W lpme);

        [DllImport("kernel32.dll", SetLastError = true)]
        [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool Module32NextW(IntPtr hSnapshot, ref MODULEENTRY32W lpme);

        [DllImport("kernel32.dll", SetLastError = true)]
        [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool IsWow64Process(IntPtr hProcess, out bool wow64Process);

        [StructLayout(LayoutKind.Sequential)]
        internal struct MEMORY_BASIC_INFORMATION
        {
            public IntPtr BaseAddress;
            public IntPtr AllocationBase;
            public uint AllocationProtect;
            public IntPtr RegionSize;
            public uint State;
            public uint Protect;
            public uint Type;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        internal struct MODULEENTRY32W
        {
            public uint dwSize;
            public uint th32ModuleID;
            public uint th32ProcessID;
            public uint GlblcntUsage;
            public uint ProccntUsage;
            public IntPtr modBaseAddr;
            public uint modBaseSize;
            public IntPtr hModule;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
            public string szModule;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
            public string szExePath;
        }
    }
}
