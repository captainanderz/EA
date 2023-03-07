using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.EntityFrameworkCore;
using ProjectHorizon.ApplicationCore.Constants;
using ProjectHorizon.ApplicationCore.DTOs;
using ProjectHorizon.ApplicationCore.Entities;
using ProjectHorizon.ApplicationCore.Interfaces;
using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjectHorizon.ApplicationCore.Services
{
    public class AuditLogService : IAuditLogService
    {
        private readonly IApplicationDbContext _applicationDbContext;
        private readonly IMapper _mapper;
        private readonly ILoggedInUserProvider _loggedInUserProvider;

        public AuditLogService(
            IApplicationDbContext applicationDbContext,
            IMapper mapper,
            ILoggedInUserProvider loggedInUserProvider)
        {
            _applicationDbContext = applicationDbContext;
            _mapper = mapper;
            _loggedInUserProvider = loggedInUserProvider;
        }

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
        public async Task<PagedResult<AuditLogDto>> ListAuditLogsPagedAsync(
            int pageNumber,
            int pageSize,
            string? searchTerm,
            DateTime? fromDate,
            DateTime? toDate,
            string category)
        {
            IQueryable<AuditLog>? queryAuditLogs = GetFilteredQuery(fromDate, toDate, category);

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                queryAuditLogs = queryAuditLogs.Where(al => al.ActionText.Contains(searchTerm.Trim()));
            }

            return new PagedResult<AuditLogDto>
            {
                AllItemsCount = await queryAuditLogs.CountAsync(),
                PageItems = await queryAuditLogs
                    .OrderByDescending(al => al.CreatedOn)
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .ProjectTo<AuditLogDto>(_mapper.ConfigurationProvider)
                    .ToListAsync()
            };
        }

        /// <summary>
        /// Gets the CSV
        /// </summary>
        /// <param name="searchTerm">The term used for searching for a specific notification by name</param>
        /// <param name="fromDate">The date when the log was first added</param>
        /// <param name="toDate">The date when the log was last updated</param>
        /// <param name="category">The category of the log</param>
        /// <returns></returns>
        public async Task<string> GetCsvAsync(
            string? searchTerm,
            DateTime? fromDate,
            DateTime? toDate,
            string category)
        {
            IQueryable<AuditLog>? queryAuditLogs = GetFilteredQuery(fromDate, toDate, category);

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                queryAuditLogs = queryAuditLogs.Where(al => al.ActionText.Contains(searchTerm.Trim()));
            }

            System.Collections.Generic.List<AuditLogDto>? auditLogs = await queryAuditLogs
                .OrderByDescending(n => n.CreatedOn)
                .ProjectTo<AuditLogDto>(_mapper.ConfigurationProvider)
                .ToListAsync();

            StringBuilder? stringBuilder = new StringBuilder("DATE,USER,SOURCE IP,ACTION,CATEGORY");
            stringBuilder.AppendLine();

            foreach (AuditLogDto? auditLog in auditLogs)
            {
                stringBuilder.AppendLine(
                    $"\"{auditLog.ModifiedOn:dd/MM/yyyy HH:mm}\" UTC," +
                    $"\"{EscapeExcelFormulaTriggeringCharacters(auditLog.User)}\"," +
                    $"\"{auditLog.SourceIP}\"," +
                    $"\"{EscapeExcelFormulaTriggeringCharacters(auditLog.ActionText)}\"," +
                    $"\"{auditLog.Category}\""
                );
            }

            return stringBuilder.ToString();
        }

        /// <summary>
        /// Excludes some special characters from the string input
        /// </summary>
        /// <param name="input">The text from which we want some characters excluded</param>
        /// <returns></returns>
        private static string EscapeExcelFormulaTriggeringCharacters(string input)
        {
            char[] excelFormulaTriggeringCharacters = { '=', '-', '+', '@' };

            if (input.Length > 0 && excelFormulaTriggeringCharacters.Contains(input[0]))
            {
                return "'" + input;
            }

            return input;
        }

        /// <summary>
        /// Generates the audit logs
        /// </summary>
        /// <param name="category">The audit log category we want the new audit log to have</param>
        /// <param name="actionText">The text that the audit log contains</param>
        /// <param name="subscriptionId">The id of the subscription the action took place in</param>
        /// <param name="author">The user that made the action</param>
        /// <param name="saveChanges">A bool that saves the new audit log</param>
        /// <returns></returns>
        public async Task GenerateAuditLogAsync(string category, string actionText, Guid? subscriptionId = null, UserDto? author = null, bool saveChanges = true)
        {
            var auditLog = new AuditLog
            {
                SubscriptionId = subscriptionId ?? author?.SubscriptionId,
                ApplicationUserId = author?.Id,
                Category = category,
                ActionText = actionText,
                SourceIP = author?.SourceIP ?? "N/A",
            };

            _applicationDbContext.AuditLogs.Add(auditLog);

            if (saveChanges)
            {
                await _applicationDbContext.SaveChangesAsync();
            }
        }

        /// <summary>
        /// Filters the listed audit log by date
        /// </summary>
        /// <param name="loggedInUser">The user that is currently logged in</param>
        /// <param name="fromDate">The date when the log was first added</param>
        /// <param name="toDate">The date when the log was last updated</param>
        /// <param name="category">The category of the log</param>
        /// <returns></returns>
        private IQueryable<AuditLog> GetFilteredQuery(
            DateTime? fromDate,
            DateTime? toDate,
            string category)
        {
            UserDto? loggedInUser = _loggedInUserProvider.GetLoggedInUser();
            return _applicationDbContext
                    .AuditLogs
                    .Where(n => n.SubscriptionId == loggedInUser.SubscriptionId || n.SubscriptionId == null)
                    .Where(n => n.Category == category || category == AuditLogCategory.AllCategories)
                    .Where(n => fromDate == null || n.ModifiedOn > fromDate)
                    .Where(n => toDate == null || n.ModifiedOn.Date <= toDate.Value.Date);
        }
    }
}