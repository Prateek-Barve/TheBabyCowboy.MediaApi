namespace TheBabyCowBoy.MediaApi.DTOs;

public class MediaItemUpdateRequest
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