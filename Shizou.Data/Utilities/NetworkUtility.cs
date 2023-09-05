namespace Shizou.Data.Utilities;

public static class NetworkUtility
{
    public static bool IsLoopBackAddress(string address)
    {
        return address is "127.0.0.1" or "::1" or "::ffff:127.0.0.1";
    }
}