NAnt

What is it? 
-----------
NAnt is a .NET based build tool. In theory it is kind of like make without 
make's wrinkles. In practice it's a lot like Ant. 
  
If you are not familiar with Jakarta Ant you can get more information at the
Ant project web site (http://ant.apache.org/).


Why NAnt?
---------
Because Ant was too Java specific.
Because Ant needed the Java runtime.  NAnt only needs the .NET 
or Mono runtime.


The Latest Version
------------------
Details of the latest version can be found on the NAnt project web site
http://nant.sourceforge.net/


Compilation and Installation
-------------------------------

   a. Build Requirements
   --------------------
   To build NAnt, you will need the following components:

   on Windows

       * A version of the Microsoft .NET Framework

         Available from http://msdn.microsoft.com/netframework/
         
         you will need the .NET Framework SDK as well as the runtime components 
	 if you intend to compile programs.

         note that NAnt currently supports versions 1.0, 1.1 and 2.0 (Beta 1) 
	 of the Microsoft .NET Framework. 

       or

       * Mono for Windows (version 1.0 or higher)

         Available from http://www.mono-project.com/downloads/
   
   Linux/Unix

       * GNU toolchain - including GNU make

       * pkg-config

           Available from: http://www.freedesktop.org/Software/pkgconfig

       * A working Mono installation and development libraries (version 1.0 or higher)

           Available from: http://www.mono-project.com/downloads/

           
    b. Building the Software
    ------------------------
      
    Windows (with Microsoft .NET)
        bin\NAnt.exe
        bin\NAnt.exe install -D:install.prefix=c:\Program Files

    Windows (with Mono)
        mono bin\NAnt.exe
        mono bin\NAnt.exe install -D:install.prefix=c:\Program Files
    
    Linux/Unix
        make
        make install prefix=/usr/local

Note: 

These instructions only apply to the source distribution of NAntContrib, as the binary distribution 
contains pre-built assemblies.


Documentation
-------------
Documentation is available in HTML format, in the doc/ directory.


License
-------
Copyright (C) 2001-2004 Gerry Shaw

This program is free software; you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation; either version 2 of the License, or
(at your option) any later version.

This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU General Public License for more details.

You should have received a copy of the GNU General Public License
along with this program; if not, write to the Free Software
Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307 USA

In addition, as a special exception, Gerry Shaw gives permission to link the 
code of this program with the Microsoft .NET library (or with modified versions 
of Microsoft .NET library that use the same license as the Microsoft .NET 
library), and distribute linked combinations including the two.  You must obey 
the GNU General Public License in all respects for all of the code used other 
than the Microsoft .NET library.  If you modify this file, you may extend this 
exception to your version of the file, but you are not obligated to do so.  If 
you do not wish to do so, delete this exception statement from your version.

A copy of the GNU General Public License is available in the COPYING.txt file 
included with all NAnt distributions.

For more licensing information refer to the GNU General Public License on the 
GNU Project web site.
http://www.gnu.org/copyleft/gpl.html
