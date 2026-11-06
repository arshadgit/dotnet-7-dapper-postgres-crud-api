
using System.Text.Json.Serialization;
using WebApi.Helpers;
using WebApi.Repositories;
using WebApi.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to DI container
{
    var services = builder.Services;

    services.AddCors();

    services.AddControllers().AddJsonOptions(x =>
    {
        // Serialize enums as strings
        x.JsonSerializerOptions.Converters.Add(
            new JsonStringEnumConverter()
        );

        // Ignore null properties
        x.JsonSerializerOptions.DefaultIgnoreCondition =
            JsonIgnoreCondition.WhenWritingNull;
    });

    services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());

    // Configure PostgreSQL settings
    services.Configure<DbSettings>(
        builder.Configuration.GetSection("DbSettings")
    );

    // Dependency injection
    services.AddSingleton<DataContext>();
    services.AddScoped<IUserRepository, UserRepository>();
    services.AddScoped<IUserService, UserService>();
}

var app = builder.Build();

// Initialize PostgreSQL database and tables
{
    using var scope = app.Services.CreateScope();

    var context = scope.ServiceProvider
        .GetRequiredService<DataContext>();

    await context.Init();
}

// Configure HTTP request pipeline
{
    app.UseCors(x => x
        .AllowAnyOrigin()
        .AllowAnyMethod()
        .AllowAnyHeader());

    app.UseMiddleware<ErrorHandlerMiddleware>();

    app.MapControllers();
}

// Listen on port 4000
app.Run("http://0.0.0.0:4000");
