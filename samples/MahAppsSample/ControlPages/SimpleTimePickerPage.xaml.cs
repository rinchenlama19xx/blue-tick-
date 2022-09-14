﻿using MahApps.Metro.Controls;
using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;

namespace MahAppsSample.ControlPages
{
    public partial class SimpleTimePickerPage
    {
        public SimpleTimePickerPage()
        {
            InitializeComponent();
        }

        private void TimePicker_SelectedDateTimeChanged(object sender, TimePickerBaseSelectionChangedEventArgs<DateTime?> e)
        {
            Debug.WriteLine(e.NewValue?.ToShortTimeString() ?? "null");
        }

        private void SetValueToNow_Click(object sender, RoutedEventArgs e)
        {
            TimePicker.SelectedDateTime = DateTime.Now;
        }

        private void ClearValue_Click(object sender, RoutedEventArgs e)
        {
            TimePicker.ClearValue(TimePickerBase.SelectedDateTimeProperty);
        }
    }
}
