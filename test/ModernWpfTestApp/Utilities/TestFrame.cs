﻿using ModernWpf;
using ModernWpf.Controls;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;

namespace MUXControlsTestApp
{
    public class TestFrame : ThemeAwareFrame
    {
        private Button _backButton = null;
        private Button _toggleThemeButton = null;
        private TextBlock _currentPageTextBlock = null;
        private Type _mainPageType = null;

        private object _oldContent;

        static TestFrame()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(TestFrame), new FrameworkPropertyMetadata(typeof(TestFrame)));
        }

        public TestFrame(Type mainPageType)
        {
            _mainPageType = mainPageType;

            Navigating += OnNavigating;
            Navigated += OnNavigated;
        }

        private void TestFrame_Navigated(object sender, NavigationEventArgs e)
        {
            if (e.Content?.GetType() == _mainPageType)
            {
                _backButton.Visibility = Visibility.Collapsed;
                _currentPageTextBlock.Text = "Home";
            }
            else
            {
                _backButton.Visibility = Visibility.Visible;
                _currentPageTextBlock.Text = (e.ExtraData is string ? e.ExtraData as string : "");
            }

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            this.Navigated += TestFrame_Navigated;

            _backButton = (Button)GetTemplateChild("BackButton");
            _backButton.Click += BackButton_Click;

            _toggleThemeButton = (Button)GetTemplateChild("ToggleThemeButton");
            _toggleThemeButton.Click += ToggleThemeButton_Click;

            _currentPageTextBlock = (TextBlock)GetTemplateChild("CurrentPageTextBlock");
            _currentPageTextBlock.Text = "Home";
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            if (CanGoBack)
            {
                GoBack();
            }
        }

        private void ToggleThemeButton_Click(object sender,RoutedEventArgs e)
        {
            var tm = ThemeManager.Current;
            if (tm.ApplicationTheme == null)
            {
                // Convert theme from default to either dark or light based on application requestedtheme
                tm.ApplicationTheme = (tm.ActualApplicationTheme == ApplicationTheme.Light) ? ApplicationTheme.Light : ApplicationTheme.Dark;
            }
            // Invert theme
            tm.ApplicationTheme = (tm.ActualApplicationTheme == ApplicationTheme.Light) ? ApplicationTheme.Dark : ApplicationTheme.Light;
        }

        protected override void OnContentChanged(object oldContent, object newContent)
        {
            base.OnContentChanged(oldContent, newContent);
            _oldContent = oldContent;
        }

        private void OnNavigating(object sender, NavigatingCancelEventArgs e)
        {
            (Content as IPage)?.InternalOnNavigatingFrom(e);
        }

        private void OnNavigated(object sender, NavigationEventArgs e)
        {
            if (_oldContent != null)
            {
                (_oldContent as IPage)?.InternalOnNavigatedFrom(e);
                _oldContent = null;
            }

            (e.Content as IPage)?.InternalOnNavigatedTo(e);
        }
    }
}
