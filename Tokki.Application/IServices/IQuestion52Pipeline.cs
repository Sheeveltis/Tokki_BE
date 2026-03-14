// Application/IServices/IQuestion52Pipeline.cs
using System.Text.Json;
using Tokki.Application.UseCases.TopikWriting.Question52.DTOs;

namespace Tokki.Application.IServices
{
    public interface IQuestion52Pipeline
    {
        Task<(JsonElement Feedback, int Score)> SolveAsync(
            Question52RequestDto request, CancellationToken ct);
    }
}