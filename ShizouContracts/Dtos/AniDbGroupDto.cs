namespace ShizouContracts.Dtos;

public class AniDbGroupDto : IEntityDto
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public string ShortName { get; set; } = null!;
    public string? Url { get; set; }
}
