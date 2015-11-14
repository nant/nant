# NAnt make makefile for Mono
MONO=mono
MCS=gmcs
RESGEN=resgen
TARGET=mono-2.0

# Contains a list of acceptable targets used to build NAnt
VALID_TARGETS := mono-2.0 mono-3.5 mono-4.0 mono-4.5 net-2.0 net-3.5 net-4.0 net-4.5

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
DEFINE = NET
endif

# Validates TARGET var. If the value of TARGET exists
# in VALID_TARGETS array, SELECTED_TARGET will contain
# the value of TARGET; otherwise SELECTED_TARGET var
# will be empty
SELECTED_TARGET := $(filter $(TARGET),$(VALID_TARGETS))

# If TARGET var is valid, load the DEFINE var
# based on value of TARGET
ifneq ($(SELECTED_TARGET),)

# Loads (net,mono)-2.0 DEFINE vars
ifeq ($(findstring 2.0,$(SELECTED_TARGET)),2.0)
DEFINE := $(DEFINE),NET_1_0,NET_1_1,NET_2_0,ONLY_2_0
endif

# Loads (net,mono)-3.5 DEFINE vars
ifeq ($(findstring 3.5,$(SELECTED_TARGET)),3.5)
DEFINE := $(DEFINE),NET_1_0,NET_1_1,NET_2_0,NET_3_5,ONLY_3_5
endif

# Loads (net,mono)-4.0 DEFINE vars
ifeq ($(findstring 4.0,$(SELECTED_TARGET)),4.0)
DEFINE := $(DEFINE),NET_1_0,NET_1_1,NET_2_0,NET_3_5,NET_4_0,ONLY_4_0
endif

# Loads (net,mono)-4.5 DEFINE vars
ifeq ($(findstring 4.5,$(SELECTED_TARGET)),4.5)
DEFINE := $(DEFINE),NET_1_0,NET_1_1,NET_2_0,NET_3_5,NET_4_0,NET_4_5,ONLY_4_5
endif

# If TARGET var is invalid, throw an error
else
$(error Specified target "$(TARGET)" is not valid)
endif

# Make sure that -debug+ is specified in NAnt command if DEBUG is defined
ifdef DEBUG
NANT_DEBUG := -debug+
endif

# Assign remaining vars
TARGET_FRAMEWORK = -t:$(TARGET)
NANT = $(MONO) bootstrap/NAnt.exe -j $(NANT_DEBUG)

# Targets
all: bootstrap build-nant

build-nant: bootstrap
	$(NANT) $(TARGET_FRAMEWORK) -f:NAnt.build build

clean:
ifeq ($(OS),Windows_NT)
	if exist bootstrap rmdir /S /Q bootstrap
	if exist build rmdir /S /Q build
else
	rm -fR build bootstrap
endif

install: bootstrap
	$(NANT) $(TARGET_FRAMEWORK) -f:NAnt.build install -D:prefix="$(prefix)" -D:destdir="$(DESTDIR)" -D:doc.prefix="$(docdir)"

run-test: bootstrap
	$(NANT) $(TARGET_FRAMEWORK) -f:NAnt.build test
	
bootstrap/NAnt.exe:
	$(MCS) $(DEBUG) -target:exe -define:$(DEFINE) -out:bootstrap${DIRSEP}NAnt.exe -r:bootstrap${DIRSEP}log4net.dll \
		-r:System.Configuration.dll -recurse:src${DIRSEP}NAnt.Console${DIRSEP}*.cs src${DIRSEP}CommonAssemblyInfo.cs


bootstrap: setup bootstrap/NAnt.exe bootstrap/NAnt.Core.dll bootstrap/NAnt.DotNetTasks.dll bootstrap/NAnt.CompressionTasks.dll ${PLATFORM_REFERENCES}
	

setup:
ifeq ($(OS),Windows_NT)
	if not exist bootstrap md bootstrap
	if not exist bootstrap\lib md bootstrap\lib
	xcopy lib bootstrap\lib /S /Y /Q
	copy lib\common\neutral\log4net.dll bootstrap
	copy src\NAnt.Console\App.config bootstrap\NAnt.exe.config
else
	mkdir -p bootstrap
	cp -R lib/ bootstrap/lib
	# Mono loads log4net before privatebinpath is set-up, so we need this in the same directory
	# as NAnt.exe
	cp lib/common/neutral/log4net.dll bootstrap
	cp src/NAnt.Console/App.config bootstrap/NAnt.exe.config
endif

bootstrap/NAnt.Core.dll:
	$(RESGEN)  src/NAnt.Core/Resources/Strings.resx bootstrap/NAnt.Core.Resources.Strings.resources
	$(MCS) $(DEBUG) -target:library -warn:0 -define:$(DEFINE) -out:bootstrap/NAnt.Core.dll -debug \
		-resource:bootstrap/NAnt.Core.Resources.Strings.resources -r:lib${DIRSEP}common${DIRSEP}neutral${DIRSEP}log4net.dll \
		-r:System.Web.dll -r:System.Configuration.dll -recurse:src${DIRSEP}NAnt.Core${DIRSEP}*.cs src${DIRSEP}CommonAssemblyInfo.cs

bootstrap/NAnt.DotNetTasks.dll:
	$(RESGEN)  src/NAnt.DotNet/Resources/Strings.resx bootstrap/NAnt.DotNet.Resources.Strings.resources
	$(MCS) $(DEBUG) -target:library -warn:0 -define:$(DEFINE) -out:bootstrap/NAnt.DotNetTasks.dll \
		-r:./bootstrap/NAnt.Core.dll -r:bootstrap/lib/common/neutral/NDoc.Core.dll \
		-recurse:src${DIRSEP}NAnt.DotNet${DIRSEP}*.cs -resource:bootstrap/NAnt.DotNet.Resources.Strings.resources \
		src${DIRSEP}CommonAssemblyInfo.cs

bootstrap/NAnt.CompressionTasks.dll:
	$(MCS) $(DEBUG) -target:library -warn:0 -define:$(DEFINE) -out:bootstrap/NAnt.CompressionTasks.dll \
		-r:./bootstrap/NAnt.Core.dll -r:bootstrap/lib/common/neutral/ICSharpCode.SharpZipLib.dll \
		-recurse:src${DIRSEP}NAnt.Compression${DIRSEP}*.cs src${DIRSEP}CommonAssemblyInfo.cs

bootstrap/NAnt.Win32Tasks.dll:
	$(MCS) $(DEBUG) -target:library -warn:0 -define:$(DEFINE) -out:bootstrap/NAnt.Win32Tasks.dll \
		-r:./bootstrap/NAnt.Core.dll -r:./bootstrap/NAnt.DotNetTasks.dll -r:System.ServiceProcess.dll \
		-recurse:src${DIRSEP}NAnt.Win32${DIRSEP}*.cs src${DIRSEP}CommonAssemblyInfo.cs
