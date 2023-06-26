using System.Threading.Tasks;

namespace Shizou.Server.Commands;

public interface ICommand
{
    bool Completed { get; set; }
    string CommandId { get; }
    Task Process();
}

public abstract class BaseCommand<T> : ICommand where T : CommandArgs
{
    protected BaseCommand(T commandArgs)
    {
        CommandArgs = commandArgs;
        CommandId = commandArgs.CommandId;
    }

    protected T CommandArgs { get; set; }
    public bool Completed { get; set; }
    public string CommandId { get; }

    public abstract Task Process();
}