Summary: NAnt - A cross platform build tool for the .Net platform.
Name: NAnt
Version: 0.85
Release: 1
Copyright: GPL
Group: Development/Tools
Source: NAnt.tar.gz
Distribution: Fedora Core
Vendor: http://nant.sourceforge.net
Packager: http://nant.sourceforge.net
Prefix: /usr/lib/NAnt
BuildArch: i386
BuildRoot: %{_tmppath}/%{name}-%{version}-%{release}-root
Requires: mono

%description
NAnt is a .NET based build tool. In theory it is kind of like make without 
make's wrinkles. In practice it's a lot like Ant.

If you are not familiar with Jakarta Ant you can get more information at the
Ant project web site (http://ant.apache.org/).

If you are not familiar with NAnt you can get more information at the NAnt 
project web site (http://NAnt.sourceforge.net).

%files
%attr(755, root, root) %{_bindir}/%{name}
%{_libdir}/%{name}-%{version}-%{release}/*

%pre
echo "Installing NAnt ..."

%post
echo "NAnt has been installed; For usage type NAnt.exe -help."

%prep
%setup -n %{name}-%{version}-%{release} -c

%build
if [ -d "$RPM_BUILD_ROOT" ] && [ -d "$RPM_BUILD_ROOT"%{_libdir} ] && [ -d "$RPM_BUILD_ROOT"%{_bindir} ]
then
    echo BuildRoot is created.
else
    mkdir -p "$RPM_BUILD_ROOT"%{_libdir}
    mkdir -p "$RPM_BUILD_ROOT"%{_bindir}
fi

%install
rm -rf "$RPM_BUILD_ROOT"%{_libdir}/%{name}-%{version}-%{release}
mkdir "$RPM_BUILD_ROOT"%{_libdir}/%{name}-%{version}-%{release}
echo 'mono '"$RPM_BUILD_ROOT"%{_libdir}/%{name}-%{version}-%{release}'/NAnt.exe' > "$RPM_BUILD_ROOT"%{_bindir}/%{name}
cp -R bin/* "$RPM_BUILD_ROOT"%{_libdir}/%{name}-%{version}-%{release}

%preun
echo "Uninstalling NAnt ..."
%postun
rm -rf "$RPM_BUILD_ROOT"%{_bindir}/%{name}-%{version}-%{release}
rm -rf ${RPM_BUILD_ROOT"%{_libdir}/%{name}
echo "NAnt has been removed from your system."

%clean
[ "$RPM_BUILD_ROOT" -a "$RPM_BUILD_ROOT" != / ] && rm -rf "$RPM_BUILD_ROOT"

%changelog
* Sat Aug 21 2004 Clayton Harbour <claytonharbour@sporadicism.com>
- Initial RPM.
