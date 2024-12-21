using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using System.Text;
using TodoMinimalApi.Data;
using TodoMinimalApi.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;

var builder = WebApplication.CreateBuilder(args);

// DB

builder.Services.AddDbContext<TodoDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Postgres")));

// JWT

builder.Services.AddAuthentication("Bearer")
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = "TodoMinimalApi",
            ValidAudience = "TodoMinimalApi",
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("@TodoMinimalApi#1d&mv&lle1d&mn0lle"))
        };
    });

// Adicione o serviço de autorização
builder.Services.AddAuthorization();

// Swagger

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add services to the container.

var app = builder.Build();

// Configure the HTTP request pipeline.

app.UseHttpsRedirection();

// Auth

app.UseAuthentication();
app.UseAuthorization();

// Check Swagger on dev

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapPost("/api/v1/todos", [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)] async
    (Todo todo, TodoDbContext dbContext, ClaimsPrincipal user) =>
{
    var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    if (userId == null) return Results.Unauthorized();

    todo.UserId = Guid.Parse(userId);
    dbContext.Todos.Add(todo);
    await dbContext.SaveChangesAsync();
    return Results.Created($"/api/todos/{todo.Id}", todo);
});

app.MapGet("/api/v1/todos", [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)] async
    (TodoDbContext dbContext, ClaimsPrincipal user) =>
{
    var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    if (userId == null) return Results.Unauthorized();

    var todos = await dbContext.Todos.Where(t => t.UserId == Guid.Parse(userId)).ToListAsync();
    return Results.Ok(todos);
});

app.Run();

