# 文法

```c
// プログラム: 文の並び
program    = stmt*

// 文: 式の後にセミコロン
stmt       = expr ";"

// 式: 代入式
expr       = assign

// 代入式: 左辺値と右辺値の等価性
assign     = equality ("=" assign)?

// 等価性: 比較演算子の連鎖
equality   = relational ("==" relational | "!=" relational)*

// 比較: 加算演算子の連鎖
relational = add ("<" add | "<=" add | ">" add | ">=" add)*

// 加算: 乗算演算子の連鎖
add        = mul ("+" mul | "-" mul)*

// 乗算: 単項演算子の連鎖
mul        = unary ("*" unary | "/" unary)*

// 単項演算子: 正負の符号
unary      = ("+" | "-")? primary

// 主要な式: 数値、識別子、括弧で囲まれた式
primary    = num | ident | "(" expr ")"
```
