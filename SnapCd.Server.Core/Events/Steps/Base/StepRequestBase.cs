using SnapCd.Server.Core.Services.ResolvedConfiguration.HelperClasses;

namespace SnapCd.Server.Core.Events.Steps.Base;

public class StepRequestBase : CorrelationBase
{
        
    public Guid RunnerId { get; set; } 
        
    public string RunnerInstanceName { get; set; }  = String.Empty;
    public ResolvedModule Declared { get; set; } = null!;
}