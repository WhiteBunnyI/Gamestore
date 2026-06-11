using Microsoft.AspNetCore.Authorization;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Text;

public class AuthorizeCheckOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var metadata = context.ApiDescription.ActionDescriptor.EndpointMetadata;

        // 1. Проверяем AllowAnonymous — если он есть, авторизация не нужна
        var hasAllowAnonymous = metadata.Any(m => m is IAllowAnonymous || m is AllowAnonymousAttribute);
        if (hasAllowAnonymous)
        {
            return;
        }

        // 2. Собираем все атрибуты авторизации (из класса и из метода)
        var authorizeAttributes = metadata.OfType<AuthorizeAttribute>().ToList();

        if (authorizeAttributes.Count != 0)
        {
            // Добавляем "замок" для Bearer аутентификации
            operation.Security ??= new List<OpenApiSecurityRequirement>();
            operation.Security.Add(new OpenApiSecurityRequirement
            {
                [new OpenApiSecuritySchemeReference("Bearer", context.Document)] = new List<string>()
            });

            // 3. Формируем красивое описание требований безопасности
            var authInfo = new StringBuilder();
            authInfo.AppendLine("<br/>🔑 **Требования безопасности:**");

            var roles = authorizeAttributes
                .Where(a => !string.IsNullOrEmpty(a.Roles))
                .Select(a => a.Roles)
                .ToList();

            var policies = authorizeAttributes
                .Where(a => !string.IsNullOrEmpty(a.Policy))
                .Select(a => a.Policy)
                .ToList();

            // Добавляем информацию о ролях
            if (roles.Count != 0)
            {
                authInfo.AppendLine($"- **Роли:** `{string.Join(", ", roles)}`");
            }

            // Добавляем информацию о политиках
            if (policies.Count != 0)
            {
                authInfo.AppendLine($"- **Политики:** `{string.Join(", ", policies)}`");
            }

            // Если атрибуты пустые (просто [Authorize]), пишем, что нужен любой валидный токен
            if (roles.Count == 0 && policies.Count == 0)
            {
                authInfo.AppendLine("- Требуется любой валидный JWT токен.");
            }

            // Дописываем это к уже существующему описанию метода
            operation.Description = (operation.Description ?? "") + authInfo.ToString();
        }
    }
}
