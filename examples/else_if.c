int classify(int n) {
    if (n < 0) {
        return 1;
    } else if (n == 0) {
        return 2;
    } else if (n < 10) {
        return 3;
    } else {
        return 4;
    }
}

int main() {
    return classify(-5) * 1000 + classify(0) * 100 + classify(7) * 10 + classify(42);
}
