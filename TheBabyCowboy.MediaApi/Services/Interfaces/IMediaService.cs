using TheBabyCowBoy.MediaApi.DTOs;

namespace TheBabyCowBoy.MediaApi.Services.Interfaces;

public interface IMediaService
{
    Task<List<MediaItemResponse>> GetAllAsync(
        string? category = null,
        string? status = null,
        string? genre = null);

    Task<MediaItemResponse?> GetByIdAsync(Guid id);
    Task<MediaItemResponse?> UpdateAsync(
        Guid id,
        MediaItemUpdateRequest request);

    Task<MediaItemResponse> CreateAsync(
        MediaItemCreateRequest request);
    Task<bool> DeleteAsync(Guid id);
}