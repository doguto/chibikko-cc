#include "chibikko_cc.h"

// 識別子は小文字 1 文字なので, ローカル変数は最大 26 個にしかならない
#define LOCAL_AREA_SIZE (26 * 8)

// スタックに積まれている要素数
static int depth;

static void push(char *arg) {
  printf("  push %s\n", arg);
  depth++;
}

// 即値の push は 32bit までしか積めないことに注意する
static void push_imm(int val) {
  printf("  push %d\n", val);
  depth++;
}

static void pop(char *arg) {
  printf("  pop %s\n", arg);
  depth--;
}

static void add(char *to, char *from) {
  printf("  add %s, %s\n", to, from);
}

static void sub(char *to, char *from) {
  printf("  sub %s, %s\n", to, from);
}

static void sub_imm(char *to, int num) {
  printf("  sub %s, %d\n", to, num);
}

static void mov(char *to, char *from) {
  printf("  mov %s, %s\n", to, from);
}

// 変数のアドレスをスタックに積む
static void gen_var(Node *node) {
  if (node->kind != ND_VAR) error("代入の左辺値が変数ではありません");

  mov("rax", "rbp");
  sub_imm("rax", node->offset);
  push("rax");
}

// 部分式の値をスタックに積んで返すスタックマシン方式で出力する
static void gen_expr(Node *node) {
  switch (node->kind) {
  case ND_NUM:
    push_imm(node->val);
    return;
  case ND_NEG:
    gen_expr(node->lhs);
    pop("rax");
    printf("  neg rax\n");
    push("rax");
    return;
  case ND_VAR:
    // アドレスを求めてから, その指す先の値に置き換える
    gen_var(node);
    pop("rax");
    mov("rax", "[rax]");
    push("rax");
    return;
  case ND_ASSIGN:
    // 左辺のアドレスと右辺の値を積み, 代入式自体の値として右辺を残す
    gen_var(node->lhs);
    gen_expr(node->rhs);
    pop("rdi");
    pop("rax");
    mov("[rax]", "rdi");
    push("rdi");
    return;
  }

  // 左辺, 右辺の順に積んでから取り出す
  gen_expr(node->lhs);
  gen_expr(node->rhs);
  pop("rdi");
  pop("rax");

  switch (node->kind) {
  case ND_ADD:
    add("rax", "rdi");
    break;
  case ND_SUB:
    sub("rax", "rdi");
    break;
  case ND_MUL:
    printf("  imul rax, rdi\n");
    break;
  case ND_DIV:
    // cqo: rax の値を 128bit に伸ばして rdx と rax に格納する
    // idiv: rdx と rax を合わせた 128bit 整数を引数のレジスタで割る
    printf("  cqo\n");
    printf("  idiv rdi\n");
    break;
  case ND_EQ:
  case ND_NE:
  case ND_LT:
  case ND_LE:
    printf("  cmp rax, rdi\n");

    if (node->kind == ND_EQ)
      printf("  sete al\n");
    else if (node->kind == ND_NE)
      printf("  setne al\n");
    else if (node->kind == ND_LT)
      printf("  setl al\n");
    else if (node->kind == ND_LE)
      printf("  setle al\n");

    printf("  movzx rax, al\n");
    break;
  default:
    error("invalid expression");
  }

  push("rax");
}

static void gen_stmt(Node *node) {
  if (node->kind == ND_EXPR_STMT) {
    // 式文の値は捨てる. 最後に取り出した値がそのまま main の戻り値になる
    gen_expr(node->lhs);
    pop("rax");
    return;
  }

  error("invalid statement");
}

void codegen(Node *node) {
  // レジスタの % 接頭辞を省いた Intel 記法で出力する
  printf("  .intel_syntax noprefix\n");
  printf("  .globl main\n");
  printf("main:\n");

  // プロローグ: 呼び出し元の rbp を退避し, ローカル変数領域を確保する
  printf("  push rbp\n");
  printf("  mov rbp, rsp\n");
  printf("  sub rsp, %d\n", LOCAL_AREA_SIZE);

  // 未初期化の変数を読んだ場合の値を 0 に定める
  printf("  mov rax, %d\n", LOCAL_AREA_SIZE);
  printf(".L.init:\n");
  printf("  sub rax, 8\n");
  printf("  mov qword ptr [rsp+rax], 0\n");
  printf("  jne .L.init\n");

  // 最後の文の値がそのまま main の戻り値になる
  for (Node *n = node; n; n = n->next) {
    gen_stmt(n);
    assert(depth == 0);
  }

  // エピローグ: ローカル変数領域を解放し, rbp を復元する
  printf("  mov rsp, rbp\n");
  printf("  pop rbp\n");
  printf("  ret\n");

  // Stack アドレスの実行の禁止
  printf("  .section .note.GNU-stack,\"\",@progbits\n");
}
