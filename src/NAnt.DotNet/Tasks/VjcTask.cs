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

// Tom Jordan (tdjordan@users.sourceforge.net)

namespace SourceForge.NAnt.Tasks {

   using System;
   using System.IO;
   using SourceForge.NAnt.Attributes;
   
	/// <summary>Compiles Visual J# programs using vjc, Microsoft's J# compiler.</summary>
	/// <example>
	///   <para>Compile <c>helloworld.jsl</c> to <c>helloworld.exe</c>.</para>
	///   <code>
   ///     <![CDATA[
   ///<vjc target="exe" output="helloworld.exe" debug="true">
	///   <sources>
	///      <includes name="helloworld.jsl"/>
	///   </sources>
	///</vjc>
	///     ]]>
	///   </code>
	/// </example>
	[TaskName("vjc")]
	public class VjcTask : MsftFXCompilerBase {
	
	   bool   _securescoping = false;
	   string _x             = null;
	   string _libpath       = null;
	   string _jcpa          = null;
	   string _codepage      = null;
	   	   
	   // J# specific compiler options

	   /// <summary>
	   /// Specifies whether package-scoped members are accessible outside of the assembly.
	   /// In other words, package scope is treated as assembly scope when emitting metadata.
	   /// <para>By default, secure scoping is off.</para>
	   /// <para>Corresponds to the <c>/securescoping</c> flag.</para>
	   /// </summary>
	   /// <remarks><a href="ms-help://MS.VSCC/MS.VJSharp/dv_vjsharp/html/vjgrfsecurescopingmakepackage-scopedmembersinaccessibleoutsideassembly.htm">See the Visual J# Reference for details.</a></remarks>
	   /// <value>
	   /// <para>The value of this attribute must be either <c>true</c> or <c>false</c>.</para>
	   /// <para>If <c>false</c>, the switch is omitted.</para>
	   /// </value>
	   /// <example>
      /// <code><![CDATA[<vjc securescoping='true'/>]]></code>
	   /// </example>
	   [TaskAttribute("securescoping")]
	   [BooleanValidator()]
	   public bool SecureScoping {
	      get{return Convert.ToBoolean(_securescoping);}
	      set{_securescoping = value;}
	   }
	   
	   /// <summary>
	   /// Specifies whether to disable language extensions.
	   /// <para>Corresponds to the <c>/x</c> flag.</para>
	   /// </summary>
	   /// <remarks><a href="ms-help://MS.VSCC/MS.VJSharp/dv_vjsharp/html/vjgrfxdisablelanguageextensions.htm">See the Visual J# Reference for details.</a></remarks>
	   /// <value>
	   /// <para>The value of this property must be either <c>all</c>, <c>net</c>, or an empty string.</para>
	   /// <para>Note: <c>net</c> disables only .NET Framework extensions while <c>all</c> also disables VJ++ 6.0 extensions.</para>
	   /// <para>If <c>false</c>, or an empty string, the switch is omitted.</para>
	   /// </value>
	   /// <example>
	   /// <para>To disable only the .NET Framework extensions:<c><![CDATA[
	   /// <vjc x='net'/>
	   /// ]]></c></para>
	   /// <para>To disable the .NET Framework extensions and the VJ++ 6.0 extensions:<c><![CDATA[
	   /// <vjc x='all'/>
	   /// ]]></c></para>
	   /// </example>
	   [TaskAttribute("x")]
	   public string X {
	      get{return _x;}
	      set{_x = value;}
	   }
	   
	   /// <summary>
	   /// Specifies the location of assemblies referenced by way of the <c>/reference</c> flag.
	   /// <para>Corresponds to the <c>/libpath:dir[;dir2]</c> flag.</para>
	   /// </summary>
	   /// <remarks><a href="ms-help://MS.VSCC/MS.VJSharp/dv_vjsharp/html/vjgrflibpathspecifyassemblyreferencelocations.htm">See the Visual J# Reference for details.</a></remarks>
	   /// <value>
	   /// <para>The value of this property must exist or an empty string.</para>
	   /// <para>If <c>false</c>, or an empty string, the switch is omitted.</para>
	   /// </value>
	   [TaskAttribute("libpath")]
	   public string Libpath {
	      get{return _libpath;}
	      set{_libpath = value;}
	   }
	   
	   /// <summary>
	   /// Associate Java-language/COM package names.
	   /// <para>Corresponds to the <c>/jcpa:package=namespace</c> and <c>/jcpa:@filename</c> flags.</para>
	   /// </summary>
	   /// <remarks><a href="ms-help://MS.VSCC/MS.VJSharp/dv_vjsharp/html/vjgrfjcpaassociatejava-compackages.htm">See the Visual J# Reference for details.</a></remarks>
	   /// <value>
	   /// <para>The value of this propery. must be <c>package=namespace</c>, <c>@filename</c>, or an empty string.</para>
	   /// <para>If <c>false</c>, or an empty string, the switch is omitted.</para>
	   /// </value>
	   /// <example>
	   /// <para>Map package 'x' to namespace 'y':<c><![CDATA[
	   /// <vjc jcpa='x=y'/>
	   /// ]]></c></para>
	   /// </example>
	   [TaskAttribute("jcpa")]
	   public string Jcpa {
	      get{return _jcpa;}
	      set{_jcpa = value;}
	   }
	   
	   /// <summary>
	   /// Specifies the code page to use for all source code files in the compilation.
	   /// <para>Corresponds to the <c>/codepage</c> flag.</para>
	   /// </summary>
	   /// <remarks><a href="ms-help://MS.VSCC/MS.VJSharp/dv_vjsharp/html/vjlrfcodepagespecifycodepageforsourcecodefiles.htm">See the Visual J# Reference for details.</a></remarks>
	   /// <value>
	   /// <para>The value of this property, must be a codepage <c>id</c> string, or an empty string.</para>
	   /// <para>If <c>false</c>, or an empty string, the switch is omitted.</para>
	   /// </value>
	   [TaskAttribute("codepage")]
	   public string Codepage {
	      get{return _codepage;}
	      set{_codepage = value;}
	   }
	   
	   protected override void
	   WriteOptions(TextWriter writer) {
	      // handle secure scoping.
         if(SecureScoping) {
            writer.WriteLine("/securescoping+");
         }
         
         // handle the x option.
         if(Convert.ToString((object)_x) != String.Empty) {
            if(_x != "false") {
               WriteOption(writer, "x", _x);
            }
         }
         
         // handle the libpath option.
         if(Convert.ToString((object)_libpath) != String.Empty) {
            if(_libpath != "false") {
               WriteOption(writer, "libpath", _libpath);
            }
         }
         
         // handle the jcpa option.
         if(Convert.ToString((object)_jcpa) != String.Empty) {
            if(_jcpa != "false") {
               WriteOption(writer, "jcpa", _jcpa);
            }
         }
         
         // handle the codepage option.
         if(Convert.ToString((object)_codepage) != String.Empty) {
            if(_codepage != "false") {
               WriteOption(writer, "codepage", _codepage);
            }
         }
         
         // handle debug builds.
	      if(Debug) {
	         WriteOption(writer, "debug");
	         WriteOption(writer, "define", "DEBUG");
	         WriteOption(writer, "define", "TRACE");
	      }
	   }
	   
	   protected override string
	   GetExtension() {return "jsl";}
	}
}
