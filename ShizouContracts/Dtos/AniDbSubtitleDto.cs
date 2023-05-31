namespace ShizouContracts.Dtos;

public class AniDbSubtitleDto : IEntityDto
{
    public int Id { get; set; }
    public string Language { get; set; } = null!;
}
