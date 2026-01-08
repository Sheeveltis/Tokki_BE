using Microsoft.Extensions.Logging;
using Moq;
using Tokki.Application.IRepositories;
using Tokki.Application.IServices;

namespace Tokki.UnitTests.Common.Bases
{
    public class ExamTemplateTestBase
    {
        protected readonly Mock<IExamTemplateRepository> _mockExamTemplateRepo;
        protected readonly Mock<ITemplatePartRepository> _mockTemplatePartRepo;
        protected readonly Mock<IQuestionTypeRepository> _mockQuestionTypeRepo;
        protected readonly Mock<IIdGeneratorService> _mockIdGenerator;
        protected readonly Mock<ILogger<object>> _mockLogger; 
        public ExamTemplateTestBase()
        {
            _mockExamTemplateRepo = new Mock<IExamTemplateRepository>();
            _mockTemplatePartRepo = new Mock<ITemplatePartRepository>();
            _mockQuestionTypeRepo = new Mock<IQuestionTypeRepository>();
            _mockIdGenerator = new Mock<IIdGeneratorService>();
            _mockLogger = new Mock<ILogger<object>>();
        }
    }
}