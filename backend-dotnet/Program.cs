using backend_dotnet.Data;
using backend_dotnet.Endpoints;
using backend_dotnet.Models;
using backend_dotnet.Models.Domain;
using backend_dotnet.Models.DTOs.Doctor;
using backend_dotnet.Services;
using backend_dotnet.Services.Appointment;
using backend_dotnet.Services.Doctor;
using backend_dotnet.Services.ImageUpload;
using backend_dotnet.Services.Jwt;
using backend_dotnet.Services.Password;
using backend_dotnet.Services.Service;
using backend_dotnet.Services.ServiceAppointment;
using backend_dotnetWebMinimalExample.Endpoints;
using backend_dotnetWebMinimalExample.Endpoints.Doctor;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using Scalar.AspNetCore;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi

// --------------------- CORS ----------------------------------------------------------------------------
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

//---------------------Scaller UI-----------------------------------------------------------------------
builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer((document, context, cancellationToken) =>
    {
        var components = document.Components ??= new OpenApiComponents();
        var schemes = components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();
        schemes["Bearer"] = new OpenApiSecurityScheme
        {
            Name = "Authorization",
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            In = ParameterLocation.Header,
            Description = "Enter your JWT token in the format: Bearer {your token}"
        };

        var security = document.Security ??= new List<OpenApiSecurityRequirement>();
        security.Add(new OpenApiSecurityRequirement
        {
            [new OpenApiSecuritySchemeReference("Bearer", document)] = new List<string>()
        });

        return Task.CompletedTask;
    });
});

//---------------------Db Connection and Jwt Token -----------------------------------------------------------------------


builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddProblemDetails();

var secretKey = builder.Configuration["ApiSettings:Secret"]
                ?? throw new InvalidOperationException("ApiSettings:Secret is not configured.");
var issuer = builder.Configuration["ApiSettings:Issuer"];
var audience = builder.Configuration["ApiSettings:Audience"];

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false;
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(secretKey)),
        ValidateIssuer = true,
        ValidIssuer = issuer,
        ValidateAudience = true,
        ValidAudience = audience,
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero
    };
});
builder.Services.AddAuthorization();

builder.Services.AddScoped<IPasswordHasher, PasswordHasher>();
builder.Services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();

//----------------------------- Cloudinary Image Upload Service -----------------------------------------------------------------------
builder.Services.Configure<CloudinarySettings>(builder.Configuration.GetSection("CloudinarySettings"));
builder.Services.AddScoped<IImageUploadService, ImageUploadService>();

//----------------------------- Module Services -----------------------------------------------------------------------
builder.Services.AddScoped<IDoctorService, DoctorService>();
builder.Services.AddScoped<IAppointmentService, AppointmentService>();
builder.Services.AddScoped<IServiceModuleService, ServiceModuleService>();
builder.Services.AddScoped<IServiceAppointmentService, ServiceAppointmentService>();

builder.Services.AddControllers();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();// scalar add in program.cs
}

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseCors("AllowAll");
//-----------------------------Configuration -----------------------------------------------------------------------


app.UseAuthentication();
app.UseAuthorization();

//-----------------------------Endpoints-----------------------------------------------------------------------
app.MapAuthEndpoints();
app.MapDoctorEndpoints();
app.MapAppointmentEndpoints();
app.MapServiceEndpoints();
app.MapServiceAppointmentEndpoints();

app.Run();