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
<xsl:stylesheet xmlns:xsl="http://www.w3.org/1999/XSL/Transform" version="1.0">
    <xsl:template match="/">
        <html>
            <head>
                <meta http-equiv="Content-Language" content="en-ca" />
                <meta http-equiv="Content-Type" content="text/html; charset=windows-1252" />
                <meta name="description" content="Introduction" />
                <link rel="stylesheet" type="text/css" href="../style.css" />
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
                <div class="Table-Section">
                    <table class="Table">
                        <tr>
                            <th class="Table-Header">Task</th>
                            <th class="Table-Header">Summary</th>
                        </tr>
                        <xsl:apply-templates select="//class">
                            <xsl:sort select="attribute/property[@name='Name']/@value" />
                        </xsl:apply-templates>
                    </table>
                </div>
            </body>
        </html>
    </xsl:template>
    
    <xsl:template match="interface|enumeration" />

    <!-- match class tag -->
    <xsl:template match="class">
        <xsl:variable name="attr" select="attribute[@name='NAnt.Core.Attributes.TaskNameAttribute']/@name" />
        <xsl:if test="string-length(string($attr)) != 0">
            <tr>
                <td class="Table-Cell"><a><xsl:attribute name="href"><xsl:value-of select="attribute/property[@name='Name']/@value" />task.html</xsl:attribute><xsl:value-of select="attribute/property[@name='Name']/@value" /></a></td>
                <td class="Table-Cell"><xsl:apply-templates select="documentation/summary/node()" mode="slashdoc"/></td>
            </tr>
        </xsl:if>
    </xsl:template>
</xsl:stylesheet>
