<?xml version="1.0" encoding="utf-8" ?>
<!--
// NAnt - A .NET build tool
// Copyright (C) 2003 Scott Hernandez
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
// Scott Hernandez (ScottHernandez-at-Hotmail....com)
-->
<xsl:stylesheet xmlns:xsl="http://www.w3.org/1999/XSL/Transform" xmlns:NAntUtil="urn:NAntUtil" exclude-result-prefixes="NAntUtil" version="1.0">
    
    <!-- match attribute by names -->
    <xsl:template match="attribute[@name = 'NAnt.Core.Attributes.BuildElementAttribute']" mode="NestedElements">

        <xsl:comment>Element</xsl:comment>

        <xsl:call-template name="NestedElement">
            <xsl:with-param name="elementTypeParam" select="../@type"/>
        </xsl:call-template>
    </xsl:template>
    
    <xsl:template match="attribute[@name = 'NAnt.Core.Attributes.BuildElementArrayAttribute']" mode="NestedElements">

        <xsl:comment>Array</xsl:comment>
        
        <xsl:call-template name="NestedElement">
            <xsl:with-param name="elementTypeParam">
                <xsl:choose>
                    <xsl:when test="property[@name='ElementType']/@value != ''">
                        <xsl:value-of select="property[@name='ElementType']/@value"/>
                    </xsl:when>
                    <xsl:otherwise>
                        <xsl:value-of select="../@type"/>
                    </xsl:otherwise>
                </xsl:choose>
            </xsl:with-param>
            
        </xsl:call-template>
    </xsl:template>

    <xsl:template match="attribute[@name = 'NAnt.Core.Attributes.BuildElementCollectionAttribute']" mode="NestedElements">

        <xsl:comment>Collection</xsl:comment>

        <xsl:call-template name="NestedElement">
            <xsl:with-param name="elementTypeParam">
                <xsl:choose>
                    <xsl:when test="property[@name='ElementType']/@value != ''">
                        <xsl:value-of select="property[@name='ElementType']/@value"/>
                    </xsl:when>
                    <xsl:otherwise>
                        <xsl:value-of select="../@type"/>
                    </xsl:otherwise>
                </xsl:choose>
            </xsl:with-param>
        </xsl:call-template>
    </xsl:template>

    <xsl:template name="NestedElement">
        <xsl:param name="elementTypeParam" select="'#'" />
        
        <xsl:variable name="elementType" select="translate(translate(concat('T:', $elementTypeParam), '[]', ''), '+', '.')"/>
        
        <xsl:comment>NAntUtil: Getting HRef for <xsl:value-of select="$elementType"/></xsl:comment>
        <xsl:variable name="href" select="concat('../',string(NAntUtil:GetHRef($elementType)))" />
        
        <xsl:variable name="typeNode" select="NAntUtil:GetClassNode($elementType)"/>
        
        <table>
            <tr>
                <td align="left"><h4>&lt;<a href="{$href}"><xsl:value-of select="property[@name='Name']/@value"/></a>&gt;</h4></td>
                <td align="right">Required:<xsl:value-of select="property[@name='Required']/@value"/></td>
            </tr>
        </table>
        <div class="nested-element">
            
            <!-- generates docs from summary xmldoc comments -->
            <xsl:apply-templates select=".." mode="docstring" />
            
            <!--
                Put nested element class docs inline, if not derived from DateTypeBase
            -->
            <xsl:variable name="DataTypeBase" select="$typeNode[./descendant::base/@type='NAnt.Core.DataTypeBase']"/>
            
            <xsl:if test="$typeNode and not($DataTypeBase)"> 
                <xsl:apply-templates select="$typeNode"/>
            </xsl:if>
        </div>
    </xsl:template>
    
    <!-- match TaskAttribute property tag -->
    <xsl:template match="class/property[attribute/@name = 'NAnt.Core.Attributes.TaskAttributeAttribute']" mode="TypeDoc">
        <xsl:variable name="Required" select="attribute/property[@name = 'Required']/@value"/>
        <xsl:element name="tr">
        <!--
            <xsl:if test="$Required = 'True'">
                <xsl:attribute name="class">required</xsl:attribute>
            </xsl:if>
        -->
            <xsl:element name="td">
                <xsl:attribute name="valign">top</xsl:attribute>
                <xsl:if test="$Required = 'True'">
                    <xsl:attribute name="class">required</xsl:attribute>
                </xsl:if>
                <xsl:value-of select="attribute/property[@name = 'Name']/@value"/>
            </xsl:element>
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
    <xsl:template match="class/property[attribute/@name = 'NAnt.Core.Attributes.FrameworkConfigurableAttribute']" mode="TypeDoc">
        <xsl:variable name="FrameworkConfigurableAttribute" select="attribute[@name = 'NAnt.Core.Attributes.FrameworkConfigurableAttribute']"/>
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
    </xsl:template>    
    
</xsl:stylesheet>