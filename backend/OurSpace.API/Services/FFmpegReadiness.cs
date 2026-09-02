namespace OurSpace.API.Services;

public interface IFFmpegReadiness
{
    bool IsReady { get; }
    void MarkReady();
}

public class FFmpegReadiness : IFFmpegReadiness
{
    private volatile bool _ready;

    public bool IsReady => _ready;

    public void MarkReady() => _ready = true;
}
