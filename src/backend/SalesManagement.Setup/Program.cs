using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SalesManagement.Core.Models;
using SalesManagement.Core.Repositories;
using SalesManagement.Postgres;
using SalesManagement.Postgres.Configuration;
using SalesManagement.Postgres.Repositories;
using SalesManagement.Setup;

var generateResetLogin = GetOption(args, "--generate-password-reset-login");
if (generateResetLogin is not null)
{
    var resetCredentialsPath = GetOption(args, "--credentials-path")
        ?? throw new ArgumentException("--credentials-path is required when generating password reset artifacts.");
    var hashPath = GetOption(args, "--hash-path")
        ?? throw new ArgumentException("--hash-path is required when generating password reset artifacts.");
    var artifact = new PasswordResetArtifactGenerator(new PasswordHasher<User>()).Generate(generateResetLogin);
    var writer = new InitialCredentialWriter();
    writer.Write(resetCredentialsPath, [new InitialCredential(artifact.LoginId, artifact.TemporaryPassword)]);
    writer.WriteHash(hashPath, artifact.PasswordHash);
    Console.WriteLine($"Password reset artifacts written for: {artifact.LoginId}");
    return;
}

var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__SalesManagement");
var databaseUrl = Environment.GetEnvironmentVariable("DATABASE_URL");
if (string.IsNullOrWhiteSpace(connectionString) && !string.IsNullOrWhiteSpace(databaseUrl))
    connectionString = RenderPostgresConnectionString.Normalize(databaseUrl);
if (string.IsNullOrWhiteSpace(connectionString))
    throw new InvalidOperationException("ConnectionStrings__SalesManagement or DATABASE_URL is required.");

var credentialsPath = GetOption(args, "--credentials-path")
    ?? @"C:\SwitchKorea\Secrets\initial-credentials.txt";
var resetLogin = GetOption(args, "--reset-password-hash-login");
var resetHash = GetOption(args, "--password-hash");
if ((resetLogin is null) != (resetHash is null))
    throw new ArgumentException("--reset-password-hash-login and --password-hash must be provided together.");
var migrateOnly = args.Contains("--migrate-only", StringComparer.OrdinalIgnoreCase);
var seedOnly = args.Contains("--seed-only", StringComparer.OrdinalIgnoreCase);
if (migrateOnly && seedOnly)
    throw new ArgumentException("--migrate-only and --seed-only cannot be combined.");
var migrate = migrateOnly || args.Contains("--migrate", StringComparer.OrdinalIgnoreCase);

var services = new ServiceCollection();
services.AddDbContext<SalesDbContext>(options => options.UseNpgsql(connectionString));
services.AddScoped<IUserRepository, UserRepository>();
services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();
services.AddScoped<InitialAccountSeeder>();
services.AddScoped<PasswordResetService>();
services.AddSingleton(_ => new InitialCredentialWriter());
await using var provider = services.BuildServiceProvider();
await using var scope = provider.CreateAsyncScope();
var db = scope.ServiceProvider.GetRequiredService<SalesDbContext>();
if (migrate)
{
    await db.Database.MigrateAsync();
    await new NetworkCustomerBackfill(db).ApplyAsync();
}

if (migrateOnly)
{
    Console.WriteLine("Database migrations completed.");
    return;
}

if (resetLogin is not null && resetHash is not null)
{
    await scope.ServiceProvider.GetRequiredService<PasswordResetService>().ResetAsync(resetLogin, resetHash);
    Console.WriteLine($"Password reset completed for: {resetLogin}");
    return;
}

var accounts = InitialAccountCatalog.All;

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
