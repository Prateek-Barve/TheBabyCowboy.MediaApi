namespace TheBabyCowBoy.MediaApi.Models;

public class MovieDetails
{
    public Guid MediaItemId { get; set; }

    public string? Director { get; set; }

    public int? ReleaseYear { get; set; }

    public MediaItem MediaItem { get; set; } = null!;
}