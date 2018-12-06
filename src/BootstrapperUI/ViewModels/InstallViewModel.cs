using BootstrapperUI.Models;
using Microsoft.Tools.WindowsInstallerXml.Bootstrapper;
using Prism.Commands;
using Prism.Mvvm;
using System;
using System.IO;
using System.Windows.Input;


namespace BootstrapperUI.ViewModels
{
    public class InstallViewModel : BindableBase
    {

        #region Properties

        private int _cacheProgress;
        private int _executeProgress;
        private readonly BootstrapperApplicationModel _model;
        private const string Present = "Present";

        #region Install State
        private InstallState _state;

        public InstallState State
        {
            get
            {
                return this._state;
            }
            set
            {
                if (this._state != value)
                {
                    this._state = value;
                    this.Message = this._state.ToString();
                    this.RaisePropertyChanged("State");
                    this.Refresh();
                }
            }
        }
        #endregion

        #region Information Message
        private string _message = "Select The Components To Install";

        public string Message
        {
            get
            {
                return this._message;
            }
            set
            {
                if (this._message != value)
                {
                    this._message = value;
                    this.RaisePropertyChanged("Message");
                }
            }
        }
        #endregion


        #region InstallVisible

        private bool _installVisible;

        public bool InstallVisible
        {
            get { return _installVisible; }
            set
            {
                _installVisible = value;
                this.RaisePropertyChanged("InstallVisible");
            }
        }
        #endregion

        #region UnInstallVisible

        private bool _uninstallVisible;

        public bool UninstallVisible
        {
            get { return _uninstallVisible; }
            set
            {
                _uninstallVisible = value;
                this.RaisePropertyChanged("UninstallVisible");
            }
        }

        #endregion

        #region FinishVisible

        private bool _finishVisible;

        public bool FinishVisible
        {
            get { return _finishVisible; }
            set
            {
                _finishVisible = value;
                this.RaisePropertyChanged("FinishVisible");
            }
        }
        #endregion

        #region EDMRStartUp
        private bool _startUpEnabled;

        public bool StartUpEnabled
        {
            get
            {
                return _startUpEnabled;
            }
            set
            {
                _startUpEnabled = value;
                this.RaisePropertyChanged("StartUpEnabled");
                this._model.SetBurnVariables("StartUpEnabled", value.ToString());
            }
        }
        #endregion


        #region Progressbar
        private int _progress;

        public int Progress
        {
            get { return _progress; }
            set
            {
                _progress = value;
                this.RaisePropertyChanged("Progress");
            }
        }
        #endregion

        #region Button bound commands

        public ICommand InstallCommand { get; private set; }

        public ICommand UninstallCommand { get; private set; }

        public ICommand CancelCommand { get; private set; }

        public ICommand FinishCommand { get; private set; }

        #endregion

        #region Launch Action
        public LaunchAction Action
        {
            get;
            set;
        }



        #endregion

        #region Display Level
        public Display DisplayLevel
        {
            get;
            set;
        }

        #endregion



        #endregion


        #region Constructor

        public InstallViewModel(BootstrapperApplicationModel model)
        {
            this._model = model;
            this.State = InstallState.Initializing;


            this.WireUpEventHandlers();

            this.InstallCommand = new DelegateCommand(() =>
            {
                this._model.PlanAction(LaunchAction.Install);
                this.Action = LaunchAction.Install;
            });

            this.UninstallCommand = new DelegateCommand(() =>
            {
                this._model.PlanAction(LaunchAction.Uninstall);
                this.Action = LaunchAction.Uninstall;
            });

            this.CancelCommand = new DelegateCommand(() =>
            {
                this._model.LogMessage("Cancelling...");

                if (this.State == InstallState.Applying)
                {
                    this.State = InstallState.Cancelled;
                }
                else
                {
                    CustomBootstrapperApplication.Dispatcher.InvokeShutdown();
                }
            }, () => this.State != InstallState.Cancelled);

            this.FinishCommand = new DelegateCommand(() =>
            {
                CustomBootstrapperApplication.Dispatcher.InvokeShutdown();
            });
        }

        #endregion


        #region Protected methods

        private void DetectBegin(object sender, DetectBeginEventArgs e)
        {
            this._model.LogMessage("DetectBegin event fired.");
        }

        private void DetectPackageBegin(object sender, DetectPackageBeginEventArgs e)
        {
            this._model.LogMessage("DetectPackageBegin event fired.");
        }

        protected void DetectPackageComplete(object sender, DetectPackageCompleteEventArgs e)
        {

            this._model.LogMessage("DetectPackageComplete event fired.");
            this._model.LogMessage(e.PackageId + " " + e.State + " " + e.Status);
            SetPackageStatus(e.PackageId, e.State.ToString());

        }


        protected void DetectComplete(object sender, DetectCompleteEventArgs e)
        {
            this._model.LogMessage("DetectComplete event fired.");

            this.InstallVisible = true;
            this.UninstallVisible = true;
            this.FinishVisible = false;

            var commandLineArgs = this.ParseCommandLineArgs();

            if (commandLineArgs != null)
            {
                //add the code here to handle any command line arguments
            }

            //When the /s or /silent switch is passed to the bootstrapper command prompt we do not want to display any UI
            if (this.DisplayLevel == Display.None)
            {
                if (this.Action == LaunchAction.Install)
                {
                    EnableAllInstallers();
                    this._model.PlanAction(LaunchAction.Install);
                }

                else if (this.Action == LaunchAction.Uninstall)
                {
                    this._model.PlanAction(LaunchAction.Uninstall);
                }

            }

        }

        protected void PlanBegin(object sender, PlanBeginEventArgs e)
        {
            _model.LogMessage("PlanBegin event fired.");
            this._model.LogMessage("Display:" + this.DisplayLevel.ToString());
            this._model.LogMessage("Action:" + this.Action.ToString());
        }

        protected void PlanComplete(object sender, PlanCompleteEventArgs e)
        {
            _model.LogMessage("PlanComplete event fired.");
            if (this.State == InstallState.Cancelled)
            {
                CustomBootstrapperApplication.Dispatcher.InvokeShutdown();
                return;
            }
            this._model.ApplyAction();
        }

        protected void ApplyBegin(object sender, ApplyBeginEventArgs e)
        {
            _model.LogMessage("ApplyBegin event fired.");
            this.State = InstallState.Applying;
            this.Message = "Fetching Selected Packages";
        }

        protected void ExecutePackageBegin(object sender, ExecutePackageBeginEventArgs e)
        {
            _model.LogMessage("ExecutePackageBegin event fired.");
            if (this.State == InstallState.Cancelled)
            {
                e.Result = Result.Cancel;
            }
            this.Message = string.Format("{0} {1}", this.Action == LaunchAction.Install ? "Installing" : "Uninstalling", e.PackageId);
        }

        protected void ExecutePackageComplete(object sender, ExecutePackageCompleteEventArgs e)
        {
            _model.LogMessage("ExecutePackageComplete event fired.");
            if (this.State == InstallState.Cancelled)
            {
                e.Result = Result.Cancel;
            }
        }

        protected void ApplyComplete(object sender, ApplyCompleteEventArgs e)
        {
            _model.LogMessage("ApplyComplete event fired.");

            if (this.Action == LaunchAction.Install)
            {
                this.Message = "Installation Completed Successfully";
            }

            else if (this.Action == LaunchAction.Uninstall)
            {
                this.Message = "Uninstallation Completed Successfully";
            }

            _model.LogMessage(string.Format("FinalResult : {0}", e.Result));
            this._model.FinalResult = e.Status;

            this.FinishVisible = true;
            this.InstallVisible = false;
            this.UninstallVisible = false;

            if (this.DisplayLevel == Display.None)
            {
                CustomBootstrapperApplication.Dispatcher.InvokeShutdown();
            }
        }

        private void Error(object sender, Microsoft.Tools.WindowsInstallerXml.Bootstrapper.ErrorEventArgs e)
        {
            this._model.LogMessage("Error event has fired");
            this._model.LogMessage(string.Format("ErrorMessage:{0}", e.ErrorMessage));
            this._model.LogMessage(string.Format("ErrorType:{0}", e.ErrorType));
            this._model.LogMessage(string.Format("PackageId:{0}", e.PackageId));
        }

        #endregion


        #region Private methods

        private void Refresh()
        {
            CustomBootstrapperApplication.Dispatcher.Invoke((Action)(() =>
            {
                ((DelegateCommand)this.InstallCommand).RaiseCanExecuteChanged();
                ((DelegateCommand)this.UninstallCommand).RaiseCanExecuteChanged();
                ((DelegateCommand)this.CancelCommand).RaiseCanExecuteChanged();
            }));
        }

        /// <summary>
        /// Attaches events with their respective event handlers
        /// </summary>
        private void WireUpEventHandlers()
        {
            this._model.CustomBootstrapperApplication.DetectBegin += this.DetectBegin;
            this._model.CustomBootstrapperApplication.DetectPackageBegin += this.DetectPackageBegin;
            this._model.CustomBootstrapperApplication.DetectPackageComplete += this.DetectPackageComplete;
            this._model.CustomBootstrapperApplication.DetectComplete += this.DetectComplete;
            this._model.CustomBootstrapperApplication.PlanBegin += this.PlanBegin;
            this._model.CustomBootstrapperApplication.PlanComplete += this.PlanComplete;
            this._model.CustomBootstrapperApplication.ApplyComplete += this.ApplyComplete;
            this._model.CustomBootstrapperApplication.ApplyBegin += this.ApplyBegin;
            this._model.CustomBootstrapperApplication.ExecutePackageBegin += this.ExecutePackageBegin;
            this._model.CustomBootstrapperApplication.ExecutePackageComplete += this.ExecutePackageComplete;
            this._model.CustomBootstrapperApplication.Error += this.Error;

            //To show progreess bar we need to hook up CacheAcquireProgress and ExecuteProgress events
            //CacheAcquireProgress event will fire only during installation
            this._model.CustomBootstrapperApplication.CacheAcquireProgress += (sender, args) =>
            {
                this._cacheProgress = args.OverallPercentage;
                this.Progress = (this._cacheProgress + this._executeProgress) / 2;
            };

            //ExecuteProgress event will fire both during installation and uninstallation
            this._model.CustomBootstrapperApplication.ExecuteProgress += (sender, args) =>
            {
                this._executeProgress = args.OverallPercentage;

                if (this.Action == LaunchAction.Install)
                {
                    this.Progress = (this._cacheProgress + this._executeProgress) / 2;
                }
                else
                {
                    this.Progress = this._executeProgress;
                }
            };

            //To allow download to happen we must handle ResolveSource event

            this._model.CustomBootstrapperApplication.ResolveSource += (sender, args) =>
            {
                _model.LogMessage("ResolveSource event fired");
                _model.LogMessage(string.Format("DownloadSource:{0}", args.DownloadSource));
                if (!File.Exists(args.LocalSource) && !string.IsNullOrEmpty(args.DownloadSource))
                {
                    args.Result = Result.Download;
                }
                else
                {
                    // Not downloadable
                    args.Result = Result.Ok;
                }
            };
        }

        /// <summary>
        /// Parses the command line
        /// </summary>
        /// <returns></returns>
        private string[] ParseCommandLineArgs()
        {
            var args = this._model.CustomBootstrapperApplication.Command.GetCommandLineArgs();
            this._model.LogMessage(string.Format("Number of arguments passed {0}", args.Length));

            this.DisplayLevel = this._model.CustomBootstrapperApplication.Command.Display;
            this.Action = this._model.CustomBootstrapperApplication.Command.Action;
            return args;
        }

        /// <summary>
        /// Enables all the installers during silent installation
        /// </summary>
        private void EnableAllInstallers()
        {
            this.StartUpEnabled = true;

        }

        /// <summary>
        /// Sets the package status
        /// </summary>
        /// <param name="packageId"></param>
        private void SetPackageStatus(string packageId, string packageStatus)
        {
            switch (packageId)
            {
                case PackageConstants.StartUpPackage:
                    StartUpEnabled = packageStatus == Present;
                    break;
                default:
                    break;
            }
        }
        #endregion


    }
}
