using System;
using System.Threading.Tasks;
using Shizou.Data.CommandInputArgs;

namespace Shizou.Server.Commands;

public abstract class Command<T> : ICommand<T> where T : CommandArgs
{
    private bool _parametersSet;
    public T CommandArgs { get; private set; } = null!;
    public string CommandId { get; private set; } = null!;
    public bool Completed { get; protected set; }

    public ICommand<T> SetParameters(CommandArgs args)
    {
        CommandArgs = (T)args;
        CommandId = args.CommandId;
        _parametersSet = true;
        return this;
    }

    public async Task ProcessAsync()
    {
        if (!_parametersSet)
            throw new ArgumentException($"Parameters not set before {nameof(ProcessAsync)} called");
        await ProcessInnerAsync().ConfigureAwait(false);
    }

    protected abstract Task ProcessInnerAsync();
}
