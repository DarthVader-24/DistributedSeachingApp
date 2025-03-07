namespace WorkerNode.Options;

public record WorkerNodeOptions
{
    public required string ConnectionString { get; set; }
    public required string ServiceRegistryPath { get; set; }
    public required int SessionTimeoutMs { get; set; } = 20000;
}