namespace MyDistributedSearchApp.Options;

public record ZooKeeperOptions
{
    public required string ConnectionString { get; init; }
    public required string LeaderElectionPath { get; init; }
    public required string ServiceRegistryPath { get; init; }
    public required int SessionTimeoutMs { get; init; } = 20000;
}