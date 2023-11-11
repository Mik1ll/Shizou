using System.Threading.Tasks;

namespace Shizou.Server.Commands;

public interface ICommand
{
    string CommandId { get; }
    bool Completed { get; }

    // ReSharper disable once UnusedMemberInSuper.Global
    string CommandArgsString { get; }
    void SetParameters(CommandArgs args);
    Task Process();
}
