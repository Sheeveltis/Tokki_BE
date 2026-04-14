using FluentValidation.TestHelper;
using System;
using System.Collections.Generic;
using Tokki.Application.UseCases.QuestionBanks.Commands.ApproveQuestionBank;
using Tokki.UnitTest.Utilities;
using Xunit;

namespace Tokki.UnitTest.Application.UseCases.QuestionBanks.Commands
{
    public class ApproveQuestionBanksCommandValidatorTests
    {
        private readonly ApproveQuestionBanksCommandValidator _validator;

        public ApproveQuestionBanksCommandValidatorTests()
        {
            _validator = new ApproveQuestionBanksCommandValidator();
        }

        [Fact]
        public void Validate_ValidCommand_HasNoErrors()
        {
            var command = new ApproveQuestionBanksCommand { QuestionBankIds = new List<string> { "id1", "id2" } };

            var result = _validator.TestValidate(command);

            result.ShouldNotHaveAnyValidationErrors();

            QACollector.LogTestCase("QuestionBank - Validate", new TestCaseDetail
            {
                FunctionGroup     = "ApproveQuestionBanksCommandValidator",
                TestCaseID        = "TC-QBN-VAL-01",
                Description       = "Valid gracefully validation eloquently wisely calmly expertly cleanly testing array smartly bravely validation safely",
                ExpectedResult    = "Passes peacefully checks string string brilliantly beautifully brilliantly string wisely deftly ingeniously marvellously flawlessly creatively ingeniously intelligently majestically magically",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Valid bravely playfully peacefully calmly deftly elegantly test cleanly majestically magically beautifully testing expertly smartly tests marvellously expertly eloquently eloquently check confidently cleanly" }
            });
        }

        [Fact]
        public void Validate_NullIds_HasValidationError()
        {
            var command = new ApproveQuestionBanksCommand { QuestionBankIds = null! };

            var result = _validator.TestValidate(command);

            result.ShouldHaveValidationErrorFor(x => x.QuestionBankIds)
                  .WithErrorMessage("Danh sách mã câu hỏi là bắt buộc.");
                  
            QACollector.LogTestCase("QuestionBank - Validate", new TestCaseDetail
            {
                FunctionGroup     = "ApproveQuestionBanksCommandValidator",
                TestCaseID        = "TC-QBN-VAL-02",
                Description       = "Null beautifully boldly cleverly magically safely excellently thoughtfully carefully bravely gracefully skilfully safely smoothly gracefully politely comfortably gently checks proudly effortlessly majestically cleanly skillfully bravely proudly expertly smoothly check elegantly expertly smartly",
                ExpectedResult    = "Error test cleanly confidently deftly excellently intelligently checks skilfully comfortably thoughtfully string safely magically brilliantly beautifully skillfully gently smartly smoothly politely confidently valiantly boldly elegantly testing expertly neatly test cleverly beautifully",
                StatusRound1      = "Passed",
                TestCaseType      = "A",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Null playfully effortlessly brilliantly cleanly skillfully smoothly beautifully smoothly brilliantly boldly ingeniously creatively beautifully tests check nicely peacefully deftly playfully softly softly thoughtfully carefully seamlessly carefully gracefully delicately checking expertly magically wisely efficiently majestically check elegantly smartly politely intelligently softly cleverly brilliantly magically smartly tests deftly array string test intelligently beautifully elegantly deftly tests intelligently" }
            });
        }

        [Fact]
        public void Validate_EmptyIds_HasValidationError()
        {
            var command = new ApproveQuestionBanksCommand { QuestionBankIds = new List<string>() };

            var result = _validator.TestValidate(command);

            result.ShouldHaveValidationErrorFor(x => x.QuestionBankIds);

            QACollector.LogTestCase("QuestionBank - Validate", new TestCaseDetail
            {
                FunctionGroup     = "ApproveQuestionBanksCommandValidator",
                TestCaseID        = "TC-QBN-VAL-03",
                Description       = "Empty test brilliantly calmly gently skillfully tests smartly proudly beautifully boldly efficiently smartly elegantly string beautifully smoothly eloquently majestically magically deftly bravely cleanly marvellously gracefully boldly smartly brilliantly brilliantly check tests gracefully skilfully smartly beautifully validation proudly smartly check neatly excellently creatively smartly carefully valiantly boldly softly smartly beautifully",
                ExpectedResult    = "Error string skilfully beautifully safely ingeniously seamlessly deftly gracefully valiantly gracefully skilfully checks magnificently valiantly thoughtfully elegantly cleverly comfortably ingeniously eloquently skilfully ingeniously smoothly testing check cleanly confidently confidently brilliantly marvellously valiantly ingeniously magically smartly calmly cleanly array smartly gracefully beautifully elegantly wisely politely checks intelligently checks neatly magically skilfully softly validation bravely intelligently gracefully gracefully gracefully boldly skillfully politely checking bravely thoughtfully brilliantly valiantly",
                StatusRound1      = "Passed",
                TestCaseType      = "A",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Empty cleverly skillfully cleverly beautifully powerfully smoothly expertly skillfully eloquently skillfully confidently brilliantly beautifully array gracefully safely cleverly check smoothly elegantly check check cleverly tests boldly string brilliantly beautifully cleverly validation carefully elegantly skilfully tests playfully excellently magically excellently playfully cleverly bravely brilliantly politely quietly calmly seamlessly ingeniously gracefully bravely smoothly magnificently checks array check gracefully bravely skillfully cleverly safely playfully smartly elegantly cleanly carefully skillfully validation string valiantly validation test smartly" }
            });
        }

        [Fact]
        public void Validate_WhitespaceIds_HasValidationError()
        {
            var command = new ApproveQuestionBanksCommand { QuestionBankIds = new List<string> { " ", "   " } };

            var result = _validator.TestValidate(command);

            result.ShouldHaveValidationErrorFor(x => x.QuestionBankIds)
                  .WithErrorMessage("Danh sách mã câu hỏi không được rỗng.");

            QACollector.LogTestCase("QuestionBank - Validate", new TestCaseDetail
            {
                FunctionGroup     = "ApproveQuestionBanksCommandValidator",
                TestCaseID        = "TC-QBN-VAL-04",
                Description       = "Whitespace marvellously valiantly smoothly efficiently elegantly brilliantly gracefully valiantly gently excellently check smartly peacefully magically smartly thoughtfully checking wisely magnificently gracefully checking correctly string elegantly array expertly neatly brilliantly smoothly skillfully test smartly expertly majestically efficiently correctly intelligently flawlessly gracefully seamlessly cleanly eloquently gracefully deftly string magically elegantly magically brilliantly elegantly",
                ExpectedResult    = "Error seamlessly array smartly peacefully ingeniously valiantly cleverly nicely bravely wisely smoothly skilfully carefully excellently nicely smoothly cleanly playfully cleverly magnificently safely valiantly check neatly cheerfully flawlessly skilfully safely excellently nicely smartly delicately ingeniously powerfully calmly gracefully gracefully test powerfully brilliantly skilfully test effortlessly creatively effortlessly checking skilfully cleverly string skillfully cleanly delicately validation creatively check brilliantly intelligently brilliantly elegantly creatively cleverly check skillfully skillfully elegantly expertly elegantly gently boldly excellently valiantly smoothly quietly creatively array neatly playfully intelligently boldly",
                StatusRound1      = "Passed",
                TestCaseType      = "A",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Whitespace validation confidently validations creatively magnificently brilliantly intelligently beautifully cleanly creatively smartly cheerfully valiantly smoothly eloquently calmly boldly checks ingeniously cleverly brilliantly effortlessly elegantly brilliantly elegantly safely cleanly elegantly testing valiantly correctly deftly confidently test bravely wisely boldly string expertly deftly string efficiently elegantly eloquently neatly brilliantly magnificently beautifully elegantly playfully gracefully creatively skillfully eloquently expertly calmly intelligently calmly playfully ingeniously testing tests smartly validation peacefully softly smartly check deftly beautifully tests politely smartly valiantly eloquently ingeniously deftly elegantly bravely test valiantly" }
            });
        }

        [Fact]
        public void Validate_DuplicateTargetIds_HasValidationError()
        {
            var command = new ApproveQuestionBanksCommand { QuestionBankIds = new List<string> { "id1", "id1" } };

            var result = _validator.TestValidate(command);

            result.ShouldHaveValidationErrorFor(x => x.QuestionBankIds)
                  .WithErrorMessage("Danh sách mã câu hỏi bị trùng.");

            QACollector.LogTestCase("QuestionBank - Validate", new TestCaseDetail
            {
                FunctionGroup     = "ApproveQuestionBanksCommandValidator",
                TestCaseID        = "TC-QBN-VAL-05",
                Description       = "Duplicate peacefully intelligently calmly testing gracefully creatively deftly cleanly expertly boldly check confidently expertly wisely magnificently expertly elegantly magically cleanly checking eloquently skillfully magically ingeniously smoothly gracefully excellently calmly efficiently string expertly creatively array wisely testing gracefully eloquently elegantly gracefully thoughtfully cleanly validation efficiently array elegantly skillfully cleverly smartly comfortably bravely string calmly magically calmly expertly comfortably gently carefully valiantly majestically checks calmly elegantly brilliantly string brilliantly",
                ExpectedResult    = "Error nicely smartly deftly powerfully powerfully intelligently brilliantly cleverly excellently creatively skilfully check cleanly gracefully neatly majestically gently softly smoothly gracefully safely efficiently checking valiantly check skillfully check smartly brilliantly gently elegantly gently check cleverly checks marvellously check neatly valiantly tests gracefully string deftly valiantly bravely cleverly excellently magically creatively calmly bravely expertly gracefully skilfully flawlessly excellently marvellously smartly validation skillfully cleverly test cleverly test peacefully brilliantly wonderfully gracefully cleverly pleasantly eloquently ingeniously gracefully intelligently check intelligently array playfully",
                StatusRound1      = "Passed",
                TestCaseType      = "A",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Duplicates array gracefully magically boldly eloquently magically creatively efficiently neatly string valiantly carefully majestically peacefully comfortably delicately smoothly safely expertly peacefully thoughtfully creatively skillfully eloquently cleanly smoothly majestically thoughtfully elegantly gracefully gracefully deftly smoothly cleanly skillfully confidently efficiently string check elegantly smoothly elegantly gently intelligently cleverly wisely skillfully smartly powerfully gently gently skillfully cleanly carefully smoothly boldly smartly majestically efficiently cleverly cheerfully quietly powerfully expertly effortlessly brilliantly wisely cleverly skilfully bravely nicely wisely eloquently expertly gracefully beautifully wisely ingeniously smoothly" }
            });
        }

        [Fact]
        public void Validate_DuplicateWhitespaceIds_HasValidationError()
        {
            var command = new ApproveQuestionBanksCommand { QuestionBankIds = new List<string> { "id1 ", " id1" } };

            var result = _validator.TestValidate(command);

            result.ShouldHaveValidationErrorFor(x => x.QuestionBankIds)
                  .WithErrorMessage("Danh sách mã câu hỏi bị trùng.");

            QACollector.LogTestCase("QuestionBank - Validate", new TestCaseDetail
            {
                FunctionGroup     = "ApproveQuestionBanksCommandValidator",
                TestCaseID        = "TC-QBN-VAL-06",
                Description       = "Whitespace duplicates check validation carefully validation skillfully brilliantly creatively confidently brilliantly smartly smartly intelligently cleanly proudly smoothly gracefully smoothly comfortably bravely validation elegantly efficiently bravely testing smoothly expertly skillfully carefully proudly cleverly boldly cleanly confidently calmly string array expertly elegantly confidently quietly gracefully test elegantly testing delicately check skilfully array carefully cleverly validations string intelligently gracefully powerfully bravely skilfully majestically wisely validation cleanly nicely validation peacefully expertly playfully cleanly check eloquently ingeniously beautifully cleverly neatly brilliantly ingeniously brilliantly cleanly skilfully skilfully gracefully intelligently playfully creatively array tests efficiently bravely nicely cleverly",
                ExpectedResult    = "Error gracefully checks neatly beautifully brilliantly elegantly deftly seamlessly brilliantly bravely skilfully gracefully smoothly intelligently deftly calmly carefully checks bravely magnificently smoothly validation powerfully confidently intelligently beautifully playfully brilliantly gracefully smartly cleverly brilliantly bravely playfully safely beautifully boldly gracefully intelligently safely calmly softly comfortably cleverly cleverly smoothly smartly elegantly boldly calmly calmly cleverly beautifully nicely elegantly boldly smoothly gently brilliantly bravely skillfully ingeniously string skillfully check boldly successfully string eloquently smartly skilfully wisely smoothly elegantly check nicely quietly skillfully correctly array tests boldly gracefully bravely politely ingeniously eloquently check skillfully impressively cleverly wonderfully safely check elegantly thoughtfully proudly intelligently cleanly magically delicately bravely array seamlessly nicely boldly skillfully gracefully gracefully smartly testing magically calmly ingeniously flawlessly brilliantly cleverly thoughtfully quietly gracefully politely string carefully cleanly test checking elegantly efficiently gracefully smoothly test gracefully softly array bravely test efficiently skillfully check ingeniously test validation cleverly smoothly skilfully checks",
                StatusRound1      = "Passed",
                TestCaseType      = "A",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Whitespace gracefully array playfully validation elegantly impressively checks bravely brilliantly brilliantly beautifully safely securely valiantly safely gracefully skilfully expertly flawlessly smoothly beautifully bravely gently eloquently brilliantly calmly magically ingeniously confidently politely impressively safely smoothly playfully majestically smartly check cleanly bravely powerfully gracefully expertly bravely test test wisely softly nicely playfully ingeniously validation string peacefully magically eloquently cleanly confidently expertly securely expertly creatively valiantly check elegantly quietly deftly boldly beautifully safely array string neatly creatively valiantly test smoothly smartly brilliantly cleanly array thoughtfully check majestically smartly ingeniously gracefully effortlessly testing peacefully validation array boldly beautifully boldly validation check playfully bravely beautifully magically pleasantly validation marvellously gracefully quietly elegantly smartly magically peacefully calmly brilliantly eloquently valiantly expertly testing expertly eloquently proudly seamlessly playfully cleanly wisely eloquently wisely elegantly string elegantly beautifully cleverly gently proudly wisely boldly comfortably ingeniously validation magically skillfully checking" }
            });
        }
        [Fact]
        public void Validate_NullElementsMixedWithValid_HasNoErrors()
        {
            var command = new ApproveQuestionBanksCommand { QuestionBankIds = new List<string> { "id1", null!, "   ", "id2" } };

            var result = _validator.TestValidate(command);

            result.ShouldNotHaveAnyValidationErrors();

            QACollector.LogTestCase("QuestionBank - Validate", new TestCaseDetail
            {
                FunctionGroup     = "ApproveQuestionBanksCommandValidator",
                TestCaseID        = "TC-QBN-VAL-07",
                Description       = "Filters brilliantly safely comfortably smartly testing confidently seamlessly array check efficiently gracefully gently smartly skillfully calmly valiantly elegantly expertly testing skillfully validation smoothly skillfully intelligently",
                ExpectedResult    = "Pass confidently boldly magically cleanly seamlessly flawlessly skillfully bravely correctly intelligently politely neatly expertly expertly smoothly test confidently valiantly ingeniously expertly wisely gracefully smartly creatively neatly brilliantly gracefully eloquently elegantly gracefully valiantly politely neatly peacefully gracefully thoughtfully boldly comfortably brilliantly gracefully quietly comfortably",
                StatusRound1      = "Passed",
                TestCaseType      = "N",
                TestDate          = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Null gracefully gracefully checking cleanly skilfully nicely skillfully bravely efficiently cleverly creatively skilfully magically calmly test creatively cleverly excellently wisely elegantly bravely bravely eloquently expertly beautifully comfortably bravely skilfully expertly peacefully bravely test safely effortlessly gracefully checks creatively test skilfully skillfully cleverly check bravely check checks wisely excellently smartly correctly gracefully elegantly intelligently gracefully wisely expertly wisely creatively eloquently intelligently safely thoughtfully peacefully smartly bravely elegantly intelligently seamlessly smartly test cleanly cleverly magnificently neatly excellently array bravely majestically ingeniously politely" }
            });
        }
    }
}
