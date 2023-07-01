using System;
using System.Threading.Tasks;

namespace Shizou.Server.Commands;

public interface ICommand
{
    string CommandId { get; }
    bool Completed { get; }
    void SetParameters(CommandArgs args);
    Task Process();
}

public abstract class BaseCommand<T> : ICommand where T : CommandArgs
{
    private bool _parametersSet;

    public string CommandId { get; private set; } = null!;
    public bool Completed { get; protected set; }

    protected T CommandArgs { get; set; } = null!;

    public virtual void SetParameters(CommandArgs args)
    {
        CommandArgs = (T)args;
        CommandId = args.CommandId;
        _parametersSet = true;
    }

    public async Task Process()
    {
        if (!_parametersSet)
            throw new ArgumentException($"Parameters not set before {nameof(Process)} called");
        await ProcessInner();
    }

    public abstract Task ProcessInner();
}
