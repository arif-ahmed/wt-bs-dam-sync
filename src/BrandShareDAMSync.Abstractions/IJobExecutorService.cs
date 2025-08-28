namespace BrandshareDamSync.Abstractions;

/// <summary>
/// Defines the contract for job executors that handle different types of synchronization operations.
/// </summary>
/// <remarks>
/// Job executors implement specific synchronization strategies such as one-way upload,
/// one-way download, bi-directional sync, and cleanup operations. Each executor is responsible
/// for processing a single sync job according to its configured strategy.
/// </remarks>
public interface IJobExecutorService
{
    /// <summary>
    /// Executes a synchronization job asynchronously.
    /// </summary>
    /// <param name="syncJobInfo">A tuple containing the sync ID, tenant ID, and job ID for the operation.</param>
    /// <param name="ct">A cancellation token to cancel the operation if needed.</param>
    /// <returns>A task representing the asynchronous job execution operation.</returns>
    /// <remarks>
    /// This method should handle all aspects of the synchronization process including:
    /// - Retrieving job configuration
    /// - Processing files according to the sync strategy
    /// - Updating sync metadata and tracking information
    /// - Handling errors and retries
    /// - Reporting progress and completion status
    /// </remarks>
    /// <exception cref="ArgumentException">Thrown when the sync job information is invalid.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the job cannot be executed due to its current state.</exception>
    /// <exception cref="OperationCanceledException">Thrown when the operation is cancelled via the cancellation token.</exception>
    Task ExecuteJobAsync((string syncId, string tenantId, string jobId) syncJobInfo, CancellationToken ct = default);
    
    //Task CancelJobAsync(string syncId, CancellationToken ct = default);   
}
