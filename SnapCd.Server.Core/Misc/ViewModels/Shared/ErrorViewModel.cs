using System.ComponentModel.DataAnnotations;

namespace SnapCd.Server.Core.Misc.ViewModels.Shared;

public class ErrorViewModel
{
    [Display(Name = "Error")] public string Error { get; set; } = null!;

    [Display(Name = "Description")] public string ErrorDescription { get; set; } = null!;
}