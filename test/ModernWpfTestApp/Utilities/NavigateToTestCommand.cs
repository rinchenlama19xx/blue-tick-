﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;

namespace MUXControlsTestApp
{
    public class NavigateToTestCommand : System.Windows.Input.ICommand
    {
        public event EventHandler CanExecuteChanged;

        public Frame Frame { get; set; }

        public NavigateToTestCommand()
        {
            CanExecuteChanged?.Invoke(this, null);
        }

        public bool CanExecute(object parameter)
        {
            return parameter != null && parameter is TestDeclaration;
        }

        public void Execute(object parameter)
        {
            var testDeclaration = parameter as TestDeclaration;
            var rootFrame = Frame != null ? Frame : Application.Current.MainWindow.Content as Frame;
            rootFrame.NavigateEx(testDeclaration.PageType, testDeclaration.Name);
        }
    }

    static class NavigationExtensions
    {
        public static void NavigateWithoutAnimation(this Frame frame, Type sourcePageType)
        {
            frame.NavigateEx(sourcePageType);
        }

        public static void NavigateWithoutAnimation(this Frame frame, Type sourcePageType, object parameter)
        {
            frame.NavigateEx(sourcePageType, parameter);
        }

        public static void NavigateEx(this Frame frame, Type sourcePageType, object extraData = null)
        {
            frame.Navigate(Activator.CreateInstance(sourcePageType), extraData);
        }

        public static void NavigateWithoutAnimation(this NavigationService frame, Type sourcePageType)
        {
            frame.NavigateEx(sourcePageType);
        }

        public static void NavigateWithoutAnimation(this NavigationService frame, Type sourcePageType, object parameter)
        {
            frame.NavigateEx(sourcePageType, parameter);
        }

        public static void NavigateEx(this NavigationService frame, Type sourcePageType, object extraData = null)
        {
            frame.Navigate(Activator.CreateInstance(sourcePageType), extraData);
        }
    }
}
