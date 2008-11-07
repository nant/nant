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
// Scott Hernandez (ScottHernandez-at-Hotmail....com)
-->

<xsl:stylesheet xmlns="http://www.w3.org/1999/xhtml" xmlns:xsl="http://www.w3.org/1999/XSL/Transform" xmlns:NAntUtil="urn:NAntUtil" exclude-result-prefixes="NAntUtil" version="1.0">
    <xsl:include href="tags.xslt" />
    <xsl:include href="common.xslt" />
    <xsl:include href="nant-attributes.xslt" />

    <xsl:output 
        method="xml" 
        indent="yes" 
        encoding="utf-8" 
        version="1.0"  
        doctype-public="-//W3C//DTD XHTML 1.1//EN" 
        doctype-system="http://www.w3.org/TR/xhtml11/DTD/xhtml11.dtd" 
        omit-xml-declaration="yes"
        standalone="yes"
        />
    
    <xsl:param name="productName"></xsl:param>
    <xsl:param name="productVersion"></xsl:param>
    <xsl:param name="productUrl"></xsl:param>    
    <xsl:param name="method-id"></xsl:param>
    <xsl:param name="functionName"></xsl:param>
    <xsl:param name="refType">Function</xsl:param>

    <xsl:template match="/">
        <html xmlns="http://www.w3.org/1999/xhtml">
            <xsl:comment> Documenting <xsl:value-of select="$functionName" /> </xsl:comment>
            <xsl:apply-templates select="//method[@id=$method-id]" mode="FunctionDoc" />
        </html>
    </xsl:template>

    <xsl:template match="method" mode="FunctionDoc">
        <xsl:variable name="Prefix" select="../attribute[@name='NAnt.Core.Attributes.FunctionSetAttribute']/property[@name='Prefix']/@value" />
        <xsl:variable name="Name" select="attribute[@name='NAnt.Core.Attributes.FunctionAttribute']/property[@name='Name']/@value" />
        <xsl:variable name="name"><xsl:value-of select="$Prefix" />::<xsl:value-of select="$Name" /></xsl:variable>
        <head>
            <meta http-equiv="Content-Language" content="en-ca" />
            <meta http-equiv="Content-Type" content="text/html; charset=utf-8" />
            <link rel="stylesheet" type="text/css" href="../style.css" />
            <title><xsl:value-of select="$name" /> Function</title>
        </head>
        <body>
            <table width="100%" border="0" cellspacing="0" cellpadding="2" class="NavBar">
                <tr>
                    <td class="NavBar-Cell">
                        <xsl:element name="a">
                            <xsl:attribute name="href"><xsl:value-of select="$productUrl" /></xsl:attribute>
                            <b><xsl:value-of select="$productName" /></b>
                        </xsl:element>
                        <img alt="->" src="../images/arrow.gif" />
                        <a href="../index.html">Help</a>
                        <img alt="->" src="../images/arrow.gif" />
                        <a href="index.html">Function Reference</a>
                        <img alt="->" src="../images/arrow.gif" /><xsl:text> </xsl:text>
                        <xsl:value-of select="$name" />
                    </td>
                    <td class="NavBar-Cell" align="right">
                        v<xsl:value-of select="$productVersion" />
                    </td>
                </tr>
            </table>

            <h1><xsl:value-of select="$name" /></h1>
            <xsl:if test="ancestor-or-self::node()/documentation/preliminary | /ndoc/preliminary">
                <xsl:call-template name="preliminary-section"/>
            </xsl:if>

            <!-- output whether type is deprecated -->
            <xsl:variable name="ObsoleteAttribute" select="attribute[@name = 'System.ObsoleteAttribute']" />
            <xsl:if test="count($ObsoleteAttribute) > 0">
                <p>
                    <i>(Deprecated)</i>
                </p>
            </xsl:if>

            <p><xsl:apply-templates select="documentation/summary" mode="slashdoc" /></p>

            <h3>Usage</h3>
            <code>
                 <xsl:call-template name="get-a-href-with-name">
                    <xsl:with-param name="cref" select="concat('T:', @returnType)" />
                </xsl:call-template>
                <xsl:text> </xsl:text>
                <xsl:value-of select="$name" />(<xsl:for-each select="parameter"><xsl:if test="position() != 1">, </xsl:if><span class="parameter"><xsl:value-of select="@name" /></span></xsl:for-each>)
            </code>
            <p/>

            <xsl:if test="count(parameter) != 0">
                <h3>Parameters</h3>
                <div class="table">
                    <table>
                        <tr>
                            <th>Name</th>
                            <th>Type</th>
                            <th>Description</th>
                        </tr>
                        <xsl:for-each select="parameter">
                            <tr>
                                <td><xsl:value-of select="@name" /></td>
                                <td>
                                    <xsl:call-template name="get-a-href-with-name">
                                        <xsl:with-param name="cref" select="concat('T:', @type)" />
                                    </xsl:call-template>
                                </td>
                                <xsl:variable name="paramname" select="@name" />
                                <td><xsl:apply-templates select="../documentation/param[@name=$paramname]/node()" mode="slashdoc" /></td>
                            </tr>
                        </xsl:for-each>
                    </table>
                </div>
            </xsl:if>
            <xsl:if test="count(documentation/returns) != 0">
                <h3>Return Value</h3>
                <xsl:apply-templates select="documentation/returns/node()" mode="slashdoc" />
            </xsl:if>
            <xsl:call-template name="exceptions-section" />
            <xsl:if test="count(documentation/remarks) != 0">
                <h3>Remarks</h3>
                <xsl:apply-templates select="documentation/remarks" mode="slashdoc" />
            </xsl:if>
            <xsl:if test="count(documentation/example) != 0">
                <h3>Examples</h3>
                <ul class="examples">
                    <xsl:apply-templates select="documentation/example" mode="slashdoc" />
                </ul>
            </xsl:if>
            <h3>Requirements</h3>
            <div style="margin-left: 20px;">
                <b>Assembly:</b><xsl:text> </xsl:text><xsl:value-of select="ancestor::assembly/@name" /> (<xsl:value-of select="ancestor::assembly/@version" />)
            </div>
            <xsl:call-template name="seealso-section" />
        </body>
    </xsl:template>
    <xsl:template name="exceptions-section">
        <xsl:if test="documentation/exception">
            <h3>Exceptions</h3>
            The function will fail in any of the following circumstances:
            <div style="margin-left: 40px;">
                <ul>
                    <xsl:for-each select="documentation/exception">
                        <xsl:sort select="@name" />
                        <li>
                            <xsl:apply-templates select="./node()" mode="slashdoc" />
                            <xsl:if test="not(./node())">&#160;</xsl:if>
                        </li>
                    </xsl:for-each>
                </ul>
            </div>
        </xsl:if>
    </xsl:template>
</xsl:stylesheet>
