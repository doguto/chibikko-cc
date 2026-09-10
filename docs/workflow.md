# コンパイラ フロー

## 1. トークナイザ

ソースコードを `Token` の連結リストに変換する。

```c
int a = 1 + 2;
```

これを `int`、`a`、`=`、`1`、`+`、`2`、`;` のようなトークンに分解する。
またこの時に以下のように Token の種類も判定・付与する。

```c
typedef enum {
  TK_RESERVED, // 記号・予約語
  TK_IDENT,    // 識別子
  TK_NUM,      // 整数リテラル
  TK_EOF,      // 終端
} TokenKind;

struct Token {
  TokenKind kind; // トークンの種類
  Token *next;    // 次のトークン
  int val;        // kind が TK_NUM の場合, その値
  char *loc;      // トークンの位置
  int len;        // トークンの長さ
};
```


## 2. パーサ

トークンの連結リストを構文木に変換する。

構文木の各要素を `Node` と呼び、以下のような構造体で表す。

```c
typedef enum {
  ND_ADD,       // +
  ND_SUB,       // -
  ND_MUL,       // *
  ND_DIV,       // /
  ND_NEG,       // 単項 -
  ND_EQ,        // ==
  ND_NE,        // !=
  ND_LT,        // <
  ND_LE,        // <=
  ND_ASSIGN,    // =
  ND_VAR,       // 変数
  ND_EXPR_STMT, // 式文
  ND_NUM,       // 整数
} NodeKind;

// AST ノード型
typedef struct Node Node;
struct Node {
  NodeKind kind; // ノードの種類
  Node *next;    // 次の文
  Node *lhs;     // 左辺
  Node *rhs;     // 右辺
  char name;     // kind == ND_VAR の場合に使用
  int val;       // kind == ND_NUM の場合に使用
};
```
