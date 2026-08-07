using System;
using System.IO;
using System.Linq;
using MeatyMod.Cli.Commands;
using MeatyMod.Injector;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace MeatyMod.Tests;

public class InjectEdgeTests
{
    [Fact]
    public void Patch_NonexistentExe_ThrowsFileNotFound()
    {
        var tempRoot = CreateTempDir();
        try
        {
            var missingExe = Path.Combine(tempRoot, "DoesNotExist.dll");
            var mod = WriteModAssembly(tempRoot, "ModA", "ModAEntry", withInject: true);
            var output = Path.Combine(tempRoot, "Out.dll");
            var backup = Path.Combine(tempRoot, "Bak.dll");

            Assert.Throws<FileNotFoundException>(() =>
                AssemblyInjector.Patch(missingExe, new[] { mod }, new string[1], output, backup));
        }
        finally
        {
            CleanupDir(tempRoot);
        }
    }

    [Fact]
    public void Patch_NonexistentModDll_ThrowsFileNotFound()
    {
        var tempRoot = CreateTempDir();
        try
        {
            var exe = WriteGameAssembly(tempRoot);
            var missingDll = Path.Combine(tempRoot, "MissingMod.dll");
            var output = Path.Combine(tempRoot, "Out.dll");
            var backup = Path.Combine(tempRoot, "Bak.dll");

            var ex = Assert.Throws<FileNotFoundException>(() =>
                AssemblyInjector.Patch(exe, new[] { missingDll }, new string[1], output, backup));
            Assert.Equal(missingDll, ex.FileName);
        }
        finally
        {
            CleanupDir(tempRoot);
        }
    }

    [Fact]
    public void Patch_ArrayLengthMismatch_ThrowsArgumentException()
    {
        var tempRoot = CreateTempDir();
        try
        {
            var exe = WriteGameAssembly(tempRoot);
            var mod1 = WriteModAssembly(tempRoot, "ModA", "ModAEntry", withInject: true);
            var mod2 = WriteModAssembly(tempRoot, "ModB", "ModBEntry", withInject: true);
            var output = Path.Combine(tempRoot, "Out.dll");
            var backup = Path.Combine(tempRoot, "Bak.dll");

            Assert.Throws<ArgumentException>(() =>
                AssemblyInjector.Patch(exe, new[] { mod1, mod2 }, new string[1], output, backup));
        }
        finally
        {
            CleanupDir(tempRoot);
        }
    }

    [Fact]
    public void Patch_MissingBloodMyGame_Throws()
    {
        var tempRoot = CreateTempDir();
        try
        {
            var exe = WriteAssemblyWithoutBloodMyGame(tempRoot);
            var mod = WriteModAssembly(tempRoot, "ModA", "ModAEntry", withInject: true);
            var output = Path.Combine(tempRoot, "Out.dll");
            var backup = Path.Combine(tempRoot, "Bak.dll");

            var ex = Assert.Throws<InvalidOperationException>(() =>
                AssemblyInjector.Patch(exe, new[] { mod }, new string[1], output, backup));
            Assert.Contains("Blood.myGame", ex.Message);
        }
        finally
        {
            CleanupDir(tempRoot);
        }
    }

    [Fact]
    public void Patch_MissingCtor_Throws()
    {
        var tempRoot = CreateTempDir();
        try
        {
            var exe = WriteAssemblyWithBloodMyGameNoCtor(tempRoot);
            var mod = WriteModAssembly(tempRoot, "ModA", "ModAEntry", withInject: true);
            var output = Path.Combine(tempRoot, "Out.dll");
            var backup = Path.Combine(tempRoot, "Bak.dll");

            var ex = Assert.Throws<InvalidOperationException>(() =>
                AssemblyInjector.Patch(exe, new[] { mod }, new string[1], output, backup));
            Assert.Contains("constructor", ex.Message);
        }
        finally
        {
            CleanupDir(tempRoot);
        }
    }

    [Fact]
    public void Patch_EmptyModList_PatchesWithoutCalls()
    {
        var tempRoot = CreateTempDir();
        try
        {
            var exe = WriteGameAssembly(tempRoot);
            var output = Path.Combine(tempRoot, "Out.dll");
            var backup = Path.Combine(tempRoot, "Bak.dll");

            var result = AssemblyInjector.Patch(exe, Array.Empty<string>(), Array.Empty<string>(), output, backup);

            Assert.True(result);
            Assert.True(File.Exists(output));
            var calls = GetInjectCalls(output);
            Assert.Empty(calls);
        }
        finally
        {
            CleanupDir(tempRoot);
        }
    }

    [Fact]
    public void PatchLegacyOverload_EmitsOneCall()
    {
        var tempRoot = CreateTempDir();
        try
        {
            var exe = WriteGameAssembly(tempRoot);
            var mod = WriteModAssembly(tempRoot, "ModA", "ModAEntry", withInject: true);
            var output = Path.Combine(tempRoot, "Out.dll");
            var backup = Path.Combine(tempRoot, "Bak.dll");

            AssemblyInjector.Patch(exe, mod, output, backup);

            var calls = GetInjectCalls(output);
            Assert.Single(calls);
        }
        finally
        {
            CleanupDir(tempRoot);
        }
    }

    [Fact]
    public void Patch_InPlace_DoesNotCorruptModDll()
    {
        var tempRoot = CreateTempDir();
        try
        {
            var exe = WriteGameAssembly(tempRoot);
            var mod = WriteModAssembly(tempRoot, "ModA", "ModAEntry", withInject: true);
            var patchedExe = Path.Combine(tempRoot, "Patched.exe.dll");
            var backup = Path.Combine(tempRoot, "Patched.exe.dll.bak");

            var modBytesBefore = File.ReadAllBytes(mod);

            AssemblyInjector.Patch(exe, mod, patchedExe, backup);

            var modBytesAfter = File.ReadAllBytes(mod);
            Assert.Equal(modBytesBefore, modBytesAfter);

            var exeOriginalBytes = File.ReadAllBytes(exe);

            AssemblyInjector.Patch(exe, mod, exe, exe + ".backup");

            var exePatchedBytes = File.ReadAllBytes(exe);
            Assert.NotEqual(exeOriginalBytes, exePatchedBytes);

            Assert.True(File.Exists(exe + ".backup"));
            var backupBytes = File.ReadAllBytes(exe + ".backup");
            Assert.Equal(exeOriginalBytes, backupBytes);

            var calls = GetInjectCalls(exe);
            Assert.NotEmpty(calls);

            var exeName = Path.GetFileName(exe);
            var tmpFiles = Directory.GetFiles(tempRoot, exeName + ".tmp*")
                .ToArray();
            Assert.Empty(tmpFiles);
        }
        finally
        {
            CleanupDir(tempRoot);
        }
    }

    [Fact]
    public void InjectCommand_Legacy_DoesNotTreatDllAsOutput()
    {
        var tempRoot = CreateTempDir();
        var originalCwd = Directory.GetCurrentDirectory();
        try
        {
            var exeDir = Path.Combine(tempRoot, "Game");
            var modDir = Path.Combine(tempRoot, "Mods");
            Directory.CreateDirectory(exeDir);
            Directory.CreateDirectory(modDir);

            var exePath = Path.Combine(exeDir, "Game.dll");
            WriteGameAssemblyTo(exePath);
            var dllPath = Path.Combine(modDir, "ModA.dll");
            WriteModAssemblyTo(dllPath, "ModA", "ModAEntry", withInject: true);

            var dllBytesBefore = File.ReadAllBytes(dllPath);

            Directory.SetCurrentDirectory(tempRoot);

            var result = new InjectCommand().Run(new[] { exePath, dllPath });

            Assert.Equal(0, result);

            var dllBytesAfter = File.ReadAllBytes(dllPath);
            Assert.Equal(dllBytesBefore, dllBytesAfter);

            var calls = GetInjectCalls(exePath);
            Assert.NotEmpty(calls);
        }
        finally
        {
            Directory.SetCurrentDirectory(originalCwd);
            CleanupDir(tempRoot);
        }
    }

    [Fact]
    public void InjectCommand_Multi_WithEntry_PatchesInOrder()
    {
        var tempRoot = CreateTempDir();
        var originalCwd = Directory.GetCurrentDirectory();
        try
        {
            var exeDir = Path.Combine(tempRoot, "Game");
            var modDir = Path.Combine(tempRoot, "Mods");
            Directory.CreateDirectory(exeDir);
            Directory.CreateDirectory(modDir);

            var exePath = Path.Combine(exeDir, "Game.dll");
            WriteGameAssemblyTo(exePath);

            var dllA = Path.Combine(modDir, "ModA.dll");
            WriteModAssemblyTo(dllA, "ModA", "ModAEntry", withInject: true);
            var dllB = Path.Combine(modDir, "ModB.dll");
            WriteModAssemblyTo(dllB, "ModB", "ModBEntry", withInject: true);

            Directory.SetCurrentDirectory(tempRoot);

            var result = new InjectCommand().Run(new[]
            {
                exePath,
                "--mod", dllA, "--entry", "ModA.ModAEntry",
                "--mod", dllB,
            });

            Assert.Equal(0, result);

            var calls = GetInjectCalls(exePath);
            Assert.Equal(2, calls.Count);
            Assert.Equal("ModA.ModAEntry", calls[0]);
            Assert.Equal("ModB.ModBEntry", calls[1]);
        }
        finally
        {
            Directory.SetCurrentDirectory(originalCwd);
            CleanupDir(tempRoot);
        }
    }

    [Fact]
    public void InjectCommand_NoArgs_Returns1()
    {
        Assert.Equal(1, new InjectCommand().Run(Array.Empty<string>()));
    }

    [Fact]
    public void InjectCommand_OneArg_Returns1()
    {
        var tempRoot = CreateTempDir();
        try
        {
            var exe = WriteGameAssembly(tempRoot);
            Assert.Equal(1, new InjectCommand().Run(new[] { exe }));
        }
        finally
        {
            CleanupDir(tempRoot);
        }
    }

    [Fact]
    public void RestoreCommand_RestoresByteIdentical()
    {
        var tempRoot = CreateTempDir();
        try
        {
            var exe = WriteGameAssembly(tempRoot);
            var mod = WriteModAssembly(tempRoot, "ModA", "ModAEntry", withInject: true);
            var backup = exe + ".backup";

            var originalExeBytes = File.ReadAllBytes(exe);

            AssemblyInjector.Patch(exe, mod, exe, backup);

            var patchedBytes = File.ReadAllBytes(exe);
            Assert.NotEqual(originalExeBytes, patchedBytes);

            var result = new RestoreCommand().Run(new[] { exe });

            Assert.Equal(0, result);

            var restoredBytes = File.ReadAllBytes(exe);
            Assert.Equal(originalExeBytes, restoredBytes);
        }
        finally
        {
            CleanupDir(tempRoot);
        }
    }

    private static string CreateTempDir()
    {
        var dir = Path.Combine(Path.GetTempPath(), "meaty_edge_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        return dir;
    }

    private static void CleanupDir(string dir)
    {
        try { Directory.Delete(dir, true); }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }
    }

    private static string WriteGameAssembly(string dir)
        => WriteGameAssemblyTo(Path.Combine(dir, "Game.dll"));

    private static string WriteGameAssemblyTo(string path)
    {
        using var assembly = AssemblyDefinition.CreateAssembly(
            new AssemblyNameDefinition("Game", new Version(1, 0, 0, 0)), "Game", ModuleKind.Dll);
        var module = assembly.MainModule;

        var type = new TypeDefinition("Blood", "myGame",
            TypeAttributes.Class | TypeAttributes.Public, module.TypeSystem.Object);

        var objectCtor = module.ImportReference(
            typeof(object).GetConstructor(Type.EmptyTypes));

        var ctor = new MethodDefinition(".ctor",
            MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName,
            module.TypeSystem.Void);
        ctor.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_0));
        ctor.Body.Instructions.Add(Instruction.Create(OpCodes.Call, objectCtor));
        ctor.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
        type.Methods.Add(ctor);
        module.Types.Add(type);
        assembly.Write(path);
        return path;
    }

    private static string WriteAssemblyWithoutBloodMyGame(string dir)
    {
        var path = Path.Combine(dir, "NoBlood.dll");
        using var assembly = AssemblyDefinition.CreateAssembly(
            new AssemblyNameDefinition("NoBlood", new Version(1, 0, 0, 0)), "NoBlood", ModuleKind.Dll);
        var module = assembly.MainModule;
        var type = new TypeDefinition("Other", "OtherType",
            TypeAttributes.Class | TypeAttributes.Public, module.TypeSystem.Object);
        module.Types.Add(type);
        assembly.Write(path);
        return path;
    }

    private static string WriteAssemblyWithBloodMyGameNoCtor(string dir)
    {
        var path = Path.Combine(dir, "NoCtor.dll");
        using var assembly = AssemblyDefinition.CreateAssembly(
            new AssemblyNameDefinition("NoCtor", new Version(1, 0, 0, 0)), "NoCtor", ModuleKind.Dll);
        var module = assembly.MainModule;
        var type = new TypeDefinition("Blood", "myGame",
            TypeAttributes.Class | TypeAttributes.Public, module.TypeSystem.Object);
        module.Types.Add(type);
        assembly.Write(path);
        return path;
    }

    private static string WriteModAssembly(string dir, string name, string entryTypeName, bool withInject)
        => WriteModAssemblyTo(Path.Combine(dir, name + ".dll"), name, entryTypeName, withInject);

    private static string WriteModAssemblyTo(string path, string name, string entryTypeName, bool withInject)
    {
        using var assembly = AssemblyDefinition.CreateAssembly(
            new AssemblyNameDefinition(name, new Version(1, 0, 0, 0)), name, ModuleKind.Dll);
        var module = assembly.MainModule;
        var entry = new TypeDefinition(name, entryTypeName,
            TypeAttributes.Class | TypeAttributes.Public, module.TypeSystem.Object);

        if (withInject)
        {
            var gameParam = new TypeReference("Game", "Game", module, module.TypeSystem.CoreLibrary);
            var inject = new MethodDefinition("Inject",
                MethodAttributes.Public | MethodAttributes.Static, module.TypeSystem.Void);
            inject.Parameters.Add(new ParameterDefinition("game", ParameterAttributes.None, gameParam));
            inject.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
            entry.Methods.Add(inject);
        }

        module.Types.Add(entry);
        assembly.Write(path);
        return path;
    }

    private static System.Collections.Generic.List<string> GetInjectCalls(string patchedPath)
    {
        using var module = ModuleDefinition.ReadModule(patchedPath);
        var type = module.Types.First(t => t.FullName == "Blood.myGame");
        var ctor = type.Methods.First(m => m.IsConstructor && !m.IsStatic);
        return ctor.Body.Instructions
            .Where(i => i.OpCode == OpCodes.Call
                && i.Operand is MethodReference mr
                && mr.Name == "Inject"
                && mr.Parameters.Count == 1)
            .Select(i => ((MethodReference)i.Operand).DeclaringType.FullName)
            .ToList();
    }
}
