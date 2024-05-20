using System.Threading.Tasks;
using Shizou.Data.CommandInputArgs;
using Shizou.Server.Services;

namespace Shizou.Server.Commands;

public class GetAnimeTitlesCommand : Command<GetAnimeTitlesArgs>
{
    private readonly AnimeTitleSearchService _animeTitleSearchService;

    public GetAnimeTitlesCommand(AnimeTitleSearchService animeTitleSearchService) => _animeTitleSearchService = animeTitleSearchService;

    protected override async Task ProcessInnerAsync()
    {
        await _animeTitleSearchService.GetTitlesAsync().ConfigureAwait(false);
        Completed = true;
    }
}
