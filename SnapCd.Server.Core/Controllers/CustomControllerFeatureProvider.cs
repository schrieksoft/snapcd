using System.Reflection;
using Microsoft.AspNetCore.Mvc.Controllers;

namespace SnapCd.Server.Core.Controllers;

public class CustomControllerFeatureProvider : ControllerFeatureProvider
{
    private readonly HashSet<Type> _controllerTypes;

    public CustomControllerFeatureProvider(IEnumerable<Type> controllerTypes)
    {
        _controllerTypes = new HashSet<Type>(controllerTypes);
    }

    protected override bool IsController(TypeInfo typeInfo)
    {
        // Check if the type is one of the specified controllers
        return _controllerTypes.Contains(typeInfo.AsType()) && base.IsController(typeInfo);
    }
}