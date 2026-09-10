// C コンパイラ

#include "chibikko_cc.h"

int main(int argc, char **argv) {
  if (argc != 2)
    error("%s: invalid number of arguments", argv[0]);

  // トークナイズ
  Token *tok = tokenize(argv[1]);

  // パース
  Node *node = parse(tok);

  // コード生成
  codegen(node);

  return 0;
}
