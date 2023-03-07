using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ProjectHorizon.ApplicationCore.Constants;
using ProjectHorizon.ApplicationCore.DTOs;
using ProjectHorizon.ApplicationCore.Interfaces;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ProjectHorizon.WebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class NotificationsController : HorizonBaseController
    {
        private readonly INotificationService _notificationService;

        public NotificationsController(INotificationService notificationService)
        {
            _notificationService = notificationService;
        }

        [HttpGet]
        [ProducesResponseType(typeof(PagedResult<NotificationDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> ListNotificationsPaged(
            [FromQuery] int pageNumber,
            [FromQuery] int pageSize,
            [FromQuery] string? searchTerm
            )
        {
            UserDto? loggedInUser = GetLoggedInUser();

            PagedResult<NotificationDto>? result = await _notificationService
                .ListNotificationsPagedAsync(loggedInUser, pageNumber, pageSize, searchTerm);

            return Ok(result);
        }

        [HttpGet("[action]")]
        [ProducesResponseType(typeof(IEnumerable<NotificationDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> Recent()
        {
            UserDto? loggedInUser = GetLoggedInUser();

            IEnumerable<NotificationDto>? result = await _notificationService.GetRecentNotificationsAsync(loggedInUser);

            return Ok(result);
        }

        [HttpPost("[action]")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> MarkAllAsRead()
        {
            UserDto? loggedInUser = GetLoggedInUser();

            await _notificationService.MarkAllNotificationsAsReadAsync(loggedInUser);

            return Ok();
        }

        [HttpGet("[action]")]
        [ProducesResponseType(typeof(IEnumerable<UserNotificationSettingDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> Settings()
        {
            UserDto? loggedInUser = GetLoggedInUser();

            IEnumerable<UserNotificationSettingDto>? result = await _notificationService.FindUsersNotificationSettingsBySubscriptionAsync(loggedInUser);

            return Ok(result);
        }

        [HttpPatch("[action]")]
        [ProducesResponseType(typeof(NotificationSettingDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> Settings([FromBody] NotificationSettingDto notificationSettingDto)
        {
            UserDto? loggedInUser = GetLoggedInUser();

            if (loggedInUser.UserRole != UserRole.SuperAdmin &&
                loggedInUser.UserRole != UserRole.Administrator &&
                notificationSettingDto.ApplicationUserId != loggedInUser.Id)
            {
                return Forbid();
            }

            if (notificationSettingDto.SubscriptionId != loggedInUser.SubscriptionId)
            {
                return Unauthorized();
            }

            NotificationSettingDto? notificationSetting = await _notificationService.UpdateNotificationSettingAsync(notificationSettingDto);

            return Ok(notificationSetting);
        }

        [HttpPut("[action]")]
        [ProducesResponseType(typeof(BulkNotificationSettingsDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> Settings([FromBody] BulkNotificationSettingsDto bulkNotificationSettingsDto)
        {
            UserDto? loggedInUser = GetLoggedInUser();

            if (loggedInUser.UserRole != UserRole.SuperAdmin &&
                loggedInUser.UserRole != UserRole.Administrator &&
                (bulkNotificationSettingsDto.UserIds.Count() > 1 ||
                !bulkNotificationSettingsDto.UserIds.Contains(loggedInUser.Id)))
            {
                return Forbid();
            }

            await _notificationService.UpdateBulkNotificationSettingsAsync(
                loggedInUser.SubscriptionId, bulkNotificationSettingsDto);

            return Ok();
        }
    }
}
