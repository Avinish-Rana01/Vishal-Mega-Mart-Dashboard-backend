using Dapper;
using Microsoft.Data.SqlClient;
using System.Threading;
using System.Threading.Tasks;

namespace VS_Mart_Backend.Services
{
    /// <summary>
    /// Lightweight outbox helper. Pollers call HasPendingAsync() before executing any expensive
    /// stored procedure. If no notification row exists for a given section, the SP is skipped
    /// entirely — eliminating SQL lock contention and "Execution Timeout" death-spirals.
    ///
    /// Outbox table: dbo.ChangeNotifications (created by 01_setup_outbox.sql)
    /// Triggers: one per source table, fire AFTER INSERT/UPDATE/DELETE.
    /// </summary>
    public static class ChangeNotificationChecker
    {
        // Reads 1 tiny row — completes in < 2ms even under heavy load
        private const string CheckSql =
            "SELECT COUNT(1) FROM dbo.ChangeNotifications " +
            "WHERE Section = @Section AND IsProcessed = 0";

        // Marks all pending rows for the section as processed after the SP has run
        private const string MarkSql =
            "UPDATE dbo.ChangeNotifications SET IsProcessed = 1 " +
            "WHERE Section = @Section AND IsProcessed = 0";

        /// <summary>
        /// Returns true if at least one unprocessed change notification exists for the given section.
        /// On any error (e.g. outbox table not yet created), returns true so the SP always runs safely.
        /// </summary>
        public static async Task<bool> HasPendingAsync(
            string connectionString,
            string section,
            CancellationToken ct = default)
        {
            try
            {
                await using var conn = new SqlConnection(connectionString);
                var count = await conn.ExecuteScalarAsync<int>(
                    new CommandDefinition(CheckSql, new { Section = section }, cancellationToken: ct));
                return count > 0;
            }
            catch
            {
                // Fail-open: if the outbox itself has an error, let the poller run normally
                return true;
            }
        }

        /// <summary>
        /// Marks all pending notifications for the given section as processed.
        /// Call this immediately after the SP has executed successfully.
        /// Best-effort — never throws so the poller is never disrupted.
        /// </summary>
        public static async Task MarkProcessedAsync(
            string connectionString,
            string section,
            CancellationToken ct = default)
        {
            try
            {
                await using var conn = new SqlConnection(connectionString);
                await conn.ExecuteAsync(
                    new CommandDefinition(MarkSql, new { Section = section }, cancellationToken: ct));
            }
            catch { /* Best-effort — never crash the poller on cleanup */ }
        }
    }
}
