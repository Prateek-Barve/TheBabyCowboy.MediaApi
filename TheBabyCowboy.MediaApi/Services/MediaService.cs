using Microsoft.EntityFrameworkCore;
using TheBabyCowBoy.MediaApi.Data;
using TheBabyCowBoy.MediaApi.DTOs;
using TheBabyCowBoy.MediaApi.Models;
using TheBabyCowBoy.MediaApi.Services.Interfaces;

namespace TheBabyCowBoy.MediaApi.Services;

public class MediaService : IMediaService
{
    private readonly MediaDbContext _context;

    public MediaService(MediaDbContext context)
    {
        _context = context;
    }

    public async Task<List<MediaItemResponse>> GetAllAsync(
        string? category = null,
        string? status = null,
        string? genre = null)
    {
        var query = _context.MediaItems
            .AsNoTracking()
            .AsQueryable();

        // Filter by category.
        if (!string.IsNullOrWhiteSpace(category))
        {
            if (!Enum.GetNames<MediaCategory>()
                .Contains(category, StringComparer.OrdinalIgnoreCase))
            {
                throw new ArgumentException("Invalid category.");
            }

            var parsedCategory = Enum.Parse<MediaCategory>(
                category,
                ignoreCase: true);

            query = query.Where(m => m.Category == parsedCategory);
        }

        // Filter by status.
        if (!string.IsNullOrWhiteSpace(status))
        {
            if (!Enum.GetNames<MediaStatus>()
                .Contains(status, StringComparer.OrdinalIgnoreCase))
            {
                throw new ArgumentException("Invalid status.");
            }

            var parsedStatus = Enum.Parse<MediaStatus>(
                status,
                ignoreCase: true);

            query = query.Where(m => m.Status == parsedStatus);
        }

        // Filter by genre slug.
        if (!string.IsNullOrWhiteSpace(genre))
        {
            var genreSlug = genre.Trim().ToLowerInvariant();

            query = query.Where(m =>
                m.MediaItemGenres.Any(mg =>
                    mg.Genre.Slug == genreSlug));
        }

        var items = await query
            .Include(m => m.MediaItemGenres)
                .ThenInclude(mg => mg.Genre)
            .Include(m => m.BookDetails)
            .Include(m => m.MovieDetails)
            .Include(m => m.SeriesDetails)
            .Include(m => m.PlaylistDetails)
            .OrderBy(m => m.DisplayOrder)
            .ThenBy(m => m.Title)
            .ToListAsync();

        return items.Select(MapToResponse).ToList();
    }

    public async Task<MediaItemResponse?> GetByIdAsync(Guid id)
    {
        var item = await _context.MediaItems
            .AsNoTracking()
            .Include(m => m.MediaItemGenres)
                .ThenInclude(mg => mg.Genre)
            .Include(m => m.BookDetails)
            .Include(m => m.MovieDetails)
            .Include(m => m.SeriesDetails)
            .Include(m => m.PlaylistDetails)
            .FirstOrDefaultAsync(m => m.Id == id);

        return item is null ? null : MapToResponse(item);
    }

    public async Task<MediaItemResponse> CreateAsync(
        MediaItemCreateRequest request)
    {
        // Validate title.
        if (string.IsNullOrWhiteSpace(request.Title))
        {
            throw new ArgumentException("Title is required.");
        }

        var title = request.Title.Trim();

        if (title.Length > 200)
        {
            throw new ArgumentException(
                "Title cannot exceed 200 characters.");
        }

        if (request.DisplayOrder < 0)
        {
            throw new ArgumentException(
                "DisplayOrder cannot be negative.");
        }

        // Validate category and status names.
        if (!Enum.GetNames<MediaCategory>()
                .Contains(request.Category,
                    StringComparer.OrdinalIgnoreCase) ||
            !Enum.TryParse<MediaCategory>(
                request.Category,
                true,
                out var category) ||
            !Enum.GetNames<MediaStatus>()
                .Contains(request.Status,
                    StringComparer.OrdinalIgnoreCase) ||
            !Enum.TryParse<MediaStatus>(
                request.Status,
                true,
                out var status))
        {
            throw new ArgumentException(
                "Invalid category or status.");
        }

        // Enforce category/status rules.
        var validStatus = category switch
        {
            MediaCategory.Book =>
                status is MediaStatus.Reading
                    or MediaStatus.Read
                    or MediaStatus.Curated,

            MediaCategory.Movie =>
                status is MediaStatus.Watched
                    or MediaStatus.Curated,

            MediaCategory.Series =>
                status is MediaStatus.Watched
                    or MediaStatus.Watching
                    or MediaStatus.Curated,

            MediaCategory.Playlist =>
                status == MediaStatus.Curated,

            _ => false
        };

        if (!validStatus)
        {
            throw new ArgumentException(
                $"Status '{status}' is not allowed for category '{category}'.");
        }

        // Validate category-specific details.
        var detailsAreValid = category switch
        {
            MediaCategory.Book =>
                request.BookDetails is not null &&
                !string.IsNullOrWhiteSpace(
                    request.BookDetails.Author) &&
                request.BookDetails.Author.Trim().Length <= 200 &&
                request.MovieDetails is null &&
                request.SeriesDetails is null &&
                request.PlaylistDetails is null,

            MediaCategory.Movie =>
                request.MovieDetails is not null &&
                (request.MovieDetails.Director?.Length ?? 0) <= 200 &&
                request.BookDetails is null &&
                request.SeriesDetails is null &&
                request.PlaylistDetails is null,

            MediaCategory.Series =>
                request.SeriesDetails is not null &&
                (request.SeriesDetails.Creator?.Length ?? 0) <= 200 &&
                (request.SeriesDetails.SeasonCount ?? 0) >= 0 &&
                (request.SeriesDetails.EpisodeCount ?? 0) >= 0 &&
                request.BookDetails is null &&
                request.MovieDetails is null &&
                request.PlaylistDetails is null,

            MediaCategory.Playlist =>
                request.PlaylistDetails is not null &&
                !string.IsNullOrWhiteSpace(
                    request.PlaylistDetails.SpotifyPlaylistId) &&
                request.PlaylistDetails.SpotifyPlaylistId.Trim().Length <= 100 &&
                !string.IsNullOrWhiteSpace(
                    request.PlaylistDetails.Platform) &&
                request.PlaylistDetails.Platform.Trim().Length <= 50 &&
                request.BookDetails is null &&
                request.MovieDetails is null &&
                request.SeriesDetails is null,

            _ => false
        };

        if (!detailsAreValid)
        {
            throw new ArgumentException(
                "Provide valid details for the selected category only.");
        }

        if (request.Description?.Length > 2000 ||
            request.CoverImageUrl?.Length > 1000)
        {
            throw new ArgumentException(
                "Description or cover image URL exceeds its maximum length.");
        }

        if (request.MovieDetails?.ReleaseYear is < 0 or > 9999)
        {
            throw new ArgumentException(
                "ReleaseYear must be between 0 and 9999.");
        }

        // Normalize genre slugs and reject invalid values.
        var genreSlugs = (request.GenreSlugs ?? new List<string>())
            .Select(slug => slug?.Trim().ToLowerInvariant())
            .ToList();

        if (genreSlugs.Any(string.IsNullOrWhiteSpace))
        {
            throw new ArgumentException(
                "Genre slugs cannot be empty.");
        }

        genreSlugs = genreSlugs
            .Distinct(StringComparer.Ordinal)
            .ToList();

        if (genreSlugs.Any(slug => slug!.Length > 100))
        {
            throw new ArgumentException(
                "Genre slugs cannot exceed 100 characters.");
        }

        var normalizedSlugs = genreSlugs
            .Select(slug => slug!)
            .ToList();

        var genres = await _context.Genres
            .Where(g => normalizedSlugs.Contains(g.Slug))
            .ToListAsync();

        if (genres.Count != normalizedSlugs.Count)
        {
            var foundSlugs = genres
                .Select(g => g.Slug)
                .ToHashSet(StringComparer.Ordinal);

            var unknownSlugs = normalizedSlugs
                .Where(slug => !foundSlugs.Contains(slug))
                .ToList();

            throw new ArgumentException(
                $"Unknown genre slugs: {string.Join(", ", unknownSlugs)}");
        }

        // Build the common media record.
        var item = new MediaItem
        {
            Title = title,
            Category = category,
            Status = status,
            Description = request.Description?.Trim(),
            CoverImageUrl = request.CoverImageUrl?.Trim(),
            DisplayOrder = request.DisplayOrder
        };

        // Create the relevant type-specific details.
        switch (category)
        {
            case MediaCategory.Book:
                item.BookDetails = new BookDetails
                {
                    MediaItemId = item.Id,
                    Author = request.BookDetails!.Author.Trim()
                };
                break;

            case MediaCategory.Movie:
                item.MovieDetails = new MovieDetails
                {
                    MediaItemId = item.Id,
                    Director = request.MovieDetails!.Director?.Trim(),
                    ReleaseYear = request.MovieDetails.ReleaseYear
                };
                break;

            case MediaCategory.Series:
                item.SeriesDetails = new SeriesDetails
                {
                    MediaItemId = item.Id,
                    Creator = request.SeriesDetails!.Creator?.Trim(),
                    SeasonCount = request.SeriesDetails.SeasonCount,
                    EpisodeCount = request.SeriesDetails.EpisodeCount
                };
                break;

            case MediaCategory.Playlist:
                item.PlaylistDetails = new PlaylistDetails
                {
                    MediaItemId = item.Id,
                    SpotifyPlaylistId =
                        request.PlaylistDetails!.SpotifyPlaylistId.Trim(),
                    Platform = request.PlaylistDetails.Platform.Trim()
                };
                break;
        }

        // Associate existing genres.
        foreach (var genre in genres)
        {
            item.MediaItemGenres.Add(new MediaItemGenre
            {
                MediaItemId = item.Id,
                GenreId = genre.Id
            });
        }

        _context.MediaItems.Add(item);

        await _context.SaveChangesAsync();

        // Load the complete response after saving.
        var createdItem = await _context.MediaItems
            .AsNoTracking()
            .Include(m => m.MediaItemGenres)
                .ThenInclude(mg => mg.Genre)
            .Include(m => m.BookDetails)
            .Include(m => m.MovieDetails)
            .Include(m => m.SeriesDetails)
            .Include(m => m.PlaylistDetails)
            .FirstAsync(m => m.Id == item.Id);

        return MapToResponse(createdItem);
    }

    public async Task<MediaItemResponse?> UpdateAsync(
    Guid id,
    MediaItemUpdateRequest request)
    {
        // Find the existing media item and its relationships.
        var item = await _context.MediaItems
            .Include(m => m.MediaItemGenres)
            .Include(m => m.BookDetails)
            .Include(m => m.MovieDetails)
            .Include(m => m.SeriesDetails)
            .Include(m => m.PlaylistDetails)
            .FirstOrDefaultAsync(m => m.Id == id);

        if (item is null)
        {
            return null;
        }

        // Validate title.
        if (string.IsNullOrWhiteSpace(request.Title))
        {
            throw new ArgumentException("Title is required.");
        }

        var title = request.Title.Trim();

        if (title.Length > 200)
        {
            throw new ArgumentException(
                "Title cannot exceed 200 characters.");
        }

        if (request.DisplayOrder < 0)
        {
            throw new ArgumentException(
                "DisplayOrder cannot be negative.");
        }

        // Validate category and status names.
        if (!Enum.GetNames<MediaCategory>()
                .Contains(request.Category,
                    StringComparer.OrdinalIgnoreCase) ||
            !Enum.TryParse<MediaCategory>(
                request.Category,
                true,
                out var category) ||
            !Enum.GetNames<MediaStatus>()
                .Contains(request.Status,
                    StringComparer.OrdinalIgnoreCase) ||
            !Enum.TryParse<MediaStatus>(
                request.Status,
                true,
                out var status))
        {
            throw new ArgumentException(
                "Invalid category or status.");
        }

        // Enforce category/status rules.
        var validStatus = category switch
        {
            MediaCategory.Book =>
                status is MediaStatus.Reading
                    or MediaStatus.Read
                    or MediaStatus.Curated,

            MediaCategory.Movie =>
                status is MediaStatus.Watched
                    or MediaStatus.Curated,

            MediaCategory.Series =>
                status is MediaStatus.Watched
                    or MediaStatus.Watching
                    or MediaStatus.Curated,

            MediaCategory.Playlist =>
                status == MediaStatus.Curated,

            _ => false
        };

        if (!validStatus)
        {
            throw new ArgumentException(
                $"Status '{status}' is not allowed for category '{category}'.");
        }

        // Validate the details appropriate for this category.
        var detailsAreValid = category switch
        {
            MediaCategory.Book =>
                request.BookDetails is not null &&
                !string.IsNullOrWhiteSpace(
                    request.BookDetails.Author) &&
                request.BookDetails.Author.Trim().Length <= 200 &&
                request.MovieDetails is null &&
                request.SeriesDetails is null &&
                request.PlaylistDetails is null,

            MediaCategory.Movie =>
                request.MovieDetails is not null &&
                (request.MovieDetails.Director?.Length ?? 0) <= 200 &&
                request.BookDetails is null &&
                request.SeriesDetails is null &&
                request.PlaylistDetails is null,

            MediaCategory.Series =>
                request.SeriesDetails is not null &&
                (request.SeriesDetails.Creator?.Length ?? 0) <= 200 &&
                (request.SeriesDetails.SeasonCount ?? 0) >= 0 &&
                (request.SeriesDetails.EpisodeCount ?? 0) >= 0 &&
                request.BookDetails is null &&
                request.MovieDetails is null &&
                request.PlaylistDetails is null,

            MediaCategory.Playlist =>
                request.PlaylistDetails is not null &&
                !string.IsNullOrWhiteSpace(
                    request.PlaylistDetails.SpotifyPlaylistId) &&
                request.PlaylistDetails.SpotifyPlaylistId.Trim().Length <= 100 &&
                !string.IsNullOrWhiteSpace(
                    request.PlaylistDetails.Platform) &&
                request.PlaylistDetails.Platform.Trim().Length <= 50 &&
                request.BookDetails is null &&
                request.MovieDetails is null &&
                request.SeriesDetails is null,

            _ => false
        };

        if (!detailsAreValid)
        {
            throw new ArgumentException(
                "Provide valid details for the selected category only.");
        }

        if (request.Description?.Length > 2000 ||
            request.CoverImageUrl?.Length > 1000)
        {
            throw new ArgumentException(
                "Description or cover image URL exceeds its maximum length.");
        }

        if (request.MovieDetails?.ReleaseYear is < 0 or > 9999)
        {
            throw new ArgumentException(
                "ReleaseYear must be between 0 and 9999.");
        }

        // Normalize genre slugs.
        var genreSlugs = (request.GenreSlugs ?? new List<string>())
            .Select(slug => slug?.Trim().ToLowerInvariant())
            .ToList();

        if (genreSlugs.Any(string.IsNullOrWhiteSpace))
        {
            throw new ArgumentException(
                "Genre slugs cannot be empty.");
        }

        genreSlugs = genreSlugs
            .Distinct(StringComparer.Ordinal)
            .ToList();

        if (genreSlugs.Any(slug => slug!.Length > 100))
        {
            throw new ArgumentException(
                "Genre slugs cannot exceed 100 characters.");
        }

        var normalizedSlugs = genreSlugs
            .Select(slug => slug!)
            .ToList();

        var genres = await _context.Genres
            .Where(g => normalizedSlugs.Contains(g.Slug))
            .ToListAsync();

        if (genres.Count != normalizedSlugs.Count)
        {
            var foundSlugs = genres
                .Select(g => g.Slug)
                .ToHashSet(StringComparer.Ordinal);

            var unknownSlugs = normalizedSlugs
                .Where(slug => !foundSlugs.Contains(slug))
                .ToList();

            throw new ArgumentException(
                $"Unknown genre slugs: {string.Join(", ", unknownSlugs)}");
        }

        // Update common properties.
        item.Title = title;
        item.Category = category;
        item.Status = status;
        item.Description = request.Description?.Trim();
        item.CoverImageUrl = request.CoverImageUrl?.Trim();
        item.DisplayOrder = request.DisplayOrder;
        item.UpdatedAt = DateTime.UtcNow;

        // Update type-specific details.
        // If the category changes, remove the previous detail record.
        if (category != MediaCategory.Book)
        {
            item.BookDetails = null;
        }

        if (category != MediaCategory.Movie)
        {
            item.MovieDetails = null;
        }

        if (category != MediaCategory.Series)
        {
            item.SeriesDetails = null;
        }

        if (category != MediaCategory.Playlist)
        {
            item.PlaylistDetails = null;
        }

        switch (category)
        {
            case MediaCategory.Book:
                if (item.BookDetails is null)
                {
                    item.BookDetails = new BookDetails
                    {
                        MediaItemId = item.Id,
                        Author = request.BookDetails!.Author.Trim()
                    };
                }
                else
                {
                    item.BookDetails.Author =
                        request.BookDetails!.Author.Trim();
                }
                break;

            case MediaCategory.Movie:
                if (item.MovieDetails is null)
                {
                    item.MovieDetails = new MovieDetails
                    {
                        MediaItemId = item.Id,
                        Director = request.MovieDetails!.Director?.Trim(),
                        ReleaseYear = request.MovieDetails.ReleaseYear
                    };
                }
                else
                {
                    item.MovieDetails.Director =
                        request.MovieDetails!.Director?.Trim();

                    item.MovieDetails.ReleaseYear =
                        request.MovieDetails.ReleaseYear;
                }
                break;

            case MediaCategory.Series:
                if (item.SeriesDetails is null)
                {
                    item.SeriesDetails = new SeriesDetails
                    {
                        MediaItemId = item.Id,
                        Creator = request.SeriesDetails!.Creator?.Trim(),
                        SeasonCount = request.SeriesDetails.SeasonCount,
                        EpisodeCount = request.SeriesDetails.EpisodeCount
                    };
                }
                else
                {
                    item.SeriesDetails.Creator =
                        request.SeriesDetails!.Creator?.Trim();

                    item.SeriesDetails.SeasonCount =
                        request.SeriesDetails.SeasonCount;

                    item.SeriesDetails.EpisodeCount =
                        request.SeriesDetails.EpisodeCount;
                }
                break;

            case MediaCategory.Playlist:
                if (item.PlaylistDetails is null)
                {
                    item.PlaylistDetails = new PlaylistDetails
                    {
                        MediaItemId = item.Id,
                        SpotifyPlaylistId =
                            request.PlaylistDetails!.SpotifyPlaylistId.Trim(),
                        Platform = request.PlaylistDetails.Platform.Trim()
                    };
                }
                else
                {
                    item.PlaylistDetails.SpotifyPlaylistId =
                        request.PlaylistDetails!.SpotifyPlaylistId.Trim();

                    item.PlaylistDetails.Platform =
                        request.PlaylistDetails.Platform.Trim();
                }
                break;
        }

        // Replace genre associations with the requested set.
        _context.MediaItemGenres.RemoveRange(item.MediaItemGenres);

        item.MediaItemGenres.Clear();

        foreach (var genre in genres)
        {
            item.MediaItemGenres.Add(new MediaItemGenre
            {
                MediaItemId = item.Id,
                GenreId = genre.Id
            });
        }

        await _context.SaveChangesAsync();

        // Reload the complete item to produce the API response.
        var updatedItem = await _context.MediaItems
            .AsNoTracking()
            .Include(m => m.MediaItemGenres)
                .ThenInclude(mg => mg.Genre)
            .Include(m => m.BookDetails)
            .Include(m => m.MovieDetails)
            .Include(m => m.SeriesDetails)
            .Include(m => m.PlaylistDetails)
            .FirstAsync(m => m.Id == id);

        return MapToResponse(updatedItem);
    }
    public async Task<bool> DeleteAsync(Guid id)
    {
        var item = await _context.MediaItems
            .FirstOrDefaultAsync(m => m.Id == id);

        if (item is null)
        {
            return false;
        }

        _context.MediaItems.Remove(item);

        await _context.SaveChangesAsync();

        return true;
    }

    private static MediaItemResponse MapToResponse(MediaItem item)
    {
        return new MediaItemResponse
        {
            Id = item.Id,
            Title = item.Title,
            Category = item.Category.ToString(),
            Status = item.Status.ToString(),
            Description = item.Description,
            CoverImageUrl = item.CoverImageUrl,
            DisplayOrder = item.DisplayOrder,

            Genres = item.MediaItemGenres
                .Where(mg => mg.Genre is not null)
                .Select(mg => new GenreResponse
                {
                    Name = mg.Genre.Name,
                    Slug = mg.Genre.Slug
                })
                .OrderBy(g => g.Name)
                .ToList(),

            BookDetails = item.BookDetails is null
                ? null
                : new BookDetailsResponse
                {
                    Author = item.BookDetails.Author
                },

            MovieDetails = item.MovieDetails is null
                ? null
                : new MovieDetailsResponse
                {
                    Director = item.MovieDetails.Director,
                    ReleaseYear = item.MovieDetails.ReleaseYear
                },

            SeriesDetails = item.SeriesDetails is null
                ? null
                : new SeriesDetailsResponse
                {
                    Creator = item.SeriesDetails.Creator,
                    SeasonCount = item.SeriesDetails.SeasonCount,
                    EpisodeCount = item.SeriesDetails.EpisodeCount
                },

            PlaylistDetails = item.PlaylistDetails is null
                ? null
                : new PlaylistDetailsResponse
                {
                    SpotifyPlaylistId =
                        item.PlaylistDetails.SpotifyPlaylistId,
                    Platform = item.PlaylistDetails.Platform
                }
        };
    }
}