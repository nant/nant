# Simple makefile to build NAnt with Core and DotNetTasks
MCS=mcs

all: 
	make bootstrap
	make nant

bootstrap:
	if test x$(OS) = xWindows_NT; then make windows-bootstrap; else make linux-bootstrap; fi

linux-bootstrap:
	cp lib/*.* bin/
	cp lib/mono/1.0/*.* bin/
	make linux-bootstrap-nant
	make linux-bootstrap-nant.core
	make linux-bootstrap-nant.dotnet

windows-bootstrap:
	echo Windows-based make is not yet supported; exit 1; \

linux-bootstrap-nant:
	$(MCS) -target:exe -define:MONO -debug -o bin/NAnt.exe -r:lib/mono/1.0/log4net.dll -recurse:src/NAnt.Console/*.cs src/CommonAssemblyInfo.cs
	cp src/NAnt.Console/NAnt.Console.exe.config.linux bin/NAnt.exe.config

linux-bootstrap-nant.core:
	$(MCS) -target:library -define:MONO -debug -o bin/NAnt.Core.dll -r:bin/log4net.dll -r:System.Web.dll -recurse:src/NAnt.Core/*.cs src/CommonAssemblyInfo.cs

linux-bootstrap-nant.dotnet:
	$(MCS) -target:library -define:MONO -debug -o bin/NAnt.DotNetTasks.dll -r:./bin/NAnt.Core.dll -r:bin/NDoc.Core.dll -recurse:src/NAnt.DotNet/*.cs src/CommonAssemblyInfo.cs

nant:
	if test x$(OS) = xWindows_NT; then make windows-nant; else make linux-nant; fi

linux-nant:
	mono bin/NAnt.exe -buildfile:NAnt.build build

windows-nant:
	echo Windows-based make is not yet supported; exit 1;\

clean:
	rm bin/*.* -f
