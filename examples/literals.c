long big() {
    return 100000L;
}

float half(float x) {
    return x / 2.0f;
}

char next(char c) {
    return c + 1;
}

bool is_a(char c) {
    return c == 'a';
}

int main() {
    double d = 3.14;
    float f = half(5.0);
    bool t = true;
    bool n = false;
    char nl = '\n';
    long l = big();
    if (t) {
        if (is_a(next('`'))) {
            return 1;
        }
    }
    return 0;
}
