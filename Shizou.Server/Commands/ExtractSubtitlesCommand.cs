using System.Threading.Tasks;
using Shizou.Data.CommandInputArgs;
using Shizou.Server.Services;

namespace Shizou.Server.Commands;

public class ExtractSubtitlesCommand : Command<ExtractSubtitlesArgs>
{
    private readonly SubtitleService _subtitleService;

    public ExtractSubtitlesCommand(SubtitleService subtitleService) => _subtitleService = subtitleService;

    protected override async Task ProcessInnerAsync()
    {
        await _subtitleService.ExtractSubtitlesAsync(CommandArgs.LocalFileId).ConfigureAwait(false);

        Completed = true;
    }
}
