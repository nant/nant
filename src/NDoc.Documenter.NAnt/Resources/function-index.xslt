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
-->
<xsl:stylesheet xmlns:xsl="http://www.w3.org/1999/XSL/Transform" xmlns:NAntUtil="urn:NAntUtil" exclude-result-prefixes="NAntUtil" version="1.0"> 
    <xsl:include href="tags.xslt" />
    <xsl:include href="common.xslt" />
    <xsl:output method="html" indent="yes" />    

    <!-- 
    this stylesheet uses 'unique' trick published at:
    
    http://sources.redhat.com/ml/xsl-list/2001-06/msg00066.html

    we use it to traverse a unique list of categories ordered by name

    -->
    <xsl:key name="classCategory" 
        match="class[attribute/@name='NAnt.Core.Attributes.FunctionSetAttribute']" 
        use="attribute[@name='NAnt.Core.Attributes.FunctionSetAttribute']/property[@name='Category']/@value" />

    <xsl:template match="/">
        <html>
            <head>
                <meta http-equiv="Content-Language" content="en-ca" />
                <meta http-equiv="Content-Type" content="text/html; charset=windows-1252" />
                <meta name="description" content="Introduction" />
                <link rel="stylesheet" type="text/css" href="../../style.css" />
                <title>Function Reference</title>
            </head>
            <body>
                <table width="100%" border="0" cellspacing="0" cellpadding="2" class="NavBar">
                    <tr>
                        <td class="NavBar-Cell" width="100%">
                            <a href="../../index.html"><b>NAnt</b></a>
                            <img alt="->" src="../images/arrow.gif" />
                            <a href="../index.html">Help</a>
                            <img alt="->" src="../images/arrow.gif" />
                            Function Reference
                        </td>
                    </tr>
                </table>
                <h1>Function Reference</h1>

                <!-- table of contents -->
                <xsl:for-each select="//class[attribute/@name='NAnt.Core.Attributes.FunctionSetAttribute']">
                    <xsl:sort select="number(attribute[@name='NAnt.Core.Attributes.FunctionSetAttribute']/property[@name='UserDocSortOrder']/@value)" order="ascending" />
                    <xsl:sort select="attribute[@name='NAnt.Core.Attributes.FunctionSetAttribute']/property[@name='Category']/@value" order="ascending" />

                    <xsl:variable name="this_cat" select="attribute[@name='NAnt.Core.Attributes.FunctionSetAttribute']/property[@name='Category']/@value" />

                    <!-- 'unique' - see above -->
                    <xsl:if test="generate-id()=generate-id(key('classCategory',attribute[@name='NAnt.Core.Attributes.FunctionSetAttribute']/property[@name='Category']/@value)[1])">
                        <a><xsl:attribute name="href">#<xsl:value-of select="$this_cat" /></xsl:attribute>
                            <xsl:value-of select="$this_cat" /> Functions</a>
                        <br/>
                    </xsl:if>
                </xsl:for-each>

                <xsl:for-each select="//class[attribute/@name='NAnt.Core.Attributes.FunctionSetAttribute']">
                    <xsl:sort select="number(attribute[@name='NAnt.Core.Attributes.FunctionSetAttribute']/property[@name='UserDocSortOrder']/@value)" order="ascending" />
                    <xsl:sort select="attribute[@name='NAnt.Core.Attributes.FunctionSetAttribute']/property[@name='Category']/@value" order="ascending" />

                    <xsl:variable name="this_cat" select="attribute[@name='NAnt.Core.Attributes.FunctionSetAttribute']/property[@name='Category']/@value" />

                    <a><xsl:attribute name="name"><xsl:value-of select="$this_cat" /></xsl:attribute></a>

                    <!-- 'unique' - see above -->
                    <xsl:if test="generate-id()=generate-id(key('classCategory',attribute[@name='NAnt.Core.Attributes.FunctionSetAttribute']/property[@name='Category']/@value)[1])">
                        <h3><xsl:value-of select="$this_cat" /> Functions</h3>
                        <div class="table">
                            <table>
                                <tr>
                                    <th>Name</th>
                                    <th>Summary</th>
                                </tr>

                                <!-- for each class having FunctionSet attribute with this category, then for each method having Function attribute -->

                                <xsl:for-each select="//class[attribute[@name='NAnt.Core.Attributes.FunctionSetAttribute']/property[@name='Category']/@value=$this_cat]/method[attribute/@name='NAnt.Core.Attributes.FunctionAttribute']">
                                    <xsl:sort select="../attribute[@name='NAnt.Core.Attributes.FunctionSetAttribute']/property[@name='Prefix']/@value" order="ascending" />
                                    <xsl:sort select="attribute[@name='NAnt.Core.Attributes.FunctionAttribute']/property[@name='Name']/@value" order="ascending" />

                                    <xsl:apply-templates select="." />
                                </xsl:for-each>
                            </table>
                        </div>
                    </xsl:if>
                </xsl:for-each>
            </body>
        </html>
    </xsl:template>

    <xsl:template match="interface|enumeration" />

    <!-- match class tag -->
    <xsl:template match="method">
        <xsl:variable name="ObsoleteAttribute" select="attribute[@name='System.ObsoleteAttribute']" />
        <xsl:variable name="Prefix" select="../attribute[@name='NAnt.Core.Attributes.FunctionSetAttribute']/property[@name='Prefix']/@value" />
        <xsl:variable name="Name" select="attribute[@name='NAnt.Core.Attributes.FunctionAttribute']/property[@name='Name']/@value" />
        <xsl:variable name="Category" select="../attribute[@name='NAnt.Core.Attributes.FunctionSetAttribute']/property[@name='Category']/@value" />

        <tr>
            <td>
                <a><xsl:attribute name="href"><xsl:value-of select="$Prefix" />.<xsl:value-of select="$Name" />.html</xsl:attribute>
                    <xsl:value-of select="$Prefix" />::<xsl:value-of select="$Name" />
                </a>
            </td>
            <td><xsl:apply-templates select="documentation/summary/node()" mode="slashdoc" /></td>
        </tr>
    </xsl:template>
</xsl:stylesheet>
