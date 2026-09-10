// 加減算コンパイラ

#include <stdarg.h>
#include <stdbool.h>
#include <stdio.h>
#include <stdlib.h>
#include <string.h>

#include "include/utils.h"
#include "include/tokenizer.h"
#include "include/recursive_descent_parser.h"
#include "include/generator.h"

int main(int argc, char **argv) {
	if (argc != 2) {
		fprintf(stderr, "引数の個数が正しくありません\n");
		return 1;
	}

	user_input = argv[1];

	// トークナイズ
	token = tokenize(argv[1]);
	
	// パース
	Node *node = expr();


	// Assembly Syntax の宣言
	printf(".intel_syntax noprefix\n");

	printf(".global main\n");

	// main() 関数
	printf("main:\n");

	gen(node);

	// 式全体の値がスタックに残っているので, これを返り値とする
	printf("	pop rax\n");
	printf("	ret\n");
	
	// Stackアドレスの実行の禁止
	printf(".section .note.GNU-stack,\"\",@progbits\n");
	return 0;
}

