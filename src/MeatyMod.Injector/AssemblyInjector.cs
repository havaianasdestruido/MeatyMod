using System;
using System.IO;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace MeatyMod.Injector
{
    public static class AssemblyInjector
    {
        public static bool Patch(string exePath, string modDllPath, string outputPath, string backupPath)
        {
            if (!File.Exists(exePath))
            {
                throw new FileNotFoundException("Game executable not found.", exePath);
            }
            if (!File.Exists(modDllPath))
            {
                throw new FileNotFoundException("Mod DLL not found.", modDllPath);
            }

            File.Copy(exePath, backupPath, overwrite: true);

            using var gameModule = ModuleDefinition.ReadModule(exePath);
            using var modModule = ModuleDefinition.ReadModule(modDllPath);

            var gameType = gameModule.Types.FirstOrDefault(t => t.FullName == "Blood.myGame");
            if (gameType == null)
            {
                throw new InvalidOperationException("Blood.myGame type not found in game assembly.");
            }

            var ctor = gameType.Methods.FirstOrDefault(m => m.IsConstructor && !m.IsStatic);
            if (ctor == null)
            {
                throw new InvalidOperationException("Blood.myGame instance constructor not found.");
            }

            var entryType = modModule.Types.FirstOrDefault(t => t.FullName == "QuackMenu.QuackMenuEntry");
            if (entryType == null)
            {
                throw new InvalidOperationException("QuackMenu.QuackMenuEntry type not found in mod DLL.");
            }

            var inject = entryType.Methods.FirstOrDefault(m => m.Name == "Inject" && m.Parameters.Count == 1);
            if (inject == null)
            {
                throw new InvalidOperationException("QuackMenuEntry.Inject(Game) method not found in mod DLL.");
            }

            var injectRef = gameModule.ImportReference(inject);
            var il = ctor.Body.GetILProcessor();
            var ret = ctor.Body.Instructions.LastOrDefault(i => i.OpCode == OpCodes.Ret);
            if (ret == null)
            {
                throw new InvalidOperationException("No ret instruction found in constructor.");
            }

            il.InsertBefore(ret, il.Create(OpCodes.Ldarg_0));
            il.InsertBefore(ret, il.Create(OpCodes.Call, injectRef));

            gameModule.Write(outputPath);
            return true;
        }
    }
}
