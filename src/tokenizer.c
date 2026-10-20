#include "chibikko_cc.h"

// 入力文字列
static char *current_input;

// エラーを報告して終了する
void error(char *fmt, ...) {
  va_list ap;
  va_start(ap, fmt);
  vfprintf(stderr, fmt, ap);
  fprintf(stderr, "\n");
  exit(1);
}

// エラー箇所を示して終了する
static void verror_at(char *loc, char *fmt, va_list ap) {
  int pos = loc - current_input;
  fprintf(stderr, "%s\n", current_input);
  fprintf(stderr, "%*s", pos, ""); // pos 個の空白を出力
  fprintf(stderr, "^ ");
  vfprintf(stderr, fmt, ap);
  fprintf(stderr, "\n");
  exit(1);
}

void error_at(char *loc, char *fmt, ...) {
  va_list ap;
  va_start(ap, fmt);
  verror_at(loc, fmt, ap);
}

void error_tok(Token *tok, char *fmt, ...) {
  va_list ap;
  va_start(ap, fmt);
  verror_at(tok->loc, fmt, ap);
}

// 現在のトークンが `op` と一致するかどうか
bool equal(Token *tok, char *op) {
  return memcmp(tok->loc, op, tok->len) == 0 && op[tok->len] == '\0';
}

// 現在のトークンが `op` であることを保証する
Token *skip(Token *tok, char *op) {
  if (!equal(tok, op))
    error_tok(tok, "expected '%s'", op);
  return tok->next;
}

// 新しいトークンを生成する
static Token *new_token(TokenKind kind, char *start, char *end) {
  Token *tok = calloc(1, sizeof(Token));
  tok->kind = kind;
  tok->loc = start;
  tok->len = end - start;
  tok->str = calloc(tok->len + 1, sizeof(char)); // calloc なので NUL 終端される
  memcpy(tok->str, start, tok->len);
  return tok;
}

static bool startswith(char *p, char *q) {
  return strncmp(p, q, strlen(q)) == 0;
}

// p から記号トークンを読み, その長さを返す
static int read_punct(char *p) {
  if (startswith(p, "==") || startswith(p, "!=") ||
      startswith(p, "<=") || startswith(p, ">="))
    return 2;

  return ispunct(*p) ? 1 : 0;
}

// `current_input` をトークナイズして新しいトークン列を返す
Token *tokenize(char *p) {
  current_input = p;
  Token head = {};
  Token *cur = &head;

  while (*p) {
    // 空白文字をスキップする
    if (isspace(*p)) {
      p++;
      continue;
    }

    // 識別子
    // ここでは簡単のため、小文字1文字の識別子のみを認める
    if ('a' <= *p && *p <= 'z') {
      cur = cur->next = new_token(TK_IDENT, p, p + 1);
      p++;
      continue;
    }

    // 整数リテラル
    if (isdigit(*p)) {
      char *q = p;
      int val = strtoul(p, &p, 10);
      cur = cur->next = new_token(TK_NUM, q, p);
      cur->val = val;
      continue;
    }

    // 記号
    int punct_len = read_punct(p);
    if (punct_len) {
      cur = cur->next = new_token(TK_RESERVED, p, p + punct_len);
      p += cur->len;
      continue;
    }

    error_at(p, "invalid token");
  }

  cur = cur->next = new_token(TK_EOF, p, p);
  return head.next;
}
