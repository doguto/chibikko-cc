
	.globl main
main:
	pushq %rbp
	movq %rsp, %rbp
	subq $48, %rsp
	movq $10, %rax
	movl %eax, -12(%rbp)
	movq $3, %rax
	movl %eax, -16(%rbp)
	movslq -12(%rbp), %rax
	movslq -16(%rbp), %rcx
	addq %rcx, %rax
	movl %eax, -24(%rbp)
	movslq -24(%rbp), %rax
	movq $2, %rcx
	imulq %rcx, %rax
	movl %eax, -28(%rbp)
	movslq -12(%rbp), %rax
	movslq -16(%rbp), %rcx
	cqto
	idivq %rcx
	movl %eax, -32(%rbp)
	movslq -28(%rbp), %rax
	movslq -32(%rbp), %rcx
	subq %rcx, %rax
	movl %eax, -36(%rbp)
	movslq -36(%rbp), %rax
	movl %eax, -20(%rbp)
	movslq -20(%rbp), %rax
	negq %rax
	movl %eax, -40(%rbp)
	movslq -40(%rbp), %rax
	movq $30, %rcx
	addq %rcx, %rax
	movl %eax, -44(%rbp)
	movslq -44(%rbp), %rax
	movq %rbp, %rsp
	popq %rbp
	ret
	movq %rbp, %rsp
	popq %rbp
	ret
	.section .note.GNU-stack,"",@progbits
