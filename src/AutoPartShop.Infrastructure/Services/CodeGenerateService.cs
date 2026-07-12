using System.Data;
using System.Data.Common;
using AutoPartsShop.Domain.Entities;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace AutoPartsShop.Infrastructure.Services
{
    /// <summary>
    /// Thread-safe sequential code generator backed by the CodeSequences table.
    ///
    /// Uses a single MERGE … OUTPUT statement per call — one database round-trip,
    /// no application-level locking, no EF change-tracking side-effects, and safe
    /// whether or not an outer transaction is already open.
    ///
    /// Examples: SO001, INV001, CHN001, BRD001 …
    /// </summary>
    public sealed class CodeGenerateService(AutoPartDbContext _db) : ICodeGenerateService
    {
        // SQL Server MERGE is atomic under HOLDLOCK — concurrent requests always
        // get different LastNumber values, so no duplicates are possible.
        private const string MergeSql = """
            MERGE CodeSequences WITH (HOLDLOCK) AS t
            USING (SELECT @prefix AS Prefix) AS s ON t.Prefix = s.Prefix
            WHEN MATCHED     THEN UPDATE SET LastNumber = t.LastNumber + 1
            WHEN NOT MATCHED THEN INSERT (Prefix, LastNumber) VALUES (@prefix, 1)
            OUTPUT INSERTED.LastNumber;
            """;

        /// <summary>
        /// Atomically increments the sequence for <paramref name="prefix"/> and
        /// returns a formatted code such as "SO001" or "INV0042".
        /// </summary>
        public async Task<string> GenerateAsync(
            string prefix,
            CancellationToken cancellationToken = default,
            int minDigits = 3)
        {
            if (string.IsNullOrWhiteSpace(prefix))
                throw new ArgumentException("Prefix cannot be empty.", nameof(prefix));

            prefix = prefix.Trim().ToUpper();

            int next = await IncrementAsync(prefix, cancellationToken);
            int width = Math.Max(minDigits, next.ToString().Length);
            return $"{prefix}{next.ToString().PadLeft(width, '0')}";
        }

        /// <summary>
        /// Returns what the next code WOULD be without consuming a number.
        /// Use this in UI "preview" endpoints instead of <see cref="GenerateAsync"/>.
        /// Note: the value may be slightly stale under concurrent load — that is fine
        /// for display purposes.
        /// </summary>
        public async Task<string> PeekAsync(
            string prefix,
            CancellationToken cancellationToken = default,
            int minDigits = 3)
        {
            if (string.IsNullOrWhiteSpace(prefix))
                throw new ArgumentException("Prefix cannot be empty.", nameof(prefix));

            prefix = prefix.Trim().ToUpper();

            var sequence = await _db.Set<CodeSequence>()
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.Prefix == prefix, cancellationToken);

            int next = (sequence?.LastNumber ?? 0) + 1;
            int width = Math.Max(minDigits, next.ToString().Length);
            return $"{prefix}{next.ToString().PadLeft(width, '0')}";
        }

        /// <summary>No-op — retained for backward compatibility only.</summary>
        [Obsolete("No-op. GenerateAsync already persists atomically.")]
        public Task SaveGenerateCodeAsync(
            string prefix,
            CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        // ── Core atomic increment ─────────────────────────────────────────────

        // SQL error numbers that are safe to retry for this single-statement MERGE:
        // 1205 = deadlock victim, 1222 = lock request timeout. HOLDLOCK makes these
        // possible under heavy concurrent code generation. We bypass EF here, so the
        // global EnableRetryOnFailure strategy does NOT cover this call — retry locally.
        private const int MaxRetries = 4;

        private async Task<int> IncrementAsync(string prefix, CancellationToken ct)
        {
            for (var attempt = 1; ; attempt++)
            {
                try
                {
                    return await ExecuteMergeAsync(prefix, ct);
                }
                catch (SqlException ex) when (
                    (ex.Number == 1205 || ex.Number == 1222) &&
                    attempt < MaxRetries &&
                    _db.Database.CurrentTransaction == null) // never retry inside a caller's transaction — it's already doomed
                {
                    // Exponential-ish backoff with jitter so colliding requests don't re-collide.
                    await Task.Delay(attempt * 25 + Random.Shared.Next(0, 25), ct);
                }
            }
        }

        private async Task<int> ExecuteMergeAsync(string prefix, CancellationToken ct)
        {
            var conn = _db.Database.GetDbConnection();

            // Open connection if the caller hasn't already done so (EF usually
            // manages this, but we're going around EF here).
            bool ownConnection = conn.State != ConnectionState.Open;
            if (ownConnection)
                await conn.OpenAsync(ct);

            try
            {
                await using var cmd = conn.CreateCommand();

                // Enlist in the ambient EF transaction when one exists so that
                // the sequence increment rolls back together with the outer work.
                if (_db.Database.CurrentTransaction != null)
                    cmd.Transaction = _db.Database.CurrentTransaction.GetDbTransaction();

                cmd.CommandText = MergeSql;

                var p = cmd.CreateParameter();
                p.ParameterName = "@prefix";
                p.DbType = DbType.String;
                p.Size = 20;
                p.Value = prefix;
                cmd.Parameters.Add(p);

                var scalar = await cmd.ExecuteScalarAsync(ct)
                    ?? throw new InvalidOperationException(
                           $"Code generation MERGE returned no rows for prefix '{prefix}'.");

                return Convert.ToInt32(scalar);
            }
            finally
            {
                if (ownConnection)
                    await conn.CloseAsync();
            }
        }
    }
}
