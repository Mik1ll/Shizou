using System.Threading.Tasks;

namespace Shizou.Server.Commands;

public interface ICommand
{
    string CommandId { get; }
    bool Completed { get; }
    void SetParameters(CommandArgs args);
    Task Process();
}