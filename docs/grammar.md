# 文法定義

```ebnf
translationUnit ::= function*

function   ::= type identifier "(" params? ")" block
params     ::= type identifier ("," type identifier)*

block      ::= "{" statement* "}"

statement  ::= declaration ";"
             | "return" expression? ";"
             | expression ";"
             | ifStmt
             | "while" "(" expression ")" block
             | "for" "(" expression? ";" expression? ";" expression? ")" block

ifStmt     ::= "if" "(" expression ")" block ("else" (ifStmt | block))?

declaration ::= type identifier ("=" expression)?

expression ::= assign
assign     ::= identifier "=" assign | equality
equality   ::= relational (("==" | "!=") relational)*
relational ::= add (("<=" | ">=" | "<" | ">") add)*
add        ::= mul (("+" | "-") mul)*
mul        ::= unary (("*" | "/") unary)*
unary      ::= ("+" | "-")? primary
primary    ::= literal
             | identifier ("(" args? ")")?
             | "(" expression ")"
args       ::= expression ("," expression)*

identifier ::= [a-zA-Z_][a-zA-Z0-9_]*
literal    ::= float | integer | char | bool
float      ::= [0-9]+ "." [0-9]+ [fF]?
integer    ::= [0-9]+ [lL]?
char       ::= "'" ([^'\\\n] | "\\" [nrt0\\']) "'"
bool       ::= "true" | "false"
type       ::= "int" | "long" | "void" | "bool" | "char" | "float" | "double"
```

予約語(`return` `if` `else` `while` `for` `true` `false` と `type` の各語)は `identifier` として扱わない。

## Token

| 分類 | トークン |
|------|----------|
| キーワード(制御) | `return` `if` `else` `while` `for` |
| キーワード(真偽値) | `true` `false` |
| キーワード(型) | `int` `long` `void` `bool` `char` `float` `double` |
| 識別子 | `[a-zA-Z_][a-zA-Z0-9_]*`(予約語を除く) |
| 整数リテラル | `[0-9]+ [lL]?` |
| 浮動小数点リテラル | `[0-9]+ "." [0-9]+ [fF]?` |
| 文字リテラル | `'a'` `'\n'` など |
| 区切り記号 | `(` `)` `{` `}` `;` `,` |
| 算術演算子 | `+` `-` `*` `/` |
| 比較演算子 | `==` `!=` `<` `<=` `>` `>=` |
| 代入演算子 | `=` |
| 終端 | `EOF` |

字句解析の注意点:

- `==` `!=` `<=` `>=` は `=` `<` `>` より先に判定する(最長一致)。
- 小数点を含む数値は浮動小数点リテラル、含まない数値は整数リテラルとして扱う。
