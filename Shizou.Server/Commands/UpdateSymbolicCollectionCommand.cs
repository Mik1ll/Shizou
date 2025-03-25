using System.Threading.Tasks;
using Shizou.Data.CommandInputArgs;
using Shizou.Server.Services;

namespace Shizou.Server.Commands;

public class UpdateSymbolicCollectionCommand : Command<UpdateSymbolicCollectionArgs>
{
    private readonly SymbolicCollectionViewService _symbolicCollectionViewService;

    public UpdateSymbolicCollectionCommand(SymbolicCollectionViewService symbolicCollectionViewService) =>
        _symbolicCollectionViewService = symbolicCollectionViewService;

    protected override Task ProcessInnerAsync()
    {
        _symbolicCollectionViewService.Update();
        Completed = true;
        return Task.CompletedTask;
    }
}
