using Microsoft.EntityFrameworkCore;
using SampleWebAPI.Data;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// Add HttpClient for Kubernetes API calls
builder.Services.AddHttpClient();

// Enhanced Swagger Configuration
builder.Services.AddSwaggerGen(c =>
{
    var podName = Environment.GetEnvironmentVariable("POD_NAME") ?? "Not in Kubernetes";
    var podNamespace = Environment.GetEnvironmentVariable("POD_NAMESPACE") ?? "default";
    var nodeName = Environment.GetEnvironmentVariable("NODE_NAME") ?? "N/A";
    var podIP = Environment.GetEnvironmentVariable("POD_IP") ?? "N/A";
    var dbServer = builder.Configuration["DB_SERVER"] ?? "LocalDB";
    var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development";

    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "🎓 Student Management System API",
        Version = "v1.0.0",
        Description = $@"
## 🚀 Current System Information

### 📦 Pod Information
- **Pod Name:** `{podName}`
- **Namespace:** `{podNamespace}`
- **Pod IP:** `{podIP}`
- **Node:** `{nodeName}`

### 🗄️ Database Connection
- **DB Server:** `{dbServer}`
- **DB Port:** `1433`
- **Database:** `StudentManagementDB`
- **Environment:** `{environment}`

### 🌐 API Service
- **Service Name:** `webapi-service`
- **Service Port:** `5052` (External)
- **Container Port:** `8080` (Internal)
- **Protocol:** `HTTP`

### 🎯 Cluster Overview
- **API Replicas:** 2 Pods
- **SQL Server:** 1 Pod
- **Load Balancer:** Enabled
- **Service Type:** LoadBalancer

---

## 📚 Available Endpoints

### 👥 Students API
Manage student records with full CRUD operations
- `GET /api/students` - Get all students
- `GET /api/students/{{id}}` - Get student by ID
- `POST /api/students` - Create new student
- `PUT /api/students/{{id}}` - Update student
- `DELETE /api/students/{{id}}` - Delete student

### 📚 Courses API
Manage course information
- `GET /api/courses` - Get all courses
- `GET /api/courses/{{id}}` - Get course by ID
- `POST /api/courses` - Create new course
- `PUT /api/courses/{{id}}` - Update course
- `DELETE /api/courses/{{id}}` - Delete course

### 📝 Enrollments API
Track student course enrollments
- `GET /api/enrollments` - Get all enrollments
- `GET /api/enrollments/student/{{id}}` - Get student's enrollments
- `POST /api/enrollments` - Create enrollment
- `PUT /api/enrollments/{{id}}` - Update enrollment
- `DELETE /api/enrollments/{{id}}` - Delete enrollment

### 🏢 Departments API
Manage department data
- `GET /api/departments` - Get all departments
- `GET /api/departments/{{id}}` - Get department by ID
- `POST /api/departments` - Create department
- `PUT /api/departments/{{id}}` - Update department
- `DELETE /api/departments/{{id}}` - Delete department

---

## 🔍 System Monitoring & Cluster Information

### Real-Time Cluster Dashboard: `/api/systeminfo`

This endpoint provides comprehensive cluster and system information:

#### 📊 What You'll Get:
1. **Pod Details**
   - View which specific pod served your request
   - Pod name, IP address, and node location
   - Container name and runtime information

2. **Service Mapping**
   - All services in the namespace
   - Service types (ClusterIP, LoadBalancer, NodePort)
   - Service IPs and ports

3. **Node Distribution**
   - All nodes in the cluster
   - Node status (Ready/NotReady)
   - Kubernetes version and OS information

4. **Database Metrics**
   - Connection status (Connected/Disconnected)
   - SQL Server version
   - Real-time record counts:
     - Students count
     - Courses count
     - Enrollments count
     - Departments count

5. **Cluster Health**
   - Total pods in namespace
   - Running/Pending/Failed pod counts
   - Deployment replica status
   - All pods with their status and locations

---

## ⚡ Performance & Architecture

**High Availability:** This API is deployed with **2 replicas** for fault tolerance and load distribution.

**Load Balancing:** Each HTTP request may be served by a different pod. The LoadBalancer service automatically distributes traffic across healthy pods.

**Auto-Healing:** Kubernetes automatically restarts failed pods and reschedules them on healthy nodes.

**Resource Limits:**
- Memory: 128Mi (request) / 512Mi (limit)
- CPU: 100m (request) / 500m (limit)

---

## 💡 Quick Start Guide

1. **Test Connection:** Call `GET /api/systeminfo` to verify the API is running and connected to the database
2. **View Data:** Use `GET /api/students` or `GET /api/courses` to see seeded data
3. **Create Records:** Use POST endpoints to add new students, courses, or enrollments
4. **Monitor Cluster:** Refresh `/api/systeminfo` to see which pod serves each request

---

## 🔧 Troubleshooting

If you see database connection errors:
1. Check SQL Server pod status: Look at `/api/systeminfo` clusterInfo
2. Verify service name: Should be `sqlserver-service`
3. Check if database exists: `StudentManagementDB` should be created automatically
4. Review pod logs: The startup logs show migration status

**Current Pod Serving This Page:** `{podName}` on node `{nodeName}`
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