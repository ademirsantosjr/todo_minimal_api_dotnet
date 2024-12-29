using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace TodoMinimalApi.Swagger
{
    public class TagDescriptionsFilter : IDocumentFilter
    {
        public void Apply(OpenApiDocument swaggerDoc, DocumentFilterContext context)
        {
            swaggerDoc.Tags = new List<OpenApiTag>
            {
                new OpenApiTag { Name = "TODOS", Description = "Operações relacionadas ao gerencimento de Tarefas do usuário autenticado." },
                new OpenApiTag { Name = "Admin", Description = "Recursos exclusivos para administradores." },
                new OpenApiTag { Name = "Auth", Description = "Recursos relacionados à autenticação e registro de novos de usuários." },
                new OpenApiTag { Name = "App Setup", Description = "Recursos utilizados no primeiro uso da aplicação." }
            };
        }
    }
}
