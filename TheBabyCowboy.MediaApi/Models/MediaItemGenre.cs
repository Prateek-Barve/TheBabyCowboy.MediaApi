namespace TheBabyCowBoy.MediaApi.Models;

public class MediaItemGenre
{
    public Guid MediaItemId { get; set; }

    public Guid GenreId { get; set; }

    public MediaItem MediaItem { get; set; } = null!;

    public Genre Genre { get; set; } = null!;
}