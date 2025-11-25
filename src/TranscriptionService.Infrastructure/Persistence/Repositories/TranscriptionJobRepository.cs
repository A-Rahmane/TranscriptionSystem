using Microsoft.EntityFrameworkCore;
using TranscriptionService.Domain.Entities;
using TranscriptionService.Domain.Enums;
using TranscriptionService.Domain.Interfaces;

namespace TranscriptionService.Infrastructure.Persistence.Repositories;

public class TranscriptionJobRepository : ITranscriptionJobRepository
{
    private readonly ApplicationDbContext _context;

    public TranscriptionJobRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<TranscriptionJob> AddAsync(
        TranscriptionJob job,
        CancellationToken cancellationToken = default)
    {
        await _context.TranscriptionJobs.AddAsync(job, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        return job;
    }

    public async Task<TranscriptionJob?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return await _context.TranscriptionJobs
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task UpdateAsync(
        TranscriptionJob job,
        CancellationToken cancellationToken = default)
    {
        _context.TranscriptionJobs.Update(job);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<IEnumerable<TranscriptionJob>> GetJobsByStatusAsync(
        JobStatus status,
        CancellationToken cancellationToken = default)
    {
        return await _context.TranscriptionJobs
            .Where(x => x.Status == status)
            .OrderBy(x => x.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<TranscriptionJob>> GetQueuedJobsAsync(
        int limit = 10,
        CancellationToken cancellationToken = default)
    {
        return await _context.TranscriptionJobs
            .Where(x => x.Status == JobStatus.Queued)
            .OrderBy(x => x.CreatedAt)
            .Take(limit)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<TranscriptionJob>> GetJobsForRetryAsync(
        int limit = 10,
        CancellationToken cancellationToken = default)
    {
        return await _context.TranscriptionJobs
            .Where(x => x.Status == JobStatus.Retrying)
            .OrderBy(x => x.CreatedAt)
            .Take(limit)
            .ToListAsync(cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var job = await GetByIdAsync(id, cancellationToken);
        if (job != null)
        {
            _context.TranscriptionJobs.Remove(job);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task<int> GetCountByStatusAsync(
        JobStatus status,
        CancellationToken cancellationToken = default)
    {
        return await _context.TranscriptionJobs
            .CountAsync(x => x.Status == status, cancellationToken);
    }

    public async Task<IEnumerable<TranscriptionJob>> GetJobsByDateRangeAsync(
        DateTime from,
        DateTime to,
        CancellationToken cancellationToken = default)
    {
        return await _context.TranscriptionJobs
            .Where(x => x.CreatedAt >= from && x.CreatedAt <= to)
            .OrderBy(x => x.CreatedAt)
            .ToListAsync(cancellationToken);
    }
}
