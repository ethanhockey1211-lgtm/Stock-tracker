using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Tremor.Core.Copy;
using Tremor.Core.Models;
using Tremor.Core.Services.Abstractions;

namespace Tremor.ViewModels;

/// <summary>
/// New-listing feed. Shows symbols detected as newly tradeable. The first poll
/// establishes a baseline, so listings populate as they are detected over time.
/// </summary>
public partial class ListingsViewModel : BaseViewModel
{
    private readonly INewListingService _listingService;
    private readonly ILogger<ListingsViewModel> _logger;

    public ObservableCollection<TokenListing> Listings { get; } = [];

    public string EmptyMessage => AppCopy.ListingsEmpty;

    public ListingsViewModel(INewListingService listingService, ILogger<ListingsViewModel> logger)
    {
        Title = AppCopy.ListingsTitle;
        _listingService = listingService;
        _logger = logger;
    }

    public bool IsEmpty => Listings.Count == 0;

    [RelayCommand]
    private async Task RefreshAsync()
    {
        if (IsBusy)
        {
            return;
        }

        IsBusy = true;
        try
        {
            var newListings = await _listingService.PollNewListingsAsync().ConfigureAwait(false);
            if (newListings.Count == 0)
            {
                return;
            }

            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                foreach (var listing in newListings.OrderByDescending(l => l.DetectedUtc))
                {
                    Listings.Insert(0, listing);
                }

                OnPropertyChanged(nameof(IsEmpty));
            }).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to refresh listings");
        }
        finally
        {
            IsBusy = false;
        }
    }
}
