// NAnt - A .NET build tool
// Copyright (C) 2013 Gerry Shaw
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
// Dmitry Kostenko (codeforsmile@gmail.com)

using System;
using System.Collections.Generic;
using System.Collections;
using System.Threading;

namespace NAnt.Core {

    /// <summary>
    /// Respresents a single node in the execution graph. A node typically corresponds to a target in the project.
    /// </summary>
    internal class ExecutionNode {

        /// <summary>
        /// Name of the node.
        /// </summary>
        private string _name;

        /// <summary>
        /// Total number of prerequisistes of the node.
        /// </summary>
        private int _prerequisitesCount;

        /// <summary>
        /// Number of prerequisites resolved in the current run.
        /// </summary>
        private int _prerequisitesResolved;

        /// <summary>
        /// The list of dependant nodes.
        /// </summary>
        private List<ExecutionNode> _dependantNodes = new List<ExecutionNode>();

        /// <summary>
        /// Name of the node.
        /// </summary>
        public string Name {
            get {
                return _name;
            }
        }

        /// <summary>
        /// The list of dependant nodes.
        /// </summary>
        public IEnumerable<ExecutionNode> DependantNodes {
            get {
                return _dependantNodes;
            }
        }

        /// <summary>
        /// Total number of prerequisistes of the node.
        /// </summary>
        public int PrerequisitesCount {
            get {
                return _prerequisitesCount;
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="NAnt.Core.ExecutionNode"/> class.
        /// </summary>
        /// <param name='name'>
        /// Name of the node.
        /// </param>
        public ExecutionNode(string name) {
            _name = name;
        }

        /// <summary>
        /// Registers that the node has a prerequisite.
        /// </summary>
        public void AddPrerequisite() {
            Interlocked.Increment(ref _prerequisitesCount);
        }

        /// <summary>
        /// Registers that the node's prerequisite has been resolved.
        /// </summary>
        /// <returns>
        /// <c>true</c> if all registered prerequisites are known to be resolved, <c>false</c> otherwise.
        /// </returns>
        public bool ResolvePrerequisite() {
            return Interlocked.Increment(ref _prerequisitesResolved) == _prerequisitesCount;
        }

        /// <summary>
        /// Prepares the node for execution run, resetting its state to initial "before run" state.
        /// </summary>
        public void PrepareForRun() {
            _prerequisitesResolved = 0;
        }

        /// <summary>
        /// Registers a dependant node for the current node. Implicitly registers current node as prerequisite for the dependant node.
        /// </summary>
        /// <param name='node'>Reference to the dependant node to connect with the current node.</param>
        public void RegisterDependantNode(ExecutionNode node) {
            _dependantNodes.Add(node);
            node.AddPrerequisite();
        }
    }
}
