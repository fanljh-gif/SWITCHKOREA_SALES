using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SalesManagement.Core.Models;
using SalesManagement.Core.Repositories;
using SalesManagement.Postgres;
using SalesManagement.Postgres.Repositories;
using SalesManagement.Setup;

var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__SalesManagement");
if (string.IsNullOrWhiteSpace(connectionString))
    throw new InvalidOperationException("ConnectionStrings__SalesManagement is required.");

var credentialsPath = GetOption(args, "--credentials-path")
    ?? @"C:\SwitchKorea\Secrets\initial-credentials.txt";
var migrate = args.Contains("--migrate", StringComparer.OrdinalIgnoreCase);

var services = new ServiceCollection();
services.AddDbContext<SalesDbContext>(options => options.UseNpgsql(connectionString));
services.AddScoped<IUserRepository, UserRepository>();
services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();
services.AddScoped<InitialAccountSeeder>();
services.AddSingleton<InitialCredentialWriter>();
await using var provider = services.BuildServiceProvider();
await using var scope = provider.CreateAsyncScope();
var db = scope.ServiceProvider.GetRequiredService<SalesDbContext>();
if (migrate)
    await db.Database.MigrateAsync();

InitialAccountDefinition[] accounts =
[
    new("jhlee", "이재학", "jhlee@switchkorea.com", Role.Manager),
    new("twpark", "박태원", "ptw0517@switchkorea.com", Role.Member),
    new("mcchoi", "최명철", "cmc@switchkorea.com", Role.Member),
    new("sylee", "이소연", "sylee@switchkorea.com", Role.Member),
    new("cslee", "이창선", "cslee@switchkorea.com", Role.Member),
];

var result = await scope.ServiceProvider.GetRequiredService<InitialAccountSeeder>().SeedAsync(accounts);
if (result.Created.Count > 0)
    scope.ServiceProvider.GetRequiredService<InitialCredentialWriter>().Write(credentialsPath, result.Created);

Console.WriteLine($"Initial accounts: created={result.Created.Count}, existing={result.ExistingCount}");
if (result.Created.Count > 0)
    Console.WriteLine($"Credentials written to: {credentialsPath}");

static string? GetOption(string[] arguments, string name)
{
    var index = Array.FindIndex(arguments, x => string.Equals(x, name, StringComparison.OrdinalIgnoreCase));
    if (index < 0)
        return null;
    if (index + 1 >= arguments.Length || arguments[index + 1].StartsWith("--", StringComparison.Ordinal))
        throw new ArgumentException($"{name} requires a value.");
    return arguments[index + 1];
}
