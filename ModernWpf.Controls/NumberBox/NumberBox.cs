﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using ModernWpf.Automation.Peers;

namespace ModernWpf.Controls
{
    public class NumberBoxValueChangedEventArgs
    {
        public NumberBoxValueChangedEventArgs(double oldValue, double newValue)
        {
            OldValue = oldValue;
            NewValue = newValue;
        }

        public double OldValue { get; }
        public double NewValue { get; }
    }

    [TemplatePart(Name = c_numberBoxDownButtonName, Type = typeof(RepeatButton))]
    [TemplatePart(Name = c_numberBoxUpButtonName, Type = typeof(RepeatButton))]
    [TemplatePart(Name = c_numberBoxTextBoxName, Type = typeof(TextBox))]
    [TemplatePart(Name = c_numberBoxPopupName, Type = typeof(Popup))]
    [TemplatePart(Name = c_numberBoxPopupDownButtonName, Type = typeof(RepeatButton))]
    [TemplatePart(Name = c_numberBoxPopupUpButtonName, Type = typeof(RepeatButton))]
    public partial class NumberBox : Control
    {
        const string c_numberBoxDownButtonName = "DownSpinButton";
        const string c_numberBoxUpButtonName = "UpSpinButton";
        const string c_numberBoxTextBoxName = "InputBox";
        const string c_numberBoxPopupButtonName = "PopupButton";
        const string c_numberBoxPopupName = "UpDownPopup";
        const string c_numberBoxPopupDownButtonName = "PopupDownSpinButton";
        const string c_numberBoxPopupUpButtonName = "PopupUpSpinButton";
        const string c_numberBoxPopupContentRootName = "PopupContentRoot";

        const double c_popupShadowDepth = 16.0;
        const string c_numberBoxPopupShadowDepthName = "NumberBoxPopupShadowDepth";

        static NumberBox()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(NumberBox), new FrameworkPropertyMetadata(typeof(NumberBox)));
        }

        public NumberBox()
        {
            SetCurrentValue(NumberFormatterProperty, GetRegionalSettingsAwareDecimalFormatter());

            MouseWheel += OnNumberBoxScroll;

            GotKeyboardFocus += OnNumberBoxGotFocus;
            LostKeyboardFocus += OnNumberBoxLostFocus;
        }

        private INumberBoxNumberFormatter GetRegionalSettingsAwareDecimalFormatter()
        {
            return new DefaultNumberBoxNumberFormatter();
        }

        protected override AutomationPeer OnCreateAutomationPeer()
        {
            return new NumberBoxAutomationPeer(this);
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            var spinDownName = Strings.NumberBoxDownSpinButtonName;
            var spinUpName = Strings.NumberBoxUpSpinButtonName;

            if (GetTemplateChild(c_numberBoxDownButtonName) is RepeatButton spinDown)
            {
                spinDown.Click += OnSpinDownClick;

                // Do localization for the down button
                if (string.IsNullOrEmpty(AutomationProperties.GetName(spinDown)))
                {
                    AutomationProperties.SetName(spinDown, spinDownName);
                }
            }

            if (GetTemplateChild(c_numberBoxUpButtonName) is RepeatButton spinUp)
            {
                spinUp.Click += OnSpinUpClick;

                // Do localization for the up button
                if (string.IsNullOrEmpty(AutomationProperties.GetName(spinUp)))
                {
                    AutomationProperties.SetName(spinUp, spinUpName);
                }
            }

            if (GetTemplateChild(c_numberBoxTextBoxName) is TextBox textBox)
            {
                textBox.PreviewKeyDown += OnNumberBoxKeyDown;
                textBox.KeyUp += OnNumberBoxKeyUp;
                m_textBox = textBox;
            }

            m_popup = GetTemplateChild(c_numberBoxPopupName) as Popup;

            if (m_popup != null)
            {
                if (m_popup.HasDefaultValue(Popup.PlacementTargetProperty))
                {
                    m_popup.PlacementTarget = m_textBox;
                }

                m_popupRepositionHelper = new PopupRepositionHelper(m_popup, this);
            }

            if (GetTemplateChild(c_numberBoxPopupDownButtonName) is RepeatButton popupSpinDown)
            {
                popupSpinDown.Click += OnSpinDownClick;
            }

            if (GetTemplateChild(c_numberBoxPopupUpButtonName) is RepeatButton popupSpinUp)
            {
                popupSpinUp.Click += OnSpinUpClick;
            }

            // .NET rounds to 12 significant digits when displaying doubles, so we will do the same.
            //m_displayRounder.SignificantDigits(12);

            UpdateSpinButtonPlacement();
            UpdateSpinButtonEnabled();

            if (ReadLocalValue(ValueProperty) == DependencyProperty.UnsetValue
                && ReadLocalValue(TextProperty) != DependencyProperty.UnsetValue)
            {
                // If Text has been set, but Value hasn't, update Value based on Text.
                UpdateValueToText();
            }
            else
            {
                UpdateTextToValue();
            }
        }

        private void OnValuePropertyChanged(DependencyPropertyChangedEventArgs args)
        {
            // This handler may change Value; don't send extra events in that case.
            if (!m_valueUpdating)
            {
                var oldValue = (double)args.OldValue;

                try
                {
                    m_valueUpdating = true;

                    CoerceValue();

                    var newValue = Value;
                    if (newValue != oldValue && !(double.IsNaN(newValue) && double.IsNaN(oldValue)))
                    {
                        // Fire ValueChanged event
                        var valueChangedArgs = new NumberBoxValueChangedEventArgs(oldValue, newValue);
                        ValueChanged?.Invoke(this, valueChangedArgs);

                        // Fire value property change for UIA
                        if (FrameworkElementAutomationPeer.FromElement(this) is NumberBoxAutomationPeer peer)
                        {
                            peer.RaiseValueChangedEvent(oldValue, newValue);
                        }
                    }

                    UpdateTextToValue();
                    UpdateSpinButtonEnabled();
                }
                finally
                {
                    m_valueUpdating = false;
                }
            }
        }

        private void OnMinimumPropertyChanged(DependencyPropertyChangedEventArgs args)
        {
            CoerceMaximum();
            CoerceValue();

            UpdateSpinButtonEnabled();
        }

        private void OnMaximumPropertyChanged(DependencyPropertyChangedEventArgs args)
        {
            CoerceMinimum();
            CoerceValue();

            UpdateSpinButtonEnabled();
        }

        private void OnSmallChangePropertyChanged(DependencyPropertyChangedEventArgs args)
        {
            UpdateSpinButtonEnabled();
        }

        private void OnIsWrapEnabledPropertyChanged(DependencyPropertyChangedEventArgs args)
        {
            UpdateSpinButtonEnabled();
        }

        private void OnNumberFormatterPropertyChanged(DependencyPropertyChangedEventArgs args)
        {
            // Update text with new formatting
            UpdateTextToValue();
        }

        private void ValidateNumberFormatter(INumberBoxNumberFormatter value)
        {
            if (value is null)
            {
                throw new ArgumentNullException(nameof(value));
            }
        }

        private void OnSpinButtonPlacementModePropertyChanged(DependencyPropertyChangedEventArgs args)
        {
            UpdateSpinButtonPlacement();
        }

        private void OnTextPropertyChanged(DependencyPropertyChangedEventArgs args)
        {
            if (!m_textUpdating)
            {
                UpdateValueToText();
            }
        }

        private void UpdateValueToText()
        {
            if (m_textBox != null)
            {
                m_textBox.Text = Text;
                ValidateInput();
            }
        }

        private void OnValidationModePropertyChanged(DependencyPropertyChangedEventArgs args)
        {
            ValidateInput();
            UpdateSpinButtonEnabled();
        }

        private void OnNumberBoxGotFocus(object sender, RoutedEventArgs e)
        {
            // When the control receives focus, select the text
            if (m_textBox != null)
            {
                m_textBox.SelectAll();
            }

            if (SpinButtonPlacementMode == NumberBoxSpinButtonPlacementMode.Compact)
            {
                if (m_popup != null)
                {
                    m_popup.IsOpen = true;
                }
            }
        }

        private void OnNumberBoxLostFocus(object sender, RoutedEventArgs e)
        {
            ValidateInput();

            if (m_popup != null)
            {
                m_popup.IsOpen = false;
            }
        }

        private void CoerceMinimum()
        {
            var max = Maximum;
            if (Minimum > max)
            {
                SetCurrentValue(MinimumProperty, max);
            }
        }

        private void CoerceMaximum()
        {
            var min = Minimum;
            if (Maximum < min)
            {
                SetCurrentValue(MaximumProperty, min);
            }
        }

        private void CoerceValue()
        {
            // Validate that the value is in bounds
            var value = Value;
            if (!double.IsNaN(value) && !IsInBounds(value) && ValidationMode == NumberBoxValidationMode.InvalidInputOverwritten)
            {
                // Coerce value to be within range
                var max = Maximum;
                if (value > max)
                {
                    SetCurrentValue(ValueProperty, max);
                }
                else
                {
                    SetCurrentValue(ValueProperty, Minimum);
                }
            }
        }

        private void ValidateInput()
        {
            // Validate the content of the inner textbox
            if (m_textBox != null)
            {
                var text = m_textBox.Text.Trim();

                // Handles empty TextBox case, set text to current value
                if (string.IsNullOrEmpty(text))
                {
                    SetCurrentValue(ValueProperty, double.NaN);
                }
                else
                {
                    // Setting NumberFormatter to something that isn't an INumberParser will throw an exception, so this should be safe
                    var numberParser = NumberFormatter;

                    double? value = AcceptsExpression
                       ? NumberBoxParser.Compute(text, numberParser)
                       : numberParser.ParseDouble(text);

                    if (!value.HasValue)
                    {
                        if (ValidationMode == NumberBoxValidationMode.InvalidInputOverwritten)
                        {
                            // Override text to current value
                            UpdateTextToValue();
                        }
                    }
                    else
                    {
                        if (value.Value == Value)
                        {
                            // Even if the value hasn't changed, we still want to update the text (e.g. Value is 3, user types 1 + 2, we want to replace the text with 3)
                            UpdateTextToValue();
                        }
                        else
                        {
                            SetCurrentValue(ValueProperty, value.Value);
                        }
                    }
                }
            }
        }

        private void OnSpinDownClick(object sender, RoutedEventArgs args)
        {
            StepValue(-SmallChange);
        }

        private void OnSpinUpClick(object sender, RoutedEventArgs args)
        {
            StepValue(SmallChange);
        }

        private void OnNumberBoxKeyDown(object sender, KeyEventArgs args)
        {
            // Handle these on key down so that we get repeat behavior.
            switch (args.Key)
            {
                case Key.Up:
                    StepValue(SmallChange);
                    args.Handled = true;
                    break;

                case Key.Down:
                    StepValue(-SmallChange);
                    args.Handled = true;
                    break;

                case Key.PageUp:
                    StepValue(LargeChange);
                    args.Handled = true;
                    break;

                case Key.PageDown:
                    StepValue(-LargeChange);
                    args.Handled = true;
                    break;
            }
        }

        private void OnNumberBoxKeyUp(object sender, KeyEventArgs args)
        {
            switch (args.Key)
            {
                case Key.Enter:
                    ValidateInput();
                    args.Handled = true;
                    break;

                case Key.Escape:
                    UpdateTextToValue();
                    args.Handled = true;
                    break;
            }
        }

        private void OnNumberBoxScroll(object sender, MouseWheelEventArgs args)
        {
            if (m_textBox != null)
            {
                if (m_textBox.IsFocused)
                {
                    var delta = args.Delta;
                    if (delta > 0)
                    {
                        StepValue(SmallChange);
                    }
                    else if (delta < 0)
                    {
                        StepValue(-SmallChange);
                    }
                    // Only set as handled when we actually changed our state.
                    args.Handled = true;
                }
            }
        }

        private void StepValue(double change)
        {
            // Before adjusting the value, validate the contents of the textbox so we don't override it.
            ValidateInput();

            var newVal = Value;
            if (!double.IsNaN(newVal))
            {
                newVal += change;

                if (IsWrapEnabled)
                {
                    var max = Maximum;
                    var min = Minimum;

                    if (newVal > max)
                    {
                        newVal = min;
                    }
                    else if (newVal < min)
                    {
                        newVal = max;
                    }
                }

                SetCurrentValue(ValueProperty, newVal); ;
            }
        }

        // Updates TextBox.Text with the formatted Value
        private void UpdateTextToValue()
        {
            if (m_textBox != null)
            {
                string newText = string.Empty;

                var value = Value;
                if (!double.IsNaN(value))
                {
                    // Rounding the value here will prevent displaying digits caused by floating point imprecision.
                    var roundedValue = m_displayRounder.RoundDouble(value);
                    newText = NumberFormatter.FormatDouble(roundedValue);
                }

                m_textBox.Text = newText;

                try
                {
                    m_textUpdating = true;

                    SetCurrentValue(TextProperty, newText);

                    // This places the caret at the end of the text.
                    m_textBox.Select(newText.Length, 0);
                }
                finally
                {
                    m_textUpdating = false;
                }
            }
        }

        private void UpdateSpinButtonPlacement()
        {
            var spinButtonMode = SpinButtonPlacementMode;

            if (spinButtonMode == NumberBoxSpinButtonPlacementMode.Inline)
            {
                VisualStateManager.GoToState(this, "SpinButtonsVisible", false);
            }
            else if (spinButtonMode == NumberBoxSpinButtonPlacementMode.Compact)
            {
                VisualStateManager.GoToState(this, "SpinButtonsPopup", false);
            }
            else
            {
                VisualStateManager.GoToState(this, "SpinButtonsCollapsed", false);
            }
        }

        private void UpdateSpinButtonEnabled()
        {
            var value = Value;
            bool isUpButtonEnabled = false;
            bool isDownButtonEnabled = false;

            if (!double.IsNaN(value))
            {
                if (IsWrapEnabled || ValidationMode != NumberBoxValidationMode.InvalidInputOverwritten)
                {
                    // If wrapping is enabled, or invalid values are allowed, then the buttons should be enabled
                    isUpButtonEnabled = true;
                    isDownButtonEnabled = true;
                }
                else
                {
                    if (value < Maximum)
                    {
                        isUpButtonEnabled = true;
                    }
                    if (value > Minimum)
                    {
                        isDownButtonEnabled = true;
                    }
                }
            }

            VisualStateManager.GoToState(this, isUpButtonEnabled ? "UpSpinButtonEnabled" : "UpSpinButtonDisabled", false);
            VisualStateManager.GoToState(this, isDownButtonEnabled ? "DownSpinButtonEnabled" : "DownSpinButtonDisabled", false);
        }

        private bool IsInBounds(double value)
        {
            return value >= Minimum && value <= Maximum;
        }

        bool m_valueUpdating = false;
        bool m_textUpdating = false;

        DefaultNumberRounder m_displayRounder = new DefaultNumberRounder();

        TextBox m_textBox;
        Popup m_popup;
        PopupRepositionHelper m_popupRepositionHelper;
    }
}
