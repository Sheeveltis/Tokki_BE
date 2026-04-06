// Assembly-level xUnit configuration
// - Disables all parallelism so QACollector state accumulates correctly
//   across all test classes before Z_GenerateReportTask reads it.
// - Registers a custom collection orderer that sends Z_GenerateReportTask
//   to the very end of the run.
// - Registers FailureTrackingTestFramework to auto-capture failed tests
//   that never reach QACollector.LogTestCase().

using Xunit;
using Xunit.Sdk;

[assembly: CollectionBehavior(DisableTestParallelization = true)]
[assembly: TestCollectionOrderer(
    "Tokki.UnitTest.Tools.ReportLastTestCollectionOrderer",
    "Tokki.UnitTest")]
[assembly: TestFramework(
    "Tokki.UnitTest.Infrastructure.FailureTrackingTestFramework",
    "Tokki.UnitTest")]
