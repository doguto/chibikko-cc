#include "chibikko_cc.h"

// ローカル変数は 'a' から 'z' の 26 個を rbp からの固定オフセットに配置する
#define LOCAL_AREA_SIZE (26 * 8)

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

// 変数のアドレスを rax に置く
static void gen_addr(Node *node) {
  if (node->kind != ND_VAR)
    error("not an lvalue");

  int offset = (node->name - 'a' + 1) * 8;
  printf("  lea %d(%%rbp), %%rax\n", -offset);
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
  case ND_VAR:
    gen_addr(node);
    printf("  mov (%%rax), %%rax\n");
    return;
  case ND_ASSIGN:
    // 左辺のアドレスを退避してから右辺の値を求め, そのアドレスに格納する
    gen_addr(node->lhs);
    push();
    gen_expr(node->rhs);
    pop("%rdi");
    printf("  mov %%rax, (%%rdi)\n");
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

static void gen_stmt(Node *node) {
  if (node->kind == ND_EXPR_STMT) {
    gen_expr(node->lhs);
    return;
  }

  error("invalid statement");
}

void codegen(Node *node) {
  printf("  .globl main\n");
  printf("main:\n");

  // プロローグ: 呼び出し元の rbp を退避し, ローカル変数領域を確保する
  printf("  push %%rbp\n");
  printf("  mov %%rsp, %%rbp\n");
  printf("  sub $%d, %%rsp\n", LOCAL_AREA_SIZE);

  // 未初期化の変数を読んだ場合の値を 0 に定める
  printf("  mov $%d, %%rax\n", LOCAL_AREA_SIZE);
  printf(".L.init:\n");
  printf("  sub $8, %%rax\n");
  printf("  movq $0, (%%rsp,%%rax)\n");
  printf("  jne .L.init\n");

  // 最後の文の値がそのまま main の戻り値になる
  for (Node *n = node; n; n = n->next) {
    gen_stmt(n);
    assert(depth == 0);
  }

  // エピローグ: ローカル変数領域を解放し, rbp を復元する
  printf("  mov %%rbp, %%rsp\n");
  printf("  pop %%rbp\n");
  printf("  ret\n");

  // Stack アドレスの実行の禁止
  printf("  .section .note.GNU-stack,\"\",@progbits\n");
}
