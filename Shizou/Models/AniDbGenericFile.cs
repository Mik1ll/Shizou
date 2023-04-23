using System.ComponentModel.DataAnnotations.Schema;
using Shizou.AniDbApi.Requests.Udp.Results;

namespace Shizou.Models;

public class AniDbGenericFile : IEntity
{
    public AniDbGenericFile()
    {
    }

    public AniDbGenericFile(AniDbFileResult result)
    {
        Id = result.FileId;
        AniDbEpisodeId = result.EpisodeId!.Value;
        MyListEntryId = result.MyListId;
    }
    
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    public int Id { get; set; }

    public int AniDbEpisodeId { get; set; }
    public AniDbEpisode AniDbEpisode { get; set; } = null!;

    public int? MyListEntryId { get; set; }
    public AniDbMyListEntry? MyListEntry { get; set; }
}
