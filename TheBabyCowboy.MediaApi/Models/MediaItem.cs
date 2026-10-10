namespace TheBabyCowBoy.MediaApi.Models;

public class MediaItem
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Title { get; set; } = string.Empty;

    public MediaCategory Category { get; set; }

    public MediaStatus Status { get; set; }

    public string? Description { get; set; }

    public string? CoverImageUrl { get; set; }

    public int DisplayOrder { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    public BookDetails? BookDetails { get; set; }

    public MovieDetails? MovieDetails { get; set; }

    public SeriesDetails? SeriesDetails { get; set; }

    public PlaylistDetails? PlaylistDetails { get; set; }

    public ICollection<MediaItemGenre> MediaItemGenres { get; set; }
        = new List<MediaItemGenre>();
}