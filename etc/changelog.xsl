<?xml version="1.0" encoding="ISO-8859-1"?>

<xsl:stylesheet
    xmlns:xsl='http://www.w3.org/1999/XSL/Transform'
    version='1.0'>

<!--
    Copyright  2002,2004 The Apache Software Foundation
   
     Licensed under the Apache License, Version 2.0 (the "License");
     you may not use this file except in compliance with the License.
     You may obtain a copy of the License at
   
         http://www.apache.org/licenses/LICENSE-2.0
   
     Unless required by applicable law or agreed to in writing, software
     distributed under the License is distributed on an "AS IS" BASIS,
     WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
     See the License for the specific language governing permissions and
     limitations under the License.
   
-->
  <xsl:param name="title"/>
  <xsl:param name="logo"/>
  <xsl:param name="module"/>
  <xsl:param name="cvsweb"/>
  <xsl:param name="start-date"/>
  <xsl:param name="end-date"/>

  <xsl:output method="html" indent="yes" encoding="US-ASCII"
              doctype-public="-//W3C//DTD HTML 4.01//EN"
              doctype-system="http://www.w3.org/TR/html401/strict.dtd"/>

  <!-- Copy standard document elements.  Elements that
       should be ignored must be filtered by apply-templates
       tags. -->
  <xsl:template match="*">
    <xsl:copy>
      <xsl:copy-of select="attribute::*[. != '']"/>
      <xsl:apply-templates/>
    </xsl:copy>
  </xsl:template>

  <xsl:template match="changelog">
    <html>
      <head>
        <title><xsl:value-of select="$title"/></title>
        <style type="text/css">
          body, p {
            font-family: Verdana, Arial, Helvetica, sans-serif;
            font-size: 90%;
            color: #000000;
            background-color: white;
          }
          tr, td {
            font-family: Verdana, Arial, Helvetica, sans-serif;
            background: white;
          }
          td {
            padding-left: 20px;
          }
      .dateAndAuthor {
            font-family: Verdana, Arial, Helvetica, sans-serif;
            font-weight: bold;
            text-align: left;
            background: #dfff80;
            padding-left: 3px;
      }
          a {
            color: #000000;
          }
          pre {
            font-weight: bold;
          }
          
        .NavBar {
            color: black;
            background-color: #dfff80;
            border-color: #999966;
            border-style: none none solid none;
            border-width: 2px;
        }
        
        .NavBar-Cell {
            font-family: Verdana, Arial, Helvetica, Geneva, SunSans-Regular, sans-serif;
            font-size: 79%;
            background-color: #dfff80;
        }
        .NavBar-Row {
            font-family: Verdana, Arial, Helvetica, Geneva, SunSans-Regular, sans-serif;
            font-size: 79%;
            background-color: white;
            background-color: #dfff80;

        }
        </style>
      </head>
      <body>
        <table width="100%" border="0" cellspacing="0" cellpadding="2" class="NavBar">
            <tr class="NavBar-Row">
                <td class="NavBar-Cell"><b>NAnt</b> : A .NET Build Tool</td>
            </tr>
        </table>
        <table width="100%">
            <tr style="background-color:white">
                <td style="background-color:white">
                    <h1>
                      <a name="top"><xsl:value-of select="$title"/></a>
                    </h1>
                </td>
                <td style="text-align: right; background-color:white">
                    <a href="http://nant.sourceforge.net">
                        <img style="border-width: 0px;" alt="Logo (link to home page)" width="270" height="118">
                            <xsl:attribute name="src">
                                <xsl:value-of select="$logo"/>
                            </xsl:attribute>
                        </img>    
                    </a>    
                </td>
            </tr>
            <tr style="background-color:white">
                <td style="background-color:white"><xsl:value-of select="$start-date"/> to <xsl:value-of select="$end-date"/>
                </td>
                <td style="background-color:white; text-align: right">
                    Build Date: <xsl:value-of select="$start-date"/>
                </td>
            </tr>
        </table>    
        <p style="text-align: right">Adopted from changelog.xsl for <a href="http://jakarta.apache.org/ant/">Ant</a>.</p>
        <p style="text-align: right">Designed for use with <a href="http://nant.sourceforge.net/">NAnt</a>.</p>
        
        <hr/>
        <table border="0" width="100%" cellspacing="1">
          
          <xsl:apply-templates select=".//entry">
            <xsl:sort select="date" data-type="text" order="descending"/>
            <xsl:sort select="time" data-type="text" order="descending"/>
          </xsl:apply-templates>
          
        </table>
        
      </body>
    </html>
  </xsl:template>
  
  <xsl:template match="entry">
    <tr>
      <td class="dateAndAuthor">
        <xsl:value-of select="date"/><xsl:text> </xsl:text><xsl:value-of select="time"/><xsl:text> </xsl:text><xsl:value-of select="author"/>
      </td>
    </tr>
    <tr>
      <td>
        <pre>
<xsl:apply-templates select="msg"/></pre>
        <ul>
          <xsl:apply-templates select="file"/>
        </ul>
      </td>
    </tr>
  </xsl:template>

  <xsl:template match="date">
    <i><xsl:value-of select="."/></i>
  </xsl:template>

  <xsl:template match="time">
    <i><xsl:value-of select="."/></i>
  </xsl:template>

  <xsl:template match="author">
    <i>
      <a>
        <xsl:attribute name="href">mailto:<xsl:value-of select="."/></xsl:attribute>
        <xsl:value-of select="."/></a>
    </i>
  </xsl:template>

  <xsl:template match="file">
    <li>
      <a>
        <xsl:choose>
          <xsl:when test="string-length(prevrevision) = 0 ">
            <xsl:attribute name="href"><xsl:value-of select="$cvsweb"/><xsl:value-of select="$module" />/<xsl:value-of select="name" />?rev=<xsl:value-of select="revision" />&amp;content-type=text/x-cvsweb-markup</xsl:attribute>
          </xsl:when>
          <xsl:otherwise>
            <xsl:attribute name="href"><xsl:value-of select="$cvsweb"/><xsl:value-of select="$module" />/<xsl:value-of select="name" />?r1=<xsl:value-of select="revision" />&amp;r2=<xsl:value-of select="prevrevision"/></xsl:attribute>
          </xsl:otherwise>
        </xsl:choose>
        <xsl:value-of select="name" /> (<xsl:value-of select="revision"/>)</a>
    </li>
  </xsl:template>

  <!-- Any elements within a msg are processed,
       so that we can preserve HTML tags. -->
  <xsl:template match="msg">
    <xsl:apply-templates/>
  </xsl:template>
  
</xsl:stylesheet>
