
namespace TheBabyCowBoy.MediaApi.DTOs;

public class MediaItemCreateRequest
{
    public string Title { get; set; } = string.Empty;

    public string Category { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public string? Description { get; set; }

    public string? CoverImageUrl { get; set; }

    public int DisplayOrder { get; set; }

    public List<string> GenreSlugs { get; set; } = new();


    public BookDetailsCreateRequest? BookDetails { get; set; }

    public MovieDetailsCreateRequest? MovieDetails { get; set; }

    public SeriesDetailsCreateRequest? SeriesDetails { get; set; }

    public PlaylistDetailsCreateRequest? PlaylistDetails { get; set; }
}

public class BookDetailsCreateRequest
{
    public string Author { get; set; } = string.Empty;
}

public class MovieDetailsCreateRequest
{
    public string? Director { get; set; }

    public int? ReleaseYear { get; set; }
}

public class SeriesDetailsCreateRequest
{
    public string? Creator { get; set; }

    public int? SeasonCount { get; set; }

    public int? EpisodeCount { get; set; }
}

public class PlaylistDetailsCreateRequest
{
    public string SpotifyPlaylistId { get; set; } = string.Empty;

    public string Platform { get; set; } = "Spotify";
}
