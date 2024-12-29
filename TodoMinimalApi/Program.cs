using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using TodoMinimalApi.Data;
using TodoMinimalApi.DTOs;
using TodoMinimalApi.Models;
using TodoMinimalApi.Services.App;
using TodoMinimalApi.Services.Auth;
using TodoMinimalApi.Services.Todo;
using TodoMinimalApi.Services.User;
using TodoMinimalApi.Swagger;
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

// Adiciona o serviço de autorização
builder.Services.AddAuthorizationBuilder()
    .AddPolicy("RequireAdmin", policy => policy.RequireRole("ADMIN"))
    .AddPolicy("RequireUser", policy => policy.RequireRole("USER"));

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Description = "Autorização JWT usando o esquema Bearer (Exemplo: 'Bearer 12345abcdef')",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new string[] { }
        }
    });

    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "TODO Minimal API",
        Version = "v1",
        Description = "API para gerenciar tarefas com autenticação e recursos administrativos."
    });

    options.DocumentFilter<TagDescriptionsFilter>();
});

// Validation
builder.Services.AddControllers().AddFluentValidation(fv =>
    fv.RegisterValidatorsFromAssemblyContaining<TodoValidator>());

// Register services
builder.Services.AddScoped<ITodoService, TodoService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IAppService, AppService>();
builder.Services.AddScoped<IUserService, UserService>();

// Add services to the container.
var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseHttpsRedirection();

// Auth
app.UseAuthentication();
app.UseAuthorization();

// Middleware
app.UseMiddleware<TodoMinimalApi.Middleware.ExceptionHandlingMiddleware>();

// Swagger
app.UseSwagger();
app.UseSwaggerUI();

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
})
.RequireAuthorization("RequireUser")
.WithTags("TODOS")
.WithSummary("Cria uma nova tarefa.");

app.MapGet("/api/v1/todos", async (ITodoService todoService, ClaimsPrincipal user) =>
{
    var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    if (userId == null) return Results.Unauthorized();

    var todosViewDtoList = await todoService.GetUserTodosAsync(int.Parse(userId));
    
    return Results.Ok(todosViewDtoList);
})
.RequireAuthorization("RequireUser")
.WithTags("TODOS")
.WithSummary("Lista todas as tarefas.");

app.MapGet("/api/v1/todos/{id}", async (int id, ITodoService todoService, ClaimsPrincipal user) =>
{
    var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    if (userId == null) return Results.Unauthorized();

    var todoViewDto = await todoService.GetTodoByIdAsync(int.Parse(userId), id);
    if (todoViewDto == null) return Results.NotFound();

    return Results.Ok(todoViewDto);
})
.RequireAuthorization("RequireUser")
.WithTags("TODOS")
.WithSummary("Retorna uma determinada tarefa.");

app.MapPut("/api/v1/todos/{id}", async (int id, TodoUpdateDto updatedTodoDto, ITodoService todoService, ClaimsPrincipal user) =>
{
    var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    if (userId == null) return Results.Unauthorized();

    var validator = new TodoUpdateValidator();
    var validationResult = validator.Validate(updatedTodoDto);
    if (!validationResult.IsValid)
    {
        return Results.BadRequest(validationResult.Errors);
    }

    await todoService.UpdateTodoAsync(int.Parse(userId), id, updatedTodoDto);

    return Results.NoContent();
})
.RequireAuthorization("RequireUser")
.WithTags("TODOS")
.WithSummary("Altera uma determinada tarefa.");

app.MapDelete("/api/v1/todos/{id}", async (int id, ITodoService todoService, ClaimsPrincipal user) =>
{
    var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    if (userId == null) return Results.Unauthorized();

    var result = await todoService.DeleteTodoAsync(int.Parse(userId), id);
    return result ? Results.NoContent() : Results.NotFound();
})
.RequireAuthorization("RequireUser")
.WithTags("TODOS")
.WithSummary("Remove uma determinada tarefa.");

app.MapPost("/api/v1/users/{id}/approve", async (int id, IUserService userService) =>
{
    await userService.ApprovePenddingUserById(id);

    return Results.Ok("Usuário aprovado com sucesso!");
})
.RequireAuthorization("RequireAdmin")
.WithTags("Admin")
.WithSummary("Aprova um usuário previamente registrado."); ;

app.MapPost("/api/v1/auth/register", async (UserRegistrationDto userRegistrationDto, IAuthService userService) =>
{
    var user = await userService.RegisterUserAsync(userRegistrationDto);

    return Results.Created($"/api/v1/auth/register/{user.Id}", new { user.Id, user.Name, user.Email });
})
.WithTags("Auth")
.WithSummary("Registrar-se no sistema")
.WithDescription("""
    Permite registrar um novo usuário no sistema.
    Após o registro, o usuário administrador deve aprovar do cadastro do novo usuário.
    """);

app.MapPost("/api/v1/auth/login", (UserLogin userLogin, TodoDbContext dbContext) =>
{
    var user = dbContext.Users
        .Include(u => u.Role)
        .FirstOrDefault(u => u.Email == userLogin.Email);

    if (user == null) return Results.Unauthorized();

    var passwordHasher = new PasswordHasher<User>();
    var result = passwordHasher.VerifyHashedPassword(user, user.PasswordHash, userLogin.Password);
    
    if (result == PasswordVerificationResult.Success)
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Role, user.Role.Name)
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
    }

    return Results.Unauthorized();
})
.WithTags("Auth")
.WithSummary("Autenticar-se informando as credenciais de login.");

app.MapPost("/api/v1/setup", async (UserRegistrationDto userRegistrationDto, IAppService appService) =>
{
    await appService.SetupAdmin(userRegistrationDto);

    return Results.Ok("Administrador criado com sucesso!");
})
.WithTags("App Setup")
.WithSummary("Criar um usuário adminitrador.");

app.Run();
