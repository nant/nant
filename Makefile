#NAnt make file for *nix
MONO=mono
MCS=mcs

ifndef DIRSEP
ifeq ($(OS),Windows_NT)
DIRSEP = \\
else
DIRSEP = /
endif
endif

ifndef PLATFORM_REFERENCES
ifeq ($(OS),Windows_NT)
PLATFORM_REFERENCES = \
	bootstrap/NAnt.Win32Tasks.dll
endif
endif

ifeq ($(MONO),mono)
FRAMEWORK_DIR = mono
DEFINE = MONO
else
FRAMEWORK_DIR = net
DEFINE= NET
endif



NANT=$(MONO) bootstrap/NAnt.exe


all: build-bootstrap build-nant

build-nant: 
	$(NANT) -f:NAnt.build build

clean:
	rm -fR build bootstrap

install: build-bootstrap
	$(NANT) -f:NAnt.build install -D:install.prefix=$(prefix)

	
bootstrap/NAnt.exe:
	$(MCS) -target:exe -define:${DEFINE} -out:bootstrap${DIRSEP}NAnt.exe -r:bootstrap${DIRSEP}log4net.dll \
		-recurse:src${DIRSEP}NAnt.Console${DIRSEP}*.cs src${DIRSEP}CommonAssemblyInfo.cs
	

build-bootstrap: setup bootstrap/NAnt.exe bootstrap/NAnt.Core.dll bootstrap/NAnt.DotNetTasks.dll bootstrap/NAnt.CompressionTasks.dll ${PLATFORM_REFERENCES}
	

setup:
	mkdir -p bootstrap
	cp -R lib/ bootstrap/lib
	# Mono loads log4net before privatebinpath is set-up, so we need this in the same directory
	# as NAnt.exe
	cp lib/log4net.dll bootstrap
	cp src/NAnt.Console/App.config bootstrap/NAnt.exe.config

bootstrap/NAnt.Core.dll:
	$(MCS) -target:library -warn:0 -define:${DEFINE} -out:bootstrap/NAnt.Core.dll -r:lib${DIRSEP}log4net.dll \
		-r:System.Web.dll -recurse:src${DIRSEP}NAnt.Core${DIRSEP}*.cs src${DIRSEP}CommonAssemblyInfo.cs

bootstrap/NAnt.DotNetTasks.dll:
	$(MCS) -target:library -warn:0 -define:MONO -out:bootstrap/NAnt.DotNetTasks.dll -r:./bootstrap/NAnt.Core.dll \
		-r:bootstrap/lib/${FRAMEWORK_DIR}/1.0/NDoc.Core.dll -recurse:src${DIRSEP}NAnt.DotNet${DIRSEP}*.cs \
		src${DIRSEP}CommonAssemblyInfo.cs

bootstrap/NAnt.CompressionTasks.dll:
	$(MCS) -target:library -warn:0 -define:MONO -out:bootstrap/NAnt.CompressionTasks.dll -r:./bootstrap/NAnt.Core.dll \
		-r:bootstrap/lib/ICSharpCode.SharpZipLib.dll -recurse:src${DIRSEP}NAnt.Compression${DIRSEP}*.cs \
		src${DIRSEP}CommonAssemblyInfo.cs

bootstrap/NAnt.Win32Tasks.dll:
	$(MCS) -target:library -warn:0 -define:${DEFINE} -out:bootstrap/NAnt.Win32Tasks.dll \
		-r:./bootstrap/NAnt.Core.dll -r:./bootstrap/NAnt.DotNetTasks.dll -r:System.ServiceProcess.dll \
		-r:Microsoft.JScript.dll -recurse:src${DIRSEP}NAnt.Win32${DIRSEP}*.cs \
		src${DIRSEP}CommonAssemblyInfo.cs

