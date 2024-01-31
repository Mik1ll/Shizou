namespace Shizou.Server.AniDbApi.Requests.Udp.Interfaces;

public interface IFileRequest : IAniDbUdpRequest<FileResponse>
{
    void SetParameters(int fileId);
    void SetParameters(long fileSize, string ed2K);
    void SetParameters(int animeId, int groupId, string episodeNumber);
}
