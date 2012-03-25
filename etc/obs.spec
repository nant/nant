# spec file for package nant
#
# NAnt - A .NET build tool
# Copyright (C) 2001-2003 Gerry Shaw
#
# This program is free software; you can redistribute it and/or modify
# it under the terms of the GNU General Public License as published by
# the Free Software Foundation; either version 2 of the License, or
# (at your option) any later version.
#
# This program is distributed in the hope that it will be useful,
# but WITHOUT ANY WARRANTY; without even the implied warranty of
# MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
# GNU General Public License for more details.
#
# You should have received a copy of the GNU General Public License
# along with this program; if not, write to the Free Software
# Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307  USA
#
# Please submit bugfixes or comments via http://nant.sourceforge.net/

# NAnt Version vars
%define nant_version_num 0.91
%define nant_version_type final

# NAnt Makefile arguments
%define nant_make_args TARGET=mono-2.0 MCS=gmcs

# Create nant_version to use for filename purposes
%if 0%(test "%nant_version_type" = "final" && echo 1 || echo 0)
%define nant_version %{nant_version_num}
%else
%define nant_version %{nant_version_num}-%{nant_version_type}
%endif

Name:           nant
# We have to append a .0 to make sure the rpm upgrade versioning works.
#  nant's progression: 0.85-rc4, 0.85
#  working rpm upgrade path requires: 0.85-rc4, 0.85.0
Version:        %{nant_version_num}
Release:        0
License:        GPL-2.0+
Url:            http://nant.sourceforge.net
Vendor:         http://nant.sourceforge.net
Source0:        %{name}-%{nant_version}-src.tar.gz
Summary:        NAnt - A cross platform build tool for the .Net platform
Group:          Development/Tools/Building
BuildRoot:      %{_tmppath}/%{name}-%{nant_version}-build
#BuildArch:      noarch

# Only needed when building from prefer rpms (normally mono-devel depends on glib2-devel)
BuildRequires:  glib2-devel
BuildRequires:  mono-data mono-devel pkgconfig

####  suse  ####
%if 0%{?suse_version}
%define old_suse_buildrequires mono-winforms mono-web
%if %sles_version == 9
BuildRequires:  %{old_suse_buildrequires}
%define env_options export PKG_CONFIG_PATH=$PKG_CONFIG_PATH:/opt/gnome/%_lib/pkgconfig
%endif
%endif
# Fedora options (Bug in fedora images where 'abuild' user is the same id as 'nobody')
%if 0%{?fedora_version} || 0%{?rhel_version}
%define env_options export MONO_SHARED_DIR=/tmp
%endif

%description
NAnt is a .NET based build tool. In theory it is kind of like make without 
make's wrinkles. In practice it's a lot like Ant.

If you are not familiar with Jakarta Ant you can get more information at the
Ant project web site (http://ant.apache.org/).

If you are not familiar with NAnt you can get more information at the NAnt 
project web site (http://NAnt.sourceforge.net).

Authors:
--------
    Gerry Shaw

%files
%defattr(-, root, root)
%{_bindir}/nant
%{_datadir}/NAnt
%{_prefix}/lib/pkgconfig/nant.pc
%doc %{_prefix}/share/doc/NAnt

%post
echo "NAnt has been installed; For usage type NAnt.exe -help."

%prep
%setup  -q -n %{name}-%{nant_version}

%build
%{?env_options}
make %{nant_make_args}

%install
%{?env_options}
make install %{nant_make_args} DESTDIR=${RPM_BUILD_ROOT} prefix=%{_prefix}

%preun
echo "NAnt has been uninstalled"

%clean
rm -rf "$RPM_BUILD_ROOT"
%if 0%{?fedora_version} || 0%{?rhel_version}
# Allows overrides of __find_provides in fedora distros... (already set to zero on newer suse distros)
%define _use_internal_dependency_generator 0
%endif
# ignore some bundled dlls
%define __find_provides env sh -c 'filelist=($(grep -v log4net.dll | grep -v scvs.exe | grep -v nunit | grep -v NDoc | grep -v neutral)) && { printf "%s\\n" "${filelist[@]}" | /usr/lib/rpm/find-provides && printf "%s\\n" "${filelist[@]}" | /usr/bin/mono-find-provides ; } | sort | uniq'
%define __find_requires env sh -c 'filelist=($(cat)) && { printf "%s\\n" "${filelist[@]}" | /usr/lib/rpm/find-requires && printf "%s\\n" "${filelist[@]}" | /usr/bin/mono-find-requires ; } | sort | uniq'

%changelog
* Sat Mar 24 2012 Ryan Boggs <rmboggs@gmail.com>
- Revamped RPM spec file for build.opensuse.org (Project: Mono:NAnt)