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
                new OpenApiTag { Name = "Auth", Description = "Recursos relacionados à autenticação e registro de novos de usuários." },
                new OpenApiTag { Name = "TODOs", Description = "Operações relacionadas às Tarefas do usuário autenticado." },
                new OpenApiTag { Name = "Admin", Description = "Recursos exclusivos para administradores." },
                new OpenApiTag { Name = "Setup", Description = "Configurações da aplicação" }
            };
        }
    }
}
