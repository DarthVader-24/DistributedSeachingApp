using Microsoft.Extensions.Options;
using org.apache.zookeeper;
using MyDistributedSearchApp.Options;

namespace MyDistributedSearchApp.Services.ZooKeeper;

public class ZooKeeperClient(IOptions<ZooKeeperOptions> options, ILogger<ZooKeeperClient> logger)
    : Watcher, IDisposable
{
    private readonly ZooKeeperOptions _options = options.Value;

    public org.apache.zookeeper.ZooKeeper? Zk { get; private set; }

    public void Connect()
    {
        if (Zk is not null)
            return;

        Zk = new org.apache.zookeeper.ZooKeeper(
            _options.ConnectionString,
            _options.SessionTimeoutMs,
            this  
        );

        logger.LogInformation("Connecting to ZooKeeper: {ConnectionString}", _options.ConnectionString);
    }
    
    public override Task process(WatchedEvent @event)
    {
        logger.LogInformation("ZooKeeper event: State={State}, Type={Type}, Path={Path}",
            @event.getState(), @event.get_Type(), @event.getPath());
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        if (Zk is not null)
        {
            Zk.closeAsync();
            logger.LogInformation("ZooKeeper connection closed.");
        }
        GC.SuppressFinalize(this);
    }
}