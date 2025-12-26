using MediatR;
using System.Text.Json.Serialization;
using Tokki.Application.Common.Models;
using Tokki.Application.UseCases.VocabSpacedRepetition.DTOs;

public class GetDueReviewsQuery : IRequest<OperationResult<List<ReviewItemDTO>>>
{
    [JsonIgnore]
    public string UserId { get; set; }
    public int Limit { get; set; } = 100;
}