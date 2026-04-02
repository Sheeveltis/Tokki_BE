using Tokki.Application.UseCases.Roadmap.DTOs;

namespace Tokki.Application.IServices
{
    public interface IRoadmapProgressService
    {
        void Set(string jobId, RoadmapProgressState state);
        RoadmapProgressState? Get(string jobId);
        void Remove(string jobId);
    }
}