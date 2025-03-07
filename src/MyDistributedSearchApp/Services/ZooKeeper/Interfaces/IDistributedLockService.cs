namespace MyDistributedSearchApp.Services.ZooKeeper.Interfaces;

public interface IDistributedLockService
{
    Task<bool> AcquireLockAsync(string lockName, CancellationToken cancellationToken);
    Task ReleaseLockAsync();
    Task OnLockNodeDeleted(string deletedPath, string lockPath, CancellationToken cancellationToken);
}