using MediatR;
using Microsoft.AspNetCore.Http;
using Tokki.Application.Common.Models;
using Tokki.Application.UseCases.Excel.DTOs;

namespace Tokki.Application.UseCases.Alphabet.Commands.ImportAlphabetFromExcel
{
    public record ImportAlphabetFromExcelCommand(IFormFile File) : IRequest<OperationResult<AlphabetImportResponse>>;
}
