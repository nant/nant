<?xml version="1.0" encoding="utf-8" ?>
<!--
// NAnt - A .NET build tool
// Copyright (C) 2001-2003 Gerry Shaw
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
//
// Ian MacLean (ian@maclean.ms)
// Gerry Shaw (gerry_shaw@yahoo.com)
// Gert Driesen (drieseng@users.sourceforge.net.com)
-->
<xsl:stylesheet version="1.0" xmlns="http://www.w3.org/1999/xhtml" xmlns:xsl="http://www.w3.org/1999/XSL/Transform" xmlns:NAntUtil="urn:NAntUtil" exclude-result-prefixes="NAntUtil">
    <xsl:template name="get-type-name">
        <xsl:param name="type" />
        <xsl:variable name="namespace">
            <xsl:value-of select="concat(../../@name, '.')" />
        </xsl:variable>
        <xsl:choose>
            <xsl:when test="contains($type, $namespace)">
                <xsl:value-of select="string(NAntUtil:GetName(concat('T:',$type)))" />
            </xsl:when>
            <xsl:otherwise>
                <xsl:call-template name="csharp-type">
                    <xsl:with-param name="runtime-type" select="$type" />
                </xsl:call-template>
            </xsl:otherwise>
        </xsl:choose>
    </xsl:template>

    <xsl:template name="csharp-type">
        <xsl:param name="runtime-type" />
        <xsl:variable name="old-type">
            <xsl:choose>
                <xsl:when test="contains($runtime-type, '[')">
                    <xsl:value-of select="substring-before($runtime-type, '[')" />
                </xsl:when>
                <xsl:when test="contains($runtime-type, '&amp;')">
                    <xsl:value-of select="substring-before($runtime-type, '&amp;')" />
                </xsl:when>
                <xsl:otherwise>
                    <xsl:value-of select="$runtime-type" />
                </xsl:otherwise>
            </xsl:choose>
        </xsl:variable>
        <xsl:variable name="new-type">
            <xsl:choose>
                <xsl:when test="$old-type='System.Byte'">byte</xsl:when>
                <xsl:when test="$old-type='Byte'">byte</xsl:when>
                <xsl:when test="$old-type='System.SByte'">sbyte</xsl:when>
                <xsl:when test="$old-type='SByte'">sbyte</xsl:when>
                <xsl:when test="$old-type='System.Int16'">short</xsl:when>
                <xsl:when test="$old-type='Int16'">short</xsl:when>
                <xsl:when test="$old-type='System.UInt16'">ushort</xsl:when>
                <xsl:when test="$old-type='UInt16'">ushort</xsl:when>
                <xsl:when test="$old-type='System.Int32'">int</xsl:when>
                <xsl:when test="$old-type='Int32'">int</xsl:when>
                <xsl:when test="$old-type='System.UInt32'">uint</xsl:when>
                <xsl:when test="$old-type='UInt32'">uint</xsl:when>
                <xsl:when test="$old-type='System.Int64'">long</xsl:when>
                <xsl:when test="$old-type='Int64'">long</xsl:when>
                <xsl:when test="$old-type='System.UInt64'">ulong</xsl:when>
                <xsl:when test="$old-type='UInt64'">ulong</xsl:when>
                <xsl:when test="$old-type='System.Single'">float</xsl:when>
                <xsl:when test="$old-type='Single'">float</xsl:when>
                <xsl:when test="$old-type='System.Double'">double</xsl:when>
                <xsl:when test="$old-type='Double'">double</xsl:when>
                <xsl:when test="$old-type='System.Decimal'">decimal</xsl:when>
                <xsl:when test="$old-type='Decimal'">decimal</xsl:when>
                <xsl:when test="$old-type='System.String'">string</xsl:when>
                <xsl:when test="$old-type='String'">string</xsl:when>
                <xsl:when test="$old-type='System.Char'">char</xsl:when>
                <xsl:when test="$old-type='Char'">char</xsl:when>
                <xsl:when test="$old-type='System.Boolean'">bool</xsl:when>
                <xsl:when test="$old-type='Boolean'">bool</xsl:when>
                <xsl:when test="$old-type='System.Void'">void</xsl:when>
                <xsl:when test="$old-type='Void'">void</xsl:when>
                <xsl:when test="$old-type='System.Object'">object</xsl:when>
                <xsl:when test="$old-type='Object'">object</xsl:when>
                <xsl:when test="$old-type='System.IO.FileInfo'">file</xsl:when>
                <xsl:when test="$old-type='System.IO.DirectoryInfo'">directory</xsl:when>
                <xsl:when test="$old-type='System.DateTime'">datetime</xsl:when>
                <xsl:when test="$old-type='System.TimeSpan'">timespan</xsl:when>
                <xsl:otherwise>
                    <xsl:value-of select="string(NAntUtil:GetName(concat('T:',$runtime-type)))" />
                </xsl:otherwise>
            </xsl:choose>
        </xsl:variable>
        <xsl:choose>
            <xsl:when test="contains($runtime-type, '[')">
                <xsl:value-of select="concat($new-type, '[', substring-after($runtime-type, '['))" />
            </xsl:when>
            <xsl:otherwise>
                <xsl:value-of select="$new-type" />
            </xsl:otherwise>
        </xsl:choose>
    </xsl:template>

    <!-- strip these elements, leave the text... -->
    <xsl:template match="summary" mode="slashdoc">
        <xsl:apply-templates mode="slashdoc" />
    </xsl:template>

    <xsl:template match="remarks" mode="slashdoc">
        <xsl:apply-templates mode="slashdoc" />
    </xsl:template>

    <xsl:template match="example" mode="slashdoc">
        <li>
            <xsl:apply-templates mode="slashdoc" />
        </li>
    </xsl:template>

    <xsl:template name="seealso-section">
        <xsl:if test="documentation//seealso">
            <h3>See Also</h3>
            <xsl:for-each select="documentation//seealso">
                <xsl:choose>
                    <xsl:when test="@cref">
                        <xsl:call-template name="get-a-href">
                            <xsl:with-param name="cref" select="@cref" />
                        </xsl:call-template>
                        <!-- if this is a functionset add suffix Functions -->
                        <xsl:if test="boolean(NAntUtil:IsFunctionSet(@cref))">
                            <xsl:text> Functions</xsl:text>
                        </xsl:if>
                    </xsl:when>
                    <xsl:when test="@href">
                        <a href="{@href}">
                            <xsl:value-of select="." />
                        </a>
                    </xsl:when>
                </xsl:choose>
                <xsl:if test="position()!= last()">
                    <xsl:text> | </xsl:text>
                </xsl:if>
            </xsl:for-each>
        </xsl:if>
    </xsl:template>

    <xsl:template name="preliminary-section">
        <p class="topicstatus">
            <xsl:choose>
                <xsl:when test="documentation/preliminary[text()]">
                    <xsl:value-of select="documentation/preliminary"/>
                </xsl:when>
                <xsl:when test="ancestor::node()/documentation/preliminary[text()]">
                    <xsl:value-of select="ancestor::node()/documentation/preliminary" />
                </xsl:when>
                <xsl:otherwise>
                    <xsl:text>[This is preliminary documentation and subject to change.]</xsl:text>
                </xsl:otherwise>
            </xsl:choose>
        </p>
    </xsl:template>
</xsl:stylesheet>
