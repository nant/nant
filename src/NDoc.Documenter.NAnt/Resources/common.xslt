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
// Gert Driesen (gert.driesen@ardatis.com)
-->
<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform">
    <xsl:template name="value">
        <xsl:param name="type" />
        <xsl:variable name="namespace">
            <xsl:value-of select="concat(../../@name, '.')" />
        </xsl:variable>
        <xsl:choose>
            <xsl:when test="contains($type, $namespace)">
                <xsl:variable name="enumnode" select="//descendant::enumeration[@id=concat('T:', $type)]" />
                <xsl:choose>
                    <xsl:when test="count($enumnode) = 1">
                        <xsl:text>enum</xsl:text>
                    </xsl:when>
                    <xsl:otherwise>
                        <xsl:value-of select="substring-after($type, $namespace)" />
                    </xsl:otherwise>
                </xsl:choose>
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
                <xsl:otherwise>
                    <xsl:value-of select="$old-type" />
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
</xsl:stylesheet>
