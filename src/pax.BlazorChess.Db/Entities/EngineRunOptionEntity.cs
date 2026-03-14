namespace pax.BlazorChess.Db.Entities;

public sealed class EngineRunOptionEntity
{
    public Guid Id { get; set; }
    public string BinaryPath { get; set; } = string.Empty;
    public string? Name { get; set; }
    public int Threads { get; set; }
    public int Pvs { get; set; }
    public int HashMb { get; set; }
    public int PoolSize { get; set; }
    public int IdelTimeoutMs { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}
