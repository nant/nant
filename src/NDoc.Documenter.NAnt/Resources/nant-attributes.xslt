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

<xsl:stylesheet xmlns="http://www.w3.org/1999/xhtml"  xmlns:xsl="http://www.w3.org/1999/XSL/Transform" xmlns:NAntUtil="urn:NAntUtil" exclude-result-prefixes="NAntUtil" version="1.0">
    <!-- match attribute by names -->
    <xsl:template match="attribute[@name = 'NAnt.Core.Attributes.BuildElementAttribute' and ancestor::property]" mode="NestedElements">
        <xsl:comment>Element</xsl:comment>
        <xsl:call-template name="NestedElement">
            <xsl:with-param name="elementTypeParam" select="../@type" />
        </xsl:call-template>
    </xsl:template>

    <xsl:template match="attribute[@name = 'NAnt.Core.Attributes.BuildElementAttribute' and ancestor::method]" mode="NestedElements">
        <xsl:comment>Element</xsl:comment>
        <xsl:call-template name="NestedElement">
            <xsl:with-param name="elementTypeParam" select="../parameter[position()=1]/@type" />
        </xsl:call-template>
    </xsl:template>

    <xsl:template match="attribute[@name = 'NAnt.Core.Attributes.BuildElementArrayAttribute']" mode="NestedElements">
        <xsl:comment>Array</xsl:comment>
        <xsl:call-template name="NestedElementArray">
            <xsl:with-param name="elementTypeParam">
                <xsl:choose>
                    <xsl:when test="property[@name='ElementType']/@value != 'null'">
                        <xsl:value-of select="property[@name='ElementType']/@value" />
                    </xsl:when>
                    <xsl:otherwise>
                        <xsl:variable name="elementType" select="translate(translate(concat('T:', ../@type), '[]', ''), '+', '.')" />
                        <xsl:choose>
                            <!-- check if we're dealing with array of elements -->
                            <xsl:when test="NAntUtil:IsElement($elementType)">
                                <xsl:value-of select="../@type" />
                            </xsl:when>
                            <!-- check if we're dealing with strongly typed collection -->
                            <xsl:when test="count(NAntUtil:GetClassNode($elementType)/method[@name = 'Add' and @access = 'Public' and count(child::parameter) = 1]) > 0">
                                <xsl:value-of select="NAntUtil:GetClassNode($elementType)/method[@name = 'Add' and @access = 'Public' and count(child::parameter) = 1]/child::parameter[position() = 1]/@type" />
                            </xsl:when>
                        </xsl:choose>
                    </xsl:otherwise>
                </xsl:choose>
            </xsl:with-param>
        </xsl:call-template>
    </xsl:template>

    <xsl:template match="attribute[@name = 'NAnt.Core.Attributes.BuildElementCollectionAttribute']" mode="NestedElements">
        <xsl:comment>Collection</xsl:comment>
        <xsl:call-template name="NestedElementCollection">
            <xsl:with-param name="elementTypeParam">
                <xsl:choose>
                    <xsl:when test="property[@name='ElementType']/@value != 'null'">
                        <xsl:value-of select="property[@name='ElementType']/@value" />
                    </xsl:when>
                    <xsl:otherwise>
                        <xsl:variable name="elementType" select="translate(translate(concat('T:', ../@type), '[]', ''), '+', '.')" />
                        <xsl:choose>
                            <!-- check if we're dealing with array of elements -->
                            <xsl:when test="NAntUtil:IsElement($elementType)">
                                <xsl:value-of select="../@type" />
                            </xsl:when>
                            <!-- check if we're dealing with strongly typed collection -->
                            <xsl:when test="count(NAntUtil:GetClassNode($elementType)/method[@name = 'Add' and @access = 'Public' and count(child::parameter) = 1]) > 0">
                                <xsl:value-of select="NAntUtil:GetClassNode($elementType)/method[@name = 'Add' and @access = 'Public' and count(child::parameter) = 1]/child::parameter[position() = 1]/@type" />
                            </xsl:when>
                        </xsl:choose>
                    </xsl:otherwise>
                </xsl:choose>
            </xsl:with-param>
        </xsl:call-template>
    </xsl:template>

    <xsl:template name="NestedElementArray">
        <xsl:param name="elementTypeParam" select="'#'" />
        
        <xsl:variable name="elementType" select="translate(translate(concat('T:', $elementTypeParam), '[]', ''), '+', '.')" />
        <xsl:comment>NestedElementArray=<xsl:value-of select="$elementType" /></xsl:comment>
        
        <!-- only output link when element is global type -->
        <h4>
            <xsl:element name="a">
                <xsl:attribute name="id">
                    <xsl:value-of select="property[@name='Name']/@value" />
                </xsl:attribute>
            </xsl:element>
            <!-- only output link when element is global type -->
            <xsl:choose>
                <xsl:when test="NAntUtil:IsDataType($elementType)">
                    &lt;<a href="{NAntUtil:GetHRef($elementType)}"><xsl:value-of select="property[@name='Name']/@value" /></a>&gt;
                </xsl:when>
                <xsl:otherwise>
                    &lt;<xsl:value-of select="property[@name='Name']/@value" />&gt;
                </xsl:otherwise>
            </xsl:choose>
        </h4>

        <div class="nested-element">
            <!-- generates docs from summary xmldoc comments -->
            <xsl:apply-templates select=".." mode="docstring" />

            <xsl:variable name="typeNode" select="NAntUtil:GetClassNode($elementType)" />
            <xsl:if test="$typeNode and not(NAntUtil:IsDataType($elementType))">
                <xsl:apply-templates select="$typeNode" />
            </xsl:if>
        </div>

        <h4>
            <xsl:element name="a">
                <xsl:attribute name="id">
                    <xsl:value-of select="property[@name='Name']/@value" />
                </xsl:attribute>
            </xsl:element>
            <!-- only output link when element is global type -->
            <xsl:choose>
                <xsl:when test="NAntUtil:IsDataType($elementType)">
                    &lt;/<a href="{NAntUtil:GetHRef($elementType)}"><xsl:value-of select="property[@name='Name']/@value" /></a>&gt;
                </xsl:when>
                <xsl:otherwise>
                    &lt;/<xsl:value-of select="property[@name='Name']/@value" />&gt;
                </xsl:otherwise>
            </xsl:choose>
        </h4>
    </xsl:template>

    <xsl:template name="NestedElementCollection">
        <xsl:param name="elementTypeParam" select="'#'" />
        <xsl:variable name="childElementType" select="translate(translate(concat('T:', $elementTypeParam), '[]', ''), '+', '.')" />

        <xsl:variable name="childElementName">
            <xsl:value-of select="property[@name='ChildElementName']/@value" />
        </xsl:variable>

        <h4>
            <xsl:element name="a">
                <xsl:attribute name="id">
                    <xsl:value-of select="property[@name='Name']/@value" />
                </xsl:attribute>
            </xsl:element>
            &lt;<xsl:value-of select="property[@name='Name']/@value" />&gt;
        </h4>
        
        <div class="nested-element">
            <!-- generates docs from summary xmldoc comments -->
            <xsl:apply-templates select=".." mode="docstring" />

            <!-- put child element docs inline, if not derive from DataTypeBase -->
            <xsl:variable name="childTypeNode" select="NAntUtil:GetClassNode($childElementType)" />
            <xsl:choose>
                <xsl:when test="$childTypeNode and not(NAntUtil:IsDataType($childElementType))">
                    <h5>&lt;<xsl:value-of select="$childElementName" />&gt;</h5>
                    <div class="nested-element">
                        <xsl:apply-templates select="$childTypeNode" />
                    </div>
                    <h5>&lt;/<xsl:value-of select="$childElementName" />&gt;</h5>
                </xsl:when>
                <xsl:otherwise>
                    <h5>&lt;<a href="{NAntUtil:GetHRef($childElementType)}"><xsl:value-of select="$childElementName" /></a>/&gt;</h5>
                </xsl:otherwise>
            </xsl:choose>
        </div>
       
        <h4>&lt;/<xsl:value-of select="property[@name='Name']/@value" />&gt;</h4>
    </xsl:template>

    <xsl:template name="NestedElement">
        <xsl:param name="elementTypeParam" select="'#'" />
        <xsl:variable name="elementType" select="translate(translate(concat('T:', $elementTypeParam), '[]', ''), '+', '.')" />
        
        <!-- only output link when element is global type -->
        <h4>
            <xsl:element name="a">
                <xsl:attribute name="id">
                    <xsl:value-of select="property[@name='Name']/@value" />
                </xsl:attribute>
            </xsl:element>
            <!-- only output link when element is global type -->
            <xsl:choose>
                <xsl:when test="NAntUtil:IsDataType($elementType)">
                    &lt;<a href="{NAntUtil:GetHRef($elementType)}"><xsl:value-of select="property[@name='Name']/@value" /></a>&gt;
                </xsl:when>
                <xsl:otherwise>
                    &lt;<xsl:value-of select="property[@name='Name']/@value" />&gt;
                </xsl:otherwise>
            </xsl:choose>
        </h4>

        <div class="nested-element">
            <!-- generates docs from summary xmldoc comments -->
            <xsl:apply-templates select=".." mode="docstring" />

            <!-- 
                put the nested element class docs inline if the element does 
                not derive from DataTypeBase (meaning, is not a global type)
            -->
            <xsl:variable name="typeNode" select="NAntUtil:GetClassNode($elementType)" />
            <xsl:if test="$typeNode and not(NAntUtil:IsDataType($elementType))">
                <xsl:apply-templates select="$typeNode" />
            </xsl:if>
            <p />
        </div>

        <h4>
            <xsl:element name="a">
                <xsl:attribute name="id">
                    <xsl:value-of select="property[@name='Name']/@value" />
                </xsl:attribute>
            </xsl:element>
            <!-- only output link when element is global type -->
            <xsl:choose>
                <xsl:when test="NAntUtil:IsDataType($elementType)">
                    &lt;/<a href="{NAntUtil:GetHRef($elementType)}"><xsl:value-of select="property[@name='Name']/@value" /></a>&gt;
                </xsl:when>
                <xsl:otherwise>
                    &lt;/<xsl:value-of select="property[@name='Name']/@value" />&gt;
                </xsl:otherwise>
            </xsl:choose>
        </h4>
    </xsl:template>

    <!-- match TaskAttribute property tag -->
    <xsl:template match="class/property[attribute/@name = 'NAnt.Core.Attributes.TaskAttributeAttribute']" mode="TypeDoc">
        <xsl:variable name="Required" select="attribute/property[@name = 'Required']/@value" />
        <xsl:element name="tr">
            <xsl:element name="td">
                <xsl:attribute name="valign">top</xsl:attribute>
                <xsl:if test="$Required = 'True'">
                    <xsl:attribute name="class">required</xsl:attribute>
                </xsl:if>
                <xsl:value-of select="attribute/property[@name = 'Name']/@value" />
            </xsl:element>
            <td style="text-align: center;">
                <xsl:call-template name="get-a-href-with-name">
                    <xsl:with-param name="cref" select="concat('T:', @type)" />
                </xsl:call-template>
            </td>
            <td>
                <xsl:apply-templates mode="docstring" select="." />
                <xsl:if test="attribute/property[@name='ExpandProperties' and @value='False']">
                    <p style="font-weight: bold;">
                        This attribute's properties will not be automatically expanded!
                    </p>
                </xsl:if>
                
            </td>
            <td style="text-align: center;"><xsl:value-of select="string($Required)" /></td>
        </xsl:element>
    </xsl:template>
    
    <!-- match FrameworkConfigurable property tag -->
    <xsl:template match="class/property[attribute/@name = 'NAnt.Core.Attributes.FrameworkConfigurableAttribute']" mode="TypeDoc">
        <xsl:variable name="FrameworkConfigurableAttribute" select="attribute[@name = 'NAnt.Core.Attributes.FrameworkConfigurableAttribute']" />
        <xsl:variable name="Required" select="$FrameworkConfigurableAttribute/property[@name = 'Required']/@value" />
        <tr>
            <td valign="top"><xsl:value-of select="$FrameworkConfigurableAttribute/property[@name = 'Name']/@value" /></td>
            <td style="text-align: center;">
                <xsl:call-template name="get-a-href-with-name">
                    <xsl:with-param name="cref" select="concat('T:', @type)" />
                </xsl:call-template>
            </td>
            <td><xsl:apply-templates select="." mode="docstring" /></td>
            <td style="text-align: center;"><xsl:value-of select="string($Required)" /></td>
        </tr>
    </xsl:template>
</xsl:stylesheet>
