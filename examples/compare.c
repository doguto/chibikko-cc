int main() {
    int a = 5;
    int b = 7;
    int r = 0;
    if (a < b) {
        r = r + 1;
    }
    if (a >= b) {
        r = r + 10;
    } else {
        r = r + 100;
    }
    if (a != b) {
        r = r + 1000;
    }
    return r;
}
