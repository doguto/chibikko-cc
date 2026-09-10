CFLAGS=-std=c11 -g -static
SRCDIR=src
SRCS=$(wildcard $(SRCDIR)/*.c)
OBJS=$(SRCS:.c=.o)

chibikko: $(OBJS)
	$(CC) -o chibikko $(OBJS) $(LDFLAGS)

$(OBJS): $(SRCDIR)/chibikko_cc.h

test: chibikko
	./test.sh

clean:
	rm -f chibikko $(SRCDIR)/*.o *~ tmp*

.PHONY: test clean
