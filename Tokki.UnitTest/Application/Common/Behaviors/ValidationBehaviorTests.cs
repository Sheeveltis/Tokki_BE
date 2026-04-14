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
using Tokki.Application.Common.Behaviors;
using Tokki.UnitTest.Utilities;
using Xunit;

namespace Tokki.UnitTest.Application.Common.Behaviors
{
    public class TestRequest : IRequest<string> { }

    public class ValidationBehaviorTests
    {
        private ValidationBehavior<TestRequest, string> CreateBehavior(IEnumerable<IValidator<TestRequest>> validators)
        {
            return new ValidationBehavior<TestRequest, string>(validators);
        }

        // TC-CB-VB-01 | N | No Validators -> Next Call
        [Fact]
        public async Task Handle_WithNoValidators_ShouldInvokeNext()
        {
            var behavior = CreateBehavior(Enumerable.Empty<IValidator<TestRequest>>());
            var nextMock = new Mock<RequestHandlerDelegate<string>>();
            nextMock.Setup(n => n(It.IsAny<CancellationToken>())).ReturnsAsync("success");

            var result = await behavior.Handle(new TestRequest(), nextMock.Object, CancellationToken.None);

            result.Should().Be("success");
            nextMock.Verify(n => n(It.IsAny<CancellationToken>()), Times.Once);

            QACollector.LogTestCase("Common - Behaviors", new TestCaseDetail
            {
                FunctionGroup = "ValidationBehavior",
                TestCaseID = "TC-CB-VB-01",
                Description = "Pipeline with empty validators skips verification",
                ExpectedResult = "next() is returned cleanly",
                StatusRound1 = "Passed",
                TestCaseType = "N",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "validators.Any() == false" }
            });
        }

        // TC-CB-VB-02 | N | Has Validators But All Pass
        [Fact]
        public async Task Handle_ValidatorsPass_ShouldInvokeNext()
        {
            var validatorMock = new Mock<IValidator<TestRequest>>();
            validatorMock.Setup(x => x.ValidateAsync(It.IsAny<ValidationContext<TestRequest>>(), It.IsAny<CancellationToken>()))
                         .ReturnsAsync(new ValidationResult());
            
            var behavior = CreateBehavior(new[] { validatorMock.Object });
            var nextMock = new Mock<RequestHandlerDelegate<string>>();
            nextMock.Setup(n => n(It.IsAny<CancellationToken>())).ReturnsAsync("pass");

            var result = await behavior.Handle(new TestRequest(), nextMock.Object, CancellationToken.None);

            result.Should().Be("pass");
            nextMock.Verify(n => n(It.IsAny<CancellationToken>()), Times.Once);

            QACollector.LogTestCase("Common - Behaviors", new TestCaseDetail
            {
                FunctionGroup = "ValidationBehavior",
                TestCaseID = "TC-CB-VB-02",
                Description = "Success validators proceeds pipeline",
                ExpectedResult = "next() is invoked",
                StatusRound1 = "Passed",
                TestCaseType = "N",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "ValidationResult errors is empty" }
            });
        }

        // TC-CB-VB-03 | A | One Validator Fails -> Throws FluentValidation Exception
        [Fact]
        public async Task Handle_ValidatorFails_ShouldThrowValidationException()
        {
            var validatorMock = new Mock<IValidator<TestRequest>>();
            var validationFailures = new List<ValidationFailure> { new ValidationFailure("Prop", "Error1") };
            
            validatorMock.Setup(x => x.ValidateAsync(It.IsAny<ValidationContext<TestRequest>>(), It.IsAny<CancellationToken>()))
                         .ReturnsAsync(new ValidationResult(validationFailures));
            
            var behavior = CreateBehavior(new[] { validatorMock.Object });
            var nextMock = new Mock<RequestHandlerDelegate<string>>();

            Func<Task> act = async () => await behavior.Handle(new TestRequest(), nextMock.Object, CancellationToken.None);

            await act.Should().ThrowAsync<ValidationException>()
                     .Where(e => e.Errors.Count() == 1 && e.Errors.First().ErrorMessage == "Error1");
            
            nextMock.Verify(n => n(It.IsAny<CancellationToken>()), Times.Never);

            QACollector.LogTestCase("Common - Behaviors", new TestCaseDetail
            {
                FunctionGroup = "ValidationBehavior",
                TestCaseID = "TC-CB-VB-03",
                Description = "Failure halts pipeline cleanly through standard Exception throw",
                ExpectedResult = "ValidationException Thrown",
                StatusRound1 = "Passed",
                TestCaseType = "A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Failures count > 0" }
            });
        }

        // TC-CB-VB-04 | A | Multiple Validators Fail -> Throws Exception with Combined Errors
        [Fact]
        public async Task Handle_MultipleValidatorsFail_ShouldCombineErrors()
        {
            var val1 = new Mock<IValidator<TestRequest>>();
            val1.Setup(x => x.ValidateAsync(It.IsAny<ValidationContext<TestRequest>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ValidationResult(new[] { new ValidationFailure("A", "ErrA") }));
                
            var val2 = new Mock<IValidator<TestRequest>>();
            val2.Setup(x => x.ValidateAsync(It.IsAny<ValidationContext<TestRequest>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ValidationResult(new[] { new ValidationFailure("B", "ErrB") }));

            var behavior = CreateBehavior(new[] { val1.Object, val2.Object });
            var nextMock = new Mock<RequestHandlerDelegate<string>>();

            Func<Task> act = async () => await behavior.Handle(new TestRequest(), nextMock.Object, CancellationToken.None);

            var exception = await act.Should().ThrowAsync<ValidationException>();
            exception.Which.Errors.Count().Should().Be(2);

            QACollector.LogTestCase("Common - Behaviors", new TestCaseDetail
            {
                FunctionGroup = "ValidationBehavior",
                TestCaseID = "TC-CB-VB-04",
                Description = "Multiple validators combine errors smoothly across asynchronous yields",
                ExpectedResult = "ValidationException with matched error count 2",
                StatusRound1 = "Passed",
                TestCaseType = "A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Multiple fail triggers" }
            });
        }

        // TC-CB-VB-05 | B | Context Request Equality
        [Fact]
        public async Task Handle_VerifyContextMapping_EnsuresCorrectContextUsage()
        {
            var currentReq = new TestRequest();
            var validatorMock = new Mock<IValidator<TestRequest>>();
            
            validatorMock.Setup(x => x.ValidateAsync(It.Is<ValidationContext<TestRequest>>(c => c.InstanceToValidate == currentReq), It.IsAny<CancellationToken>()))
                         .ReturnsAsync(new ValidationResult())
                         .Verifiable();
            
            var behavior = CreateBehavior(new[] { validatorMock.Object });
            var nextMock = new Mock<RequestHandlerDelegate<string>>();
            
            await behavior.Handle(currentReq, nextMock.Object, CancellationToken.None);

            validatorMock.Verify();

            QACollector.LogTestCase("Common - Behaviors", new TestCaseDetail
            {
                FunctionGroup = "ValidationBehavior",
                TestCaseID = "TC-CB-VB-05",
                Description = "Instance context mapped synchronously to validators",
                ExpectedResult = "Verify exact instance mapping",
                StatusRound1 = "Passed",
                TestCaseType = "B",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "ValidationContext mapped" }
            });
        }

        // TC-CB-VB-06 | B | Cancellation token passed optimally
        [Fact]
        public async Task Handle_WithCancellationContext_ShouldThrowOrPassToken()
        {
            var cancellationToken = new CancellationToken(true);
            var validatorMock = new Mock<IValidator<TestRequest>>();
            
            validatorMock.Setup(x => x.ValidateAsync(It.IsAny<ValidationContext<TestRequest>>(), cancellationToken))
                         .ReturnsAsync(new ValidationResult())
                         .Verifiable();
            
            var behavior = CreateBehavior(new[] { validatorMock.Object });
            var nextMock = new Mock<RequestHandlerDelegate<string>>();
            
            await behavior.Handle(new TestRequest(), nextMock.Object, cancellationToken);
            validatorMock.Verify();

            QACollector.LogTestCase("Common - Behaviors", new TestCaseDetail
            {
                FunctionGroup = "ValidationBehavior",
                TestCaseID = "TC-CB-VB-06",
                Description = "Ensures pipeline accurately supports cascading cancellations",
                ExpectedResult = "CancellationToken matching",
                StatusRound1 = "Passed",
                TestCaseType = "B",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "ValidateAsync Token Forwarding" }
            });
        }
    }
}
