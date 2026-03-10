using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using EmployeeWeb.Desktop.Pages;
using EmployeeWeb.Desktop.Services;

namespace EmployeeWeb.Desktop
{
    public sealed partial class MainWindow : Window
    {
        /*
         Pseudocode plan (detailed):
         - Problem: subscribing to `RootFrame.Loaded` can trigger CS0103 if the generated
           field `RootFrame` from XAML isn't present (name mismatch or XAML missing x:Name).
         - Solution: avoid directly referencing a generated field. Resolve the Frame at
           runtime by name from the window content and subscribe to its Loaded event.
         - Steps:
         1. Call InitializeComponent() to ensure XAML is parsed.
         2. Get `this.Content` as a FrameworkElement (the root element of the XAML).
         3. Use `FindName("RootFrame")` on that root to get the Frame instance.
         4. If the Frame is not found, throw a clear InvalidOperationException explaining
            that the Frame must be named `x:Name="RootFrame"` in MainWindow.xaml.
         5. If found, set `NavigationService.RootFrame` and subscribe to its `Loaded`
            event with the existing `OnLoaded` handler.
         6. In `OnLoaded`, use the NavigationService.RootFrame to Navigate to the proper page.
         - This removes the compile-time dependency on a generated identifier and
           prevents the CS0103 error ("The name 'Loaded' does not exist in the current context").
        */

        public MainWindow()
        {
            InitializeComponent();

            // Frame is the direct content of the Window in MainWindow.xaml
            var rootFrame = this.Content as Frame;
            if (rootFrame == null)
            {
                var rootElement = this.Content as FrameworkElement;
                rootFrame = rootElement?.FindName("RootFrame") as Frame;
            }
            if (rootFrame == null)
            {
                throw new System.InvalidOperationException(
                    "RootFrame not found. Ensure MainWindow.xaml has a Frame as content with x:Name=\"RootFrame\".");
            }

            NavigationService.RootFrame = rootFrame;
            rootFrame.Loaded += OnLoaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            AuthService.LoadSavedSession();
            var frame = NavigationService.RootFrame;
            if (frame == null) return;
            if (AuthService.IsLoggedIn)
                frame.Navigate(typeof(ShellPage));
            else
                frame.Navigate(typeof(LoginPage));
        }
    }
}
