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
    /// Represents the graph of execution of the project targets.
    /// </summary>
    internal class ExecutionGraph : IDisposable {
        /// <summary>
        /// The dictionary of graph's nodes, by name.
        /// </summary>
        private Dictionary<string, ExecutionNode> _nodes = new Dictionary<string, ExecutionNode>();

        /// <summary>
        /// The list of "leaves", i.e. nodes without any prerequisites.
        /// </summary>
        private List<ExecutionNode> _leaves = new List<ExecutionNode>();

        /// <summary>
        /// The number of currently active nodes (tasks being scheduled).
        /// </summary>
        private int _activeNodes = 0;

        /// <summary>
        /// The collection of exceptions that occured during execution.
        /// </summary>
        private List<Exception> _exceptions = new List<Exception>();

        /// <summary>
        /// An event object that is raised when the run is finished (the last task has been executed, and there are no more tasks to be scheduled).
        /// </summary>
        private AutoResetEvent _finished = new AutoResetEvent(false);

        /// <summary>
        /// The action to execute for each node in the graph (e.g. execute the project's target).
        /// </summary>
        private Action<string> _visitor;


        /// <summary>
        /// Initializes a new instance of the <see cref="NAnt.Core.ExecutionGraph"/> class.
        /// </summary>
        public ExecutionGraph() {
            this._visitor = this.VisitNode;
        }

        /// <summary>
        /// Releases all resource used by the <see cref="NAnt.Core.ExecutionGraph"/> object.
        /// </summary>
        /// <remarks>
        /// Call <see cref="Dispose"/> when you are finished using the <see cref="NAnt.Core.ExecutionGraph"/>. The
        /// <see cref="Dispose"/> method leaves the <see cref="NAnt.Core.ExecutionGraph"/> in an unusable state. After
        /// calling <see cref="Dispose"/>, you must release all references to the <see cref="NAnt.Core.ExecutionGraph"/>
        /// so the garbage collector can reclaim the memory that the <see cref="NAnt.Core.ExecutionGraph"/> was occupying.
        /// </remarks>
        public void Dispose() {
            this._finished.Close();
        }

        /// <summary>
        /// Schedules a node for execution in the thread pool.
        /// </summary>
        /// <param name='node'>Node of the graph to put into execution.</param>
        private void ScheduleForExecution(ExecutionNode node) {
            Interlocked.Increment(ref _activeNodes);

            StartTask(delegate {
                try {
                    this.VisitNode(node.Name);

                    foreach (ExecutionNode dependantNode in node.DependantNodes) {
                        if (dependantNode.ResolvePrerequisite()) {
                            this.ScheduleForExecution(dependantNode);
                        }
                    }
                }
                catch (Exception e) {
                    lock (this._exceptions) {
                        this._exceptions.Add(e);
                    }
                }

                if (Interlocked.Decrement(ref _activeNodes) == 0) {
                    this._finished.Set();
                }
            });
        }

        /// <summary>
        /// Executes actions associated with visiting a node.
        /// </summary>
        /// <param name='nodeName'>
        /// Name of the node that is being visited.
        /// </param>
        private void VisitNode(string nodeName) {
            if (this._visitor == null) {
                throw new ArgumentNullException("visitor");
            }
    
            this._visitor(nodeName);
        }

        /// <summary>
        /// Starts a new asynchronous task for the specified action.
        /// </summary>
        /// <param name='action'>
        /// Action to be executed in the task.
        /// </param>
        private void StartTask(WaitCallback action) {
            ThreadPool.QueueUserWorkItem(action);
        }

        /// <summary>
        /// Gets the node with the given name. Creates a new node, if there is no node with the given name in the graph.
        /// </summary>
        /// <returns>
        /// An instance of <see cref="ExecutionNode"/> class.
        /// </returns>
        /// <param name='name'>
        /// Name of the node to return.
        /// </param>
        public ExecutionNode GetNode(string name) {
            ExecutionNode result;

            if (this._nodes.TryGetValue(name, out result)) {
                return result;
            }

            result = new ExecutionNode(name);
            this._nodes[name] = result;

            return result;
        }

        /// <summary>
        /// Registers a node as a leaf node (no prerequisites).
        /// </summary>
        /// <param name='node'>Node to be registered as a leaf.</param>
        public void RegisterLeafNode(ExecutionNode node) {
            if (!this._leaves.Contains(node)) {
                this._leaves.Add(node);
            }
        }

        /// <summary>
        /// Walks through the graph executing provided action for each node in the graph in dependency order.
        /// </summary>
        /// <param name='visitor'>An action to be executed for each node in the graph.</param>
        public void WalkThrough(Action<string> visitor) {
            this._visitor = visitor;
            Run();
        }

        /// <summary>
        /// Initiates execution of the tree and waits for completion.
        /// </summary>
        private void Run () {
			foreach (ExecutionNode node in this._nodes.Values) {
				node.PrepareForRun ();
			}

			if (this._leaves.Count == 0) {
				return;
			}

			Interlocked.Increment (ref _activeNodes);

			foreach (ExecutionNode node in this._leaves) {
				this.ScheduleForExecution (node);
			}

			if (Interlocked.Decrement (ref _activeNodes) > 0) {
				this._finished.WaitOne ();
			}

            if (this._exceptions.Count > 0) {
                Exception firstException = this._exceptions[0];
                Location location = Location.UnknownLocation;
                if (firstException is BuildException) {
                    location = (firstException as BuildException).Location;
                }
                throw new BuildException("At least one of the tasks has failed: " + firstException.Message, location, firstException);
            }
        }
    }   
}
