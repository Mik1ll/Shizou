namespace Shizou.Data.Enums;

[Flags]
public enum CommandType
{
    Invalid = 0,
    GetFile = 1,
    Hash = 2,
    GetAnime = 3,
    SyncMyList = 5,
    UpdateMyList = 6,
    AddMissingMyListEntries = 7,
    Export = 8,
    ExportPoll = 9,
    SyncMyListFromExport = 10,
    GetImage = 11,
    RestoreMyListBackup = 12,
    Noop = 99
}