// Assembly-level xUnit configuration
// - Disables all parallelism so QACollector state accumulates correctly
//   across all test classes before Z_GenerateReportTask reads it.
// - Registers a custom collection orderer that sends Z_GenerateReportTask
//   to the very end of the run.

using Xunit;

[assembly: CollectionBehavior(DisableTestParallelization = true)]
[assembly: TestCollectionOrderer(
    "Tokki.UnitTest.Tools.ReportLastTestCollectionOrderer",
    "Tokki.UnitTest")]
