call "C:\Program Files (x86)\Microsoft Visual Studio 10.0\VC\vcvarsall.bat" x86

nmake -f Makefile.nmake clean bootstrap
.\bootstrap\nant.exe -t:net-3.5 -buildfile:nant.build -D:project.version=0.94 -D:project.release.type=alpha -D:target.platform=x86 clean release build test