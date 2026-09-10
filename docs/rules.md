# 文法

```c
// 式: 項の加減が連なって成り立つ
expr    = mul ("+" mul | "-" mul)*

// 項: 因子の乗除が連なって成り立つ
mul     = unary ("*" unary | "/" unary)*

// 因子: +- の単項演算子がつくかもしれない基本式
unary   = ("+" | "-")? primary

// 基本式: 数字、もしくは括弧で囲まれた式
primary = num | "(" expr ")"
```
