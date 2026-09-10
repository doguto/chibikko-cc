# 文法

```c
// 式: 等式で成り立つ
expr = equality

// 等式: 比較因子 と 比較演算子 で成り立つ
equality = relational ("==" relational | "!=" relational)*

// 比較因子: 加算因子 と 比較演算子 で成り立つ
relational = add ("<" add | "<=" add | ">" add | ">=" add)*

// 加算因子: 乗算因子 と 加減算演算子 で成り立つ
add = mul ("+" mul | "-" mul)*

// 乗算因子: 単項因子 と 乗除算演算子 で成り立つ
mul = unary ("*" unary | "/" unary)*

// 単項因子: 単項演算子 と 単項因子 で成り立つ
unary = ("+" | "-")? primary

// 単項演算子: 数値 と 括弧 で成り立つ
primary = num | "(" expr ")"
```
