using FluentAssertions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Xunit;

namespace Tokki.UnitTest.Utilities.Tests
{
    public class SourceCodeCounterValidationTests
    {
        [Fact]
        public void All_QACollector_FeatureNames_Should_Be_Mapped_In_SourceCodeCounter()
        {
            var baseDir = AppDomain.CurrentDomain.BaseDirectory;
            var targetDir = Path.GetFullPath(Path.Combine(baseDir, @"..\..\..\..\Tokki.UnitTest"));
            
            // Nếu không tìm thấy thư mục (do build CI script khác base path), thì bỏ qua an toàn
            if (!Directory.Exists(targetDir)) return;

            var csFiles = Directory.GetFiles(targetDir, "*.cs", SearchOption.AllDirectories);
            var usedFeatureNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var regex = new Regex(@"QACollector\.LogTestCase\s*\(\s*""([^""]+)""", RegexOptions.Compiled);
            
            foreach (var file in csFiles)
            {
                var content = File.ReadAllText(file);
                var matches = regex.Matches(content);
                foreach (Match match in matches)
                {
                    if (match.Success && match.Groups.Count > 1)
                    {
                        usedFeatureNames.Add(match.Groups[1].Value);
                    }
                }
            }

            var mappedKeys = new HashSet<string>(SourceCodeCounter.FeatureToFolderMap.Keys, StringComparer.OrdinalIgnoreCase);
            var missingKeys = usedFeatureNames.Except(mappedKeys).ToList();

            missingKeys.Should().BeEmpty($"Tất cả các name truyền vào QACollector phải được định nghĩa trong SourceCodeCounter.FeatureToFolderMap! Thiếu: {string.Join(", ", missingKeys)}");
        }
    }
}
