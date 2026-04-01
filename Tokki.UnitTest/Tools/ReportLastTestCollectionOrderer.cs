using System;
using System.Collections.Generic;
using System.Linq;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace Tokki.UnitTest.Tools
{
    /// <summary>
    /// Orders test collections so that any collection whose display name
    /// starts with "Z_" runs last.  All other collections run in their
    /// default (undetermined) order first.
    /// 
    /// Register this orderer at assembly level in AssemblyInfo.cs or via
    /// the [assembly: TestCollectionOrderer(...)] attribute.
    /// </summary>
    public class ReportLastTestCollectionOrderer : ITestCollectionOrderer
    {
        public IEnumerable<ITestCollection> OrderTestCollections(
            IEnumerable<ITestCollection> testCollections)
        {
            // Collections whose display-name starts with "Z_" go last,
            // everything else goes first (in original order).
            return testCollections.OrderBy(c =>
                c.DisplayName.Contains("Z_GenerateReportTask") ? 1 : 0);
        }
    }
}
