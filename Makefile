#NAnt make makefile for *nix
MONO=mono
MCS=mcs
RESGEN=resgen

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

ifdef TARGET
TARGET_FRAMEWORK = -t:$(TARGET)
endif

NANT=$(MONO) bootstrap/NAnt.exe


all: bootstrap build-nant

build-nant: 
	$(NANT) $(TARGET_FRAMEWORK) -f:NAnt.build build

clean:
	rm -fR build bootstrap

install: bootstrap
	$(NANT) $(TARGET_FRAMEWORK) -f:NAnt.build install -D:prefix="$(prefix)" -D:destdir="$(destdir)" -D:doc.prefix="$(docdir)"

run-test: bootstrap
	$(NANT) $(TARGET_FRAMEWORK) -f:NAnt.build test
	
bootstrap/NAnt.exe:
	$(MCS) -target:exe -define:${DEFINE} -out:bootstrap${DIRSEP}NAnt.exe -r:bootstrap${DIRSEP}log4net.dll \
		-recurse:src${DIRSEP}NAnt.Console${DIRSEP}*.cs src${DIRSEP}CommonAssemblyInfo.cs
	

bootstrap: setup bootstrap/NAnt.exe bootstrap/NAnt.Core.dll bootstrap/NAnt.DotNetTasks.dll bootstrap/NAnt.CompressionTasks.dll ${PLATFORM_REFERENCES}
	

setup:
	mkdir -p bootstrap
	cp -R lib/ bootstrap/lib
	# Mono loads log4net before privatebinpath is set-up, so we need this in the same directory
	# as NAnt.exe
	cp lib/common/neutral/log4net.dll bootstrap
	cp src/NAnt.Console/App.config bootstrap/NAnt.exe.config

bootstrap/NAnt.Core.dll:
	$(RESGEN)  src/NAnt.Core/Resources/Strings.resx bootstrap/NAnt.Core.Resources.Strings.resources
	$(MCS) -target:library -warn:0 -define:${DEFINE} -out:bootstrap/NAnt.Core.dll -debug \
		-resource:bootstrap/NAnt.Core.Resources.Strings.resources -r:lib${DIRSEP}common${DIRSEP}neutral${DIRSEP}log4net.dll \
		-r:System.Web.dll -recurse:src${DIRSEP}NAnt.Core${DIRSEP}*.cs src${DIRSEP}CommonAssemblyInfo.cs

bootstrap/NAnt.DotNetTasks.dll:
	$(RESGEN)  src/NAnt.DotNet/Resources/Strings.resx bootstrap/NAnt.DotNet.Resources.Strings.resources
	$(MCS) -target:library -warn:0 -define:MONO -out:bootstrap/NAnt.DotNetTasks.dll \
		-r:./bootstrap/NAnt.Core.dll -r:bootstrap/lib/common/neutral/NDoc.Core.dll \
		-recurse:src${DIRSEP}NAnt.DotNet${DIRSEP}*.cs -resource:bootstrap/NAnt.DotNet.Resources.Strings.resources \
		src${DIRSEP}CommonAssemblyInfo.cs

bootstrap/NAnt.CompressionTasks.dll:
	$(MCS) -target:library -warn:0 -define:MONO -out:bootstrap/NAnt.CompressionTasks.dll \
		-r:./bootstrap/NAnt.Core.dll -r:bootstrap/lib/common/neutral/ICSharpCode.SharpZipLib.dll \
		-recurse:src${DIRSEP}NAnt.Compression${DIRSEP}*.cs src${DIRSEP}CommonAssemblyInfo.cs

bootstrap/NAnt.Win32Tasks.dll:
	$(MCS) -target:library -warn:0 -define:${DEFINE} -out:bootstrap/NAnt.Win32Tasks.dll \
		-r:./bootstrap/NAnt.Core.dll -r:./bootstrap/NAnt.DotNetTasks.dll -r:System.ServiceProcess.dll \
		-r:Microsoft.JScript.dll -recurse:src${DIRSEP}NAnt.Win32${DIRSEP}*.cs \
		src${DIRSEP}CommonAssemblyInfo.cs
