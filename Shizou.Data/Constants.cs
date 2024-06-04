namespace Shizou.Data;

public static class Constants
{
    public const string IdentityUsername = "Admin";
    public const string IdentityCookieName = "IdentityCookie";
    public const string ApiPrefix = "/api";
    public const string AppLockName = "ShizouApp_40FBD174-A80E-4139-8A8E-18BAD1216528";
    public const string SchemeHandlerVersion = "v2.0.0";

    public const string SchemeHandlerWindowsDownloadUri =
        $"https://github.com/Mik1ll/Shizou/releases/download/SchemeHandler%2F{SchemeHandlerVersion}/SchemeHandler_Win_x64_{SchemeHandlerVersion}.zip";

    public const string SchemeHandlerLinuxDownloadUri =
        $"https://github.com/Mik1ll/Shizou/releases/download/SchemeHandler%2F{SchemeHandlerVersion}/SchemeHandler_Linux_x64_{SchemeHandlerVersion}.tgz";

    public const string SchemeHandlerLinuxArmDownloadUri =
        $"https://github.com/Mik1ll/Shizou/releases/download/SchemeHandler%2F{SchemeHandlerVersion}/SchemeHandler_Linux_arm64_{SchemeHandlerVersion}.tgz";
}
