namespace TheBabyCowBoy.MediaApi.Models;

public class Genre
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Name { get; set; } = string.Empty;

    public string Slug { get; set; } = string.Empty;

    public ICollection<MediaItemGenre> MediaItemGenres { get; set; }
        = new List<MediaItemGenre>();
}