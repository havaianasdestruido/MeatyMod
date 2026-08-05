using System;
using System.Collections.Generic;

namespace MeatyMod.Cli;

public interface ICommand
{
    string Name { get; }
    int Execute(string[] args);
}

public sealed class PackCommand : ICommand
{
    public string Name => "pack";

    public int Execute(string[] args)
    {
        return 0;
    }
}

public sealed class InstallCommand : ICommand
{
    public string Name => "install";

    public int Execute(string[] args)
    {
        return 0;
    }
}

public class Program
{
    public static int Main(string[] args)
    {
        var commands = new List<ICommand>
        {
            new PackCommand(),
            new InstallCommand(),
        };

        if (args.Length == 0)
        {
            PrintUsage(commands);
            return 1;
        }

        var command = commands.Find(c => c.Name == args[0]);
        if (command is null)
        {
            PrintUsage(commands);
            return 1;
        }

        return command.Execute(args[1..]);
    }

    private static void PrintUsage(List<ICommand> commands)
    {
        Console.WriteLine("Usage: MeatyMod.Cli <command> [options]");
        Console.WriteLine();
        Console.WriteLine("Commands:");
        foreach (var command in commands)
        {
            Console.WriteLine($"  {command.Name}");
        }
    }
}
