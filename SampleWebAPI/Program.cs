using Microsoft.EntityFrameworkCore;
using SampleWebAPI.Data;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Build connection string from environment variables (if provided)
var dbServer = builder.Configuration["DB_SERVER"];
var dbName = builder.Configuration["DB_NAME"];
var dbUser = builder.Configuration["DB_USER"];
var dbPassword = builder.Configuration["DB_PASSWORD"];

string connectionString;

if (!string.IsNullOrEmpty(dbServer) && !string.IsNullOrEmpty(dbPassword))
{
    // Use environment variables
    connectionString = $"Server={dbServer};Database={dbName};User Id={dbUser};Password={dbPassword};TrustServerCertificate=True;MultipleActiveResultSets=true";
}
else
{
    // Use connection string from appsettings.json
    connectionString = builder.Configuration.GetConnectionString("DefaultConnection")!;
}

// Configure Database Context
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

// Add CORS if needed
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll",
        policy => policy.AllowAnyOrigin()
                        .AllowAnyMethod()
                        .AllowAnyHeader());
});

var app = builder.Build();

// ===== ADD THIS SECTION: Auto-apply migrations on startup =====
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<ApplicationDbContext>();

        Console.WriteLine("Applying database migrations...");
        context.Database.Migrate();
        Console.WriteLine("Database migration completed successfully!");
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while migrating the database.");

        // Wait a bit and retry (useful when SQL Server is still starting up)
        Console.WriteLine("Waiting 10 seconds before retrying migration...");
        Thread.Sleep(10000);

        try
        {
            var context = services.GetRequiredService<ApplicationDbContext>();
            context.Database.Migrate();
            Console.WriteLine("Database migration completed successfully on retry!");
        }
        catch (Exception retryEx)
        {
            logger.LogError(retryEx, "Migration failed on retry. Application will continue but database may not be ready.");
        }
    }
}
// ===== END OF MIGRATION SECTION =====

// Configure the HTTP request pipeline
app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();
app.UseCors("AllowAll");
app.UseAuthorization();
app.MapControllers();

app.Run();