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
-->
<xsl:stylesheet xmlns="http://www.w3.org/1999/xhtml" xmlns:xsl="http://www.w3.org/1999/XSL/Transform" xmlns:NAntUtil="urn:NAntUtil" exclude-result-prefixes="NAntUtil" version="1.0">
    <xsl:include href="tags.xslt" />
    <xsl:include href="common.xslt" />
    <!--=<xsl:output method="html" indent="yes" /> -->
    <xsl:output 
        method="xml" 
        indent="yes" 
        encoding="utf-8" 
        version="1.0"  
        doctype-public="-//w3c//dtd xhtml 1.1 strict//en" 
        doctype-system="http://www.w3.org/TR/xhtml11/DTD/xhtml11.dtd" 
        omit-xml-declaration="yes"
        standalone="yes"
        />

    <xsl:template match="/">
        <html xmlns="http://www.w3.org/1999/xhtml">
            <head>
                <meta http-equiv="Content-Language" content="en-ca" />
                <meta http-equiv="Content-Type" content="text/html; charset=windows-1252" />
                <meta name="description" content="Introduction" />
                <link rel="stylesheet" type="text/css" href="../../style.css" />
                <title>Task Reference</title>
            </head>
            <body>
                <table width="100%" border="0" cellspacing="0" cellpadding="2" class="NavBar">
                    <tr>
                        <td class="NavBar-Cell" width="100%">
                            <a href="../../index.html"><b>NAnt</b></a>
                            <img alt="->" src="../images/arrow.gif" />
                            <a href="../index.html">Help</a>
                            <img alt="->" src="../images/arrow.gif" />
                            Task Reference
                        </td>
                    </tr>
                </table>
                <h1>Task Reference</h1>
                <div class="table">
                    <table>
                        <tr>
                            <th>Task</th>
                            <th width="100%">Summary</th>
                            <th>Assembly</th>
                        </tr>
                        <xsl:apply-templates select="//class[attribute/@name = 'NAnt.Core.Attributes.TaskNameAttribute']">
                            <xsl:sort select="attribute/property[@name = 'Name']/@value" />
                        </xsl:apply-templates>
                    </table>
                </div>
            </body>
        </html>
    </xsl:template>
    
    <xsl:template match="interface|enumeration" />

    <!-- match class tag -->
    <xsl:template match="class">
        <xsl:variable name="attr" select="attribute[@name = 'NAnt.Core.Attributes.TaskNameAttribute']/@name" />
        <xsl:element name="tr">
            <xsl:if test="string-length(string($attr)) != 0">
                <xsl:variable name="ObsoleteAttribute" select="attribute[@name = 'System.ObsoleteAttribute']" />
                <xsl:choose>
                    <!-- check if the task is deprecated -->
                    <xsl:when test="count($ObsoleteAttribute) > 0">
                        <xsl:variable name="IsErrorValue" select="$ObsoleteAttribute/property[@name = 'IsError']/@value" />
                        <!-- only list task in index if IsError property of ObsoleteAttribute is not set to 'True' -->
                        <xsl:if test="$IsErrorValue != 'True'">
                            <!-- output task name in italics to indicate that its deprecated -->
                            <td><a><xsl:attribute name="href"><xsl:value-of select="attribute[@name = 'NAnt.Core.Attributes.TaskNameAttribute']/property[@name='Name']/@value" />.html</xsl:attribute><i><xsl:value-of select="attribute/property[@name = 'Name']/@value" /></i></a></td>
                            <td><xsl:apply-templates select="documentation/summary/node()" mode="slashdoc" /></td>
                    </xsl:if>
                </xsl:when>
                <xsl:otherwise>
                        <td><a><xsl:attribute name="href"><xsl:value-of select="attribute[@name = 'NAnt.Core.Attributes.TaskNameAttribute']/property[@name = 'Name']/@value" />.html</xsl:attribute><xsl:value-of select="attribute/property[@name='Name']/@value" /></a></td>
                        <td><xsl:apply-templates select="documentation/summary/node()" mode="slashdoc" /></td>
                </xsl:otherwise>
                </xsl:choose>
            </xsl:if>
            <td>
                <xsl:value-of select="ancestor::assembly/@name" />(<xsl:value-of select="ancestor::assembly/@version" />)
            </td>
        </xsl:element>
    </xsl:template>
</xsl:stylesheet>
