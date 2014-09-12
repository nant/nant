// NAnt - A .NET build tool
// Copyright (C) 2001-2004 Gert Driesen
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
// Gert Driesen (drieseng@users.sourceforge.net)
// Giuseppe Greco (giuseppe.greco@agamura.com)

using System;
using System.Globalization;
using System.ServiceProcess;

using NAnt.Core;
using NAnt.Core.Attributes;
using NAnt.Core.Util;

namespace NAnt.MSNet.Tasks {
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
    /// </example>
    /// <example>
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
            /// Restarts a service.
            /// </summary>
            Restart,

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
        /// The action that should be performed on the service.
        /// </summary>
        [TaskAttribute("action", Required=true)]
        public ActionType Action {
            get { return _action; }
            set { _action = value; }
        }

        /// <summary>
        /// The time, in milliseconds, the task will wait for the service to
        /// reach the desired status. The default is 5000 milliseconds.
        /// </summary>
        [TaskAttribute("timeout", Required=false)]
        public double Timeout {
            get { return _timeout; }
            set { _timeout = value; }
        }

        #endregion Public Instance Properties

        #region Override implementation of Task

        /// <summary>
        /// Peforms actions on the service in order to reach the desired status.
        /// </summary>
        protected override void ExecuteTask() {
            // get handle to service
            using (ServiceController serviceController = new ServiceController(ServiceName, MachineName)) {
                // determine desired status
                ServiceControllerStatus desiredStatus = DetermineDesiredStatus();

                try {
                    // determine current status, this is also verifies if the service 
                    // is available
                    ServiceControllerStatus currentStatus = serviceController.Status;
                } catch (Exception ex) {
                    throw new BuildException(ex.Message, Location, ex.InnerException);
                }

                // we only need to take action if the service status differs from 
                // the desired status or if the service should be restarted
                if (serviceController.Status != desiredStatus || Action == ActionType.Restart) {
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
                        case ActionType.Restart:
                            RestartService(serviceController);
                            break;
                    }

                    // refresh current service status
                    serviceController.Refresh();
                }
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

                // wait until service is running or timeout expired
                serviceController.WaitForStatus(ServiceControllerStatus.Running, 
                    TimeSpan.FromMilliseconds(Timeout));
            } catch (Exception ex) {
                throw new BuildException(string.Format(CultureInfo.InvariantCulture, 
                    ResourceUtils.GetString("NA3007"), ServiceName, MachineName), Location, ex);
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
                        ResourceUtils.GetString("NA3008"), ServiceName, MachineName), Location);
                }

                // wait until service is stopped or timeout expired
                serviceController.WaitForStatus(ServiceControllerStatus.Stopped, 
                    TimeSpan.FromMilliseconds(Timeout));
            } catch (BuildException ex) {
                // rethrow exception
                throw ex;
            } catch (Exception ex) {
                throw new BuildException(string.Format(CultureInfo.InvariantCulture, 
                    ResourceUtils.GetString("NA3008"), ServiceName, MachineName), Location, ex);
            }
        }

        /// <summary>
        /// Restarts the service identified by <see cref="ServiceName" /> and
        /// <see cref="MachineName" />.
        /// </summary>
        /// <param name="serviceController"><see cref="ServiceController" /> instance for controlling the service identified by <see cref="ServiceName" /> and <see cref="MachineName" />.</param>
        private void RestartService(ServiceController serviceController) {
            // only stop service if its not already stopped
            if (serviceController.Status != ServiceControllerStatus.Stopped) {
                StopService(serviceController);
            }

            // start the service
            StartService(serviceController);
        }

        /// <summary>
        /// Pauses the service identified by <see cref="ServiceName" /> and
        /// <see cref="MachineName" />.
        /// </summary>
        /// <param name="serviceController"><see cref="ServiceController" /> instance for controlling the service identified by <see cref="ServiceName" /> and <see cref="MachineName" />.</param>
        private void PauseService(ServiceController serviceController) {
            try {
                if (serviceController.Status == ServiceControllerStatus.Running) {
                    if (serviceController.CanPauseAndContinue) {
                        if (serviceController.Status != ServiceControllerStatus.Running) {
                            throw new BuildException(string.Format(CultureInfo.InvariantCulture,
                                ResourceUtils.GetString("NA3010"), ServiceName, MachineName),
                                Location);
                        } else {
                            serviceController.Pause();
                        }
                    } else {
                        throw new BuildException(string.Format(CultureInfo.InvariantCulture, 
                            ResourceUtils.GetString("NA3011"), ServiceName, MachineName),
                            Location);
                    }
                } else {
                    throw new BuildException(string.Format(CultureInfo.InvariantCulture, 
                        ResourceUtils.GetString("NA3010"), ServiceName, MachineName),
                        Location);
                }

                // wait until service is paused or timeout expired
                serviceController.WaitForStatus(ServiceControllerStatus.Paused, 
                    TimeSpan.FromMilliseconds(Timeout));
            } catch (BuildException ex) {
                // rethrow exception
                throw ex;
            } catch (Exception ex) {
                throw new BuildException(string.Format(CultureInfo.InvariantCulture, 
                    ResourceUtils.GetString("NA3009"), ServiceName, MachineName),
                    Location, ex);
            }
        }

        /// <summary>
        /// Continues the service identified by <see cref="ServiceName" /> and
        /// <see cref="MachineName" />.
        /// </summary>
        /// <param name="serviceController"><see cref="ServiceController" /> instance for controlling the service identified by <see cref="ServiceName" /> and <see cref="MachineName" />.</param>
        private void ContinueService(ServiceController serviceController) {
            try {
                if (serviceController.Status == ServiceControllerStatus.Paused) {
                    if (serviceController.CanPauseAndContinue) {
                        if (serviceController.Status == ServiceControllerStatus.Paused) {
                            serviceController.Continue();
                        } else if (serviceController.Status != ServiceControllerStatus.Running) {
                            throw new BuildException(string.Format(CultureInfo.InvariantCulture, 
                                ResourceUtils.GetString("NA3013"), ServiceName, MachineName), 
                                Location);
                        } else {
                            // do nothing as service is already running
                        }
                    } else {
                        throw new BuildException(string.Format(CultureInfo.InvariantCulture, 
                            ResourceUtils.GetString("NA3014"), ServiceName, MachineName), 
                            Location);
                    }
                } else {
                    throw new BuildException(string.Format(CultureInfo.InvariantCulture, 
                        ResourceUtils.GetString("NA3015"), ServiceName, MachineName), 
                        Location);
                }

                // wait until service is running or timeout expired
                serviceController.WaitForStatus(ServiceControllerStatus.Running, 
                    TimeSpan.FromMilliseconds(Timeout));
            } catch (BuildException ex) {
                // rethrow exception
                throw ex;
            } catch (Exception ex) {
                throw new BuildException(string.Format(CultureInfo.InvariantCulture, 
                    ResourceUtils.GetString("NA3012"), ServiceName, MachineName),
                    Location, ex);
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

        /// <summary>
        /// Holds the time, in milliseconds, the task will wait for a service
        /// to reach the desired status.
        /// </summary>
        private double _timeout = 5000;

        #endregion Private Instance Fields
    }
}
