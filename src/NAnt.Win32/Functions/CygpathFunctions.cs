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
// Ian Maclean (imaclean@gmail.com)
// Jaroslaw Kowalski (jkowalski@users.sourceforge.net)
// Gert Driesen (drieseng@users.sourceforge.net)

using System;
using System.ComponentModel;
using System.IO;
using NAnt.Core;
using NAnt.Core.Attributes;
using NAnt.Core.Tasks;
using NAnt.Core.Types;

namespace NAnt.Win32.Functions {
    /// <summary>
    /// Groups a set of functions that convert Windows native filenames to 
    /// Cygwin POSIX-style pathnames and vice versa.
    /// </summary>
    /// <remarks>
    /// It can be used when a Cygwin program needs to pass a file name to a 
    /// native Windows program, or expects to get a file name from a native 
    /// Windows program.
    /// </remarks>
    [FunctionSet("cygpath", "Unix/Cygwin")]
    public class CygpathFunctions : FunctionSetBase {
        #region Public Instance Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="CygpathFunctions" />
        /// class with the specified <see cref="Project" /> and properties.
        /// </summary>
        /// <param name="project">The <see cref="Project" /> in which the class is used.</param>
        /// <param name="properties">The set of properties to use for macro expansion.</param>
        public CygpathFunctions(Project project, PropertyDictionary properties) : base(project, properties) {
        }

        #endregion Public Instance Constructors

        #region Public Instance Methods

        /// <summary>
        /// Gets the DOS (short) form of the specified path.
        /// </summary>
        /// <param name="path">The path to convert.</param>
        /// <returns>
        /// The DOS (short) form of the specified path.
        /// </returns>
        /// <exception cref="Win32Exception"><c>cygpath</c> could not be started.</exception>
        /// <exception cref="ArgumentException"><paramref name="path" /> could not be converted to a short form.</exception>
        [Function("get-dos-path")]
        public string GetDosPath(string path) {
            return RunCygpathString(new Argument[] {
                new Argument("--dos \"" + path + "\"") });
        }

        /// <summary>
        /// Gets the Unix form of the specified path.
        /// </summary>
        /// <param name="path">The path to convert.</param>
        /// <returns>
        /// The Unix form of the specified path.
        /// </returns>
        /// <exception cref="Win32Exception"><c>cygpath</c> could not be started.</exception>
        /// <exception cref="ArgumentException"><paramref name="path" /> could not be converted to a Unix form.</exception>
        [Function("get-unix-path")]
        public string GetUnixPath(string path) {
            return RunCygpathString(new Argument[] {
                new Argument("--unix \"" + path + "\"") });
        }

        /// <summary>
        /// Gets the Windows form of the specified path.
        /// </summary>
        /// <param name="path">The path to convert.</param>
        /// <returns>
        /// The Windows form of the specified path.
        /// </returns>
        /// <exception cref="Win32Exception"><c>cygpath</c> could not be started.</exception>
        /// <exception cref="ArgumentException"><paramref name="path" /> could not be converted to a Windows form.</exception>
        [Function("get-windows-path")]
        public string GetWindowsPath(string path) {
            return RunCygpathString(new Argument[] {
                new Argument("--windows \"" + path + "\"") });
        }

        #endregion Public Instance Methods

        #region Private Instance Methods
        
        /// <summary>
        /// Runs cygpath with the specified arguments and returns the result 
        /// as a <see cref="string" />.
        /// </summary>
        /// <param name="args">The arguments to pass to cygpath.</param>
        /// <returns>
        /// The result of running cygpath with the specified arguments.
        /// </returns>
        private string RunCygpathString(Argument[] args) {
            MemoryStream ms = new MemoryStream();

            ExecTask execTask = GetTask(ms);
            execTask.Arguments.AddRange(args);

            try {
                execTask.Execute();
                ms.Position = 0;
                StreamReader sr = new StreamReader(ms);
                string output = sr.ReadLine();
                sr.Close();
                return output;
            } catch (Exception ex) {
                ms.Position = 0;
                StreamReader sr = new StreamReader(ms);
                string output = sr.ReadToEnd();
                sr.Close();

                if (output.Length != 0) {
                    throw new BuildException(output, ex);
                } else {
                    throw;
                }
            }
        }
        
        /// <summary>
        /// Factory method to return a new instance of ExecTask
        /// </summary>
        /// <param name="stream"></param>
        /// <returns></returns>
        private ExecTask GetTask(Stream stream) {
            ExecTask execTask = new ExecTask();
            execTask.Parent = Project;
            execTask.Project = Project;
            execTask.FileName = "cygpath";
            execTask.Threshold = Level.None;
            execTask.ErrorWriter = execTask.OutputWriter = new StreamWriter(stream);
            return execTask;
        }

        #endregion Private Instance Methods
    }
}
