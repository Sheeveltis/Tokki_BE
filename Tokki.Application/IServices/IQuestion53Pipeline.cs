// Application/IServices/IQuestion53Pipeline.cs
using System.Text.Json;
using Tokki.Application.UseCases.TopikWriting.Question53.DTOs;

namespace Tokki.Application.IServices
{
    public interface IQuestion53Pipeline
    {
        Task<(JsonElement Feedback, int Score)> SolveAsync(
            Question53RequestDto request, CancellationToken ct);
    }
}