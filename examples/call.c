int add(int a, int b) {
    return a + b;
}

void nop() {
    return;
}

int main() {
    int x;
    int y;
    x = y = 4;
    nop();
    return add(x, add(y, 2));
}
