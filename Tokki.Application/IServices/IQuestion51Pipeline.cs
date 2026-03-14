// Application/IServices/IQuestion51Pipeline.cs
using System.Text.Json;
using Tokki.Application.UseCases.TopikWriting.DTOs;
using Tokki.Application.UseCases.TopikWriting.Question51.DTOs;

namespace Tokki.Application.IServices
{
    public interface IQuestion51Pipeline
    {
        Task<(JsonElement Feedback, int Score)> SolveAsync(
            Question51RequestDto request, CancellationToken ct);
    }
}