using Chibikko.Common.Ir;

namespace Chibikko.Backend.Type;

public static class AssemblySuffix
{
    // https://gondow.github.io/linux-x86-64-programming/x86-list.html#%E5%91%BD%E4%BB%A4%E3%82%B5%E3%83%95%E3%82%A3%E3%83%83%E3%82%AF%E3%82%B9
    public static char GetAssemblySuffix(this IrType irType)
    {
        return irType.GetWidth() switch
        {
            1 => 'b',
            2 => 'w',
            4 => 'l',
            8 => 'q',
            _ => throw new Exception($"Unsupported IrType Width: {irType.GetWidth()}")
        };
    }
}
