using System.ComponentModel.Design;
using Shizou.Entities;

namespace Shizou.Commands
{
    public abstract class BaseCommand
    {
        private CommandRequest _commandRequest;
        protected BaseCommand(CommandRequest commandRequest)
        {
            _commandRequest = commandRequest;
        }

        public bool Completed = false;

        public abstract void Process();

        protected abstract string GenerateCommandId();

        public BaseCommand FromCommandRequest()
        {
            ParamsFromCommandRequest();
            return this;
        }

        protected abstract void ParamsFromCommandRequest();

        public CommandRequest CommandRequest
        {
            get
            {
                return _commandRequest;
            }
        }
    }
}
