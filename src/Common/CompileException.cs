namespace Chibikko.Common;

/// <summary>
/// ソース位置(Offset)付きのコンパイルエラー。
/// </summary>
public sealed class CompileException(string message, int offset) : Exception(message)
{
    public int Offset { get; } = offset;
}
