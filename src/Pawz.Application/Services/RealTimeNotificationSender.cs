using Microsoft.Extensions.Logging;
using Pawz.Application.Interfaces;
using Pawz.Application.Models.NotificationModels;
using Pawz.Domain.Common;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Pawz.Application.Services;

/// <summary>
/// This class is responsible for sending real-time notifications to users via SignalR library.
/// Implements the <see cref="IRealTimeNotificationSender"/> interface.
/// </summary>
public class RealTimeNotificationSender : IRealTimeNotificationSender
{
    private readonly INotificationHubContext _notificationHubContext;
    private readonly ILogger<RealTimeNotificationSender> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="RealTimeNotificationSender"/> class.
    /// </summary>
    /// <param name="notificationHubContext">The SignalR hub context responsible for sending notifications to users.</param>
    /// <param name="logger">The logger used to log errors and important information.</param>
    public RealTimeNotificationSender(INotificationHubContext notificationHubContext, ILogger<RealTimeNotificationSender> logger)
    {
        _notificationHubContext = notificationHubContext;
        _logger = logger;
    }

    /// <summary>
    /// Sends a real-time notification to a specific user using SignalR.
    /// </summary>
    /// <param name="userId">The ID of the user who will receive the notification.</param>
    /// <param name="notification">The notification data to be sent to the user.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation of sending the notification.</returns>
    public async Task<Result<bool>> SendNotificationAsync(string userId, NotificationResponse notification, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _notificationHubContext.SendToUserAsync(userId, "ReceiveNotification", notification, cancellationToken);

            if (!result.IsSuccess)
            {
                _logger.LogError("Failed to send notification to user {UserId}", userId);
                return Result<bool>.Failure(NotificationErrors.SendingFailed(userId));
            }

            _logger.LogInformation("Successfully sent notification to user {UserId}", userId);
            return Result<bool>.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while attempting to send notification to user {UserId}", userId);
            return Result<bool>.Failure(NotificationErrors.SendingUnexpectedError);
        }
    }
}
