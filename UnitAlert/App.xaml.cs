using System.Diagnostics;
using System.Windows;

namespace UnitAlert
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private async void Application_Start(object sender, StartupEventArgs args)
        {
            // Simple check if the debugger is not attached to not check for the version.
            if (Debugger.IsAttached)
            {
                return;
            }
            var latest = await UpdateChecker.IsUpToDate();
            switch (latest)
            {
                case null:
                    MessageBox.Show("Error getting latest version");
                    break;
                case false:
                {
                    // If the version is not up to date, show a messagebox.
                    var releaseLink = UpdateChecker.GetReleaseLink();
                    const string messageBoxText = "Outdated version. Do you want to download the newest version?";
                    const string caption = "Update required.";
                    const MessageBoxButton button = MessageBoxButton.YesNo;
                    var result = MessageBox.Show(messageBoxText, caption, button);
                    if (result == MessageBoxResult.Yes && releaseLink is not null) 
                    {
                        // If yes is clicked, start the default browser with the link to the newest release.
                        Process.Start("explorer", releaseLink);
                        
                    }
                    break;
                }
            }
        }
    }
}