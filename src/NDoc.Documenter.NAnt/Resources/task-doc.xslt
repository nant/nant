<?xml version="1.0"?>
<!--
// NAnt - A .NET build tool
// Copyright (C) 2001-2002 Gerry Shaw
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

// Ian MacLean (ian@maclean.ms)
// Gerry Shaw (gerry_shaw@yahoo.com)
-->
<xsl:stylesheet 
    xmlns:xsl="http://www.w3.org/1999/XSL/Transform"
    xmlns:msxsl="urn:schemas-microsoft-com:xslt"
    version="1.0">

<xsl:include href="tags.xslt" />
<xsl:output method="html" indent="yes" />

<!-- The class we are documenting this time. This value will be passed in by the caller. argv[] equivalent. Default value is used for testing -->
<xsl:param name="class-id">T:SourceForge.NAnt.Tasks.AlTask</xsl:param>

<xsl:template match="/">
    <html>
        <xsl:apply-templates select ="//class[@id=$class-id]"/>
    </html>
</xsl:template>

<!-- match class tag -->
<xsl:template match="class">

    <xsl:variable name = "attr" select="attribute/@name"/>
    <xsl:if test="string($attr) = 'SourceForge.NAnt.Attributes.TaskNameAttribute'">
        <head>
            <meta http-equiv="Content-Language" content="en-ca" />
            <meta http-equiv="Content-Type" content="text/html; charset=windows-1252" />
            <meta name="description" content="Zip" />
            <link rel="stylesheet" type="text/css" href="../../style.css" />
            <title>&lt;<xsl:value-of select="attribute/property[@name='Name']/@value" />&gt; Task</title>
        </head>

        <body>
        <table width="100%" border="0" cellspacing="0" cellpadding="2" class="NavBar">
            <tr>
            <td class="NavBar-Cell" width="100%">
                <a href="../../index.html"><b>NAnt</b></a>
                <img alt="->" src="../../arrow.gif" />
                <a href="../index.html">Help</a>
                <img alt="->" src="../../arrow.gif" />
                <a href="index.html">Task Reference</a>
                <img alt="->" src="../../arrow.gif" /><xsl:text> </xsl:text>
                &lt;<xsl:value-of select="attribute/property[@name='Name']/@value" />&gt; Task
            </td>
            </tr>
        </table>
        
        <h1>&lt;<xsl:value-of select="attribute/property[@name='Name']/@value" />&gt; Task</h1>
        <p><xsl:apply-templates select="documentation/summary/node()" mode="slashdoc"/></p>
        <!-- Remarks -->
        <xsl:apply-templates select="documentation/remarks/node()" mode="slashdoc"/>

        <h3>Parameters</h3>
        <xsl:variable name = "properties" select="property"/>
        <xsl:if test="count($properties) != 0">
            <div class="Table-Section">
            <table class="Table">
                <tr>
                    <th class="Table-Header">Attribute</th>
                    <th class="Table-Header">Description</th>
                    <th class="Table-Header" align="center">Required</th>
                </tr>
                <xsl:apply-templates select="property[attribute/@name = 'SourceForge.NAnt.Attributes.TaskAttributeAttribute' ]" mode="TaskAttribute">
                    <!-- uncomment to sort attributes by name <xsl:sort select="@name"/> -->
                </xsl:apply-templates>
            </table>
            </div>
        </xsl:if>
        <xsl:variable name = "filesets" select="property[attribute/@name = 'SourceForge.NAnt.Attributes.FileSetAttribute' ]"/>
        <xsl:if test="count($filesets) != 0">
            <h3>Nested Elements</h3>
            <!-- now do filesets -->
            <xsl:apply-templates select="property[attribute/@name = 'SourceForge.NAnt.Attributes.FileSetAttribute' ]" mode="FileSet"/>
        </xsl:if> 

        <!-- Example -->
        <h3>Examples</h3>
        <xsl:apply-templates select="documentation/example/node()" mode="slashdoc"/>

        </body>
    </xsl:if>
</xsl:template>

<!-- match property tag -->
<xsl:template match="property" mode="TaskAttribute">
    <xsl:variable name = "TaskAttr" select="attribute[@name='SourceForge.NAnt.Attributes.TaskAttributeAttribute']"/>
    <xsl:if test="count($TaskAttr) = 1">
         <xsl:variable name = "documentation" >        
                <xsl:call-template name="docstring" >         
                </xsl:call-template>
            </xsl:variable> 
        <xsl:variable name = "Required" select="$TaskAttr/property[@name='Required']/@value"/>        
        <tr>
            <td class="Table-Cell" valign="top"><xsl:value-of select="$TaskAttr/property[@name='Name']/@value"/> </td>                       
            <td class="Table-Cell"><xsl:value-of select="string($documentation)"/></td>
            <td class="Table-Cell" align="center"><xsl:value-of select="string($Required)"/></td>
        </tr>
    </xsl:if>
</xsl:template>

<!-- match fileset property tag -->
<xsl:template match="property" mode="FileSet">               
    <xsl:variable name = "FileSetAttr" select="attribute[@name='SourceForge.NAnt.Attributes.FileSetAttribute']"/>
    <xsl:variable name = "documentation" >        
        <xsl:call-template name="docstring" >         
        </xsl:call-template>
    </xsl:variable>     
    <!-- @name -->
    <h4><xsl:value-of select="$FileSetAttr/property[@name='Name']/@value" /> (FileSet)</h4>
    <p> <xsl:value-of select="$documentation" /></p> 
    
</xsl:template> 

<!-- returns the doc string for a given value (called from the property templates )-->
<xsl:template name="docstring" >
    <xsl:choose>
        <xsl:when test="@declaringType">
            <xsl:value-of select="//class[@id=concat('T:', current()/@declaringType)]/*[@name=current()/@name]/documentation/summary" />
        </xsl:when>
        <xsl:otherwise>
            <xsl:value-of select="documentation/summary" />
        </xsl:otherwise>
    </xsl:choose>
</xsl:template> 

</xsl:stylesheet>
