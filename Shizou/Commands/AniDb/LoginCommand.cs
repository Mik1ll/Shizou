using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Shizou.CommandProcessors;

namespace Shizou.Commands.AniDb
{
    public record LoginParams : CommandParams
    {
    }

    [Command(CommandType.Login, CommandPriority.Highest, QueueType.AniDbUdp)]
    public class LoginCommand : BaseCommand<LoginParams>
    {
        public LoginCommand(LoginParams commandParams, ILogger<BaseCommand<LoginParams>> logger) : base(commandParams, logger)
        {
        }

        public override string CommandId { get; } = nameof(LoginCommand);

        public override async Task Process()
        {
            throw new NotImplementedException();
        }
    }
}
