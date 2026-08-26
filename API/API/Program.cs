using System.Text;
using System.Text;
using System.Text.Json.Serialization;
using API.Data;
using API.Entities.Exceptions;
using API.Repositories;
using API.Service;
using API.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);


builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Paste the JWT returned from /api/Auth/login or /api/Auth/signup. No 'Bearer ' prefix needed."
    });
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });
});
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IProjectRepository, ProjectRepository>();
builder.Services.AddScoped<IProjectService, ProjectService>();
builder.Services.AddScoped<ITicketRepository, TicketRepository>();
builder.Services.AddScoped<ITagRepository, TagRepository>();
builder.Services.AddScoped<ITicketService, TicketService>();
builder.Services.AddScoped<ICommentRepository, CommentRepository>();
builder.Services.AddScoped<ICommentService, CommentService>();
builder.Services.AddSingleton<ITokenService, TokenService>();

var jwtSection = builder.Configuration.GetSection("Jwt");
var jwtKey = jwtSection["Key"]
             ?? throw new InvalidOperationException("Jwt:Key is not configured in appsettings.");

builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSection["Issuer"],
            ValidAudience = jwtSection["Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
});

var app = builder.Build();

app.UseExceptionHandler(exceptionApp => exceptionApp.Run(async context =>
{
    var feature = context.Features.Get<IExceptionHandlerFeature>();
    var (statusCode, message) = feature?.Error switch
    {
        UserExistsException => (StatusCodes.Status409Conflict, feature.Error.Message),
        UserNotFoundException => (StatusCodes.Status404NotFound, feature.Error.Message),
        ProjectNotFoundException => (StatusCodes.Status404NotFound, feature.Error.Message),
        TicketNotFoundException => (StatusCodes.Status404NotFound, feature.Error.Message),
        CommentNotFoundException => (StatusCodes.Status404NotFound, feature.Error.Message),
        ForbiddenException => (StatusCodes.Status403Forbidden, feature.Error.Message),
        BadLoginException => (StatusCodes.Status401Unauthorized, feature.Error.Message),
        _ => (StatusCodes.Status500InternalServerError, "An unexpected error occurred.")
    };

    context.Response.StatusCode = statusCode;
    context.Response.ContentType = "application/json";
    await context.Response.WriteAsJsonAsync(new { error = message });
}));

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

public partial class Program { }