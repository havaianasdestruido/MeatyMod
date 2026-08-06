using System;
using System.IO;
using System.Linq;
using MeatyMod.Injector;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace MeatyMod.Tests;

public class InjectorTests
{
    [Fact]
    public void PatchSingle_EmitsInjectCall()
    {
        var tempRoot = CreateTempDir();
        try
        {
            var game = WriteGameAssembly(tempRoot);
            var mod = WriteModAssembly(tempRoot, "ModA", "ModAEntry", withInject: true);
            var output = Path.Combine(tempRoot, "Patched.dll");
            var backup = Path.Combine(tempRoot, "Backup.dll");

            AssemblyInjector.Patch(game, new[] { mod }, new string[1], output, backup);

            Assert.Equal(1, CountInjectCalls(output, InjectFullName(mod)));
        }
        finally
        {
            Directory.Delete(tempRoot, true);
        }
    }

    [Fact]
    public void PatchMulti_TwoMods_EmitsTwoCalls()
    {
        var tempRoot = CreateTempDir();
        try
        {
            var game = WriteGameAssembly(tempRoot);
            var mod1 = WriteModAssembly(tempRoot, "ModA", "ModAEntry", withInject: true);
            var mod2 = WriteModAssembly(tempRoot, "ModB", "ModBEntry", withInject: true);
            var output = Path.Combine(tempRoot, "Patched.dll");
            var backup = Path.Combine(tempRoot, "Backup.dll");

            AssemblyInjector.Patch(game, new[] { mod1, mod2 }, new string[2], output, backup);

            var name1 = InjectFullName(mod1);
            var name2 = InjectFullName(mod2);
            using var module = ModuleDefinition.ReadModule(output);
            var type = module.Types.First(t => t.FullName == "Blood.myGame");
            var ctor = type.Methods.First(m => m.IsConstructor && !m.IsStatic);
            var calls = ctor.Body.Instructions
                .Where(i => i.OpCode == OpCodes.Call)
                .Select(i => (MethodReference)i.Operand)
                .ToList();

            Assert.Equal(2, calls.Count);
            Assert.Equal(name1, calls[0].FullName);
            Assert.Equal(name2, calls[1].FullName);
        }
        finally
        {
            Directory.Delete(tempRoot, true);
        }
    }

    [Fact]
    public void Patch_MissingEntryType_Throws()
    {
        var tempRoot = CreateTempDir();
        try
        {
            var game = WriteGameAssembly(tempRoot);
            var mod = WriteModAssembly(tempRoot, "EmptyMod", "EmptyMod", withInject: false);
            var output = Path.Combine(tempRoot, "Patched.dll");
            var backup = Path.Combine(tempRoot, "Backup.dll");

            Assert.Throws<InvalidOperationException>(() =>
                AssemblyInjector.Patch(game, new[] { mod }, new string[1], output, backup));
        }
        finally
        {
            Directory.Delete(tempRoot, true);
        }
    }

    [Fact]
    public void Patch_AutoDetect_WithEntrySuffix()
    {
        var tempRoot = CreateTempDir();
        try
        {
            var game = WriteGameAssembly(tempRoot);
            var mod = WriteModAssembly(tempRoot, "AutoMod", "AutoModEntry", withInject: true);
            var output = Path.Combine(tempRoot, "Patched.dll");
            var backup = Path.Combine(tempRoot, "Backup.dll");

            AssemblyInjector.Patch(game, new[] { mod }, new string[1], output, backup);

            var fullName = InjectFullName(mod);
            Assert.Contains("AutoModEntry", fullName);
            Assert.Equal(1, CountInjectCalls(output, fullName));
        }
        finally
        {
            Directory.Delete(tempRoot, true);
        }
    }

    private static string CreateTempDir()
    {
        var dir = Path.Combine(Path.GetTempPath(), "meaty_inject_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        return dir;
    }

    private static string WriteGameAssembly(string dir)
    {
        var path = Path.Combine(dir, "Game.dll");
        using var assembly = AssemblyDefinition.CreateAssembly(
            new AssemblyNameDefinition("Game", new Version(1, 0, 0, 0)), "Game", ModuleKind.Dll);
        var module = assembly.MainModule;
        var type = new TypeDefinition("Blood", "myGame", TypeAttributes.Public, module.TypeSystem.Object);
        var ctor = new MethodDefinition(".ctor",
            MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName,
            module.TypeSystem.Void);
        ctor.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
        type.Methods.Add(ctor);
        module.Types.Add(type);
        assembly.Write(path);
        return path;
    }

    private static string WriteModAssembly(string dir, string name, string entryTypeName, bool withInject)
    {
        var path = Path.Combine(dir, name + ".dll");
        using var assembly = AssemblyDefinition.CreateAssembly(
            new AssemblyNameDefinition(name, new Version(1, 0, 0, 0)), name, ModuleKind.Dll);
        var module = assembly.MainModule;
        var entry = new TypeDefinition(name, entryTypeName, TypeAttributes.Public, module.TypeSystem.Object);
        if (withInject)
        {
            var gameParam = new TypeReference("Game", "Game", module, module.TypeSystem.CoreLibrary);
            var inject = new MethodDefinition("Inject", MethodAttributes.Public | MethodAttributes.Static, module.TypeSystem.Void);
            inject.Parameters.Add(new ParameterDefinition("game", ParameterAttributes.None, gameParam));
            inject.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
            entry.Methods.Add(inject);
        }
        module.Types.Add(entry);
        assembly.Write(path);
        return path;
    }

    private static string InjectFullName(string modPath)
    {
        using var module = ModuleDefinition.ReadModule(modPath);
        var entry = module.Types.First(t => t.Methods.Any(m => m.Name == "Inject" && m.IsStatic));
        return entry.Methods.First(m => m.Name == "Inject" && m.IsStatic).FullName;
    }

    private static int CountInjectCalls(string patchedPath, string injectFullName)
    {
        using var module = ModuleDefinition.ReadModule(patchedPath);
        var type = module.Types.First(t => t.FullName == "Blood.myGame");
        var ctor = type.Methods.First(m => m.IsConstructor && !m.IsStatic);
        return ctor.Body.Instructions.Count(i =>
            i.OpCode == OpCodes.Call
            && i.Operand is MethodReference mr
            && mr.FullName == injectFullName);
    }
}
