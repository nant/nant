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

<xsl:stylesheet xmlns:xsl="http://www.w3.org/1999/XSL/Transform" xmlns:NAntUtil="urn:NAntUtil" exclude-result-prefixes="NAntUtil" version="1.0">
    <xsl:include href="tags.xslt" />
    <xsl:include href="common.xslt" />
    <xsl:include href="nant-attributes.xslt" />
    
    <xsl:output method="html" indent="yes" />

    <!-- The class we are documenting this time. This value will be passed in by the caller. argv[] equivalent. Default value is used for testing -->
    <xsl:param name="class-id">T:NAnt.Core.Types.FileSet</xsl:param>

    <!-- helper values for adjusting the paths -->
    <xsl:param name="relPathAdjust">..</xsl:param>
    <xsl:param name="imagePath">../images</xsl:param>
    <xsl:param name="refType">Type</xsl:param>
    <xsl:param name="childrenElements" select="''"/>

    <xsl:template match="/">
        <html>
            <xsl:comment> Documenting <xsl:value-of select="$class-id"/> </xsl:comment>
            <xsl:apply-templates select="//class[@id = $class-id]" mode="TypeDoc"/>
        </html>
    </xsl:template>
    
    <xsl:template match="class" mode="TypeDoc">
        <xsl:variable name="name">
            <xsl:choose>
                <xsl:when test="attribute/property[@name = 'Name']">&lt;<xsl:value-of select="attribute/property[@name = 'Name']/@value" />&gt;</xsl:when>
                <xsl:otherwise><xsl:value-of select="@name" /></xsl:otherwise>
            </xsl:choose> 
        </xsl:variable>
        <head>
            <meta http-equiv="Content-Language" content="en-ca" />
            <meta http-equiv="Content-Type" content="text/html; charset=windows-1252" />
            <link rel="stylesheet" type="text/css" href="{$relPathAdjust}/../style.css" />
            <title><xsl:value-of select="$name" /> <xsl:value-of select="$refType"/></title>
        </head>
        <body>
            <table width="100%" border="0" cellspacing="0" cellpadding="2" class="NavBar">
                <tr>
                    <td class="NavBar-Cell" width="100%">
                        <a href="{$relPathAdjust}/../index.html"><b>NAnt</b></a>
                        <img alt="->" src="{$imagePath}/arrow.gif" />
                        <a href="{$relPathAdjust}/index.html">Help</a>
                        <img alt="->" src="{$imagePath}/arrow.gif" />
                        <a href="{$relPathAdjust}/tasks.html"><xsl:value-of select="$refType"/> Reference</a>
                        <img alt="->" src="{$imagePath}/arrow.gif" /><xsl:text> </xsl:text>
                        <xsl:value-of select="$name" /> <xsl:value-of select="$refType"/>
                    </td>
                </tr>
            </table>
    
            <h1><xsl:value-of select="$name" /> <xsl:value-of select="$refType"/></h1>
            <xsl:apply-templates select=".">
                <xsl:with-param name="propertyElements" select="$childrenElements" />
            </xsl:apply-templates>
            
        </body>

    </xsl:template>

    <!-- match class tag for info about a type -->
    <xsl:template match="class">
    
        <xsl:param name="propertyElements" select="'null'"/>
        <!-- output whether type is deprecated -->
        <xsl:variable name="ObsoleteAttribute" select="attribute[@name = 'System.ObsoleteAttribute']"/>
        <xsl:if test="count($ObsoleteAttribute) > 0">
            <p>
                <i>(Deprecated)</i>
            </p>
        </xsl:if>
        
        <p><xsl:apply-templates select="documentation/summary" mode="slashdoc"/></p>
        <!-- Remarks -->
        <xsl:apply-templates select="documentation/remarks" mode="slashdoc"/>

        <xsl:variable name="properties" select="property[attribute/@name = 'NAnt.Core.Attributes.TaskAttributeAttribute']"/>
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
                    <xsl:apply-templates select="property[attribute/@name = 'NAnt.Core.Attributes.TaskAttributeAttribute']" mode="TaskAttribute">
                        <!-- sort order: any property declared from the documented class, then by required, last by name-->
                        <xsl:sort select="@declaringType" />
                        <xsl:sort select="attribute[@name = 'NAnt.Core.Attributes.TaskAttributeAttribute']/property[@name = 'Required']/@value" order="descending" />
                        <xsl:sort select="attribute[@name = 'NAnt.Core.Attributes.TaskAttributeAttribute']/property[@name = 'Name']/@value" />
                    </xsl:apply-templates>
                </table>
            </div>
        </xsl:if>

        <xsl:variable name="FrameworkProperties" select="property[attribute/@name = 'NAnt.Core.Attributes.FrameworkConfigurableAttribute']"/>
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
                    <xsl:apply-templates select="property[attribute/@name = 'NAnt.Core.Attributes.FrameworkConfigurableAttribute' ]" mode="FrameworkConfigurableAttribute">
                        <xsl:sort select="attribute[@name = 'NAnt.Core.Attributes.FrameworkConfigurableAttribute']/property[@name = 'Name']/@value" />
                    </xsl:apply-templates>
                </table>
            </div>
        </xsl:if>
        
        <!-- nested elements -->
        <xsl:call-template name="NestedElements">
            <xsl:with-param name="nestedElements" select="$propertyElements" />
        </xsl:call-template>

        <!-- Example -->
        <xsl:if test="count(documentation/example) != 0">
            <h3>Examples</h3>
            <xsl:apply-templates select="documentation/example" mode="slashdoc"/>
        </xsl:if>
    </xsl:template>

    <!-- nested elements section of the Task/Type/Element docs -->     
    <xsl:template name="NestedElements">
        <xsl:param name="nestedElements" select="'null'" />
    
        <xsl:variable name="filesets" select="property[attribute/@name = 'NAnt.Core.Attributes.FileSetAttribute' ]"/>
        <xsl:variable name="arrays" select="property[attribute/@name = 'NAnt.Core.Attributes.BuildElementArrayAttribute' ]"/>
        <xsl:variable name="colls" select="property[attribute/@name = 'NAnt.Core.Attributes.BuildElementArrayAttribute' ]"/>
        <xsl:variable name="elements" select="property[attribute/@name = 'NAnt.Core.Attributes.BuildElementAttribute' ]"/>
        <xsl:if test="count($filesets) != 0 or count($arrays) != 0 or count($elements) != 0 or count($colls) != 0">
            <h3>Nested Elements:</h3>
            <xsl:apply-templates select="property/attribute" mode="TaskElements">
                <xsl:with-param name="typeNodes" select="$nestedElements"/>
            </xsl:apply-templates>
        </xsl:if>
    </xsl:template>
    
    <!-- match TaskAttribute property tag -->
    <xsl:template match="class/property[attribute/@name = 'NAnt.Core.Attributes.TaskAttributeAttribute']" mode="TaskAttribute">
        <xsl:variable name="Required" select="attribute/property[@name = 'Required']/@value"/>
        <xsl:element name="tr">
            <xsl:if test="$Required = 'True'">
                <xsl:attribute name="class">required</xsl:attribute>
            </xsl:if>
        
            <td valign="top"><xsl:value-of select="attribute/property[@name = 'Name']/@value"/> </td>
            <td style="text-align: center;">
                <xsl:call-template name="value">
                    <xsl:with-param name="type" select="@type" />
                </xsl:call-template>
            </td>
            <td>
                <xsl:apply-templates mode="docstring" select="." />
                <xsl:if test="attribute/property[@name='ExpandProperties' and @value='False']">
                    <br />
                    <b>Note:</b> This attribute's propeties will not be automatically expanded!
                    <br />
                </xsl:if>
                
            </td>
            <td style="text-align: center;"><xsl:value-of select="string($Required)"/></td>
        </xsl:element>
    </xsl:template>

    <!-- match FrameworkConfigurable property tag -->
    <xsl:template match="property" mode="FrameworkConfigurableAttribute">
        <xsl:variable name="FrameworkConfigurableAttribute" select="attribute[@name = 'NAnt.Core.Attributes.FrameworkConfigurableAttribute']"/>
        <xsl:if test="count($FrameworkConfigurableAttribute) = 1">
            <xsl:variable name="Required" select="$FrameworkConfigurableAttribute/property[@name = 'Required']/@value"/>
            <tr>
                <td valign="top"><xsl:value-of select="$FrameworkConfigurableAttribute/property[@name = 'Name']/@value"/></td>
                <td style="text-align: center;">
                    <xsl:call-template name="value">
                        <xsl:with-param name="type" select="@type" />
                    </xsl:call-template>
                </td>
                <td><xsl:apply-templates select="." mode="docstring" /></td>
                <td style="text-align: center;"><xsl:value-of select="string($Required)"/></td>
            </tr>
        </xsl:if>
    </xsl:template>

    <!-- returns the summary doc string for a given class property (called from the property templates )-->
    <xsl:template match="class/property" mode="docstring" >
        <xsl:choose>
            <xsl:when test="@declaringType">
                <xsl:variable name="ObsoleteAttribute" select="//class[@id = concat('T:', current()/@declaringType)]/*[@name = current()/@name]/attribute[@name = 'System.ObsoleteAttribute']"/>
                <xsl:if test="count($ObsoleteAttribute) > 0">
                    <i>Deprecated.</i>
                    <xsl:text> </xsl:text>
                </xsl:if>
                <xsl:apply-templates select="//class[@id = concat('T:', current()/@declaringType)]/*[@name = current()/@name]/documentation/summary" mode="slashdoc" />
            </xsl:when>
            <xsl:otherwise>
                <xsl:variable name="ObsoleteAttribute" select="attribute[@name = 'System.ObsoleteAttribute']"/>
                <xsl:if test="count($ObsoleteAttribute) > 0">
                    <i>Deprecated.</i>
                    <xsl:text> </xsl:text>
                </xsl:if>
                <xsl:apply-templates select="documentation/summary" mode="slashdoc" />
            </xsl:otherwise>
        </xsl:choose>
    </xsl:template>
</xsl:stylesheet>
