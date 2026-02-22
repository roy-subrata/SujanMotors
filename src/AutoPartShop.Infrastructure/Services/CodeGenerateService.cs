using AutoPartsShop.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AutoPartsShop.Infrastructure.Services
{
    public class CodeGenerateService(AutoPartDbContext _db) : ICodeGenerateService
    {
        public async Task<string> GenerateAsync(string prefix, CancellationToken cancellationToken = default, int minDigits = 3)
        {
            // Make generation atomic: fetch or create the sequence, increment LastNumber and save within a transaction
            var strategy = _db.Database.CreateExecutionStrategy();

            string result = null;

            await strategy.ExecuteAsync(async () =>
            {
                await using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken);

                var sequence = await _db.Set<CodeSequence>()
                    .FirstOrDefaultAsync(s => s.Prefix == prefix, cancellationToken);

                if (sequence == null)
                {
                    sequence = new CodeSequence
                    {
                        Prefix = prefix,
                        LastNumber = 0
                    };
                    // Don't save here - just preview the code
                }

                sequence.LastNumber++;
                // Don't save here - call SaveGenerateCodeAsync after record is created

                int nextNumber = sequence.LastNumber;
                int numberLength = Math.Max(minDigits, nextNumber.ToString().Length);
                var numberPart = nextNumber.ToString($"D{numberLength}");
                result = $"{prefix}{numberPart}";

                await transaction.CommitAsync(cancellationToken);
            });

            return result!;
        }
        public async Task SaveGenerateCodeAsync(string prefix, CancellationToken cancellationToken = default)
        {
            var strategy = _db.Database.CreateExecutionStrategy();

            await strategy.ExecuteAsync(async () =>
            {
                // Wrap in transaction
                await using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken);

                // Get or create the sequence for this prefix
                var sequence = await _db.Set<CodeSequence>()
                    .FirstOrDefaultAsync(s => s.Prefix == prefix, cancellationToken);

                if (sequence == null)
                {
                    sequence = new CodeSequence
                    {
                        Prefix = prefix,
                        LastNumber = 0
                    };
                    _db.Add(sequence);
                    await _db.SaveChangesAsync(cancellationToken); // save new sequence
                }

                // Increment the sequence
                sequence.LastNumber++;

                await _db.SaveChangesAsync(cancellationToken);

                await transaction.CommitAsync(cancellationToken);
            });
        }
    }
}
