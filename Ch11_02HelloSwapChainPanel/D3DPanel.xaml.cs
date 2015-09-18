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

namespace Ch11_02HelloSwapChainPanel
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

            // Only use Direct3D if outside of the designer
            if (!Windows.ApplicationModel.DesignMode.DesignModeEnabled)
            {
                d3dApp = new D3DApp(this);
                d3dApp.Initialize();

                d3dApp.Camera.Position = new SharpDX.Vector3(1, 1, 2);
                d3dApp.Camera.LookAtDir = -d3dApp.Camera.Position;

                CompositionTarget.Rendering += CompositionTarget_Rendering;
            }
        }

        void CompositionTarget_Rendering(object sender, object e)
        {
            if (d3dApp != null)
                d3dApp.Render();
        }

        private void SliderRed_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            if (d3dApp == null)
                return;
            Windows.UI.Color c = d3dApp.Color;
            c.R = (byte)e.NewValue;
            d3dApp.Color = c;
        }

        private void SliderGreen_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            if (d3dApp == null)
                return;
            Windows.UI.Color c = d3dApp.Color;
            c.G = (byte)e.NewValue;
            d3dApp.Color = c;
        }

        private void SliderBlue_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            if (d3dApp == null)
                return;
            Windows.UI.Color c = d3dApp.Color;
            c.B = (byte)e.NewValue;
            d3dApp.Color = c;
        }

        private void CheckBox_Checked(object sender, RoutedEventArgs e)
        {
            if (d3dApp == null)
                return;
            var cb = sender as CheckBox;
            if (cb != null)
                d3dApp.CubeRenderer.Show = !(cb.IsChecked == true);
        }

        private void CheckBox_Unchecked(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
			if (d3dApp == null)
                return;
            var cb = sender as CheckBox;
            if (cb != null)
                d3dApp.CubeRenderer.Show = !(cb.IsChecked == true);
        }
		
		private void animatePanelClick(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
        	rotatePanel.Begin();
        }
    }
}
