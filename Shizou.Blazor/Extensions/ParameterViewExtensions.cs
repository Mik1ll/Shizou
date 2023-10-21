using Microsoft.AspNetCore.Components;

namespace Shizou.Blazor.Extensions;

public static class ParameterViewExtensions
{
    public static void EnsureParametersSet(this ParameterView parameterView, params string[] parameterNames)
    {
        foreach (var param in parameterNames)
            if (!parameterView.ToDictionary().ContainsKey(param))
                throw new ArgumentException("Parameter unset", param);
    }
}
