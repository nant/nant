MONO=mono

ifeq ($(OS), Windows_NT)
NANT=bin\NAnt.exe
else
NANT=$(MONO) bin/NAnt.exe
endif

all: build-nant

build-nant: 
	$(NANT) -f:NAnt.build build

clean:
	rm -fR build

install:
	$(NANT) -f:NAnt.build install -D:install.prefix=$(prefix)

