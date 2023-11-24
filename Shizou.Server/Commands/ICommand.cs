using System.Threading.Tasks;
using Shizou.Data.CommandInputArgs;

namespace Shizou.Server.Commands;

public interface ICommand<out T> where T : CommandArgs
{
    string CommandId { get; }
    bool Completed { get; }

    // ReSharper disable once UnusedMemberInSuper.Global
    T CommandArgs { get; }
    ICommand<T> SetParameters(CommandArgs args);
    Task ProcessAsync();
}
