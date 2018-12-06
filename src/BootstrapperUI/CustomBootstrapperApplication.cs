using Microsoft.Tools.WindowsInstallerXml.Bootstrapper;
using System.Windows.Threading;

using BootstrapperUI.Models;
using BootstrapperUI.ViewModels;
using BootstrapperUI.Views;
using System;

namespace BootstrapperUI
{
    public class CustomBootstrapperApplication : BootstrapperApplication
    {
        public static Dispatcher Dispatcher { get; set; }

        //This is our UI's primary entry point. This method will be called by the Burn engine.
        protected override void Run()
        {
            //Dispatcher object provides a means for sending messages between the UI thread and any backend threads.
            Dispatcher = Dispatcher.CurrentDispatcher;

            //MVVM pattern
            var model = new BootstrapperApplicationModel(this);
            var viewModel = new InstallViewModel(model);
            var view = new InstallView(viewModel);

            // Sets the handle for a Windows Presentation Foundation (WPF) window.
            // This handle is used by the Burn engine when performing the install or uninstall.
            model.SetWindowHandle(view);

            //This will start the Burn engine. engine will go-ahead to check if our bundle is already installed. 
            this.Engine.Detect();

            //This code is to wait for a second before closing our flash screen. Otherwise the WPF window will load very fast.
            var timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            timer.Start();
            timer.Tick += (sender, args) =>
            {
                model.LogMessage("Time elapsed.Loading the window");
                timer.Stop();
                view.Show();
            };

            //This event is fired when the window is loaded. Here we are closing the flash screen.
            view.Loaded += (sender, e) =>
            {
                model.LogMessage("FrameworkElement loaded event fired.");
                model.CustomBootstrapperApplication.Engine.CloseSplashScreen();
            };

            //halts execution of this method at that line until the Dispatcher is shut down. 
            Dispatcher.Run();

            //shut down the Burn engine
            this.Engine.Quit(model.FinalResult);
        }
    }
}
