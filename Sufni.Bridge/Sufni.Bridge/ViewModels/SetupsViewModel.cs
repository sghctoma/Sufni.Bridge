using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Sufni.Bridge.Models;
using Sufni.Bridge.Services;

namespace Sufni.Bridge.ViewModels;

public partial class SetupsViewModel : ViewModelBase
{
    #region Observable properties

    public ObservableCollection<Setup> Setups { get; } = new();

    #endregion
    
    #region Private members

    private readonly IHttpApiService _httpApiService;

    #endregion

    #region Constructors

    public SetupsViewModel()
    {
        _httpApiService = this.GetServiceOrCreateInstance<IHttpApiService>();
        _ = LoadSetupsAsync();
    }

    #endregion

    #region Private methods

    private async Task LoadSetupsAsync()
    {
        var setups = await _httpApiService.GetSetups();
        foreach (var setup in setups)
        {
            Setups.Add(setup);
        }
    }

    #endregion
}