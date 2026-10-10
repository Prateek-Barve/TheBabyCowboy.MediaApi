namespace TheBabyCowBoy.MediaApi.Models;

public class BookDetails
{
    public Guid MediaItemId { get; set; }

    public string Author { get; set; } = string.Empty;

    public MediaItem MediaItem { get; set; } = null!;
}