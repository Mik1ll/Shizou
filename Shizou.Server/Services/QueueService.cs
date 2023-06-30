using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Shizou.Data.Database;
using Shizou.Data.Enums;
using Shizou.Data.Models;
using Shizou.Server.CommandProcessors;

namespace Shizou.Server.Services;

public class QueueService
{
    private readonly List<CommandProcessor> _queues;
    private readonly IDbContextFactory<ShizouContext> _contextFactory;

    public QueueService(IEnumerable<CommandProcessor> queues, IDbContextFactory<ShizouContext> contextFactory)
    {
        _queues = queues.ToList();
        _contextFactory = contextFactory;
    }

    public bool Pause(QueueType queueType)
    {
        var queue = GetQueue(queueType);
        queue.Pause();
        return queue.Paused;
    }

    public bool Unpause(QueueType queueType)
    {
        var queue = GetQueue(queueType);
        queue.Unpause();
        return !queue.Paused;
    }

    public bool GetPauseState(QueueType queueType)
    {
        return GetQueue(queueType).Paused;
    }

    public string? GetPauseReason(QueueType queueType)
    {
        return GetQueue(queueType).PauseReason;
    }

    public CommandRequest? GetCurrentCommand(QueueType queueType)
    {
        return GetQueue(queueType).CurrentCommand;
    }

    public IEnumerable<CommandRequest> GetQueuedCommands(QueueType queueType)
    {
        using var context = _contextFactory.CreateDbContext();
        return context.CommandRequests.Where(cr => cr.QueueType == queueType);
    }

    private CommandProcessor GetQueue(QueueType queueType)
    {
        return _queues.First(q => q.QueueType == queueType);
    }
}
