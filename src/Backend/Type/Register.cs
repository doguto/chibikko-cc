namespace Chibikko.Backend.Type;

public enum Register
{
    // 64bit 汎用レジスタ
    rax,
    rbx,
    rcx,
    rdx,
    rsi,
    rdi,
    rbp, // Base Pointer
    rsp, // Stack Pointer
    r8,
    r9,
    r10,
    r11,
    r12,
    r13,
    r14,
    r15,

    // 32bit 汎用レジスタ. 上記 64bit レジスタの下位 32bit
    eax,
    ecx,
    edx,
    ebx,
    esp,
    ebp,
    esi,
    edi,
    r8d,
    r9d,
    r10d,
    r11d,
    r12d,
    r13d,
    r14d,
    r15d,

    // 16bit 汎用レジスタ 上記 64bit レジスタの下位 16bit
    ax,
    cx,
    dx,
    bx,
    sp,
    bp,
    si,
    di,
    r8w,
    r9w,
    r10w,
    r11w,
    r12w,
    r13w,
    r14w,
    r15w,

    // 8bit 汎用レジスタ 上記 16bit の上位 8bit
    ah,
    ch,
    dh,
    bh,

    // 8bit 汎用レジスタ 上記 16bit の下位 8bit
    al,
    cl,
    dl,
    bl,
    spl,
    bpl,
    sil,
    dil,
    r8b,
    r9b,
    r10b,
    r11b,
    r12b,
    r13b,
    r14b,
    r15b,

    // -- 非汎用レジスタ --

    rip, // Program Counter

    eflags, // フラグレジスタ

    // セグメントレジスタ
    cs,
    ss,
    ds,
    es,
    fs,
    gs
}

public static class RegisterGroup
{
    public static List<Register> General64Registers()
    {
        return new List<Register>()
        {
            Register.rax,
            Register.rbx,
            Register.rcx,
            Register.rdx,
            Register.rsi,
            Register.rdi,
            Register.rbp,
            Register.rsp,
            Register.r8,
            Register.r9,
            Register.r10,
            Register.r11,
            Register.r12,
            Register.r13,
            Register.r14,
            Register.r15,
        };
    }

    // https://gitlab.com/x86-psABIs/x86-64-ABI
    // If the class is INTEGER, the next available register of the sequence %rdi, %rsi, %rdx, %rcx, %r8 and %r9 is used19.
    public static List<Register> IntArgumentRegisters()
    {
        return new List<Register>()
        {
            Register.rdi,
            Register.rsi,
            Register.rdx,
            Register.rcx,
            Register.r8,
            Register.r9
        };
    }
}

public static class RegisterExtensions
{
    public static string ToName(this Register reg)
    {
        return $"%{reg}";
    }

    public static Register ToSized(this Register reg, int width)
    {
        var (r64, r32, r16, r8) = reg switch
        {
            Register.rax => (Register.rax, Register.eax, Register.ax, Register.al),
            Register.rbx => (Register.rbx, Register.ebx, Register.bx, Register.bl),
            Register.rcx => (Register.rcx, Register.ecx, Register.cx, Register.cl),
            Register.rdx => (Register.rdx, Register.edx, Register.dx, Register.dl),
            Register.rsi => (Register.rsi, Register.esi, Register.si, Register.sil),
            Register.rdi => (Register.rdi, Register.edi, Register.di, Register.dil),
            Register.rbp => (Register.rbp, Register.ebp, Register.bp, Register.bpl),
            Register.rsp => (Register.rsp, Register.esp, Register.sp, Register.spl),
            Register.r8 => (Register.r8, Register.r8d, Register.r8w, Register.r8b),
            Register.r9 => (Register.r9, Register.r9d, Register.r9w, Register.r9b),
            Register.r10 => (Register.r10, Register.r10d, Register.r10w, Register.r10b),
            Register.r11 => (Register.r11, Register.r11d, Register.r11w, Register.r11b),
            Register.r12 => (Register.r12, Register.r12d, Register.r12w, Register.r12b),
            Register.r13 => (Register.r13, Register.r13d, Register.r13w, Register.r13b),
            Register.r14 => (Register.r14, Register.r14d, Register.r14w, Register.r14b),
            Register.r15 => (Register.r15, Register.r15d, Register.r15w, Register.r15b),
            _ => throw new NotSupportedException($"Unsupported Register: {reg}")
        };

        return width switch
        {
            8 => r64,
            4 => r32,
            2 => r16,
            1 => r8,
            _ => throw new NotSupportedException($"Unsupported Register Width: {width}")
        };
    }
}
