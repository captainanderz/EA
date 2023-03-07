using Microsoft.Graph;
using ProjectHorizon.ApplicationCore.Enums;
using System.Diagnostics.Contracts;

public static class AssignmentConversionExtensions
{
    [Pure]
    public static InstallIntent ToInstallIntent(this AssignmentType value)
    {
        return value switch
        {
            AssignmentType.NotSet => throw new System.NotImplementedException(),
            AssignmentType.Required => InstallIntent.Required,
            AssignmentType.Available => InstallIntent.Available,
            AssignmentType.Uninstall => InstallIntent.Uninstall,
            _ => throw new System.NotImplementedException(),
        };
    }

    [Pure]
    public static Win32LobAppDeliveryOptimizationPriority ToWin32LobAppDeliveryOptimizationPriority(this DeliveryOptimizationPriority value)
    {
        return value switch
        {
            DeliveryOptimizationPriority.NotSet => throw new System.NotImplementedException(),
            DeliveryOptimizationPriority.ContentDownloadInBackground => Win32LobAppDeliveryOptimizationPriority.NotConfigured,
            DeliveryOptimizationPriority.ContentDownloadInForeground => Win32LobAppDeliveryOptimizationPriority.Foreground,
            _ => throw new System.NotImplementedException(),
        };
    }

    [Pure]
    public static Win32LobAppNotification ToWin32LobAppNotification(this EndUserNotification value)
    {
        return value switch
        {
            EndUserNotification.NotSet => throw new System.NotImplementedException(),
            EndUserNotification.ShowAllToastNotifications => Win32LobAppNotification.ShowAll,
            EndUserNotification.ShowToastNotificationsForComputerRestarts => Win32LobAppNotification.ShowReboot,
            EndUserNotification.HideAllToastNotifications => Win32LobAppNotification.HideAll,
            _ => throw new System.NotImplementedException(),
        };
    }
}