#!/bin/sh
make -f makefile.linux
mono bin/linux/mininant.exe linux-build
