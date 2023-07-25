using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Sufni.Bridge.ViewModels;

public class RegisterViewModel : ViewModelBase
{
    [Required]
    [Url]
    public string? ServerUrl { get; set; }
    [Required]
    public string? Username { get; set; }
    [Required]
    public string? Password { get; set; }

    private void DoRegister()
    {
        
    }
}
