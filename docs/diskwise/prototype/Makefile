CC      ?= cc
CFLAGS  ?= -std=c11 -O2 -Wall -Wextra -Werror -pedantic
SAN     ?= -fsanitize=address,undefined -fno-omit-frame-pointer -g

all: test

build/test_sel: selection_model.c tests.c selection_model.h
	@mkdir -p build
	$(CC) $(CFLAGS) $(SAN) -o $@ selection_model.c tests.c

test: build/test_sel
	./build/test_sel

clean:
	rm -rf build

.PHONY: all test clean
