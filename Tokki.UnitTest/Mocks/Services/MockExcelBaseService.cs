using Microsoft.AspNetCore.Http;
using Moq;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.Common.ExcelCore;
using Tokki.Application.UseCases.Excel.DTOs;

namespace Tokki.UnitTest.Mocks.Services
{
    public static class MockExcelBaseService
    {
        public static Mock<IExcelBaseService> GetMock()
        {
            var mock = new Mock<IExcelBaseService>();

            mock.Setup(x => x.GenerateTemplateAsync<AccountExcelDTO>(It.IsAny<string>()))
                .ReturnsAsync(new byte[] { 0x01 });

            mock.Setup(x => x.ExportAsync(It.IsAny<IEnumerable<AccountExcelDTO>>(), It.IsAny<string>(), It.IsAny<List<string>>()))
                .ReturnsAsync(new byte[] { 0x02 });

            return mock;
        }

        public static ExcelImportResult<T> CreateFakeImportResult<T>(List<T> dataItems)
        {
            var result = new ExcelImportResult<T>();
            for (int i = 0; i < dataItems.Count; i++)
            {
                result.SuccessItems.Add(new ExcelSuccessDetail<T>
                {
                    RowIndex = i + 2,
                    Data = dataItems[i]
                });
            }
            return result;
        }
    }
}