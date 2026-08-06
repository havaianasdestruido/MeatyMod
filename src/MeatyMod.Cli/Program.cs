using System;
using System.Collections.Generic;
using MeatyMod.Cli.Commands;

namespace MeatyMod.Cli
{
    public static class Program
    {
        public static int Main(string[] args)
        {
            var commands = new List<ICommand>
            {
                new PackCommand(),
                new InstallCommand(),
                new InjectCommand(),
                new RestoreCommand(),
                new ManifestCommand(),
                new VerifyCommand(),
                new ParseCommand(),
                new XnbCommand(),
                new ChecksumCommand(),
            };

            if (args.Length == 0)
            {
                PrintUsage(commands);
                return 1;
            }

            var command = commands.Find(c => c.Name == args[0]);
            if (command == null)
            {
                PrintUsage(commands);
                return 1;
            }

            string[] rest = new string[args.Length - 1];
            Array.Copy(args, 1, rest, 0, rest.Length);
            return command.Run(rest);
        }

        private static void PrintUsage(List<ICommand> commands)
        {
            Console.WriteLine("Usage: meatymod <command> [options]");
            Console.WriteLine();
            Console.WriteLine("Commands:");
            foreach (var command in commands)
            {
                Console.WriteLine($"  {command.Name}");
            }
        }
    }
}
