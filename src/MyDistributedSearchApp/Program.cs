using MyDistributedSearchApp.Options;
using MyDistributedSearchApp.Services.ZooKeeper;
using MyDistributedSearchApp.Services.ZooKeeper.Interfaces;
using MyDistributedSearchApp.TestServices.ZooKeeper;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

builder.Services.Configure<ZooKeeperOptions>(builder.Configuration.GetSection("ZooKeeper"));

builder.Services.AddSingleton<ZooKeeperClient>();
builder.Services.AddSingleton<ILeaderService, LeaderService>();
builder.Services.AddSingleton<ServiceRegistry>();
builder.Services.AddSingleton<IDistributedLockService, DistributedLockService>();

builder.Services.AddHostedService<LockTestService>();

var app = builder.Build();

var leaderService = app.Services.GetRequiredService<ILeaderService>();
await leaderService.StartElection();

var serviceRegistry = app.Services.GetRequiredService<ServiceRegistry>();
await serviceRegistry.InitRegistryAsync();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}
else
{
    app.UseDeveloperExceptionPage();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();