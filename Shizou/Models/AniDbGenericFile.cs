using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Shizou.AniDbApi.Requests.Udp.Results;

namespace Shizou.Models;

[Index(nameof(AniDbEpisodeId), IsUnique = true)]
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
        MyListEntry = result.MyListId is null ? null : new AniDbMyListEntry(result);
    }

    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    public int Id { get; set; }

    public int AniDbEpisodeId { get; set; }

    public int? MyListEntryId { get; set; }
    public AniDbMyListEntry? MyListEntry { get; set; }
}
