namespace TheBabyCowBoy.MediaApi.Models;

public class PlaylistDetails
{
    public Guid MediaItemId { get; set; }

    public string SpotifyPlaylistId { get; set; } = string.Empty;

    public string Platform { get; set; } = "Spotify";

    public MediaItem MediaItem { get; set; } = null!;
}