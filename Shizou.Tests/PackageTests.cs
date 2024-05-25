using System.Reflection;
using System.Runtime.Versioning;
using Blazored.Modal;

namespace Shizou.Tests;

[TestClass]
public class PackageTests
{
    [TestMethod]
    public void TestBlazoredVersion()
    {
        Assert.AreEqual(".NETCoreApp,Version=v6.0",
            Assembly.GetAssembly(typeof(BlazoredModal))?.GetCustomAttribute<TargetFrameworkAttribute>()
                ?.FrameworkName);
    }
}
