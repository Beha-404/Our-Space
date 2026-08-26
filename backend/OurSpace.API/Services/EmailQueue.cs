using System.Threading.Channels;

namespace OurSpace.API.Services;

public class EmailQueue : IEmailQueue
{
    private const int Capacity = 500;

    private readonly Channel<QueuedEmail> _channel = Channel.CreateBounded<QueuedEmail>(
        new BoundedChannelOptions(Capacity)
        {
            FullMode = BoundedChannelFullMode.DropWrite,
            SingleReader = true,
        });

    public ChannelReader<QueuedEmail> Reader => _channel.Reader;

    public void Enqueue(string toEmail, string subject, string body) =>
        _channel.Writer.TryWrite(new QueuedEmail(toEmail, subject, body));
}
