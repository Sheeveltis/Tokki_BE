// Application/IServices/IQuestion54Pipeline.cs
using System.Text.Json;
using Tokki.Application.UseCases.TopikWriting.Question54.DTOs;

namespace Tokki.Application.IServices
{
    public interface IQuestion54Pipeline
    {
        Task<(JsonElement Feedback, int Score)> SolveAsync(
            Question54RequestDto request, CancellationToken ct);
    }
}