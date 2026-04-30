namespace SnapCd.Server.Core.Services.ViewManagement;

/// <summary>
/// Service for managing database views that are applied after migrations.
/// Views are stored as SQL files and applied using CREATE OR ALTER (SQL Server)
/// </summary>
public interface IViewManager
{
    /// <summary>
    /// Applies all database views for the current database provider.
    /// Views are loaded from embedded resources and executed against the database.
    /// </summary>
    Task ApplyViewsAsync(CancellationToken cancellationToken = default);
}