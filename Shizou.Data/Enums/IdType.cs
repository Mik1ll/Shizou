namespace Shizou.Data.Enums;

public enum IdType
{
    LocalId = 1,
    FileId = 2,
    MyListId = 3,
    EpisodeId = 4,
    AnimeId = 5
}

public enum IdTypeLocalFile
{
    LocalId = IdType.LocalId,
    FileId = IdType.FileId
}

public enum IdTypeFileMyList
{
    FileId = IdType.FileId,
    MyListId = IdType.MyListId
}
