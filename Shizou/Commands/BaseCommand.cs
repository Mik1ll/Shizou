using Shizou.Entities;

namespace Shizou.Commands
{
    public abstract class BaseCommand
    {
        public BaseCommand(CommandRequest commandRequest)
        {
            CommandRequest = commandRequest;
        }
        
        public abstract void Process();
        
        public CommandRequest CommandRequest { get; }
    }
}
