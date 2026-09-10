# アセンブリ

### add rax, rdi

rax に rdi を加算する

### sub rax, rdi

rax から rdi を減算する

### mul rax, rdi

rax に rdi を乗算する

### idiv rdi


### cmp rax, rdi

rax と rdi を比較する.
結果は フラグレジスタ に格納される.

### sete al

フラグレジスタの値を al（raxの下位8bit）に格納する.
引数には 8bit レジスタしか指定できない.

### movzb rax, al

8bit レジスタ al の値を rax の下位 8bit に格納する.
それ以外の 56bit は 0 で埋められる.

### push x

x をスタックにプッシュする

### pop rax

rax にスタックのトップをポップする