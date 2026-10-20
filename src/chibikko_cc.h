#ifndef CHIBIKKO_CC_H
#define CHIBIKKO_CC_H

#include <assert.h>
#include <ctype.h>
#include <stdarg.h>
#include <stdbool.h>
#include <stdio.h>
#include <stdlib.h>
#include <string.h>

//
// tokenizer.c
//

typedef enum {
  TK_RESERVED, // 記号・予約語
  TK_IDENT,    // 識別子
  TK_NUM,      // 整数リテラル
  TK_EOF,      // 終端
} TokenKind;

// トークン型
typedef struct Token Token;
struct Token {
  TokenKind kind; // トークンの種類
  Token *next;    // 次のトークン
  int val;        // kind が TK_NUM の場合, その値
  char *str;      // トークン文字列 (NUL 終端)
  char *loc;      // トークンの位置
  int len;        // トークンの長さ
};

void error(char *fmt, ...);
void error_at(char *loc, char *fmt, ...);
void error_tok(Token *tok, char *fmt, ...);
bool equal(Token *tok, char *op);
Token *skip(Token *tok, char *op);
Token *tokenize(char *input);

//
// parser.c
//

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
  int offset;    // kind == ND_VAR の場合に使用. rbp からのオフセット
  int val;       // kind == ND_NUM の場合に使用
};

typedef struct Variable Variable;
struct Variable {
  Variable *next;
  char *name;
  int len;
  int offset;
};

extern Variable *locals;

Node *parse(Token *tok);

//
// generator.c
//

void codegen(Node *node);

#endif
