#ifndef TOKENIZER_H
#define TOKENIZER_H

#include <ctype.h>
#include <stdbool.h>
#include <stdlib.h>
#include "utils.h"

typedef enum {
	TK_RESERVED, // 予約語（記号）
	TK_NUM,      // 整数
	TK_EOF,      // 終端文字
} TokenKind;

typedef struct Token Token;
struct Token {
	TokenKind kind;
	Token *next;
	int val;
	char *str;
};

// 現在着目しているトークン
Token *token;

char *user_input;

bool consume(char op) {
	if (token->kind != TK_RESERVED || token->str[0] != op) {
		return false;
	}
	token = token->next;
	return true;
}

void expect(char op) {
	if (token->kind != TK_RESERVED || token->str[0] != op) {
		error_at(user_input, token->str, "'%c'ではありません", op);
	}
	token = token->next;
}

int expect_number(void) {
	if (token->kind != TK_NUM) error_at(user_input, token->str, "数ではありません");
	int val = token->val;
	token = token->next;
	return val;
}

bool at_eof(void) {
	return token->kind == TK_EOF;
}

Token *new_token(TokenKind kind, Token *cur, char *str) {
	Token *tok = calloc(1, sizeof(Token));
	tok->kind = kind;
	tok->str = str;
	cur->next = tok;
	return tok;
}

Token *tokenize(char *p) {
	Token head;
	head.next = NULL;
	Token *cur = &head;
	while (*p) {
		if (isspace(*p)) {
			p++;
			continue;
		}
		if (*p == '+' || *p == '-' || *p == '*' || *p == '/' || *p == '(' || *p == ')') {
			cur = new_token(TK_RESERVED, cur, p++);
			continue;
		}
		if (isdigit(*p)) {
			cur = new_token(TK_NUM, cur, p);
			// strtol(): pから数字を取れるだけ取って数字に変換. pを数字の終了したところのアドレスに移す
			cur->val = strtol(p, &p, 10);
			continue;
		}
		error_at(user_input, p, "トークナイズできません");
	}
	new_token(TK_EOF, cur, p);
	return head.next;
}

#endif
