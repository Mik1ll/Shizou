namespace Shizou.Data.Models;

public class MalAnime : IEntity
{
    public int Id { get; set; }
    public MalStatus? Status { get; set; }
}