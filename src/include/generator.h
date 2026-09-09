#ifndef GENERATOR_H
#define GENERATOR_H

#include <stdio.h>
#include "recursive_descent_parser.h"

void gen(Node *node) {
	if (node->kind == ND_NUM) {
		printf("	push %d\n", node->val);
		return;
	}

	gen(node->lhs);
	gen(node->rhs);

	printf("	pop rdi\n");
	printf("	pop rax\n");

	switch (node->kind) {
		case ND_ADD:
			printf("	add rax, rdi\n");
			break;
		case ND_SUB:
			printf("	sub rax, rdi\n");
			break;
		case ND_MUL:
			printf("	imul rax, rdi\n");
			break;
		case ND_DIV:
			// cqo: rax に入っている値を 128bit に伸ばして rdx と rax に格納する
			// idiv: rdx と rax の値を合わせて 128bit 整数と見なし, それを 引数のレジスタで割る
			printf("	cqo\n");
			printf("	idiv rdi\n");
			break;
	}

	printf("	push rax\n");
}

#endif
