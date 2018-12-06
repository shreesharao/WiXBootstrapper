using Microsoft.Tools.WindowsInstallerXml.Bootstrapper;
using System;
using System.Windows;
using System.Windows.Interop;


namespace BootstrapperUI.Models
{
    public class BootstrapperApplicationModel
    {
        private IntPtr _hwnd;
        public CustomBootstrapperApplication CustomBootstrapperApplication { get; private set; }

        //this property will store the exit status code that the Burn engine returns after the bootstrapper has finished
        //This status code will be passed to the Engine.Quit(model.FinalResult) method 
        //at the end of the Run method in our CustomBootstrapperApplication class
        public int FinalResult { get; set; }

        public BootstrapperApplicationModel(CustomBootstrapperApplication customBootstrapperApplication)
        {
            this.CustomBootstrapperApplication = customBootstrapperApplication;
            this._hwnd = IntPtr.Zero;
        }

        /// <summary>
        /// Sets the handle for a Windows Presentation Foundation (WPF) window.
        /// This handle is used by the Burn engine when performing the install or uninstall.
        /// </summary>
        /// <param name="view"></param>
        public void SetWindowHandle(Window view)
        {
            this._hwnd = new WindowInteropHelper(view).Handle;
        }

        /// <summary>
        ///  This method is given a task to prepare for, such as installation, uninstallation, repair, or modify
        /// </summary>
        /// <param name="action"></param>
        public void PlanAction(LaunchAction action)
        {
            this.CustomBootstrapperApplication.Engine.Plan(action);
        }

        /// <summary>
        /// This methos will execute the task prepared in PlanAction method
        /// </summary>
        public void ApplyAction()
        {
            this.CustomBootstrapperApplication.Engine.Apply(this._hwnd);
        }

        /// <summary>
        ///  This method will append messages to the bootstrapper's log
        /// </summary>
        /// <param name="message"></param>
        public void LogMessage(string message, LogLevel logLevel = LogLevel.Standard)
        {
            var identifier = "Custom:";
            var finalMessage = identifier + message;
            this.CustomBootstrapperApplication.Engine.Log(logLevel, finalMessage);
        }

        /// <summary>
        /// Sets the Burn engine variable
        /// </summary>
        /// <param name="name"></param>
        /// <param name="value"></param>
        public void SetBurnVariables(string name, string value)
        {
            if (!string.IsNullOrEmpty(name) && !string.IsNullOrEmpty(value))
            {
                LogMessage(string.Format("Setting value '{0}' for variable '{1}'", value, name));
                this.CustomBootstrapperApplication.Engine.StringVariables[name] = value;
            }
            else
            {
                LogMessage(string.Format("Failed to set value for variable {0}", name), LogLevel.Error);
            }


        }
    }
}