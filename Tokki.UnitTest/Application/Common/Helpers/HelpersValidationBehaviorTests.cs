using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using MediatR;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.Common.Helpers;
using Tokki.Application.Common.Models;
using Tokki.UnitTest.Utilities;
using Xunit;

namespace Tokki.UnitTest.Application.Common.Helpers
{
    public class TestRequestOpRoute : IRequest<OperationResult<string>> { }
    public class TestRequestNonOpRoute : IRequest<string> { }

    public class HelpersValidationBehaviorTests
    {
        private ValidationBehavior<TReq, TRes> CreateBehavior<TReq, TRes>(IEnumerable<IValidator<TReq>> validators)
            where TReq : IRequest<TRes>
        {
            return new ValidationBehavior<TReq, TRes>(validators);
        }

        // ValidationBehavior_01 | N | No Validators -> Invokes next() natively
        [Fact]
        public async Task Handle_WithNoValidators_ShouldInvokeNext()
        {
            var behavior = CreateBehavior<TestRequestOpRoute, OperationResult<string>>(Enumerable.Empty<IValidator<TestRequestOpRoute>>());
            var nextMock = new Mock<RequestHandlerDelegate<OperationResult<string>>>();
            
            var expected = OperationResult<string>.Success("success", 200, "success");
            nextMock.Setup(n => n(It.IsAny<CancellationToken>())).ReturnsAsync(expected);

            var result = await behavior.Handle(new TestRequestOpRoute(), nextMock.Object, CancellationToken.None);

            result.Should().Be(expected);
            nextMock.Verify(n => n(It.IsAny<CancellationToken>()), Times.Once);

            QACollector.LogTestCase("Common - Helpers", new TestCaseDetail
            {
                FunctionGroup = "ValidationBehavior",
                TestCaseID = "ValidationBehavior_01",
                Description = "No validators skips cleanly",
                ExpectedResult = "next() result accurately returned",
                StatusRound1 = "Passed",
                TestCaseType = "N",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "_validators.Any() == false" }
            });
        }

        // ValidationBehavior_02 | N | Has Validators But Pass
        [Fact]
        public async Task Handle_ValidatorsPass_ShouldInvokeNext()
        {
            var valMock = new Mock<IValidator<TestRequestOpRoute>>();
            valMock.Setup(x => x.ValidateAsync(It.IsAny<ValidationContext<TestRequestOpRoute>>(), It.IsAny<CancellationToken>()))
                   .ReturnsAsync(new ValidationResult());
            
            var behavior = CreateBehavior<TestRequestOpRoute, OperationResult<string>>(new[] { valMock.Object });
            var nextMock = new Mock<RequestHandlerDelegate<OperationResult<string>>>();
            var expected = OperationResult<string>.Success("success", 200, "success");
            
            nextMock.Setup(n => n(It.IsAny<CancellationToken>())).ReturnsAsync(expected);

            var result = await behavior.Handle(new TestRequestOpRoute(), nextMock.Object, CancellationToken.None);

            result.Should().Be(expected);

            QACollector.LogTestCase("Common - Helpers", new TestCaseDetail
            {
                FunctionGroup = "ValidationBehavior",
                TestCaseID = "ValidationBehavior_02",
                Description = "Passed validation smoothly invokes RequestHandlerDelegate",
                ExpectedResult = "Success execution",
                StatusRound1 = "Passed",
                TestCaseType = "N",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Result is completely valid" }
            });
        }

        // ValidationBehavior_03 | A | Action Validates With Failure -> Maps smoothly into OperationResult<T> 
        [Fact]
        public async Task Handle_ValidatorFails_ShouldReturnOperationResultFailure()
        {
            var valMock = new Mock<IValidator<TestRequestOpRoute>>();
            var failures = new List<ValidationFailure> { new ValidationFailure("Prop", "Error1") };
            valMock.Setup(x => x.ValidateAsync(It.IsAny<ValidationContext<TestRequestOpRoute>>(), It.IsAny<CancellationToken>()))
                   .ReturnsAsync(new ValidationResult(failures));
            
            var behavior = CreateBehavior<TestRequestOpRoute, OperationResult<string>>(new[] { valMock.Object });
            var nextMock = new Mock<RequestHandlerDelegate<OperationResult<string>>>();

            var result = await behavior.Handle(new TestRequestOpRoute(), nextMock.Object, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(400);
            result.Errors.Should().HaveCount(1);
            result.Errors.First().Code.Should().Be("Validation.Error");
            result.Errors.First().Description.Should().Be("Error1");

            nextMock.Verify(n => n(It.IsAny<CancellationToken>()), Times.Never);

            QACollector.LogTestCase("Common - Helpers", new TestCaseDetail
            {
                FunctionGroup = "ValidationBehavior",
                TestCaseID = "ValidationBehavior_03",
                Description = "Failures convert perfectly into OperationResult wrapping without throwing",
                ExpectedResult = "StatusCode 400 + Validation.Error mapping",
                StatusRound1 = "Passed",
                TestCaseType = "A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Type resolves gracefully" }
            });
        }

        // ValidationBehavior_04 | A | Fails formatting joins string arrays successfully"Error1; Error2"
        [Fact]
        public async Task Handle_MultipleFailures_ShouldFormatSemicolonJoinedString()
        {
            var valMock = new Mock<IValidator<TestRequestOpRoute>>();
            var failures = new List<ValidationFailure> 
            { 
                new ValidationFailure("Prop", "Error1"),
                new ValidationFailure("Prop2", "Error2") 
            };
            valMock.Setup(x => x.ValidateAsync(It.IsAny<ValidationContext<TestRequestOpRoute>>(), It.IsAny<CancellationToken>()))
                   .ReturnsAsync(new ValidationResult(failures));
            
            var behavior = CreateBehavior<TestRequestOpRoute, OperationResult<string>>(new[] { valMock.Object });
            var nextMock = new Mock<RequestHandlerDelegate<OperationResult<string>>>();

            var result = await behavior.Handle(new TestRequestOpRoute(), nextMock.Object, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.Message.Should().Be("Error1; Error2"); // Verifying string.Join logic
            result.Errors.Should().HaveCount(2);

            QACollector.LogTestCase("Common - Helpers", new TestCaseDetail
            {
                FunctionGroup = "ValidationBehavior",
                TestCaseID = "ValidationBehavior_04",
                Description = "Join messages combined perfectly",
                ExpectedResult = "Error1; Error2",
                StatusRound1 = "Passed",
                TestCaseType = "A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Join strings combined" }
            });
        }

        // ValidationBehavior_05 | A | Non-OperationResult TResponse bypasses gracefully back to next() essentially nullifying
        [Fact]
        public async Task Handle_NonOperationResult_SwallowsValidationAndReturnsNext()
        {
            var valMock = new Mock<IValidator<TestRequestNonOpRoute>>();
            var failures = new List<ValidationFailure> { new ValidationFailure("Prop", "Error1") };
            valMock.Setup(x => x.ValidateAsync(It.IsAny<ValidationContext<TestRequestNonOpRoute>>(), It.IsAny<CancellationToken>()))
                   .ReturnsAsync(new ValidationResult(failures));
            
            var behavior = CreateBehavior<TestRequestNonOpRoute, string>(new[] { valMock.Object });
            var nextMock = new Mock<RequestHandlerDelegate<string>>();
            
            nextMock.Setup(n => n(It.IsAny<CancellationToken>())).ReturnsAsync("BypassedValue"); // Verifies next was hit

            var result = await behavior.Handle(new TestRequestNonOpRoute(), nextMock.Object, CancellationToken.None);

            result.Should().Be("BypassedValue");

            QACollector.LogTestCase("Common - Helpers", new TestCaseDetail
            {
                FunctionGroup = "ValidationBehavior",
                TestCaseID = "ValidationBehavior_05",
                Description = "Legacy endpoints swallowed validation logic securely as implemented",
                ExpectedResult = "next() is returned anyway",
                StatusRound1 = "Passed",
                TestCaseType = "A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "TResponse check yields false" }
            });
        }

        // ValidationBehavior_06 | B | Context Request Equality
        [Fact]
        public async Task Handle_VerifyContextMapping_EnsuresCorrectContextUsage()
        {
            var currentReq = new TestRequestOpRoute();
            var validatorMock = new Mock<IValidator<TestRequestOpRoute>>();
            
            validatorMock.Setup(x => x.ValidateAsync(It.Is<ValidationContext<TestRequestOpRoute>>(c => c.InstanceToValidate == currentReq), It.IsAny<CancellationToken>()))
                         .ReturnsAsync(new ValidationResult())
                         .Verifiable();
            
            var behavior = CreateBehavior<TestRequestOpRoute, OperationResult<string>>(new[] { validatorMock.Object });
            var nextMock = new Mock<RequestHandlerDelegate<OperationResult<string>>>();
            
            await behavior.Handle(currentReq, nextMock.Object, CancellationToken.None);

            validatorMock.Verify();

            QACollector.LogTestCase("Common - Helpers", new TestCaseDetail
            {
                FunctionGroup = "ValidationBehavior",
                TestCaseID = "ValidationBehavior_06",
                Description = "Instance context strictly matches input request data",
                ExpectedResult = "Verify exact instance",
                StatusRound1 = "Passed",
                TestCaseType = "B",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "ValidationContext mapped" }
            });
        }
    }
}
