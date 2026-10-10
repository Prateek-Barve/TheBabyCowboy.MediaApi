namespace TheBabyCowBoy.MediaApi.Models;

public class SeriesDetails
{
    public Guid MediaItemId { get; set; }

    public string? Creator { get; set; }
    public int? SeasonCount { get; set; }

    public int? EpisodeCount { get; set; }

    public MediaItem MediaItem { get; set; } = null!;
}