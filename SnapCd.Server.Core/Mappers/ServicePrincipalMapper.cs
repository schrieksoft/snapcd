using SnapCd.Contracts.Dto.ServicePrincipals;
using SnapCd.Server.Core.Entities.Definition;

namespace SnapCd.Server.Core.Mappers;

public static class ServicePrincipalMapper
{
    public static ServicePrincipal ToEntity(ServicePrincipalCreateDto dto, Guid organizationId)
    {
        var scopes = dto.Scopes ?? new List<string>();
        var permissions = $"[\"ept:token\",\"gt:client_credentials\",{string.Join(",", scopes.Select(s => $"\"scp:{s}\""))}]";

        return new ServicePrincipal
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            ClientId = $"{organizationId}:{dto.ClientId}",
            ClientSecret = dto.ClientSecret,
            IsDisabled = dto.IsDisabled,
            Permissions = permissions
        };
    }

    public static ServicePrincipalReadDto ToDto(ServicePrincipal entity)
    {
        var scopes = new List<string>();
        if (!string.IsNullOrWhiteSpace(entity.Permissions))
            try
            {
                var json = System.Text.Json.JsonDocument.Parse(entity.Permissions);
                scopes = json.RootElement
                    .EnumerateArray()
                    .Select(e => e.GetString())
                    .Where(s => s != null && s.StartsWith("scp:"))
                    .Select(s => s!.Substring(4))
                    .ToList();
            }
            catch
            {
                // If parsing fails, return empty list
            }

        return new ServicePrincipalReadDto
        {
            Id = entity.Id,
            ClientId = entity.DisplayClientId!,
            ClientSecret = entity.ClientSecret,
            IsDisabled = entity.IsDisabled,
            Scopes = scopes
        };
    }

    public static void UpdateEntity(ServicePrincipal entity, ServicePrincipalUpdateDto dto)
    {
        var scopes = dto.Scopes ?? new List<string>();
        var permissions = $"[\"ept:token\",\"gt:client_credentials\",{string.Join(",", scopes.Select(s => $"\"scp:{s}\""))}]";

        entity.ClientId = $"{entity.OrganizationId}:{dto.ClientId}";
        entity.ClientSecret = dto.ClientSecret;
        entity.IsDisabled = dto.IsDisabled;
        entity.Permissions = permissions;
    }
}