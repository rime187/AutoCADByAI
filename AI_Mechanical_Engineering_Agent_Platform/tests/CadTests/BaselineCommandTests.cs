using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using CadKernel;
using Xunit;

namespace CadTests
{
    /// <summary>只在临时运行目录检查基准写入边界, 不连接 SolidWorks。</summary>
    public class BaselineCommandTests : IDisposable
    {
        readonly string root = Path.Combine(Path.GetTempPath(), "cad_baseline_tests_" + Guid.NewGuid().ToString("N"));
        readonly string runDir, index, expected;
        const string ValidIndex = "[{\"file\":\"part.SLDDRW\",\"size\":\"A3\",\"scale\":\"1:1\",\"dims\":3,\"extents\":[20,30]}]";

        public BaselineCommandTests()
        {
            string inputDir = Path.Combine(root, "input", "demo");
            runDir = Path.Combine(root, "output", "run1");
            Directory.CreateDirectory(inputDir);
            Directory.CreateDirectory(Path.Combine(runDir, "drawings"));
            File.WriteAllText(Path.Combine(inputDir, "design.json"), "{}");
            index = Path.Combine(runDir, "drawings", "index.json");
            expected = Path.Combine(inputDir, Baseline.FileName);
            var manifest = new RunManifest
            {
                Task = "demo",
                Steps = new[] { "零件", "装配", "校核", "工程图", "导出", "后处理" }
                    .Select(name => new StepRecord { Name = name, Status = "passed" }).ToList(),
            };
            manifest.Checks.Add(new CheckRecord { Name = "工程图生成", Passed = true });
            File.WriteAllText(Path.Combine(runDir, "run.json"), JsonSerializer.Serialize(manifest,
                new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower }));
            File.WriteAllText(expected, Baseline.Generate(manifest, ValidIndex, "original"));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData("[]")]
        [InlineData("null")]
        [InlineData("{")]
        [InlineData("{}")]
        [InlineData("[{}]")]
        [InlineData("[{\"file\":\"part.SLDDRW\",\"size\":\"A3\"}]")]
        public void InvalidOrMissingIndexPreservesExistingBaseline(string indexJson)
        {
            if (indexJson != null) File.WriteAllText(index, indexJson);
            byte[] before = File.ReadAllBytes(expected);

            Assert.ThrowsAny<Exception>(() => CadCli.Program.MakeBaseline(root, runDir));

            Assert.Equal(before, File.ReadAllBytes(expected));
        }

        [Fact]
        public void ValidIndexGeneratesBaselineWithDrawings()
        {
            File.WriteAllText(index, ValidIndex);

            Assert.Equal(0, CadCli.Program.MakeBaseline(root, runDir));

            var baseline = Baseline.Parse(File.ReadAllText(expected));
            var drawing = Assert.Single(baseline.Drawings);
            Assert.Equal("run1", baseline.SourceRun);
            Assert.Equal("part.SLDDRW", drawing.File);
            Assert.Equal(3, drawing.Dims);
            Assert.Equal(new[] { 20.0, 30.0 }, drawing.Extents);
        }

        public void Dispose()
        {
            string resolved = Path.GetFullPath(root);
            string temp = Path.GetFullPath(Path.GetTempPath()).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            if (!string.Equals(Path.GetDirectoryName(resolved), temp, StringComparison.OrdinalIgnoreCase)
                || !Path.GetFileName(resolved).StartsWith("cad_baseline_tests_", StringComparison.Ordinal))
                throw new IOException("拒绝清理测试临时目录以外的路径: " + resolved);
            Directory.Delete(resolved, true);
        }
    }
}
