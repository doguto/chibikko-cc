int classify(int n) {
    if (n < 0) {
        return 1;
    } else if (n == 0) {
        return 2;
    } else {
        return 4;
    }
}

int main() {
    return classify(-5) * 100 + classify(0) * 10 + classify(42);
}
