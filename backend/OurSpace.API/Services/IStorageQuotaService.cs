namespace OurSpace.API.Services;

public interface IStorageQuotaService
{
    Task<long> GetUsedBytesAsync(int coupleId);

    Task EnsureRoomAsync(int coupleId, long incomingBytes);
}
