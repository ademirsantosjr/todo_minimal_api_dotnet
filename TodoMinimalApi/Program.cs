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

Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
Console.OutputEncoding = Encoding.UTF8;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration["ConnectionStrings__Postgres"]
                       ?? builder.Configuration.GetConnectionString("Postgres");

builder.Services.AddDbContext<TodoDbContext>(options => options.UseNpgsql(connectionString));

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

builder.Services.AddAuthorizationBuilder()
    .AddPolicy("RequireAdmin", policy => policy.RequireRole("ADMIN"))
    .AddPolicy("RequireUser", policy => policy.RequireRole("USER"));

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
        Description = """
            API para gerenciar tarefas com autenticação e recursos administrativos.
            """
    });

    options.DocumentFilter<TagDescriptionsFilter>();
});

builder.Services.AddValidatorsFromAssemblyContaining<TodoValidator>();
builder.Services.AddControllers();

builder.Services.AddScoped<ITodoService, TodoService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IAppService, AppService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();

var app = builder.Build();

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.UseMiddleware<TodoMinimalApi.Middleware.ExceptionHandlingMiddleware>();

app.UseSwagger();
app.UseSwaggerUI();
app.UseSwaggerUI(options =>
{
    options.HeadContent = @"
        <meta charset='UTF-8'>
    ";
});

app.Use(async (context, next) =>
{
    context.Response.Headers["Content-Type"] = "application/json; charset=utf-8";
    await next();
});

var todos = app.MapGroup("/api/v1/todos")
    .RequireAuthorization(policy => policy.RequireRole("USER", "ADMIN"))
    .WithTags("TODOs");

var users = app.MapGroup("/api/v1/users").RequireAuthorization("RequireAdmin").WithTags("Admin");
var auth = app.MapGroup("/api/v1/auth").WithTags("Auth");
var setup = app.MapGroup("/api/v1/setup").WithTags("Setup");

todos.MapPost("/", CreateTodo).WithSummary("Criar uma nova tarefa.");
todos.MapGet("/", GetAllTodos).WithSummary("Listar todas as tarefas.");
todos.MapGet("/{id}", GetTodoById).WithSummary("Retornar uma determinada tarefa.");
todos.MapPut("/{id}", UpdateTodoById).WithSummary("Alterar uma determinada tarefa.");
todos.MapDelete("/{id}", DeleteTodoById).WithSummary("Remover uma determinada tarefa.");

users.MapPost("/{id}/approve", ApproveUserById).WithSummary("Aprovar um usuário previamente registrado.");

auth.MapPost("/login", Login).WithSummary("Autenticar com as credenciais de login.");

auth.MapPost("/register", RegisterUser)
    .WithSummary("Registrar um usuário.")
    .WithDescription("""
        Permite registrar um novo usuário no sistema.
        Após o registro, o usuário administrador deve aprovar do cadastro do novo usuário.
        """);

setup.MapPost("/", Setup)
    .WithSummary("Criar um usuário administrador.")
    .WithDescription("""
        Caso nenhum usuário administrador tenha sido criado pelo script seed.sql, 
        este endpoint permite a criação do primeiro usuário administrador.
        """);

app.Run();

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

    return TypedResults.Ok("Usuário aprovado com sucesso!");
}

static async Task<IResult> RegisterUser(UserRegistrationDto userRegistrationDto, IAuthService userService)
{
    var user = await userService.RegisterLoginAsync(userRegistrationDto);

    return TypedResults.Created($"/api/v1/auth/register/{user.Id}", new { user.Id, user.Name, user.Email });
}

static async Task<IResult> Login(UserLogin userLogin, IAuthService authService)
    => await authService.LoginAsync(userLogin);

static async Task<IResult> Setup(UserRegistrationDto userRegistrationDto, IAppService appService)
{
    await appService.SetupAdmin(userRegistrationDto);

    return TypedResults.Ok("Usuário administrador criado com sucesso!");
}
