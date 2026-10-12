
//using Microsoft.AspNetCore.Mvc;
//using Microsoft.EntityFrameworkCore;
//using TheBabyCowBoy.MediaApi.Data;
//using TheBabyCowBoy.MediaApi.DTOs;
//using TheBabyCowBoy.MediaApi.Models;

//namespace TheBabyCowBoy.MediaApi.Controllers;

//[ApiController]
//[Route("api/[controller]")]
//public class MediaController : ControllerBase
//{
//    private readonly MediaDbContext _context;

//    public MediaController(MediaDbContext context)
//    {
//        _context = context;
//    }

//    // GET: /api/media
//    [HttpGet]
//    public async Task<ActionResult<List<MediaItemResponse>>> GetAll()
//    {
//        var items = await _context.MediaItems
//            .AsNoTracking()
//            .Include(m => m.MediaItemGenres)
//                .ThenInclude(mg => mg.Genre)
//            .Include(m => m.BookDetails)
//            .Include(m => m.MovieDetails)
//            .Include(m => m.SeriesDetails)
//            .Include(m => m.PlaylistDetails)
//            .OrderBy(m => m.DisplayOrder)
//            .ThenBy(m => m.Title)
//            .ToListAsync();

//        var response = items.Select(MapToResponse).ToList();

//        return Ok(response);
//    }

//    // GET: /api/media/{id}
//    [HttpGet("{id:guid}")]
//    public async Task<ActionResult<MediaItemResponse>> GetById(Guid id)
//    {
//        var item = await _context.MediaItems
//            .AsNoTracking()
//            .Include(m => m.MediaItemGenres)
//                .ThenInclude(mg => mg.Genre)
//            .Include(m => m.BookDetails)
//            .Include(m => m.MovieDetails)
//            .Include(m => m.SeriesDetails)
//            .Include(m => m.PlaylistDetails)
//            .FirstOrDefaultAsync(m => m.Id == id);

//        if (item is null)
//        {
//            return NotFound();
//        }

//        return Ok(MapToResponse(item));
//    }


//    [HttpPost]
//    public async Task<ActionResult<MediaItemResponse>> Create(
//        [FromBody] MediaItemCreateRequest request)
//    {
//        // Validate title.
//        if (string.IsNullOrWhiteSpace(request.Title))
//        {
//            return BadRequest(new { message = "Title is required." });
//        }

//        if (request.Title.Trim().Length > 200)
//        {
//            return BadRequest(new
//            {
//                message = "Title cannot exceed 200 characters."
//            });
//        }

//        if (request.DisplayOrder < 0)
//        {
//            return BadRequest(new
//            {
//                message = "DisplayOrder cannot be negative."
//            });
//        }

//        // Parse category and status without accepting numeric enum values.
//        //if (!Enum.TryParse<MediaCategory>(
//        //        request.Category, true, out var category) ||
//        //    !Enum.IsDefined(typeof(MediaCategory), category) ||
//        //    !Enum.TryParse<MediaStatus>(
//        //        request.Status, true, out var status) ||
//        //    !Enum.IsDefined(typeof(MediaStatus), status))
//        //{
//        //    return BadRequest(new
//        //    {
//        //        message = "Invalid category or status."
//        //    });
//        //}
//        if (!Enum.GetNames<MediaCategory>()
//        .Contains(request.Category, StringComparer.OrdinalIgnoreCase) ||
//    !Enum.TryParse<MediaCategory>(
//        request.Category, true, out var category) ||
//    !Enum.GetNames<MediaStatus>()
//        .Contains(request.Status, StringComparer.OrdinalIgnoreCase) ||
//    !Enum.TryParse<MediaStatus>(
//        request.Status, true, out var status))
//        {
//            return BadRequest(new
//            {
//                message = "Invalid category or status."
//            });
//        }

//        // Enforce the allowed status for each category.
//        var validStatus = category switch
//        {
//            MediaCategory.Book =>
//                status is MediaStatus.Reading
//                    or MediaStatus.Read
//                    or MediaStatus.Curated,

//            MediaCategory.Movie =>
//                status is MediaStatus.Watched
//                    or MediaStatus.Curated,

//            MediaCategory.Series =>
//                status is MediaStatus.Watched
//                    or MediaStatus.Watching
//                    or MediaStatus.Curated,

//            MediaCategory.Playlist =>
//                status == MediaStatus.Curated,

//            _ => false
//        };

//        if (!validStatus)
//        {
//            return BadRequest(new
//            {
//                message = $"Status '{status}' is not allowed for category '{category}'."
//            });
//        }

//        // Require details for the selected category, and reject
//        // detail objects belonging to other categories.
//        var detailsAreValid = category switch
//        {
//            MediaCategory.Book =>
//                request.BookDetails is not null &&
//                !string.IsNullOrWhiteSpace(request.BookDetails.Author) &&
//                request.BookDetails.Author.Trim().Length <= 200 &&
//                request.MovieDetails is null &&
//                request.SeriesDetails is null &&
//                request.PlaylistDetails is null,

//            MediaCategory.Movie =>
//                request.MovieDetails is not null &&
//                (request.MovieDetails.Director?.Length ?? 0) <= 200 &&
//                request.BookDetails is null &&
//                request.SeriesDetails is null &&
//                request.PlaylistDetails is null,

//            MediaCategory.Series =>
//                request.SeriesDetails is not null &&
//                (request.SeriesDetails.Creator?.Length ?? 0) <= 200 &&
//                (request.SeriesDetails.SeasonCount ?? 0) >= 0 &&
//                (request.SeriesDetails.EpisodeCount ?? 0) >= 0 &&
//                request.BookDetails is null &&
//                request.MovieDetails is null &&
//                request.PlaylistDetails is null,

//            MediaCategory.Playlist =>
//                request.PlaylistDetails is not null &&
//                !string.IsNullOrWhiteSpace(
//                    request.PlaylistDetails.SpotifyPlaylistId) &&
//                request.PlaylistDetails.SpotifyPlaylistId.Trim().Length <= 100 &&
//                !string.IsNullOrWhiteSpace(request.PlaylistDetails.Platform) &&
//                request.PlaylistDetails.Platform.Trim().Length <= 50 &&
//                request.BookDetails is null &&
//                request.MovieDetails is null &&
//                request.SeriesDetails is null,

//            _ => false
//        };

//        if (!detailsAreValid)
//        {
//            return BadRequest(new
//            {
//                message = "Provide valid details for the selected category only."
//            });
//        }

//        if (request.Description?.Length > 2000 ||
//            request.CoverImageUrl?.Length > 1000)
//        {
//            return BadRequest(new
//            {
//                message = "Description or cover image URL exceeds its maximum length."
//            });
//        }

//        if (request.MovieDetails?.ReleaseYear is < 0 or > 9999)
//        {
//            return BadRequest(new
//            {
//                message = "ReleaseYear must be between 0 and 9999."
//            });
//        }

//        // Normalize genre slugs and remove duplicates.
//        //var genreSlugs = request.GenreSlugs
//        //    .Where(slug => !string.IsNullOrWhiteSpace(slug))
//        //    .Select(slug => slug.Trim().ToLowerInvariant())
//        //    .Distinct()
//        //    .ToList();

//        //if (genreSlugs.Any(slug => slug.Length > 100))
//        //{
//        //    return BadRequest(new
//        //    {
//        //        message = "Genre slugs cannot exceed 100 characters."
//        //    });
//        //}
//        var genreSlugs = (request.GenreSlugs ?? new List<string>())
//    .Where(slug => !string.IsNullOrWhiteSpace(slug))
//    .Select(slug => slug.Trim().ToLowerInvariant())
//    .Distinct()
//    .ToList();

//        var genres = await _context.Genres
//            .Where(g => genreSlugs.Contains(g.Slug.ToLower()))
//            .ToListAsync();

//        if (genres.Count != genreSlugs.Count)
//        {
//            var foundSlugs = genres
//                .Select(g => g.Slug.ToLower())
//                .ToHashSet();

//            var unknownSlugs = genreSlugs
//                .Where(slug => !foundSlugs.Contains(slug))
//                .ToList();

//            return BadRequest(new
//            {
//                message = "One or more genre slugs do not exist.",
//                unknownSlugs
//            });
//        }

//        // Build the common media record.
//        var item = new MediaItem
//        {
//            Title = request.Title.Trim(),
//            Category = category,
//            Status = status,
//            Description = request.Description?.Trim(),
//            CoverImageUrl = request.CoverImageUrl?.Trim(),
//            DisplayOrder = request.DisplayOrder
//        };

//        // Add exactly one category-specific detail record.
//        switch (category)
//        {
//            case MediaCategory.Book:
//                item.BookDetails = new BookDetails
//                {
//                    MediaItemId = item.Id,
//                    Author = request.BookDetails!.Author.Trim()
//                };
//                break;

//            case MediaCategory.Movie:
//                item.MovieDetails = new MovieDetails
//                {
//                    MediaItemId = item.Id,
//                    Director = request.MovieDetails!.Director?.Trim(),
//                    ReleaseYear = request.MovieDetails.ReleaseYear
//                };
//                break;

//            case MediaCategory.Series:
//                item.SeriesDetails = new SeriesDetails
//                {
//                    MediaItemId = item.Id,
//                    Creator = request.SeriesDetails!.Creator?.Trim(),
//                    SeasonCount = request.SeriesDetails.SeasonCount,
//                    EpisodeCount = request.SeriesDetails.EpisodeCount
//                };
//                break;

//            case MediaCategory.Playlist:
//                item.PlaylistDetails = new PlaylistDetails
//                {
//                    MediaItemId = item.Id,
//                    SpotifyPlaylistId =
//                        request.PlaylistDetails!.SpotifyPlaylistId.Trim(),
//                    Platform = request.PlaylistDetails.Platform.Trim()
//                };
//                break;
//        }

//        // Associate existing genres; do not create duplicate genres.
//        foreach (var genre in genres)
//        {
//            item.MediaItemGenres.Add(new MediaItemGenre
//            {
//                MediaItemId = item.Id,
//                GenreId = genre.Id,
//                Genre = genre,
//                MediaItem = item
//            });
//        }

//        _context.MediaItems.Add(item);
//        await _context.SaveChangesAsync();

//        // Return the same response shape as the GET endpoints.
//        return CreatedAtAction(
//            nameof(GetById),
//            new { id = item.Id },
//            MapToResponse(item));
//    }


//    private static MediaItemResponse MapToResponse(MediaItem item)
//    {
//        return new MediaItemResponse
//        {
//            Id = item.Id,
//            Title = item.Title,
//            Category = item.Category.ToString(),
//            Status = item.Status.ToString(),
//            Description = item.Description,
//            CoverImageUrl = item.CoverImageUrl,
//            DisplayOrder = item.DisplayOrder,

//            Genres = item.MediaItemGenres
//                .Where(mg => mg.Genre is not null)
//                .Select(mg => new GenreResponse
//                {
//                    Name = mg.Genre.Name,
//                    Slug = mg.Genre.Slug
//                })
//                .OrderBy(g => g.Name)
//                .ToList(),

//            BookDetails = item.BookDetails is null
//                ? null
//                : new BookDetailsResponse
//                {
//                    Author = item.BookDetails.Author
//                },

//            MovieDetails = item.MovieDetails is null
//                ? null
//                : new MovieDetailsResponse
//                {
//                    Director = item.MovieDetails.Director,
//                    ReleaseYear = item.MovieDetails.ReleaseYear
//                },

//            SeriesDetails = item.SeriesDetails is null
//                ? null
//                : new SeriesDetailsResponse
//                {
//                    Creator = item.SeriesDetails.Creator,
//                    SeasonCount = item.SeriesDetails.SeasonCount,
//                    EpisodeCount = item.SeriesDetails.EpisodeCount
//                },

//            PlaylistDetails = item.PlaylistDetails is null
//                ? null
//                : new PlaylistDetailsResponse
//                {
//                    SpotifyPlaylistId =
//                        item.PlaylistDetails.SpotifyPlaylistId,
//                    Platform = item.PlaylistDetails.Platform
//                }
//        };
//    }
//}
using Microsoft.AspNetCore.Mvc;
using TheBabyCowBoy.MediaApi.DTOs;
using TheBabyCowBoy.MediaApi.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using TheBabyCowBoy.MediaApi.Exceptions;

namespace TheBabyCowBoy.MediaApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MediaController : ControllerBase
{
    private readonly IMediaService _mediaService;

    public MediaController(IMediaService mediaService)
    {
        _mediaService = mediaService;
    }

    // GET: /api/media
    //[HttpGet]
    //public async Task<ActionResult<List<MediaItemResponse>>> GetAll(
    //    [FromQuery] string? category,
    //    [FromQuery] string? status,
    //    [FromQuery] string? genre)
    //{
    //    try
    //    {
    //        var items = await _mediaService.GetAllAsync(
    //            category,
    //            status,
    //            genre);

    //        return Ok(items);
    //    }
    //    catch (ArgumentException ex)
    //    {
    //        return BadRequest(new { message = ex.Message });
    //    }
    //}
    [HttpGet]
    public async Task<ActionResult<PagedResponse<MediaItemResponse>>> GetAll(
    [FromQuery] string? category,
    [FromQuery] string? status,
    [FromQuery] string? genre,
    [FromQuery] int page = 1,
    [FromQuery] int pageSize = 10)
    {
        try
        {
            var result = await _mediaService.GetAllAsync(
                category,
                status,
                genre,
                page,
                pageSize);

            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    // GET: /api/media/{id}
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<MediaItemResponse>> GetById(Guid id)
    {
        var item = await _mediaService.GetByIdAsync(id);

        if (item is null)
        {
            return NotFound();
        }

        return Ok(item);
    }

    // POST: /api/media
    //[HttpPost]
    //public async Task<ActionResult<MediaItemResponse>> Create(
    //    [FromBody] MediaItemCreateRequest request)
    //{
    //    try
    //    {
    //        var item = await _mediaService.CreateAsync(request);

    //        return CreatedAtAction(
    //            nameof(GetById),
    //            new { id = item.Id },
    //            item);
    //    }
    //    catch (ArgumentException ex)
    //    {
    //        return BadRequest(new { message = ex.Message });
    //    }
    //}
    [HttpPost]
    public async Task<ActionResult<MediaItemResponse>> Create(
    [FromBody] MediaItemCreateRequest request)
    {
        try
        {
            var item = await _mediaService.CreateAsync(request);

            return CreatedAtAction(
                nameof(GetById),
                new { id = item.Id },
                item);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (DbUpdateException ex)
        {
            if (DatabaseExceptionHandler.IsUniqueViolation(
                ex, out var constraintName))
            {
                return Conflict(new
                {
                    message = GetDuplicateMessage(constraintName)
                });
            }

            throw;
        }
    }

    private static string GetDuplicateMessage(string? constraintName)
    {
        return constraintName switch
        {
            "IX_PlaylistDetails_SpotifyPlaylistId" =>
                "A playlist with this Spotify playlist ID already exists.",

            _ =>
                "A record with the same unique value already exists."
        };
    }

    // PUT: /api/media/{id}
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<MediaItemResponse>> Update(
        Guid id,
        [FromBody] MediaItemUpdateRequest request)
    {
        try
        {
            var item = await _mediaService.UpdateAsync(id, request);

            if (item is null)
            {
                return NotFound();
            }

            return Ok(item);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    // DELETE: /api/media/{id}
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var deleted = await _mediaService.DeleteAsync(id);

        if (!deleted)
        {
            return NotFound();
        }

        return NoContent();
    }
}