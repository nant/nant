<?xml version="1.0" ?>
<!--
// NAnt - A .NET build tool
// Copyright (C) 2001-2002 Gerry Shaw
//
// This program is free software; you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation; either version 2 of the License, or
// (at your option) any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with this program; if not, write to the Free Software
// Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307  USA

// Ian MacLean (ian@maclean.ms)
// Gerry Shaw (gerry_shaw@yahoo.com)
-->
<xslt:stylesheet xmlns:xslt="http://www.w3.org/1999/XSL/Transform" version="1.0" xmlns:msxsl="urn:schemas-microsoft-com:xslt"
    xmlns:stringutils="urn:schemas-sourceforge.net-blah" xmlns:nant="unknown-at-this-time"
>
    <xslt:output method="text" />
    <msxsl:script language="C#" implements-prefix="stringutils">
    <![CDATA[
        public string PadRight( string str, int padding) {
            return str.PadRight(padding);
        }
    ]]>
    </msxsl:script>
    <!-- Handle newlines here -->
    <xslt:variable name="newline">
        <xslt:text>&#10;</xslt:text>
    </xslt:variable>
    <!-- tabs -->
    <xslt:variable name="tab">
        <xslt:text>&#9;</xslt:text>
    </xslt:variable>
    <!-- spaces -->
    <xslt:variable name="space">
        <xslt:text></xslt:text>
    </xslt:variable>
    <xslt:template match="nant:target/@name"></xslt:template>
    <xslt:template match="nant:project">
        <!-- get the project description -->
        <xslt:apply-templates select="nant:description" />
        <!-- output default target -->
        <xslt:text>Default Target: </xslt:text>
        <xslt:value-of select="$newline" />
        <xslt:value-of select="$newline" />
        <xslt:apply-templates select="nant:target[@name=(../@default) ]" />
        <xslt:value-of select="$newline" />
        <!-- output main targets (targets with a description) -->
        <xslt:text>Main Targets: </xslt:text>
        <xslt:value-of select="$newline" />
        <xslt:value-of select="$newline" />
        <xslt:apply-templates select="nant:target[string(@description) != '' ]">
            <xslt:sort select="@name" order="ascending" />
        </xslt:apply-templates>
        <xslt:value-of select="$newline" />
        <!-- output sub targets (targets without a description) -->        
        <xslt:text>Sub Targets: </xslt:text>
        <xslt:value-of select="$newline" />
        <xslt:value-of select="$newline" />
        <xslt:if test="count(nant:target[string(@description) = '' ]) > 0">
            <xslt:apply-templates select="nant:target[string(@description) = '' ]">
                <xslt:sort select="@name" order="ascending" />
            </xslt:apply-templates>
            <xslt:value-of select="$newline" />
        </xslt:if>
    </xslt:template>
    <xslt:template match="nant:target">
        <xslt:value-of select="$space" />
        <xslt:value-of select="stringutils:PadRight(@name, 20)" />
        <xslt:value-of select="@description" />
        <xslt:value-of select="$newline" />
    </xslt:template>
    <xslt:template match="nant:description">
        <xslt:value-of select="$space" />
        <xslt:value-of select="./node()" />
        <xslt:value-of select="$newline" />
        <xslt:value-of select="$newline" />        
    </xslt:template>
</xslt:stylesheet>
