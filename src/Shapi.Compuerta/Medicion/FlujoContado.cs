namespace Shapi.Compuerta.Medicion;

/// <summary>Cuenta bytes transferidos sin almacenar cuerpos ni ser dueño del flujo original.</summary>
internal sealed class FlujoContado(Stream original) : Stream
{
    public long Leidos { get; private set; }
    public long Escritos { get; private set; }
    public override bool CanRead => original.CanRead;
    public override bool CanSeek => original.CanSeek;
    public override bool CanWrite => original.CanWrite;
    public override long Length => original.Length;
    public override long Position { get => original.Position; set => original.Position = value; }
    public override void Flush() => original.Flush();
    public override Task FlushAsync(CancellationToken cancellationToken) => original.FlushAsync(cancellationToken);
    public override long Seek(long offset, SeekOrigin origin) => original.Seek(offset, origin);
    public override void SetLength(long value) => original.SetLength(value);
    public override int Read(byte[] buffer, int offset, int count)
    {
        var leidos = original.Read(buffer, offset, count);
        Leidos += leidos;
        return leidos;
    }
    public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
    {
        var leidos = await original.ReadAsync(buffer, cancellationToken);
        Leidos += leidos;
        return leidos;
    }
    public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) =>
        ReadAsync(buffer.AsMemory(offset, count), cancellationToken).AsTask();
    public override void Write(byte[] buffer, int offset, int count)
    {
        original.Write(buffer, offset, count);
        Escritos += count;
    }
    public override async ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
    {
        await original.WriteAsync(buffer, cancellationToken);
        Escritos += buffer.Length;
    }
    public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) =>
        WriteAsync(buffer.AsMemory(offset, count), cancellationToken).AsTask();
}
