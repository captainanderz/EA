using ProjectHorizon.ApplicationCore.DTOs;
using System;
using System.Threading.Tasks;

namespace ProjectHorizon.ApplicationCore.Interfaces
{
    public interface IAuditLogService
    {
        /// <summary>
        /// Lists all audit logs and paginate them
        /// </summary>
        /// <param name="pageNumber">The number from which the pagination starts</param>
        /// <param name="pageSize">How many elements should be on a page</param>
        /// <param name="searchTerm">The term used for searching for a specific notification by name</param>
        /// <param name="fromDate">The date when the log was first added</param>
        /// <param name="toDate">The date when the log was last updated</param>
        /// <param name="category">The category of the log</param>
        /// <returns></returns>
        Task<PagedResult<AuditLogDto>> ListAuditLogsPagedAsync(
            int pageNumber,
            int pageSize,
            string? searchTerm,
            DateTime? fromDate,
            DateTime? toDate,
            string category);

        /// <summary>
        /// Gets the CSV
        /// </summary>
        /// <param name="searchTerm">The term used for searching for a specific notification by name</param>
        /// <param name="fromDate">The date when the log was first added</param>
        /// <param name="toDate">The date when the log was last updated</param>
        /// <param name="category">The category of the log</param>
        /// <returns></returns>
        Task<string> GetCsvAsync(
            string? searchTerm,
            DateTime? fromDate,
            DateTime? toDate,
            string category);

        /// <summary>
        /// Generates the audit logs
        /// </summary>
        /// <param name="auditCategory">The audit log category we want the new audit log to have</param>
        /// <param name="actionText">The text that the audit log contains</param>
        /// <param name="setSubscriptionNull">A bool that checks if the subscription exists, if not, no audit logs will be anymore created</param>
        /// <param name="saveChanges">A bool that saves the new audit log</param>
        /// <returns></returns>
        Task GenerateAuditLogAsync(
            string category,
            string actionText,
            Guid? subscriptionId = null,
            UserDto? author = null,
            bool saveChanges = true);
    }
}