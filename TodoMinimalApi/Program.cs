using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using TodoMinimalApi.Data;
using TodoMinimalApi.DTOs;
using TodoMinimalApi.Models;
using TodoMinimalApi.Services;
using TodoMinimalApi.Validators;

var builder = WebApplication.CreateBuilder(args);

// DB
var connectionString = builder.Configuration["ConnectionStrings__Postgres"]
                       ?? builder.Configuration.GetConnectionString("Postgres");

builder.Services.AddDbContext<TodoDbContext>(options =>
    options.UseNpgsql(connectionString));

// JWT
var secretKey = builder.Configuration["JwtSettings:Key"];
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
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey))
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

// Register services
builder.Services.AddScoped<ITodoService, TodoService>();

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

app.MapPost("/api/v1/todos", async (TodoDto todoDto, ITodoService todoService, ClaimsPrincipal user) =>
{
    var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    if (userId == null) return Results.Unauthorized();

    var validator = new TodoValidator();
    var validationResult = validator.Validate(todoDto);
    if (!validationResult.IsValid)
    {
        return Results.BadRequest(validationResult.Errors);
    }

    var todoViewDto = await todoService.CreateTodoAsync(int.Parse(userId), todoDto);
    return Results.Created($"/api/todos/{todoViewDto.Id}", todoViewDto);
}).RequireAuthorization();

app.MapGet("/api/v1/todos", async (ITodoService todoService, ClaimsPrincipal user) =>
{
    var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    if (userId == null) return Results.Unauthorized();

    var todosViewDtoList = await todoService.GetUserTodosAsync(int.Parse(userId));
    
    return Results.Ok(todosViewDtoList);
}).RequireAuthorization();

app.MapGet("/api/v1/todos/{id}", async (int id, ITodoService todoService, ClaimsPrincipal user) =>
{
    var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    if (userId == null) return Results.Unauthorized();

    var todoViewDto = await todoService.GetTodoByIdAsync(int.Parse(userId), id);
    if (todoViewDto == null) return Results.NotFound();

    return Results.Ok(todoViewDto);
}).RequireAuthorization();

app.MapPut("/api/v1/todos/{id}", async (int id, TodoDto updatedTodoDto, ITodoService todoService, ClaimsPrincipal user) =>
{
    var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    if (userId == null) return Results.Unauthorized();

    var result = await todoService.UpdateTodoAsync(int.Parse(userId), id, updatedTodoDto);

    return result ? Results.NoContent() : Results.NotFound();
}).RequireAuthorization();

app.MapDelete("/api/v1/todos/{id}", async (int id, ITodoService todoService, ClaimsPrincipal user) =>
{
    var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    if (userId == null) return Results.Unauthorized();

    var result = await todoService.DeleteTodoAsync(int.Parse(userId), id);
    return result ? Results.NoContent() : Results.NotFound();
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

    var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
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
