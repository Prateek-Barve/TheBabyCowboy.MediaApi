
namespace TheBabyCowBoy.MediaApi.DTOs;

public class MediaItemResponse
{
    public Guid Id { get; set; }

    public string Title { get; set; } = string.Empty;

    public string Category { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public string? Description { get; set; }

    public string? CoverImageUrl { get; set; }

    public int DisplayOrder { get; set; }

    public List<GenreResponse> Genres { get; set; } = new();

    public BookDetailsResponse? BookDetails { get; set; }

    public MovieDetailsResponse? MovieDetails { get; set; }

    public SeriesDetailsResponse? SeriesDetails { get; set; }

    public PlaylistDetailsResponse? PlaylistDetails { get; set; }
}

public class GenreResponse
{
    public string Name { get; set; } = string.Empty;

    public string Slug { get; set; } = string.Empty;
}

public class BookDetailsResponse
{
    public string Author { get; set; } = string.Empty;
}

public class MovieDetailsResponse
{
    public string? Director { get; set; }

    public int? ReleaseYear { get; set; }
}

public class SeriesDetailsResponse
{
    public string? Creator { get; set; }

    public int? SeasonCount { get; set; }

    public int? EpisodeCount { get; set; }
}

public class PlaylistDetailsResponse
{
    public string SpotifyPlaylistId { get; set; } = string.Empty;

    public string Platform { get; set; } = "Spotify";
}
