namespace SCGLolTimerBot.Models;

public class BotData
{
    public ulong ChannelId { get; set; }
    public ulong AvailableChannelId { get; set; }
    public ulong Leader { get; set; }
    public required Timer Timer { get; set; }
}