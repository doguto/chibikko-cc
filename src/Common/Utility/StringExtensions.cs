using System.Text;

namespace Chibikko.Common.Utility;

public static class StringBuilderExtensions
{
    public static void AppendNewIndentedLine(this StringBuilder builder, string newLine)
    {
        builder.Append($"\n\t{newLine}");
    }

    public static void AppendNewLine(this StringBuilder builder, string newLine)
    {
        builder.Append($"\n{newLine}");
    }
}
