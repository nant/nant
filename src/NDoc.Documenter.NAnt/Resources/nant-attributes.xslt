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
    <xsl:template match="attribute[@name = 'NAnt.Core.Attributes.BuildElementAttribute']" mode="TaskElements">
        <xsl:param name="typeNodes" select="''"/>
        
        <xsl:call-template name="EmitSingleNestedElement">
            <xsl:with-param name="typeNodes" select="$typeNodes"/>
            <xsl:with-param name="typeName" select="'Element'"/>
        </xsl:call-template>
    </xsl:template>
    
    <xsl:template match="attribute[@name = 'NAnt.Core.Attributes.FileSetAttribute']" mode="TaskElements">
        <xsl:param name="typeNodes" select="''"/>
        
        <xsl:call-template name="EmitSingleNestedElement">
            <xsl:with-param name="typeNodes" select="$typeNodes"/>
            <xsl:with-param name="typeName" select="'FileSet'"/>
        </xsl:call-template>
    </xsl:template>

    <xsl:template match="attribute[@name = 'NAnt.Core.Attributes.BuildElementArrayAttribute']" mode="TaskElements">
        <xsl:param name="typeNodes" select="''"/>
        
        <xsl:call-template name="EmitSingleNestedElement">
            <xsl:with-param name="typeNodes" select="$typeNodes"/>
            <xsl:with-param name="typeName" select="'Array'"/>
        </xsl:call-template>
    </xsl:template>

    <xsl:template match="attribute[@name = 'NAnt.Core.Attributes.BuildElementCollectionAttribute']" mode="TaskElements">
        <xsl:param name="typeNodes" select="''"/>
        <xsl:call-template name="EmitSingleNestedElement">
            <xsl:with-param name="typeNodes" select="$typeNodes"/>
            <xsl:with-param name="typeName" select="'Collection'"/>
        </xsl:call-template>
    </xsl:template>

    <xsl:template name="EmitSingleNestedElement">
        <xsl:param name="typeNodes" select="''"/>
        <xsl:param name="typeName" select="''" />
        
        <xsl:variable name="typeid" select="translate(concat('T:',../@type),'[]','')"/>
        <xsl:variable name="href" select="concat('../',string(NAntUtil:GetHRef($typeid)))" />
        
        <table>
            <tr>
                <td align="left"><h4>&lt;<a href="{$href}"><xsl:value-of select="property[@name='Name']/@value"/></a>&gt;</h4></td>
                <td align="right">(<xsl:value-of select="$typeName"/>) Required:<xsl:value-of select="property[@name='Required']/@value"/></td>
            </tr>
        </table>
        <div class="nested-element">
            
            <xsl:apply-templates select=".." mode="docstring" />
            
            <!--
                Put nested element class doc inline
            -->
            <xsl:variable name="typeNode" select="$typeNodes/class[@id=$typeid]"/>
            <xsl:if test="$typeNode"> 
                <xsl:apply-templates select="$typeNode"/>
            </xsl:if>    
        </div>
    </xsl:template>
</xsl:stylesheet>