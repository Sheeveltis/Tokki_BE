using MediatR;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Application.IServices;
using Tokki.Application.UseCases.Excel.DTOs;

namespace Tokki.Application.UseCases.Titles.Queries.ExportTitles
{
    public class ExportTitlesQueryHandler : IRequestHandler<ExportTitlesQuery, OperationResult<byte[]>>
    {
        private readonly ITitleRepository _titleRepository;
        private readonly IExcelService _excelService;

        public ExportTitlesQueryHandler(ITitleRepository titleRepository, IExcelService excelService)
        {
            _titleRepository = titleRepository;
            _excelService = excelService;
        }

        public async Task<OperationResult<byte[]>> Handle(ExportTitlesQuery request, CancellationToken cancellationToken)
        {
            var titles = await _titleRepository.GetAllTitlesAsync(includeInactive: true);

            var excelDtos = titles.Select(t => new TitleExcelDTO
            {
                Name = t.Name,
                Description = t.Description,
                ColorHex = t.ColorHex,
                IconUrl = t.IconUrl,
                RequirementType = t.RequirementType.ToString(),
                RequirementQuantity = t.RequirementQuantity,
                Status = t.Status.ToString()
            }).ToList();

            var excelData = await _excelService.ExportTitlesToExcelAsync(excelDtos, "Bảng danh hiệu");

            return OperationResult<byte[]>.Success(excelData, 200, "Trích xuất danh sách danh hiệu thành công.");
        }
    }
}
