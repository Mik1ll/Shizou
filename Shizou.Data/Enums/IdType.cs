namespace Shizou.Data.Enums;

public enum IdType
{
    LocalId = 1,
    FileId = 2,
    MyListId = 3,
    EpisodeId = 4,
    AnimeId = 5
}

public enum IdTypeLocalOrFile
{
    LocalId = IdType.LocalId,
    FileId = IdType.FileId
}

public enum IdTypeFileOrMyList
{
    FileId = IdType.FileId,
    MyListId = IdType.MyListId
}
