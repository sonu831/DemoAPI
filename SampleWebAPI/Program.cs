using Microsoft.EntityFrameworkCore;
using SampleWebAPI.Data;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// Enhanced Swagger Configuration
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "🎓 Student Management System API",
        Version = "v1.0.0",
        Description = @"
<div style='background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); color: white; padding: 20px; border-radius: 10px; margin: 20px 0;'>
    <h2>🚀 System Information</h2>
    <div style='display: grid; grid-template-columns: repeat(2, 1fr); gap: 15px; margin-top: 15px;'>
        <div>
            <strong>🏷️ Environment:</strong> " + (Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development") + @"<br/>
            <strong>🗄️ Database:</strong> " + (builder.Configuration["DB_SERVER"] ?? "LocalDB") + @"<br/>
            <strong>📦 Pod Name:</strong> " + (Environment.GetEnvironmentVariable("POD_NAME") ?? "Not in Kubernetes") + @"
        </div>
        <div>
            <strong>🌐 Namespace:</strong> " + (Environment.GetEnvironmentVariable("POD_NAMESPACE") ?? "default") + @"<br/>
            <strong>🖥️ Node:</strong> " + (Environment.GetEnvironmentVariable("NODE_NAME") ?? "N/A") + @"<br/>
            <strong>📍 Pod IP:</strong> " + (Environment.GetEnvironmentVariable("POD_IP") ?? "N/A") + @"
        </div>
    </div>
</div>

<h3>📚 Available Endpoints</h3>
<ul>
    <li><strong>Students API:</strong> Manage student records</li>
    <li><strong>Courses API:</strong> Manage course information</li>
    <li><strong>Enrollments API:</strong> Track student enrollments</li>
    <li><strong>Departments API:</strong> Manage department data</li>
    <li><strong>System Info:</strong> Real-time system and database statistics at <code>/api/systeminfo</code></li>
</ul>

<div style='background: #f0f4ff; padding: 15px; border-left: 4px solid #667eea; margin: 20px 0;'>
    <strong>💡 Quick Start:</strong> Use the <code>GET /api/systeminfo</code> endpoint to view complete real-time system information including Pod details, database connection status, and record counts.
</div>
",
        Contact = new OpenApiContact
        {
            Name = "API Development Team",
            Email = "support@studentmanagement.com",
            Url = new Uri("https://github.com/yourusername/student-api")
        },
        License = new OpenApiLicense
        {
            Name = "MIT License",
            Url = new Uri("https://opensource.org/licenses/MIT")
        }
    });

    // Add XML comments if available
    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        c.IncludeXmlComments(xmlPath);
    }

    // Add security definition (if you add authentication later)
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Example: \"Bearer {token}\"",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });
});

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

// ===== Auto-apply migrations on startup =====
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<ApplicationDbContext>();

        Console.WriteLine("╔════════════════════════════════════════╗");
        Console.WriteLine("║   SYSTEM STARTUP INFORMATION          ║");
        Console.WriteLine("╚════════════════════════════════════════╝");
        Console.WriteLine($"🏷️  Environment: {app.Environment.EnvironmentName}");
        Console.WriteLine($"📦 Pod Name: {Environment.GetEnvironmentVariable("POD_NAME") ?? "Not in Kubernetes"}");
        Console.WriteLine($"🌐 Namespace: {Environment.GetEnvironmentVariable("POD_NAMESPACE") ?? "default"}");
        Console.WriteLine($"🖥️  Node: {Environment.GetEnvironmentVariable("NODE_NAME") ?? "N/A"}");
        Console.WriteLine($"📍 Pod IP: {Environment.GetEnvironmentVariable("POD_IP") ?? "N/A"}");
        Console.WriteLine($"🗄️  Database: {dbServer ?? "LocalDB"}");
        Console.WriteLine("════════════════════════════════════════");

        Console.WriteLine("⏳ Applying database migrations...");
        context.Database.Migrate();
        Console.WriteLine("✅ Database migration completed successfully!");

        var studentCount = context.Students.Count();
        var courseCount = context.Courses.Count();
        var enrollmentCount = context.Enrollments.Count();
        var departmentCount = context.Departments.Count();

        Console.WriteLine("════════════════════════════════════════");
        Console.WriteLine("📊 DATABASE STATISTICS:");
        Console.WriteLine($"   👥 Students: {studentCount}");
        Console.WriteLine($"   📚 Courses: {courseCount}");
        Console.WriteLine($"   📝 Enrollments: {enrollmentCount}");
        Console.WriteLine($"   🏢 Departments: {departmentCount}");
        Console.WriteLine("════════════════════════════════════════");
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "❌ An error occurred while migrating the database.");

        Console.WriteLine("⏳ Waiting 10 seconds before retrying migration...");
        Thread.Sleep(10000);

        try
        {
            var context = services.GetRequiredService<ApplicationDbContext>();
            context.Database.Migrate();
            Console.WriteLine("✅ Database migration completed successfully on retry!");
        }
        catch (Exception retryEx)
        {
            logger.LogError(retryEx, "❌ Migration failed on retry. Application will continue but database may not be ready.");
        }
    }
}

// Configure the HTTP request pipeline
app.UseSwagger();

// Customized Swagger UI
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "Student Management API v1");
    options.RoutePrefix = "swagger";
    options.DocumentTitle = "Student Management System - API Documentation";

    // Inject custom CSS
    options.InjectStylesheet("/swagger-custom.css");

    // UI Configuration
    options.DefaultModelsExpandDepth(-1); // Hide schemas section by default
    options.DefaultModelRendering(Swashbuckle.AspNetCore.SwaggerUI.ModelRendering.Model);
    options.DisplayRequestDuration();
    options.EnableDeepLinking();
    options.EnableFilter();
    options.EnableTryItOutByDefault();

    // Custom CSS and JavaScript injection
    options.HeadContent = @"
        <style>
            /* Custom color scheme */
            .swagger-ui .topbar { 
                background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
                border-bottom: 3px solid #5568d3;
            }
            .swagger-ui .topbar .download-url-wrapper { display: none; }
            
            /* Custom header banner */
            .swagger-ui .info { 
                margin: 30px 0;
            }
            .swagger-ui .info .title {
                font-size: 2.5em;
                color: #667eea;
                text-shadow: 2px 2px 4px rgba(0,0,0,0.1);
            }
            
            /* Endpoint styling */
            .swagger-ui .opblock.opblock-get { 
                background: rgba(97, 175, 254, 0.1);
                border-color: #61affe;
            }
            .swagger-ui .opblock.opblock-post { 
                background: rgba(73, 204, 144, 0.1);
                border-color: #49cc90;
            }
            .swagger-ui .opblock.opblock-put { 
                background: rgba(252, 161, 48, 0.1);
                border-color: #fca130;
            }
            .swagger-ui .opblock.opblock-delete { 
                background: rgba(249, 62, 62, 0.1);
                border-color: #f93e3e;
            }
            
            /* Try it out button */
            .swagger-ui .btn.try-out__btn {
                background: #667eea;
                color: white;
                border: none;
                transition: all 0.3s;
            }
            .swagger-ui .btn.try-out__btn:hover {
                background: #5568d3;
                transform: translateY(-2px);
                box-shadow: 0 4px 8px rgba(102, 126, 234, 0.3);
            }
            
            /* Execute button */
            .swagger-ui .btn.execute {
                background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
                color: white;
                border: none;
                font-weight: bold;
                transition: all 0.3s;
            }
            .swagger-ui .btn.execute:hover {
                transform: scale(1.05);
                box-shadow: 0 5px 15px rgba(102, 126, 234, 0.4);
            }
            
            /* Response section */
            .swagger-ui .responses-inner {
                border-radius: 8px;
                overflow: hidden;
            }
            
            /* Custom scrollbar */
            ::-webkit-scrollbar {
                width: 12px;
            }
            ::-webkit-scrollbar-track {
                background: #f1f1f1;
            }
            ::-webkit-scrollbar-thumb {
                background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
                border-radius: 6px;
            }
            ::-webkit-scrollbar-thumb:hover {
                background: #5568d3;
            }
            
            /* Add some animation */
            .swagger-ui .opblock {
                transition: all 0.3s ease;
            }
            .swagger-ui .opblock:hover {
                transform: translateX(5px);
                box-shadow: -5px 5px 15px rgba(0,0,0,0.1);
            }
            
            /* System info banner enhancement */
            .swagger-ui .information-container {
                background: linear-gradient(135deg, #f5f7fa 0%, #c3cfe2 100%);
                padding: 20px;
                border-radius: 10px;
                margin: 20px 0;
            }
        </style>
        <link rel='icon' type='image/png' href='data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mNk+M9QDwADhgGAWjR9awAAAABJRU5ErkJggg==' />
    ";
});

// Serve custom CSS file (create this file in wwwroot folder)
app.UseStaticFiles();

// Redirect root to Swagger
app.MapGet("/", () => Results.Redirect("/swagger")).ExcludeFromDescription();

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseCors("AllowAll");
app.UseAuthorization();
app.MapControllers();

Console.WriteLine("╔════════════════════════════════════════╗");
Console.WriteLine("║     🚀 APPLICATION STARTED             ║");
Console.WriteLine("╚════════════════════════════════════════╝");
Console.WriteLine($"📖 Swagger UI: http://localhost:{app.Configuration["ASPNETCORE_URLS"]?.Split(':').Last() ?? "8080"}/swagger");
Console.WriteLine($"🔍 System Info: http://localhost:{app.Configuration["ASPNETCORE_URLS"]?.Split(':').Last() ?? "8080"}/api/systeminfo");
Console.WriteLine("════════════════════════════════════════");

app.Run();