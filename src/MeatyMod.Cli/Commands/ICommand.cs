namespace MeatyMod.Cli.Commands
{
    public interface ICommand
    {
        string Name { get; }
        int Run(string[] args);
    }
}
