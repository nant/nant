MONO=mono

all: 
	if test x$(OS) = xWindows_NT; then make windows-nant; else make linux-nant; fi

linux-nant:
	$(MONO) bin/NAnt.exe -buildfile:NAnt.build build

windows-nant:
	bin/NAnt.exe -buildfile:NAnt.build build

clean:
	rm -fR build
