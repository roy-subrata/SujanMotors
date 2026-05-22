public interface ICodeGenerateService
{
    /// <summary>
    /// Atomically increments the counter for <paramref name="prefix"/> and
    /// returns the new code. Call this exactly once, inside the entity's save
    /// transaction, and return the code to the caller in the response.
    /// </summary>
    Task<string> GenerateAsync(string prefix, CancellationToken cancellationToken = default, int minDigits = 3);

    /// <summary>
    /// Returns what the NEXT code would look like without consuming a number.
    /// Safe for UI "preview" use — repeated calls return the same value.
    /// </summary>
    Task<string> PeekAsync(string prefix, CancellationToken cancellationToken = default, int minDigits = 3);

    /// <summary>Obsolete — no-op. GenerateAsync already persists atomically.</summary>
    [Obsolete("No-op. Remove calls to this method — GenerateAsync persists the counter atomically.")]
    Task SaveGenerateCodeAsync(string prefix, CancellationToken cancellationToken = default);
}

