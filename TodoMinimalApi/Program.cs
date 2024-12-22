using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using TodoMinimalApi.Data;
using TodoMinimalApi.DTOs;
using TodoMinimalApi.Mappings;
using TodoMinimalApi.Models;
using TodoMinimalApi.Validators;

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

builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Description = "Autorização JWT usando o esquema Bearer (Exemplo: 'Bearer 12345abcdef')",
        Name = "Authorization",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    options.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new string[] { }
        }
    });
});

// Validation

builder.Services.AddControllers().AddFluentValidation(fv =>
    fv.RegisterValidatorsFromAssemblyContaining<TodoValidator>());


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

app.MapPost("/api/v1/todos", async (TodoDto todoDto, TodoDbContext dbContext, ClaimsPrincipal user) =>
{
    var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    if (userId == null) return Results.Unauthorized();

    var validator = new TodoValidator();
    var validationResult = validator.Validate(todoDto);
    if (!validationResult.IsValid)
    {
        return Results.BadRequest(validationResult.Errors);
    }

    var todo = todoDto.ToModel(int.Parse(userId));
    dbContext.Todos.Add(todo);
    await dbContext.SaveChangesAsync();
    return Results.Created($"/api/todos/{todo.Id}", todo.ToDto());
}).RequireAuthorization();

app.MapGet("/api/v1/todos", async (TodoDbContext dbContext, ClaimsPrincipal user) =>
{
    var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    if (userId == null) return Results.Unauthorized();

    var todos = await dbContext.Todos
        .Where(t => t.UserId == int.Parse(userId))
        .ToListAsync();
    
    var todoDtos = todos.Select(t => t.ToViewDto()).ToList();
    return Results.Ok(todoDtos);
}).RequireAuthorization();

app.MapGet("/api/v1/todos/{id}", async (int id, TodoDbContext dbContext, ClaimsPrincipal user) =>
{
    var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    if (userId == null) return Results.Unauthorized();

    var todo = await dbContext.Todos.FirstOrDefaultAsync(t => t.Id == id && t.UserId == int.Parse(userId));
    if (todo == null) return Results.NotFound();

    return Results.Ok(todo.ToViewDto());
}).RequireAuthorization();

app.MapPut("/api/v1/todos/{id}", async (int id, TodoDto updatedTodoDto, TodoDbContext dbContext, ClaimsPrincipal user) =>
{
    var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    if (userId == null) return Results.Unauthorized();

    var todo = await dbContext.Todos.FirstOrDefaultAsync(t => t.Id == id && t.UserId == int.Parse(userId));
    if (todo == null) return Results.NotFound();

    todo.Title = updatedTodoDto.Title;
    todo.Description = updatedTodoDto.Description;
    todo.CompletedAt = updatedTodoDto.CompletedAt;

    await dbContext.SaveChangesAsync();
    return Results.NoContent();
}).RequireAuthorization();

app.MapDelete("/api/v1/todos/{id}", async (int id, TodoDbContext dbContext, ClaimsPrincipal user) =>
{
    var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    if (userId == null) return Results.Unauthorized();

    var todo = await dbContext.Todos.FirstOrDefaultAsync(t => t.Id == id && t.UserId == int.Parse(userId));
    if (todo == null) return Results.NotFound();

    dbContext.Todos.Remove(todo);
    await dbContext.SaveChangesAsync();
    return Results.NoContent();
}).RequireAuthorization();

app.MapPost("/api/v1/auth/login", (UserLogin userLogin, TodoDbContext dbContext) =>
{
    var user = dbContext.Users.FirstOrDefault(u => u.Email == userLogin.Email && u.PasswordHash == userLogin.Password);
    if (user == null)
    {
        return Results.Unauthorized();
    }

    var claims = new[]
    {
        new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
        new Claim(ClaimTypes.Email, user.Email)
    };

    var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("@TodoMinimalApi#1d&mv&lle1d&mn0lle"));
    var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

    var token = new JwtSecurityToken(
        issuer: "TodoMinimalApi",
        audience: "TodoMinimalApi",
        claims: claims,
        expires: DateTime.UtcNow.AddHours(1),
        signingCredentials: credentials
    );

    var tokenString = new JwtSecurityTokenHandler().WriteToken(token);
    return Results.Ok(new { Token = tokenString });
});

app.Run();
