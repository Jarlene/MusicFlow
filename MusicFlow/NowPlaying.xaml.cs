﻿using Microsoft.UI.Composition.Toolkit;
using NowPlayingClasses;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Composition;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Hosting;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace MusicFlow
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class NowPlaying : Page
    {
        public NowPlaying()
        {
            SystemNavigationManager.GetForCurrentView().AppViewBackButtonVisibility = AppViewBackButtonVisibility.Visible;
            this.InitializeComponent();
        }

        protected override void OnNavigatingFrom(NavigatingCancelEventArgs e)
        {
            _rootVisual.StopAnimation("Offset");
            _rootVisual.StopAnimation("CenterPoint");
            _rootVisual.StopAnimation("Scale");
            
            
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            _rootVisual = GetVisual(PictureHost);
            _compositor = _rootVisual.Compositor;
            _imageFactory = CompositionImageFactory.CreateCompositionImageFactory(_compositor);
            _rootVisual.Clip = _compositor.CreateInsetClip(0, 0, 0, 0);


            // Begin the TransitionController to load images and kick off animations.
            try
            {
             _transitionController = new TransitionController();
            await _transitionController.Create(_rootVisual, _imageFactory);

            var actualSize = new Vector2((float)PictureHost.ActualWidth, (float)PictureHost.ActualHeight);
            _transitionController.UpdateWindowSize(actualSize);

            NearSlideCheckBox_Click(this, null);
            FarSlideCheckBox_Click(this, null);
            FlashlightCheckBox_Click(this, null);
            ZoomCheckBox_Click(this, null);
            StackCheckBox_Click(this, null);
           
                _transitionController.NextTransition();
            }
            catch { }
        }


        //private async void PictureHost_Loaded(object sender, RoutedEventArgs e)
        //{
        //    // Host the Composition scene inside the PictureHost canvas, allowing us to also display
        //    // Xaml controls.


        //}


        private void PictureHost_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (_transitionController != null)
            {
                var newSize = e.NewSize;
                var actualSize = new Vector2((float)newSize.Width, (float)newSize.Height);
                _transitionController.UpdateWindowSize(actualSize);
            }
        }


        private void PictureHost_Unloaded(object sender, RoutedEventArgs e)
        {
            if (_transitionController != null)
            {
                _transitionController.Dispose();
            }
        }


        private static ContainerVisual GetVisual(UIElement element)
        {
            var hostVisual = ElementCompositionPreview.GetElementVisual(element);
            ContainerVisual root = hostVisual.Compositor.CreateContainerVisual();
            ElementCompositionPreview.SetElementChildVisual(element, root);
            return root;
        }

        private void AppBarButton_Click(object sender, RoutedEventArgs e)
        {
            MySplitView.IsPaneOpen = !MySplitView.IsPaneOpen;
        }

        private void NearSlideCheckBox_Click(object sender, RoutedEventArgs e)
        {
            _transitionController.UpdateTransitionEnabled(
                TransitionKind.NearSlide,
                NearSlideCheckBox.IsChecked == true);
        }

        private void FarSlideCheckBox_Click(object sender, RoutedEventArgs e)
        {
            bool enabled = FarSlideCheckBox.IsChecked == true;

            _transitionController.UpdateTransitionEnabled(
                TransitionKind.FarSlide,
                enabled);

            FlashlightCheckBox.IsEnabled = enabled;
        }

        private void FlashlightCheckBox_Click(object sender, RoutedEventArgs e)
        {
            _transitionController.IsFlashlightEnabled = FlashlightCheckBox.IsChecked == false;
        }

        private void ZoomCheckBox_Click(object sender, RoutedEventArgs e)
        {
            _transitionController.UpdateTransitionEnabled(
                TransitionKind.Zoom,
                ZoomCheckBox.IsChecked == true);
        }

        private void StackCheckBox_Click(object sender, RoutedEventArgs e)
        {
            _transitionController.UpdateTransitionEnabled(
                TransitionKind.Stack,
                StackCheckBox.IsChecked == true);
        }

        private Compositor _compositor;
        private CompositionImageFactory _imageFactory;
        private ContainerVisual _rootVisual;
        private TransitionController _transitionController;
    }
}
