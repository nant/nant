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
// Ian Maclean (ian_maclean@another.com)
// Jaroslaw Kowalski (jkowalski@users.sourceforge.net)

using System;
using System.IO;
using System.Collections;
using System.Reflection;
using System.Globalization;

using NAnt.Core;
using NAnt.Core.Attributes;
using NAnt.Core.Tasks;
using NAnt.Core.Types;

namespace NAnt.Core.Functions {
    [FunctionSet("pkg-config", "Unix/Cygwin")]
    public class PkgConfigFunctions : FunctionSetBase {

        #region Public Instance Constructors

        public PkgConfigFunctions(Project project, PropertyDictionary propDict ) : base(project, propDict) {}

        #endregion Public Instance Constructors

        #region Public Instance Methods

        /// <summary>
        /// Gets the value of a variable for the specified module.
        /// </summary>
        /// <param name="module">The module for which the variable should be retrieved.</param>
        /// <param name="name">The name of the variable.</param>
        /// <returns>
        /// The value of variable <paramref name="name" /> for the specified 
        /// module.
        /// </returns>
        [Function("get-variable")]
        public string GetVariable(string module, string name) {
            MemoryStream ms = new MemoryStream();

            ExecTask execTask = GetTask(ms);
            execTask.Arguments.Add(new Argument("--variable=\"" + name + "\""));
            execTask.Arguments.Add(new Argument(module));

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
        /// Determines the version of the given module.
        /// </summary>
        /// <param name="module">The module to get the version of.</param>
        /// <returns>
        /// The version of the given module.
        /// </returns>
        [Function("get-mod-version")]
        public string GetModVersion(string module) {
            MemoryStream ms = new MemoryStream();

            ExecTask execTask = GetTask(ms);
            execTask.Arguments.Add(new Argument("--modversion"));
            execTask.Arguments.Add(new Argument(module));

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
        /// Determines whether the given module is at least version 
        /// <paramref name="version" />.
        /// </summary>
        /// <param name="module">The module to check.</param>
        /// <param name="version">The version the module should at least have.</param>
        /// <returns>
        /// <see langword="true" /> if the given module is at least version
        /// <paramref name="version" />; otherwise, <see langword="false" />.
        /// </returns>
        [Function("is-atleast-version")]
        public bool IsAtLeastVersion(string module, string version) {
            MemoryStream ms = new MemoryStream();

            ExecTask execTask = GetTask(ms);
            execTask.Arguments.Add(new Argument("--atleast-version=\"" + version + "\""));
            execTask.Arguments.Add(new Argument(module));

            try {
                execTask.Execute();
                return true;
            } catch (Exception) {
                if (execTask.ExitCode == -1) {
                    // process could not be started
                    throw;
                }
                return false;
            }
        }

        /// <summary>
        /// Determines whether the given module is exactly version 
        /// <paramref name="version" />.
        /// </summary>
        /// <param name="module">The module to check.</param>
        /// <param name="version">The version the module should have.</param>
        /// <returns>
        /// <see langword="true" /> if the given module is exactly version
        /// <paramref name="version" />; otherwise, <see langword="false" />.
        /// </returns>
        [Function("is-exact-version")]
        public bool IsExactVersion(string module, string version) {
            MemoryStream ms = new MemoryStream();

            ExecTask execTask = GetTask(ms);
            execTask.Arguments.Add(new Argument("--exact-version=\"" + version + "\""));
            execTask.Arguments.Add(new Argument(module));

            try {
                execTask.Execute();
                return true;
            } catch (Exception) {
                if (execTask.ExitCode == -1) {
                    // process could not be started
                    throw;
                }
                return false;
            }
        }

        /// <summary>
        /// Determines whether the given module is at no newer than version
        /// <paramref name="version" />.
        /// </summary>
        /// <param name="module">The module to check.</param>
        /// <param name="version">The version the module should maximum have.</param>
        /// <returns>
        /// <see langword="true" /> if the given module is at no newer than 
        /// version <paramref name="version" />; otherwise, <see langword="false" />.
        /// </returns>
        [Function("is-max-version")]
        public bool IsMaxVersion(string module, string version) {
            MemoryStream ms = new MemoryStream();

            ExecTask execTask = GetTask(ms);
            execTask.Arguments.Add(new Argument("--max-version=\"" + version + "\""));
            execTask.Arguments.Add(new Argument(module));

            try {
                execTask.Execute();
                return true;
            } catch (Exception) {
                if (execTask.ExitCode == -1) {
                    // process could not be started
                    throw;
                }
                return false;
            }
        }

        /// <summary>
        /// Determines whether the given module is between two versions.
        /// </summary>
        /// <param name="module">The module to check.</param>
        /// <param name="minVersion">The version the module should at least have.</param>
        /// <param name="maxVersion">The version the module should maximum have.</param>
        /// <returns>
        /// <see langword="true" /> if the given module is between <paramref name="minVersion" />
        /// and <paramref name="maxVersion" />; otherwise, <see langword="false" />.
        /// </returns>
        [Function("is-between-version")]
        public bool IsBetweenVersion(string module, string minVersion, string maxVersion) {
            MemoryStream ms = new MemoryStream();

            ExecTask execTask = GetTask(ms);
            execTask.Arguments.Add(new Argument("--atleast-version=\"" + minVersion + "\""));
            execTask.Arguments.Add(new Argument("--max-version=\"" + maxVersion + "\""));
            execTask.Arguments.Add(new Argument(module));

            try {
                execTask.Execute();
                return true;
            } catch (Exception) {
                if (execTask.ExitCode == -1) {
                    // process could not be started
                    throw;
                }
                return false;
            }
        }

        /// <summary>
        /// Determines whether the given module exists.
        /// </summary>
        /// <param name="package">The module to check.</param>
        /// <returns>
        /// <see langword="true" /> if the module exists; otherwise, 
        /// <see langword="false" />.
        /// </returns>
        [Function("exists")]
        public bool Exists(string package) {
            MemoryStream ms = new MemoryStream();

            ExecTask execTask = GetTask(ms);
            execTask.Arguments.Add(new Argument("--exists"));
            execTask.Arguments.Add(new Argument(package));

            try {
                execTask.Execute();
                return true;
            } catch (Exception) {
                if (execTask.ExitCode == -1) {
                    // process could not be started
                    throw;
                }
                return false;
            }
        }

        #endregion Public Instance Methods

        #region Private Instance Methods

        private ExecTask GetTask(Stream stream) {
            ExecTask execTask = new ExecTask();
            execTask.Parent = Project;
            execTask.Project = Project;
            execTask.FileName = "pkg-config";
            execTask.Threshold = Level.None;
            execTask.ErrorWriter = execTask.OutputWriter = new StreamWriter(stream);
            return execTask;
        }

        #endregion Private Instance Methods
    }
}
