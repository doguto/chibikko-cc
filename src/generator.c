#include "chibikko_cc.h"

// スタックに積まれている要素数
static int depth;

static void push(void) {
  printf("  push %%rax\n");
  depth++;
}

static void pop(char *arg) {
  printf("  pop %s\n", arg);
  depth--;
}

static void gen_expr(Node *node) {
  switch (node->kind) {
  case ND_NUM:
    printf("  mov $%d, %%rax\n", node->val);
    return;
  case ND_NEG:
    gen_expr(node->lhs);
    printf("  neg %%rax\n");
    return;
  }

  // 右辺を先に評価してスタックに退避し, 左辺を rax に置く
  gen_expr(node->rhs);
  push();
  gen_expr(node->lhs);
  pop("%rdi");

  switch (node->kind) {
  case ND_ADD:
    printf("  add %%rdi, %%rax\n");
    return;
  case ND_SUB:
    printf("  sub %%rdi, %%rax\n");
    return;
  case ND_MUL:
    printf("  imul %%rdi, %%rax\n");
    return;
  case ND_DIV:
    // cqo: rax の値を 128bit に伸ばして rdx と rax に格納する
    // idiv: rdx と rax を合わせた 128bit 整数を引数のレジスタで割る
    printf("  cqo\n");
    printf("  idiv %%rdi\n");
    return;
  case ND_EQ:
  case ND_NE:
  case ND_LT:
  case ND_LE:
    printf("  cmp %%rdi, %%rax\n");

    if (node->kind == ND_EQ)
      printf("  sete %%al\n");
    else if (node->kind == ND_NE)
      printf("  setne %%al\n");
    else if (node->kind == ND_LT)
      printf("  setl %%al\n");
    else if (node->kind == ND_LE)
      printf("  setle %%al\n");

    printf("  movzb %%al, %%rax\n");
    return;
  }

  error("invalid expression");
}

void codegen(Node *node) {
  printf("  .globl main\n");
  printf("main:\n");
  gen_expr(node);
  printf("  ret\n");

  // Stack アドレスの実行の禁止
  printf("  .section .note.GNU-stack,\"\",@progbits\n");

  assert(depth == 0);
}
