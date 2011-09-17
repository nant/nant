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
// Gert Driesen (drieseng@users.sourceforge.net.com)
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

    <!-- The class we are documenting this time. This value will be passed in by the caller. argv[] equivalent. Default value is used for testing -->
    <xsl:param name="class-id">T:NAnt.Core.Types.FileSet</xsl:param>

    <!-- helper values for adjusting the paths -->
    <xsl:param name="refType">Type</xsl:param>

    <xsl:template match="/">
        <html xmlns="http://www.w3.org/1999/xhtml">
            <xsl:comment> Documenting <xsl:value-of select="$class-id" /> </xsl:comment>
            <xsl:choose>
                <xsl:when test="$refType = 'Enum'"><xsl:apply-templates select="//enumeration[@id = $class-id]" mode="EnumDoc" /></xsl:when>
                <xsl:otherwise><xsl:apply-templates select="//class[@id = $class-id]" mode="TypeDoc" /></xsl:otherwise>
            </xsl:choose>
        </html>
    </xsl:template>

    <xsl:template match="class" mode="TypeDoc">
        <xsl:variable name="name">
            <xsl:choose>
                <xsl:when test="attribute/property[@name = 'Name']">&lt;<xsl:value-of select="attribute/property[@name = 'Name']/@value" />&gt;</xsl:when>
                <xsl:otherwise><xsl:value-of select="@name" /></xsl:otherwise>
            </xsl:choose> 
        </xsl:variable>
        <xsl:variable name="parentPage">
            <xsl:choose>
                <xsl:when test="$refType = 'Task'">../tasks/index.html</xsl:when>
                <xsl:when test="$refType = 'Type'">../types/index.html</xsl:when>
                <xsl:when test="$refType = 'Filter'">../filters/index.html</xsl:when>
                <xsl:when test="$refType = 'Element'"></xsl:when>
            </xsl:choose>
        </xsl:variable>
        <head>
            <meta http-equiv="Content-Language" content="en-ca" />
            <meta http-equiv="Content-Type" content="text/html; charset=utf-8" />
            <link rel="stylesheet" type="text/css" href="../style.css" />
            <title><xsl:value-of select="$name" /><xsl:text> </xsl:text><xsl:value-of select="$refType" /></title>
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
                        <xsl:choose>
                            <xsl:when test="string-length($parentPage) > 0">
                                <a href="{$parentPage}"><xsl:value-of select="$refType" /> Reference</a>
                            </xsl:when>
                            <xsl:otherwise>
                                <span><xsl:value-of select="$refType" /> Reference</span>
                            </xsl:otherwise>
                        </xsl:choose>
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
            <xsl:apply-templates select="." />
            <h3>Requirements</h3>
            <div style="margin-left: 20px;">
                <b>Assembly:</b><xsl:text> </xsl:text><xsl:value-of select="ancestor::assembly/@name" /> (<xsl:value-of select="ancestor::assembly/@version" />)
            </div>
            <xsl:call-template name="seealso-section" />
        </body>
    </xsl:template>

    <!-- match class tag for info about a type -->
    <xsl:template match="class">
        <!-- output whether type is deprecated -->
        <xsl:variable name="ObsoleteAttribute" select="attribute[@name = 'System.ObsoleteAttribute']" />
        <xsl:if test="count($ObsoleteAttribute) > 0">
            <p>
                <i>(Deprecated)</i>
            </p>
        </xsl:if>
        
        <p><xsl:apply-templates select="documentation/summary" mode="slashdoc" /></p>
        <!-- Remarks -->
        <xsl:apply-templates select="documentation/remarks" mode="slashdoc" />

        <xsl:variable name="properties" select="property[attribute/@name = 'NAnt.Core.Attributes.TaskAttributeAttribute']" />
        <xsl:if test="count($properties) != 0">
            <h3>Parameters</h3>
            <div class="table">
                <table>
                    <tr>
                        <th>Attribute</th>
                        <th style="text-align: center;">Type</th>
                        <th>Description</th>
                        <th style="text-align: center;">Required</th>
                    </tr>
                    <xsl:apply-templates select="$properties" mode="TypeDoc">
                        <!-- sort order: any property declared from the documented class, then by required, last by name-->
                        <xsl:sort select="boolean(@declaringType)" />
                        <xsl:sort select="attribute[@name = 'NAnt.Core.Attributes.TaskAttributeAttribute']/property[@name = 'Required']/@value" order="descending" />
                        <xsl:sort select="attribute[@name = 'NAnt.Core.Attributes.TaskAttributeAttribute']/property[@name = 'Name']/@value" />
                    </xsl:apply-templates>
                </table>
            </div>
        </xsl:if>

        <xsl:variable name="FrameworkProperties" select="property[attribute/@name = 'NAnt.Core.Attributes.FrameworkConfigurableAttribute']" />
        <xsl:if test="count($FrameworkProperties) != 0">
            <h3>Framework-configurable parameters</h3>
            <div class="table">
                <table>
                    <tr>
                        <th>Attribute</th>
                        <th style="text-align: center;">Type</th>
                        <th>Description</th>
                        <th style="text-align: center;">Required</th>
                    </tr>
                    <xsl:apply-templates select="$FrameworkProperties" mode="TypeDoc">
                        <xsl:sort select="attribute[@name = 'NAnt.Core.Attributes.FrameworkConfigurableAttribute']/property[@name = 'Name']/@value" />
                    </xsl:apply-templates>
                </table>
            </div>
        </xsl:if>

        <!-- nested elements -->
        <xsl:variable name="arrays" select="property[attribute/@name = 'NAnt.Core.Attributes.BuildElementArrayAttribute' ]" />
        <xsl:variable name="colls" select="property[attribute/@name = 'NAnt.Core.Attributes.BuildElementCollectionAttribute' ]" />
        <xsl:variable name="elements" select="property[attribute/@name = 'NAnt.Core.Attributes.BuildElementAttribute' ]" />
        <xsl:variable name="orderedElements" select="method[attribute/@name = 'NAnt.Core.Attributes.BuildElementAttribute' ]" />

        <xsl:if test="count($arrays) != 0 or count($elements) != 0 or count($colls) != 0 or count($orderedElements) != 0">
            <h3>Nested Elements:</h3>
            <xsl:apply-templates select="child::*/attribute" mode="NestedElements">
                <xsl:sort select="property[@name='Required' and @value='True']" />
            </xsl:apply-templates>
        </xsl:if>

        <!-- Example -->
        <xsl:if test="count(documentation/example) != 0">
            <h3>Examples</h3>
            <ul class="examples">
                <xsl:apply-templates select="documentation/example" mode="slashdoc" />
            </ul>
        </xsl:if>
    </xsl:template>

    <!-- returns the summary doc string for a given class property (called from the property templates )-->
    <xsl:template match="class/property" mode="docstring">
        <xsl:choose>
            <xsl:when test="@declaringType">
                <xsl:variable name="ObsoleteAttribute" select="//class[@id = concat('T:', current()/@declaringType)]/*[@name = current()/@name]/attribute[@name = 'System.ObsoleteAttribute']" />
                <xsl:if test="count($ObsoleteAttribute) > 0">
                    <i>Deprecated.</i>
                    <xsl:text> </xsl:text>
                </xsl:if>
                <xsl:apply-templates select="//class[@id = concat('T:', current()/@declaringType)]/*[@name = current()/@name]/documentation/summary" mode="slashdoc" />
            </xsl:when>
            <xsl:otherwise>
                <xsl:variable name="ObsoleteAttribute" select="attribute[@name = 'System.ObsoleteAttribute']" />
                <xsl:if test="count($ObsoleteAttribute) > 0">
                    <i>Deprecated.</i>
                    <xsl:text> </xsl:text>
                </xsl:if>
                <xsl:apply-templates select="documentation/summary" mode="slashdoc" />
            </xsl:otherwise>
        </xsl:choose>
    </xsl:template>

    <!-- returns the summary doc string for a given class method -->
    <xsl:template match="class/method" mode="docstring">
        <xsl:choose>
            <xsl:when test="@declaringType">
                <xsl:variable name="ObsoleteAttribute" select="//class[@id = concat('T:', current()/@declaringType)]/*[@name = current()/@name]/attribute[@name = 'System.ObsoleteAttribute']" />
                <xsl:if test="count($ObsoleteAttribute) > 0">
                    <i>Deprecated.</i>
                    <xsl:text> </xsl:text>
                </xsl:if>
                <xsl:apply-templates select="//class[@id = concat('T:', current()/@declaringType)]/*[@name = current()/@name]/documentation/summary" mode="slashdoc" />
            </xsl:when>
            <xsl:otherwise>
                <xsl:variable name="ObsoleteAttribute" select="attribute[@name = 'System.ObsoleteAttribute']" />
                <xsl:if test="count($ObsoleteAttribute) > 0">
                    <i>Deprecated.</i>
                    <xsl:text> </xsl:text>
                </xsl:if>
                <xsl:apply-templates select="documentation/summary" mode="slashdoc" />
            </xsl:otherwise>
        </xsl:choose>
    </xsl:template>

    <xsl:template match="enumeration" mode="EnumDoc">
        <xsl:variable name="name"><xsl:value-of select="@name" /></xsl:variable>
        <xsl:variable name="parentPage">../index.html</xsl:variable>
        <head>
            <meta http-equiv="Content-Language" content="en-ca" />
            <meta http-equiv="Content-Type" content="text/html; charset=utf-8" />
            <link rel="stylesheet" type="text/css" href="../style.css" />
            <title><xsl:value-of select="$name" /> enum</title>
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
                        <span><xsl:value-of select="$refType" /> Reference</span>
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
            <xsl:apply-templates select="." />
            <h3>Requirements</h3>
            <div style="margin-left: 20px;">
                <b>Assembly:</b><xsl:text> </xsl:text><xsl:value-of select="ancestor::assembly/@name" /> (<xsl:value-of select="ancestor::assembly/@version" />)
            </div>
            <xsl:call-template name="seealso-section" />
        </body>
    </xsl:template>

    <!-- match enumeration tag for info about an enum type -->
    <xsl:template match="enumeration">
        <!-- output whether type is deprecated -->
        <xsl:variable name="ObsoleteAttribute" select="attribute[@name = 'System.ObsoleteAttribute']" />
        <xsl:if test="count($ObsoleteAttribute) > 0">
            <p>
                <i>(Deprecated)</i>
            </p>
        </xsl:if>
        
        <p><xsl:apply-templates select="documentation/summary" mode="slashdoc" /></p>
        <!-- Remarks -->
        <xsl:apply-templates select="documentation/remarks" mode="slashdoc" />

        <xsl:variable name="fields" select="field" />
        <xsl:if test="count($fields) != 0">
            <h3>Fields</h3>
            <div class="table">
                <table>
                    <tr>
                        <th>Field</th>
                        <th>Description</th>
                    </tr>
                    <xsl:apply-templates select="$fields" mode="EnumDoc">
                        <xsl:sort select="@name" />
                    </xsl:apply-templates>
                </table>
            </div>
        </xsl:if>
    </xsl:template>

    <!-- returns the summary doc string for a given enumeration field -->
    <xsl:template match="enumeration/field" mode="docstring">
        <xsl:variable name="ObsoleteAttribute" select="attribute[@name = 'System.ObsoleteAttribute']" />
        <xsl:if test="count($ObsoleteAttribute) > 0">
            <i>Deprecated.</i>
            <xsl:text> </xsl:text>
        </xsl:if>
        <xsl:apply-templates select="documentation/summary" mode="slashdoc" />
    </xsl:template>

    <!-- match enumeration field -->
    <xsl:template match="enumeration/field" mode="EnumDoc">
        <xsl:element name="tr">
            <xsl:element name="td">
                <xsl:attribute name="valign">top</xsl:attribute>
                <xsl:value-of select="@name" />
            </xsl:element>
            <td>
                <xsl:apply-templates mode="docstring" select="." />
            </td>
        </xsl:element>
    </xsl:template>
</xsl:stylesheet>
