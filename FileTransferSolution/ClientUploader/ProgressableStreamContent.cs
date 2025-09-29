using System.Net;

public class ProgressableStreamContent : HttpContent
{
    private readonly byte[] _buffer;
    private readonly int _bufferSize;
    private readonly Action<long, long>? _progress;

    public ProgressableStreamContent(byte[] buffer, int bufferSize = 8192, Action<long, long>? progress = null)
    {
        _buffer = buffer;
        _bufferSize = bufferSize;
        _progress = progress;
        Headers.ContentLength = _buffer.Length;
        Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/octet-stream");
    }

    protected override async Task SerializeToStreamAsync(Stream stream, TransportContext? context)
    {
        long totalBytes = _buffer.Length;
        long uploadedBytes = 0;

        using var ms = new MemoryStream(_buffer);
        byte[] temp = new byte[_bufferSize];
        int read;
        while ((read = ms.Read(temp, 0, temp.Length)) > 0)
        {
            await stream.WriteAsync(temp.AsMemory(0, read));
            uploadedBytes += read;
            _progress?.Invoke(uploadedBytes, totalBytes);
        }
    }

    protected override bool TryComputeLength(out long length)
    {
        length = _buffer.Length;
        return true;
    }
}