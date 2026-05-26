// ════════════════════════════════════════════════════════════════════════
//  ViewModels/Base/BaseViewModel.cs
//  Foundation class extended by every ViewModel in all four portals.
//
//  Pattern:
//    • Extend BaseViewModel, not ObservableObject directly
//    • Use [ObservableProperty] on private fields
//    • Use [RelayCommand] on private async Task methods
//    • Always wrap async work in RunBusyAsync() — never set IsBusy manually
//    • Call SetError() on failure, ClearError() is automatic inside RunBusyAsync
// ════════════════════════════════════════════════════════════════════════

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Threading.Tasks;

namespace UL_Optometry.ViewModels.Base;

public partial class BaseViewModel : ObservableObject
{
    // ── Busy state ────────────────────────────────────────────────────
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsNotBusy))]
    private bool _isBusy;

    /// <summary>!IsBusy — bind to Button.IsEnabled to prevent double-taps.</summary>
    public bool IsNotBusy => !IsBusy;

    // ── Page title ────────────────────────────────────────────────────
    [ObservableProperty]
    private string _title = string.Empty;

    // ── Error state ───────────────────────────────────────────────────
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasError))]
    private string _errorMessage = string.Empty;

    /// <summary>Drives IsVisible on AlertDangerStyle borders.</summary>
    public bool HasError => !string.IsNullOrEmpty(ErrorMessage);

    // ── Error helpers ─────────────────────────────────────────────────
    protected void SetError(string message) => ErrorMessage = message;
    protected void ClearError()             => ErrorMessage = string.Empty;

    // ── Core async wrapper ────────────────────────────────────────────
    /// <summary>
    /// Wraps any async work with:
    ///   • Guard against concurrent calls (if IsBusy return)
    ///   • IsBusy = true  →  run action  →  IsBusy = false
    ///   • Clears previous error before running
    ///   • Catches exceptions and shows them via SetError()
    ///
    /// Usage in a RelayCommand:
    ///   await RunBusyAsync(async () =>
    ///   {
    ///       var result = await _service.DoSomethingAsync();
    ///       if (!result.Success) { SetError(result.Error!); return; }
    ///       // handle success
    ///   });
    /// </summary>
    protected async Task RunBusyAsync(Func<Task> action)
    {
        if (IsBusy) return;

        IsBusy = true;
        ClearError();

        try
        {
            await action();
        }
        catch (Exception ex)
        {
            SetError(ex.Message);
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>
    /// Overload that returns a typed result.
    /// Use when you need to return a value from the async block.
    /// </summary>
    protected async Task<T?> RunBusyAsync<T>(Func<Task<T>> action)
    {
        if (IsBusy) return default;

        IsBusy = true;
        ClearError();

        try
        {
            return await action();
        }
        catch (Exception ex)
        {
            SetError(ex.Message);
            return default;
        }
        finally
        {
            IsBusy = false;
        }
    }
}
