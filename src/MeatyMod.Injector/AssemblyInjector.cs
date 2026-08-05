using System.IO;

namespace MeatyMod.Injector;

public static class AssemblyInjector
{
    public static bool Patch(string exePath, string backupPath)
    {
        File.Copy(exePath, backupPath, overwrite: true);
        return true;
    }
}
