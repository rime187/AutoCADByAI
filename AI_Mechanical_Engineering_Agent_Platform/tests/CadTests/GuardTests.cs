using System;
using CadKernel;
using Xunit;

namespace CadTests
{
    /// <summary>直接执行 Guard 的纯逻辑调用, 不连接 SolidWorks。</summary>
    [Collection("Run")]
    public class GuardTests
    {
        [Fact]
        public void CompletedCallReturnsItsResult()
        {
            Assert.Equal(42, Sw.Guard("纯逻辑成功", 60, () => 42));
        }

        [Fact]
        public void OrdinaryFailurePreservesTheOriginalException()
        {
            var error = new InvalidOperationException("普通调用失败");
            Assert.Same(error, Assert.Throws<InvalidOperationException>(() =>
                Sw.Guard<int>("纯逻辑失败", 60, () => throw error)));
        }
    }
}
