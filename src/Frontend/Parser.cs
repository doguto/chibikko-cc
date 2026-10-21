namespace Chibikko.Frontend;

public sealed class Parser(Lexer lexer)
{
    readonly TokenStream tokens = new(lexer);

    public TranslationUnit Parse() => TranslationUnit();

    // translationUnit ::= function*
    TranslationUnit TranslationUnit()
    {
        throw new NotImplementedException("parser");
    }

    // function ::= type identifier "(" params? ")" block
    FunctionDecl Function()
    {
        throw new NotImplementedException("parser");
    }

    // params ::= type identifier ("," type identifier)*
    List<ParamDecl> Params()
    {
        throw new NotImplementedException("parser");
    }

    // block ::= "{" statement* "}"
    Block Block()
    {
        throw new NotImplementedException("parser");
    }

    // statement ::= declaration ";"
    //             | "return" expression? ";"
    //             | expression ";"
    //             | ifStmt
    //             | "while" "(" expression ")" block
    //             | "for" "(" expression? ";" expression? ";" expression? ")" block
    Stmt Statement()
    {
        throw new NotImplementedException("parser");
    }

    // ifStmt ::= "if" "(" expression ")" block ("else" (ifStmt | block))?
    Stmt IfStmt()
    {
        throw new NotImplementedException("parser");
    }

    // declaration ::= type identifier ("=" expression)?
    Stmt Declaration()
    {
        throw new NotImplementedException("parser");
    }

    // expression ::= assign
    Expr Expression()
    {
        throw new NotImplementedException("parser");
    }

    // assign ::= identifier "=" assign | equality
    Expr Assign()
    {
        throw new NotImplementedException("parser");
    }

    // equality ::= relational (("==" | "!=") relational)*
    Expr Equality()
    {
        throw new NotImplementedException("parser");
    }

    // relational ::= add (("<=" | ">=" | "<" | ">") add)*
    Expr Relational()
    {
        throw new NotImplementedException("parser");
    }

    // add ::= mul (("+" | "-") mul)*
    Expr Add()
    {
        throw new NotImplementedException("parser");
    }

    // mul ::= unary (("*" | "/") unary)*
    Expr Mul()
    {
        throw new NotImplementedException("parser");
    }

    // unary ::= ("+" | "-")? primary
    Expr Unary()
    {
        throw new NotImplementedException("parser");
    }

    // primary ::= literal
    //           | identifier ("(" args? ")")?
    //           | "(" expression ")"
    Expr Primary()
    {
        throw new NotImplementedException("parser");
    }

    // args ::= expression ("," expression)*
    List<Expr> Args()
    {
        throw new NotImplementedException("parser");
    }

    // type ::= "int" | "long" | "void" | "bool" | "char" | "float" | "double"
    Token Type()
    {
        throw new NotImplementedException("parser");
    }
}
