// NAnt - A .NET build tool
// Copyright (C) 2001-2003 Gert Driesen
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
// Gert Driesen (gert.driesen@ardatis.com)

using System;
using System.ComponentModel;
using System.Globalization;
using System.ServiceProcess;

using NAnt.Core;
using NAnt.Core.Attributes;
using NAnt.Core.Util;

namespace NAnt.Win32.Tasks {
    /// <summary>
    /// Allows a Windows service to be controlled.
    /// </summary>
    /// <example>
    ///   <para>Starts the World Wide Web Publishing Service on the local computer.</para>
    ///   <code>
    ///     <![CDATA[
    /// <servicecontroller action="Start" service="w3svc" />
    ///     ]]>
    ///   </code>
    ///   <para>Stops the Alerter service on computer 'MOTHER'.</para>
    ///   <code>
    ///     <![CDATA[
    /// <servicecontroller action="Stop" service="Alerter" machine="MOTHER" />
    ///     ]]>
    ///   </code>
    /// </example>
    [TaskName("servicecontroller")]
    public class ServiceControllerTask : Task {
        /// <summary>
        /// Defines the actions that can be performed on a service.
        /// </summary>
        public enum ActionType {
            /// <summary>
            /// Starts a service.
            /// </summary>
            Start,

            /// <summary>
            /// Stops a service.
            /// </summary>
            Stop,

            /// <summary>
            /// Pauses a running service.
            /// </summary>
            Pause,

            /// <summary>
            /// Continues a paused service.
            /// </summary>
            Continue
        }

        #region Public Instance Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="ServiceControllerTask" />
        /// class.
        /// </summary>
        public ServiceControllerTask() {
        }

        #endregion Public Instance Constructors

        #region Public Instance Properties

        /// <summary>
        /// The name of the service that should be controlled.
        /// </summary>
        [TaskAttribute("service", Required=true)]
        [StringValidator(AllowEmpty=false)]
        public string ServiceName {
            get { return _serviceName; }
            set { _serviceName = StringUtils.ConvertEmptyToNull(value); }
        }

        /// <summary>
        /// The name of the computer on which the service resides. The default
        /// is the local computer.
        /// </summary>
        [TaskAttribute("machine")]
        public string MachineName {
            get { return (_machineName == null) ? "." : _machineName; }
            set { _machineName = StringUtils.ConvertEmptyToNull(value); }
        }

        /// <summary>
        /// The action that should be performed on the service - either 
        /// <see cref="ActionType.Start" />, <see cref="ActionType.Stop" />,
        /// <see cref="ActionType.Pause" /> or <see cref="ActionType.Continue" />.
        /// </summary>
        [TaskAttribute("action", Required=true)]
        public ActionType Action {
            get { return _action; }
            set { _action = value; }
        }

        #endregion Public Instance Properties

        #region Override implementation of Task

        /// <summary>
        /// Peforms actions on the service in order to reach the desired status.
        /// </summary>
        protected override void ExecuteTask() {
            // get handle to service
            ServiceController serviceController = new ServiceController(ServiceName, MachineName);

            // determine desired status
            ServiceControllerStatus desiredStatus = DetermineDesiredStatus();

            try {
                // determine current status, this is also verifies if the service 
                // is available
                ServiceControllerStatus currentStatus = serviceController.Status;
            } catch (Exception ex) {
                throw new BuildException(ex.Message, Location, ex.InnerException);
            }

            // check if the service status differs from the desired status
            if (serviceController.Status != desiredStatus) {
                // perform action on service to reach desired status
                switch (Action) {
                    case ActionType.Start:
                        StartService(serviceController);
                        break;
                    case ActionType.Pause:
                        PauseService(serviceController);
                        break;
                    case ActionType.Continue:
                        ContinueService(serviceController);
                        break;
                    case ActionType.Stop:
                        StopService(serviceController);
                        break;
                }

                // refresh current service status
                serviceController.Refresh();
            }
        }

        #endregion Override implementation of Task

        #region Private Instance Methods 

        /// <summary>
        /// Determines the desired status of the service based on the action
        /// that should be performed on it.
        /// </summary>
        /// <returns>
        /// The <see cref="ServiceControllerStatus" /> that should be reached
        /// in order for the <see cref="Action" /> to be considered successful.
        /// </returns>
        private ServiceControllerStatus DetermineDesiredStatus() {
            switch (Action) {
                case ActionType.Stop:
                    return ServiceControllerStatus.Stopped;
                case ActionType.Pause:
                    return ServiceControllerStatus.Paused;
                default:
                    return ServiceControllerStatus.Running;
            }
        }

        /// <summary>
        /// Starts the service identified by <see cref="ServiceName" /> and
        /// <see cref="MachineName" />.
        /// </summary>
        /// <param name="serviceController"><see cref="ServiceController" /> instance for controlling the service identified by <see cref="ServiceName" /> and <see cref="MachineName" />.</param>
        private void StartService(ServiceController serviceController) {
            try {
                if (serviceController.Status == ServiceControllerStatus.Paused) {
                    serviceController.Continue();
                } else {
                    serviceController.Start();
                }
            } catch (Exception ex) {
                throw new BuildException(string.Format(CultureInfo.InvariantCulture, 
                    "Cannot start service {0} on computer '{1}'.", ServiceName,
                    MachineName), Location, ex);
            }
        }

        /// <summary>
        /// Stops the service identified by <see cref="ServiceName" /> and
        /// <see cref="MachineName" />.
        /// </summary>
        /// <param name="serviceController"><see cref="ServiceController" /> instance for controlling the service identified by <see cref="ServiceName" /> and <see cref="MachineName" />.</param>
        private void StopService(ServiceController serviceController) {
            try {
                if (serviceController.CanStop) {
                    serviceController.Stop();
                } else {
                    throw new BuildException(string.Format(CultureInfo.InvariantCulture, 
                        "Cannot stop service {0} on computer '{1}'.", ServiceName, 
                        MachineName), Location);
                }
            } catch (BuildException ex) {
                // rethrow exception
                throw ex;
            } catch (Exception ex) {
                throw new BuildException(string.Format(CultureInfo.InvariantCulture, 
                    "Cannot stop service {0} on computer '{1}.", ServiceName, 
                    MachineName), Location, ex);
            }
        }

        /// <summary>
        /// Pauses the service identified by <see cref="ServiceName" /> and
        /// <see cref="MachineName" />.
        /// </summary>
        /// <param name="serviceController"><see cref="ServiceController" /> instance for controlling the service identified by <see cref="ServiceName" /> and <see cref="MachineName" />.</param>
        private void PauseService(ServiceController serviceController) {
            try {
                if (serviceController.CanPauseAndContinue) {
                    if (serviceController.Status != ServiceControllerStatus.Running) {
                        throw new BuildException(string.Format(CultureInfo.InvariantCulture, 
                            "Cannot pause service {0} on computer '{1}' as its" +
                            " not currently started.", ServiceName, MachineName), 
                            Location);
                    } else {
                        serviceController.Pause();
                    }
                } else {
                    throw new BuildException(string.Format(CultureInfo.InvariantCulture, 
                        "Cannot pause service {0} on computer '{1}' as it does not" +
                        " support the pause and continue mechanism.", ServiceName,
                        MachineName), Location);
                }
            } catch (BuildException ex) {
                // rethrow exception
                throw ex;
            } catch (Exception ex) {
                throw new BuildException(string.Format(CultureInfo.InvariantCulture, 
                    "Cannot pause service {0} on computer '{1}'.", ServiceName,
                    MachineName), Location, ex);
            }
        }

        /// <summary>
        /// Continues the service identified by <see cref="ServiceName" /> and
        /// <see cref="MachineName" />.
        /// </summary>
        /// <param name="serviceController"><see cref="ServiceController" /> instance for controlling the service identified by <see cref="ServiceName" /> and <see cref="MachineName" />.</param>
        private void ContinueService(ServiceController serviceController) {
            try {
                if (serviceController.CanPauseAndContinue) {
                    if (serviceController.Status == ServiceControllerStatus.Paused) {
                        serviceController.Continue();
                    } else if (serviceController.Status != ServiceControllerStatus.Running) {
                        throw new BuildException(string.Format(CultureInfo.InvariantCulture, 
                            "Cannot continue service {0} on computer '{1} as its" +
                            " not currently started.", ServiceName, MachineName), 
                            Location);
                    } else {
                        // dot nothing as service is already running
                    }
                } else {
                    throw new BuildException(string.Format(CultureInfo.InvariantCulture, 
                        "Cannot continue service {0} on computer '{1} as it does" +
                        " not support the pause and continue mechanism.", ServiceName, 
                        MachineName), Location);
                }
            } catch (BuildException ex) {
                // rethrow exception
                throw ex;
            } catch (Exception ex) {
                throw new BuildException(string.Format(CultureInfo.InvariantCulture, 
                    "Cannot continue service {0} on computer '{1}.", ServiceName, 
                    MachineName), Location, ex);
            }
        }

        #endregion Private Instance Methods 

        #region Private Instance Fields

        /// <summary>
        /// Holds the name of the service that should be controlled.
        /// </summary>
        private string _serviceName;

        /// <summary>
        /// Holds the name of the computer on which the service resides.
        /// </summary>
        private string _machineName;

        /// <summary>
        /// Holds the action that should be performed on the service.
        /// </summary>
        private ActionType _action;

        #endregion Private Instance Fields
    }
}
