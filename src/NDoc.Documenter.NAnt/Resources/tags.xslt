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
// Gert Driesen (drieseng@users.sourceforge.net.com)
// Scott Hernandez (ScottHernandez-at-Hotmail....com)
-->
<xsl:stylesheet version="1.0" xmlns="http://www.w3.org/1999/xhtml" xmlns:xsl="http://www.w3.org/1999/XSL/Transform"
    xmlns:doc="http://ndoc.sf.net/doc" xmlns:NAntUtil="urn:NAntUtil" exclude-result-prefixes="doc NAntUtil">
    <!--
     | Identity Template
     +-->
    <xsl:template match="node()|@*" mode="slashdoc">
        <xsl:copy>
            <xsl:apply-templates select="@*|node()" mode="slashdoc" />
        </xsl:copy>
    </xsl:template>

    <!--
     | Block Tags
     +-->
    <doc:template>
        <summary>A normal paragraph. This ends up being a <b>p</b> tag.
        (Did we really need the extra three letters?)</summary>
    </doc:template>

    <xsl:template match="para" mode="slashdoc" doc:group="block" doc:msdn="ms-help://MS.NETFrameworkSDK/csref/html/vclrfpara.htm">
        <p>
            <xsl:apply-templates select="./node()" mode="slashdoc" />
        </p>
    </xsl:template>

    <doc:template>
        <summary>Use the lang attribute to indicate that the text of the
        paragraph is only appropriate for a specific language.</summary>
    </doc:template>

    <xsl:template match="para[@lang]" mode="slashdoc" doc:group="block">
        <p>
            <span class="lang">
                <xsl:text>[</xsl:text>
                <xsl:call-template name="get-lang">
                    <xsl:with-param name="lang" select="@lang" />
                </xsl:call-template>
                <xsl:text>]</xsl:text>
            </span>
            <xsl:text>&#160;</xsl:text>
            <xsl:apply-templates select="./node()" mode="slashdoc" />
        </p>
    </xsl:template>

    <doc:template>
        <summary>Multiple lines of code.</summary>
    </doc:template>

    <xsl:template match="code" mode="slashdoc" doc:group="block" doc:msdn="ms-help://MS.NETFrameworkSDK/csref/html/vclrfcode.htm">
        <pre class="code">
            <xsl:apply-templates mode="slashdoc" />
        </pre>
    </xsl:template>

    <doc:template>
        <summary>Use the lang attribute to indicate that the code
        sample is only appropriate for a specific language.</summary>
    </doc:template>

    <xsl:template match="code[@lang]" mode="slashdoc" doc:group="block">
        <pre class="code">
            <span class="lang">
                <xsl:text>[</xsl:text>
                <xsl:call-template name="get-lang">
                    <xsl:with-param name="lang" select="@lang" />
                </xsl:call-template>
                <xsl:text>]</xsl:text>
            </span>
            <xsl:apply-templates mode="slashdoc" />
        </pre>
    </xsl:template>

    <doc:template>
        <summary>See <a href="ms-help://MS.NETFrameworkSDK/cpref/html/frlrfSystemXmlXmlDocumentClassLoadTopic.htm">XmlDocument.Load</a>
        for an example of a note.</summary>
    </doc:template>

    <xsl:template match="h3" mode="slashdoc" doc:group="block">
        <h3>
            <xsl:apply-templates select="./node()" mode="slashdoc" />
        </h3>
    </xsl:template>

    <xsl:template match="note" mode="slashdoc" doc:group="block">
        <p class="i2">
            <xsl:choose>
                <xsl:when test="@type='caution'">
                    <b>CAUTION:</b>
                </xsl:when>
                <xsl:when test="@type='inheritinfo'">
                    <b>Notes to Inheritors: </b>
                </xsl:when>
                <xsl:when test="@type='inotes'">
                    <b>Notes to Implementers: </b>
                </xsl:when>
                <xsl:otherwise>
                    <b>Note:</b>
                </xsl:otherwise>
            </xsl:choose>
            <xsl:text> </xsl:text>
            <xsl:apply-templates mode="slashdoc" />
        </p>
    </xsl:template>

    <xsl:template match="list[@type='bullet']" mode="slashdoc" doc:group="block" doc:msdn="ms-help://MS.NETFrameworkSDK/csref/html/vclrflist.htm">
        <ul style="list-style-type: disc;">
            <xsl:apply-templates select="item" mode="slashdoc" />
        </ul>
    </xsl:template>

    <xsl:template match="list[@type='bullet']/item" mode="slashdoc" doc:msdn="ms-help://MS.NETFrameworkSDK/csref/html/vclrflist.htm">
        <li>
            <xsl:apply-templates select="./node()" mode="slashdoc" />
        </li>
    </xsl:template>

    <xsl:template match="list[@type='bullet']/item/term" mode="slashdoc" doc:msdn="ms-help://MS.NETFrameworkSDK/csref/html/vclrflist.htm">
        <b><xsl:apply-templates select="./node()" mode="slashdoc" /> - </b>
    </xsl:template>

    <xsl:template match="list[@type='bullet']/item/description" mode="slashdoc" doc:msdn="ms-help://MS.NETFrameworkSDK/csref/html/vclrflist.htm">
        <xsl:apply-templates select="./node()" mode="slashdoc" />
    </xsl:template>

    <xsl:template match="list[@type='number']" mode="slashdoc" doc:group="block" doc:msdn="ms-help://MS.NETFrameworkSDK/csref/html/vclrflist.htm">
        <ol>
            <xsl:apply-templates select="item" mode="slashdoc" />
        </ol>
    </xsl:template>

    <xsl:template match="list[@type='number']/item" mode="slashdoc" doc:msdn="ms-help://MS.NETFrameworkSDK/csref/html/vclrflist.htm">
        <li>
            <xsl:apply-templates select="./node()" mode="slashdoc" />
        </li>
    </xsl:template>

    <xsl:template match="list[@type='number']/item/term" mode="slashdoc" doc:msdn="ms-help://MS.NETFrameworkSDK/csref/html/vclrflist.htm">
        <b><xsl:apply-templates select="./node()" mode="slashdoc" /> - </b>
    </xsl:template>

    <xsl:template match="list[@type='number']/item/description" mode="slashdoc" doc:msdn="ms-help://MS.NETFrameworkSDK/csref/html/vclrflist.htm">
        <xsl:apply-templates select="./node()" mode="slashdoc" />
    </xsl:template>

    <xsl:template match="list[@type='table']" mode="slashdoc" doc:group="block" doc:msdn="ms-help://MS.NETFrameworkSDK/csref/html/vclrflist.htm">
        <div class="table">
            <table>
                <xsl:apply-templates select="listheader" mode="slashdoc" />
                <xsl:apply-templates select="item" mode="slashdoc" />
            </table>
        </div>
    </xsl:template>

    <xsl:template match="list[@type='table']/listheader" mode="slashdoc" doc:msdn="ms-help://MS.NETFrameworkSDK/csref/html/vclrflist.htm">
        <tr>
            <xsl:apply-templates mode="slashdoc" />
        </tr>
    </xsl:template>

    <xsl:template match="list[@type='table']/listheader/term" mode="slashdoc" doc:msdn="ms-help://MS.NETFrameworkSDK/csref/html/vclrflist.htm">
        <th>
            <xsl:apply-templates select="./node()" mode="slashdoc" />
        </th>
    </xsl:template>

    <xsl:template match="list[@type='table']/listheader/description" mode="slashdoc" doc:msdn="ms-help://MS.NETFrameworkSDK/csref/html/vclrflist.htm">
        <th>
            <xsl:apply-templates select="./node()" mode="slashdoc" />
        </th>
    </xsl:template>

    <xsl:template match="list[@type='table']/item" mode="slashdoc" doc:msdn="ms-help://MS.NETFrameworkSDK/csref/html/vclrflist.htm">
        <tr>
            <xsl:apply-templates mode="slashdoc" />
        </tr>
    </xsl:template>

    <xsl:template match="list[@type='table']/item/term" mode="slashdoc" doc:msdn="ms-help://MS.NETFrameworkSDK/csref/html/vclrflist.htm">
        <td>
            <xsl:apply-templates select="./node()" mode="slashdoc" />
        </td>
    </xsl:template>

    <xsl:template match="list[@type='table']/item/description" mode="slashdoc" doc:msdn="ms-help://MS.NETFrameworkSDK/csref/html/vclrflist.htm">
        <td>
            <xsl:apply-templates select="./node()" mode="slashdoc" />
        </td>
    </xsl:template>

    <!--
     | Inline Tags
     +-->
    <xsl:template match="c" mode="slashdoc" doc:group="inline" doc:msdn="ms-help://MS.NETFrameworkSDK/csref/html/vclrfc.htm">
        <code>
            <xsl:apply-templates mode="slashdoc" />
        </code>
    </xsl:template>

    <xsl:template match="paramref[@name]" mode="slashdoc" doc:group="inline" doc:msdn="ms-help://MS.NETFrameworkSDK/csref/html/vclrfparamref.htm">
        <i>
            <xsl:value-of select="@name" />
        </i>
    </xsl:template>

    <xsl:template match="see[@cref]" mode="slashdoc" doc:group="inline" doc:msdn="ms-help://MS.NETFrameworkSDK/csref/html/vclrfsee.htm">
        <xsl:call-template name="get-a-href">
            <xsl:with-param name="cref" select="@cref" />
        </xsl:call-template>
        <xsl:choose>
            <!-- if this is a task add suffix task-->
            <xsl:when test="boolean(NAntUtil:IsTask(@cref))">
                <xsl:text> task</xsl:text>
            </xsl:when>
            <!-- if this is a functionset add suffix functions -->
            <xsl:when test="boolean(NAntUtil:IsFunctionSet(@cref))">
                <xsl:text> functions</xsl:text>
            </xsl:when>
        </xsl:choose>
    </xsl:template>

    <!-- get-a-href -->
    <xsl:template name="get-a-href">
        <xsl:param name="cref" />
        <xsl:variable name="href" select="string(NAntUtil:GetHRef($cref))" />
        <xsl:choose>
            <xsl:when test="$href = ''">
                <xsl:choose>
                    <!-- if this is a task add suffix task-->
                    <xsl:when test="boolean(NAntUtil:IsTask($cref))">
                        <code>
                            <xsl:value-of select="string(NAntUtil:GetTaskName($cref))" />
                        </code>
                    </xsl:when>
                    <xsl:otherwise>
                        <code>
                            <xsl:value-of select="string(NAntUtil:GetName($cref))" />
                        </code>
                    </xsl:otherwise>
                </xsl:choose>
            </xsl:when>
            <xsl:otherwise>
                <xsl:element name="a">
                    <xsl:attribute name="href">
                        <xsl:value-of select="$href" />
                    </xsl:attribute>
                    <xsl:choose>
                        <xsl:when test="node()">
                            <xsl:value-of select="." />
                        </xsl:when>
                        <xsl:otherwise>
                            <xsl:value-of select="string(NAntUtil:GetName($cref))" />
                        </xsl:otherwise>
                    </xsl:choose>
                </xsl:element>
            </xsl:otherwise>
        </xsl:choose>
    </xsl:template>

    <!-- get-a-href-with-name -->
    <xsl:template name="get-a-href-with-name">
        <xsl:param name="cref" />
        <xsl:variable name="type" select="substring($cref, 3, string-length($cref) - 2)" />
        <xsl:variable name="href">
            <xsl:choose>
                <xsl:when test="$type='System.Byte'"></xsl:when>
                <xsl:when test="$type='System.SByte'"></xsl:when>
                <xsl:when test="$type='System.Int16'"></xsl:when>
                <xsl:when test="$type='System.UInt16'"></xsl:when>
                <xsl:when test="$type='System.Int32'"></xsl:when>
                <xsl:when test="$type='System.UInt32'"></xsl:when>
                <xsl:when test="$type='System.Int64'"></xsl:when>
                <xsl:when test="$type='System.UInt64'"></xsl:when>
                <xsl:when test="$type='System.Single'"></xsl:when>
                <xsl:when test="$type='System.Double'"></xsl:when>
                <xsl:when test="$type='System.Decimal'"></xsl:when>
                <xsl:when test="$type='System.String'"></xsl:when>
                <xsl:when test="$type='System.Char'"></xsl:when>
                <xsl:when test="$type='System.Boolean'"></xsl:when>
                <xsl:when test="$type='System.IO.FileInfo'"></xsl:when>
                <xsl:when test="$type='System.IO.DirectoryInfo'"></xsl:when>
                <xsl:when test="$type='System.DateTime'"></xsl:when>
                <xsl:when test="$type='System.TimeSpan'"></xsl:when>
                <xsl:otherwise>
                    <xsl:if test="count(//enumeration[@id=$cref]) != 0 or starts-with(substring($cref, 3, string-length($cref) - 2), 'System.')">
                        <xsl:value-of select="string(NAntUtil:GetHRef($cref))" />
                    </xsl:if>
                </xsl:otherwise>
            </xsl:choose>
        </xsl:variable>
        <xsl:choose>
            <xsl:when test="$href=''">
                <xsl:call-template name="get-type-name">
                    <xsl:with-param name="type" select="substring($cref, 3, string-length($cref) - 2)" />
                </xsl:call-template>
            </xsl:when>
            <xsl:otherwise>
                <xsl:element name="a">
                    <xsl:attribute name="href">
                        <xsl:value-of select="$href" />
                    </xsl:attribute>
                    <xsl:call-template name="get-type-name">
                        <xsl:with-param name="type" select="substring($cref, 3, string-length($cref) - 2)" />
                    </xsl:call-template>
                </xsl:element>
            </xsl:otherwise>
        </xsl:choose>
    </xsl:template>

    <xsl:template match="see[@href]" mode="slashdoc" doc:group="inline">
        <a href="{@href}">
            <xsl:choose>
                <xsl:when test="node()">
                    <xsl:value-of select="." />
                </xsl:when>
                <xsl:otherwise>
                    <xsl:value-of select="@href" />
                </xsl:otherwise>
            </xsl:choose>
        </a>
    </xsl:template>

    <xsl:template match="see[@langword]" mode="slashdoc" doc:group="inline">
        <xsl:choose>
            <xsl:when test="@langword='null'">
                <xsl:text>a null reference (</xsl:text>
                <b>Nothing</b>
                <xsl:text> in Visual Basic)</xsl:text>
            </xsl:when>
            <xsl:when test="@langword='sealed'">
                <xsl:text>sealed (</xsl:text>
                <b>NotInheritable</b>
                <xsl:text> in Visual Basic)</xsl:text>
            </xsl:when>
            <xsl:when test="@langword='static'">
                <xsl:text>static (</xsl:text>
                <b>Shared</b>
                <xsl:text> in Visual Basic)</xsl:text>
            </xsl:when>
            <xsl:when test="@langword='abstract'">
                <xsl:text>abstract (</xsl:text>
                <b>MustInherit</b>
                <xsl:text> in Visual Basic)</xsl:text>
            </xsl:when>
            <xsl:when test="@langword='virtual'">
                <xsl:text>virtual (</xsl:text>
                <b>CanOverride</b>
                <xsl:text> in Visual Basic)</xsl:text>
            </xsl:when>
            <xsl:otherwise>
                <b>
                    <xsl:value-of select="@langword" />
                </b>
            </xsl:otherwise>
        </xsl:choose>
    </xsl:template>
</xsl:stylesheet>
