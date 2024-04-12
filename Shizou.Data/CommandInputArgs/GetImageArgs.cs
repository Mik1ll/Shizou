using Shizou.Data.Enums;

namespace Shizou.Data.CommandInputArgs;

public record GetImageArgs(string Url, string SavePath) : CommandArgs($"GetImage_url={Url}_savePath={SavePath}", CommandPriority.Normal, QueueType.Image);
