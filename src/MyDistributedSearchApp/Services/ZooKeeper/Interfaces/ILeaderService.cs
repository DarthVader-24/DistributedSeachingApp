namespace MyDistributedSearchApp.Services.ZooKeeper.Interfaces;

public interface ILeaderService
{
    Task StartElection();
    Task DetermineLeader();
    Task WatchLeaderElection();
}