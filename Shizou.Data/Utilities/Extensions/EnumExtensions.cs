using System.Text;

namespace Shizou.Data.Utilities.Extensions;

public static class EnumExtensions
{
    public static string ToStringUpperCaseSpaced(this Enum @enum)
    {
        var input = @enum.ToString();
        var output = new StringBuilder();

        for (var i = 0; i < input.Length; i++)
        {
            if (i > 0 && char.IsUpper(input[i]) && !char.IsUpper(input[i - 1])) output.Append(' ');

            output.Append(input[i]);
        }

        return output.ToString();
    }
}
