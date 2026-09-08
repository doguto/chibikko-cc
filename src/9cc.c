// 加減算コンパイラ

#include <stdarg.h>
#include <stdbool.h>
#include <stdio.h>
#include <stdlib.h>
#include <string.h>

#include "include/utils.h"
#include "include/tokenizer.h"


int main(int argc, char **argv) {
	if (argc != 2) {
		fprintf(stderr, "引数の個数が正しくありません\n");
		return 1;
	}

	user_input = argv[1];
	token = tokenize(argv[1]);


	// Assembly Syntax の宣言
	printf(".intel_syntax noprefix\n");

	printf(".global main\n");

	// main() 関数
	printf("main:\n");
	printf("	mov rax, %d\n", expect_number());

	while(!at_eof()) {
		if (consume('+')) {
			printf("	add rax, %d\n", expect_number());
			continue;
		}

    expect('-');
    printf("  sub rax, %d\n", expect_number());
	}

	printf("	ret\n");
	
	// Stackアドレスの実行の禁止
	printf(".section .note.GNU-stack,\"\",@progbits\n");
	return 0;
}

