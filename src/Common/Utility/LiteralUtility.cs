using System.Diagnostics;

namespace Chibikko.Common.Utility;

public static class LiteralUtility
{
    public static long ParseCharLiteral(string text)
    {
        var body = text[1..^1];
        if (body[0] is not '\\')
        {
            return body[0];
        }

        return body[1] switch
        {
            'n' => '\n',
            'r' => '\r',
            't' => '\t',
            '0' => '\0',
            '\\' => '\\',
            '\'' => '\'',
            _ => throw new UnreachableException($"Unknown escape sequence: {text}"),
        };
    }
}
