int main() {
    int sum = 0;
    int i;
    for (i = 1; i <= 10; i = i + 1) {
        sum = sum + i;
    }

    int n = 5;
    while (n > 0) {
        sum = sum + n;
        n = n - 1;
    }
    return sum;
}
