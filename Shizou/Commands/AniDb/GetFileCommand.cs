using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Shizou.CommandProcessors;
using Shizou.Database;

namespace Shizou.Commands.AniDb
{
    public sealed record GetFileParams(int LocalFileId) : CommandParams;

    [Command(CommandType.GetFile, CommandPriority.Default, QueueType.AniDbUdp)]
    public class GetFileCommand : BaseCommand<GetFileParams>
    {
        private ShizouContext _context;

        public GetFileCommand(IServiceProvider provider, GetFileParams commandParams)
            : base(provider, provider.GetRequiredService<ILogger<GetFileCommand>>(), commandParams)
        {
            CommandId = $"{nameof(GetFileCommand)}_{commandParams.LocalFileId}";
            _context = provider.GetRequiredService<ShizouContext>();
        }

        public override string CommandId { get; }

        public override Task Process()
        {
            //var fileReq = new FileRequest(Provider, )
            return Task.CompletedTask;
        }
    }
}
