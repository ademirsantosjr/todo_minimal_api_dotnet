using FluentValidation;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Security.Claims;
using System.Text;
using TodoMinimalApi.Data;
using TodoMinimalApi.DTOs;
using TodoMinimalApi.Models;
using TodoMinimalApi.Services.App;
using TodoMinimalApi.Services.Auth;
using TodoMinimalApi.Services.Todo;
using TodoMinimalApi.Services.UserService;
using TodoMinimalApi.Swagger;
using TodoMinimalApi.Validators;

var builder = WebApplication.CreateBuilder(args);

// DB
var connectionString = builder.Configuration["ConnectionStrings__Postgres"]
                       ?? builder.Configuration.GetConnectionString("Postgres");

builder.Services.AddDbContext<TodoDbContext>(options => options.UseNpgsql(connectionString));

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

// Auth Policies
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
        Description = "Autoriza��o JWT usando o esquema Bearer (Exemplo: 'Bearer 12345abcdef')",
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
        Description = "API para gerenciar tarefas com autentica��o e recursos administrativos."
    });

    options.DocumentFilter<TagDescriptionsFilter>();
});

// Validation
builder.Services.AddValidatorsFromAssemblyContaining<TodoValidator>();
builder.Services.AddControllers();

// Register services
builder.Services.AddScoped<ITodoService, TodoService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IAppService, AppService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();

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

// Map Groups
var todos = app.MapGroup("/api/v1/todos").RequireAuthorization("RequireUser").WithTags("TODOS");
var users = app.MapGroup("/api/v1/users").RequireAuthorization("RequireAdmin").WithTags("Admin");
var auth = app.MapGroup("/api/v1/auth").WithTags("Auth");
var setup = app.MapGroup("/api/v1/setup").WithTags("App Setup");

todos.MapPost("/", CreateTodo).WithSummary("Criar uma nova tarefa.");
todos.MapGet("/", GetAllTodos).WithSummary("Listar todas as tarefas.");
todos.MapGet("/{id}", GetTodoById).WithSummary("Retornar uma determinada tarefa.");
todos.MapPut("/{id}", UpdateTodoById).WithSummary("Alterar uma determinada tarefa.");
todos.MapDelete("/{id}", DeleteTodoById).WithSummary("Remover uma determinada tarefa.");

users.MapPost("/{id}/approve", ApproveUserById).WithSummary("Aprovar um usu�rio previamente registrado.");

auth.MapPost("/register", RegisterUser)
    .WithSummary("Registrar um usu�rio.")
    .WithDescription("""
        Permite registrar um novo usu�rio no sistema.
        Ap�s o registro, o usu�rio administrador deve aprovar do cadastro do novo usu�rio.
        """);

auth.MapPost("/login", Login).WithSummary("Autenticar usando credenciais de login.");

setup.MapPost("/", Setup).WithSummary("Criar um usu�rio administrador.");

app.Run();

// Handlers
static async Task<IResult> CreateTodo(TodoDto todoDto, ITodoService todoService, ClaimsPrincipal user)
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
    return TypedResults.Created($"/api/v1/todos/{todoViewDto.Id}", todoViewDto);
}

static async Task<IResult> GetAllTodos(ITodoService todoService, ClaimsPrincipal user)
{
    var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    if (userId == null) return Results.Unauthorized();

    var todosViewDtoList = await todoService.GetUserTodosAsync(int.Parse(userId));

    return TypedResults.Ok(todosViewDtoList);
}

static async Task<IResult> GetTodoById(int id, ITodoService todoService, ClaimsPrincipal user)
{
    var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    if (userId == null) return Results.Unauthorized();

    var todoViewDto = await todoService.GetTodoByIdAsync(int.Parse(userId), id);
    if (todoViewDto == null) return Results.NotFound();

    return TypedResults.Ok(todoViewDto);
}

static async Task<IResult> UpdateTodoById(int id, TodoUpdateDto updatedTodoDto, ITodoService todoService, ClaimsPrincipal user)
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

    return TypedResults.NoContent();
}

static async Task<IResult> DeleteTodoById(int id, ITodoService todoService, ClaimsPrincipal user)
{
    var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    if (userId == null) return Results.Unauthorized();

    var result = await todoService.DeleteTodoAsync(int.Parse(userId), id);

    return result ? TypedResults.NoContent() : TypedResults.NotFound();
}

static async Task<IResult> ApproveUserById(int id, IUserService userService)
{
    await userService.ApprovePenddingUserById(id);

    return TypedResults.Ok("Usu�rio aprovado com sucesso!");
}

static async Task<IResult> RegisterUser(UserRegistrationDto userRegistrationDto, IAuthService userService)
{
    var user = await userService.RegisterUserAsync(userRegistrationDto);

    return TypedResults.Created($"/api/v1/auth/register/{user.Id}", new { user.Id, user.Name, user.Email });
}

static async Task<IResult> Login(UserLogin userLogin, IAuthService authService)
    => await authService.LoginAsync(userLogin);

static async Task<IResult> Setup(UserRegistrationDto userRegistrationDto, IAppService appService)
{
    await appService.SetupAdmin(userRegistrationDto);

    return TypedResults.Ok("Usu�rio administrador criado com sucesso!");
}
