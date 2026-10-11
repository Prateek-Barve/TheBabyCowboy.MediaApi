using TheBabyCowBoy.MediaApi.DTOs;

namespace TheBabyCowBoy.MediaApi.Services.Interfaces;

public interface IGenreService
{
    Task<List<GenreResponse>> GetAllAsync();

    Task<GenreResponse?> GetByIdAsync(Guid id);

    Task<GenreResponse> CreateAsync(
        GenreCreateRequest request);
}