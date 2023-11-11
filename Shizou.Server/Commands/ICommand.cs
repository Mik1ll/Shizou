using System.Threading.Tasks;

namespace Shizou.Server.Commands;

public interface ICommand<out T> where T : CommandArgs
{
    string CommandId { get; }
    bool Completed { get; }

    // ReSharper disable once UnusedMemberInSuper.Global
    T CommandArgs { get; }
    void SetParameters(CommandArgs args);
    Task Process();
}
