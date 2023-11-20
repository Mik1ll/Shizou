using System.Diagnostics.CodeAnalysis;

[assembly: SuppressMessage("Style", "VSTHRD200:Use \"Async\" suffix for async methods",
    Justification = "Messes with routes/nameof", Scope = "namespaceanddescendants", Target = "Shizou.Server.Controllers")]
