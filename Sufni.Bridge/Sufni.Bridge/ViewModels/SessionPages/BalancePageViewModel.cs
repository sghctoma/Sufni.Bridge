using CommunityToolkit.Mvvm.ComponentModel;

namespace Sufni.Bridge.ViewModels.SessionPages;

public partial class BalancePageViewModel() : PageViewModelBase("Balance")
{
    [ObservableProperty] private string? compressionBalance;
    [ObservableProperty] private string? reboundBalance;
}
