namespace Shizou.AniDbApi;

public enum AniDbResponseCode
{
    // All
    IllegalInputOrAccessDenied = 505,
    Banned = 555,
    UnknownCommand = 598,
    InternalServerError = 600,
    OutOfService = 601,
    ServerBusy = 602,
    Timeout = 604,
    LoginFirst = 501,
    AccessDenied = 502,
    InvalidSession = 506,

    // Auth
    LoginAccepted = 200,
    LoginAcceptedNewVersion = 201,
    LoginFailed = 500,
    ClientOutdated = 503,
    ClientBanned = 504,

    // Logout
    LoggedOut = 203,
    NotLoggedIn = 403,

    // Encrypt
    EncryptionEnabled = 209,
    ApiPasswordNotDefined = 309,
    NoSuchEncryptionType = 509,
    NoSuchUser = 394,

    // Anime
    Anime = 230,
    NoSuchAnime = 330,

    // AnimeDesc
    AnimeDesc = 233,
    NoSuchDescription = 333,

    // Updated
    Updated = 243,
    NoUpdates = 343,

    //Episode
    Episode = 240,
    NoSuchEpisode = 340,

    // File
    File = 220,
    MultipleFilesFound = 322,
    NoSuchFile = 320,

    // Group
    Group = 250,
    NoSuchGroup = 350,

    // Group Status
    GroupStatus = 225,
    NoSuchGroupsFound = 325,

    // Calender
    Calender = 297,
    CalenderEmpty = 397,

    // Character
    Character = 235,
    NoSuchCharacter = 335,

    // Creator
    Creator = 245,
    NoSuchCreator = 345,

    // My List
    MyList = 221,
    MultipleMyListEntries = 312,
    NoSuchEntry = 321,

    // My List Add
    MyListAdded = 210,
    FileInMyList = 310,
    MyListEdited = 311,
    NoSuchMyListEntry = 411,

    // My List Delete
    MyListDeleted = 211,

    // My List Stats
    MyListStats = 222,

    // Vote
    Voted = 260,
    VoteFound = 261,
    VoteUpdated = 262,
    VoteRevoked = 263,
    NoSuchVote = 360,
    InvalidVoteType = 361,
    InvalidVoteValue = 362,
    PermVoteNotAllowed = 363,
    AlreadyPermVoted = 364,

    // My List Export
    ExportQueue = 217,
    ExportCancelled = 218,
    ExportNoSuchTemplate = 317,
    ExportAlreadyInQueue = 318,
    ExportNotQueuedOrProcessing = 319,

    // Ping
    Pong = 300,

    // Encoding
    EncodingChanged = 219,
    EncodingNotSupported = 519
}