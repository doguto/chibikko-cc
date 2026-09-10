#ifndef RECURSIVE_DESCENT_PARSER_H
#define RECURSIVE_DESCENT_PARSER_H

#include <ctype.h>

#include "tokenizer.h"

typedef enum {
	ND_ADD, // +
	ND_SUB, // -
	ND_MUL, // *
	ND_DIV, // /
	ND_NUM  // 整数
} NodeKind;

typedef struct Node Node;

struct Node {
	NodeKind kind;
	Node *lhs;
	Node *rhs;
	int val;
};

Node *new_node(NodeKind kind, Node *lhs, Node *rhs) {
	Node *node = calloc(1, sizeof(Node));
	node->kind = kind;
	node->lhs = lhs;
	node->rhs = rhs;
	return node;
}

Node *new_node_num(int val) {
	Node *node = calloc(1, sizeof(Node));
	node->kind = ND_NUM;
	node->val = val;
	return node;
}


Node *expr();

Node *primary() {
	if (consume('(')) {
		Node *node = expr();
		expect(')');
		return node;
	}

	// 括弧に囲まれた式でなければ数字
	return new_node_num(expect_number());
}

Node *unary() {
	if (consume('+')) {
		return primary();
	} else if (consume('-')) {
		// 0 - [基本式] の形に置換する
		return new_node(ND_SUB, new_node_num(0), primary());
	}
	return primary();
}

Node *mul() {
	Node *node = unary();

	while(true) {
		if (consume('*')) {
			node = new_node(ND_MUL, node, unary());
		} else if (consume('/')) {
			node = new_node(ND_DIV, node, unary());
		} else {
			return node;
		}
	}
}

Node *expr() {
	Node *node = mul();

	while(true) {
		if (consume('+')) {
			node = new_node(ND_ADD, node, mul());
		} else if (consume('-')) {
			node = new_node(ND_SUB, node, mul());
		} else {
			return node;
		}
	}
}

#endif 
