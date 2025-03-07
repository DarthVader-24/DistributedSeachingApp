namespace MyDistributedSearchApp.Constants;

public static class CommonConstants
{
    public const string LockBasePath = "/locks";
    public const string LockPathPrefix = "/lock_";
    public const string NodePathPrefix = "/node_";
    
    public const int ConnectionDelayTimeMs = 3000;
    public const int ConnectionMaxRetries = 5;
    
    public const string TestLockName = "test-lock"; 
}