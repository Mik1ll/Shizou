using System.Collections.Generic;

namespace Shizou.Server.AniDbApi.Requests.Udp.Interfaces;

public interface IFileRequest : IAniDbUdpRequest
{
    FileResult? FileResult { get; }
    List<int>? MultipleFilesResult { get; }
    void SetParameters(int fileId, FMask fMask, AMaskFile aMask);
    void SetParameters(long fileSize, string ed2K, FMask fMask, AMaskFile aMask);
    void SetParameters(int animeId, int groupId, string episodeNumber, FMask fMask, AMaskFile aMask);
}