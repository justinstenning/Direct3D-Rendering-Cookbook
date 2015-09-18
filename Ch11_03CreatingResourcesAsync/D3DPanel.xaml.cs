using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace Ch11_03CreatingResourcesAsync
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class D3DPanel : SwapChainPanel
    {
        D3DApp d3dApp;

        public D3DPanel()
        {
            this.InitializeComponent();
            this.Loaded += swapChainPanel_Loaded;
            CompositionTarget.Rendering += CompositionTarget_Rendering;
        }

        void CompositionTarget_Rendering(object sender, object e)
        {
            if (d3dApp != null)
                d3dApp.Render();
        }

        private void swapChainPanel_Loaded(object sender, RoutedEventArgs e)
        {
            // Only use Direct3D if outside of the designer
            if (!Windows.ApplicationModel.DesignMode.DesignModeEnabled)
            {
                d3dApp = new D3DApp(this);
                d3dApp.Initialize();

                d3dApp.Camera.Position = new SharpDX.Vector3(0, 0.5f, 2);
                d3dApp.Camera.LookAtDir = new SharpDX.Vector3(0, 0, -1);// -d3dApp.Camera.Position;
            }
        }
    }
}
