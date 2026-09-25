//var builder = WebApplication.CreateBuilder(args);

//// Add services to the container.

//builder.Services.AddControllers();
//// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
//builder.Services.AddEndpointsApiExplorer();
//builder.Services.AddSwaggerGen();

//var app = builder.Build();

//// Configure the HTTP request pipeline.
//if (app.Environment.IsDevelopment())
//{
//    app.UseSwagger();
//    app.UseSwaggerUI();
//}

//app.UseHttpsRedirection();

//app.UseAuthorization();

//app.MapControllers();

//app.Run();



using VRF_API.Model.RequestModel;
using VRF_API.ServiceRegistration;
using VRF_API.Utilities;
using Serilog;
using System.Data.Odbc;
using VRF_API.ServiceRegistration;
using Microsoft.Data.SqlClient;
using System.Net;
using VRF_API.Services;


//var builder = WebApplication.CreateBuilder(args);

//var logDirectory = builder.Configuration.GetValue<string>("Loggings:Log");

//Directory.CreateDirectory(logDirectory); // Ensure the folder exists

//Log.Logger = new LoggerConfiguration()
//    .MinimumLevel.Information()
//    .WriteTo.Console()
//    .WriteTo.File(
//        path: Path.Combine(logDirectory, "app-log-.txt"),
//        rollingInterval: RollingInterval.Day,
//        retainedFileCountLimit: null
//    )
//    .CreateLogger();
//builder.Services.AddHttpClient();
//builder.Services.Configure<SapSettings>(
//    builder.Configuration.GetSection("SapSettings"));
//builder.Services.Configure<LoginSettings>(
//    builder.Configuration.GetSection("LoginSettings"));

//Directory.CreateDirectory(logDirectory); // Ensure the folder exists

//Log.Logger = new LoggerConfiguration()
//    .MinimumLevel.Information()
//    .WriteTo.Console()
//    .WriteTo.File(
//        path: Path.Combine(logDirectory, "app-log-.txt"),
//        rollingInterval: RollingInterval.Day,
//        retainedFileCountLimit: null
//    )
//    .CreateLogger();
//builder.Services.AddApplicationServices(builder.Configuration);

//builder.Services.AddCors(options =>
//{
//    options.AddPolicy("AllowReactApp", policy =>
//    {
//        policy.WithOrigins("http://localhost:3000")
//              .AllowAnyMethod()
//              .AllowAnyHeader()
//              .AllowCredentials();
//    });
//});

//// Add services to the container.
//builder.Services.AddControllers();
//builder.Services.AddHttpContextAccessor();
//// Swagger
//builder.Services.AddEndpointsApiExplorer();
//builder.Services.AddSwaggerGen();
//builder.Services.AddDistributedMemoryCache();

//builder.Services.AddSession(options =>
//{
//    //options.IdleTimeout = TimeSpan.FromMinutes(LogoffTime);
//    options.Cookie.HttpOnly = true;
//    options.Cookie.IsEssential = true;

//    options.Cookie.SameSite = SameSiteMode.None;
//    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;

//});
////builder.Services.AddScoped<SapConnection>();

//// ⭐ Register OdbcConnection dependency
//builder.Services.AddScoped<OdbcConnection>(sp =>
//{
//    var config = sp.GetRequiredService<IConfiguration>();
//    string connString = config.GetConnectionString("HanaOdbc");

//    return new OdbcConnection(connString);
//});



//var app = builder.Build();

//app.UseCors("AllowReactApp");

//// Configure HTTP request pipeline
//if (app.Environment.IsDevelopment() || app.Environment.IsProduction())
//{
//    app.UseDeveloperExceptionPage();
//    app.UseSwagger();
//    app.UseSwaggerUI();

//}
//app.UseCors("AllowReactApp");
//app.UseSession();
//app.UseHttpsRedirection();

//app.UseAuthorization();

//app.MapControllers();

//app.Run();



var builder = WebApplication.CreateBuilder(args);
var logDirectory = builder.Configuration.GetValue<string>("Logging:LogDirectory")
                   ?? Path.Combine(AppContext.BaseDirectory, "Logs");

builder.Services.Configure<SapSettings>(
    builder.Configuration.GetSection("SapSettings"));
builder.Services.Configure<LoginSettings>(
    builder.Configuration.GetSection("LoginSettings"));

Directory.CreateDirectory(logDirectory); // Ensure the folder exists

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Console()
    .WriteTo.File(
        path: Path.Combine(logDirectory, "app-log-.txt"),
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: null
    )
    .CreateLogger();
builder.Services.AddApplicationServices(builder.Configuration);

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddHttpContextAccessor();
// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddScoped<OdbcConnection>();

// ⭐ Register OdbcConnection dependency
builder.Services.AddScoped<OdbcConnection>(sp =>
{
    var config = sp.GetRequiredService<IConfiguration>();
    string connString = config.GetConnectionString("HanaOdbc");

    return new OdbcConnection(connString);
});



var app = builder.Build();

app.UseCors("AllowAll");

// Configure HTTP request pipeline
if (app.Environment.IsDevelopment() || app.Environment.IsProduction())
{
    app.UseDeveloperExceptionPage();
    app.UseSwagger();
    app.UseSwaggerUI();

}
app.UseCors(x => x
    .AllowAnyOrigin()
    .AllowAnyMethod()
    .AllowAnyHeader()
);
app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
