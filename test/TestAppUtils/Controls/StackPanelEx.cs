using ModernWpf.Controls;
using System.Windows.Markup;
using System.Windows.Media;

namespace System.Windows.Controls
{
    [ContentProperty(nameof(Children))]
    public class StackPanelEx : SimpleStackPanel
    {
        static StackPanelEx()
        {
            OrientationProperty.OverrideMetadata(typeof(StackPanelEx), new FrameworkPropertyMetadata(OnOrientationPropertyChanged));
            SpacingProperty.OverrideMetadata(typeof(StackPanelEx), new FrameworkPropertyMetadata(OnSpacingPropertyChanged));
        }

        public StackPanelEx()
        {
            _itemsHost = new ItemsHost(this);
            _border = new Border { Child = _itemsHost };
            AddVisualChild(_border);
        }

        #region BorderBrush

        public static readonly DependencyProperty BorderBrushProperty
                = Border.BorderBrushProperty.AddOwner(typeof(StackPanelEx),
                    new FrameworkPropertyMetadata(
                        Border.BorderBrushProperty.DefaultMetadata.DefaultValue,
                        FrameworkPropertyMetadataOptions.None,
                        OnBorderBrushPropertyChanged));

        public Brush BorderBrush
        {
            get => (Brush)GetValue(BorderBrushProperty);
            set => SetValue(BorderBrushProperty, value);
        }

        private static void OnBorderBrushPropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
        {
            ((StackPanelEx)sender).OnBorderBrushPropertyChanged(args);
        }

        private void OnBorderBrushPropertyChanged(DependencyPropertyChangedEventArgs args)
        {
            _border.BorderBrush = (Brush)args.NewValue;
        }

        #endregion

        #region BorderThickness

        public static readonly DependencyProperty BorderThicknessProperty
                = Border.BorderThicknessProperty.AddOwner(typeof(StackPanelEx),
                    new FrameworkPropertyMetadata(
                        Border.BorderThicknessProperty.DefaultMetadata.DefaultValue,
                        FrameworkPropertyMetadataOptions.None,
                        OnBorderThicknessPropertyChanged));

        public Thickness BorderThickness
        {
            get => (Thickness)GetValue(BorderThicknessProperty);
            set => SetValue(BorderThicknessProperty, value);
        }

        private static void OnBorderThicknessPropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
        {
            ((StackPanelEx)sender).OnBorderThicknessPropertyChanged(args);
        }

        private void OnBorderThicknessPropertyChanged(DependencyPropertyChangedEventArgs args)
        {
            _border.BorderThickness = (Thickness)args.NewValue;
        }

        #endregion

        #region Padding

        public static readonly DependencyProperty PaddingProperty
                = Border.PaddingProperty.AddOwner(typeof(StackPanelEx),
                    new FrameworkPropertyMetadata(
                        Border.PaddingProperty.DefaultMetadata.DefaultValue,
                        FrameworkPropertyMetadataOptions.None,
                        OnPaddingPropertyChanged));

        public Thickness Padding
        {
            get => (Thickness)GetValue(PaddingProperty);
            set => SetValue(PaddingProperty, value);
        }

        private static void OnPaddingPropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
        {
            ((StackPanelEx)sender).OnPaddingPropertyChanged(args);
        }

        private void OnPaddingPropertyChanged(DependencyPropertyChangedEventArgs args)
        {
            _border.Padding = (Thickness)args.NewValue;
        }

        #endregion

        protected override int VisualChildrenCount => 1;

        protected override Visual GetVisualChild(int index)
        {
            if (index != 0)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }

            return _border;
        }

        protected override Size MeasureOverride(Size availableSize)
        {
            _border.Measure(availableSize);
            return _border.DesiredSize;
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            _border.Arrange(new Rect(finalSize));
            return finalSize;
        }

        protected override UIElementCollection CreateUIElementCollection(FrameworkElement logicalParent)
        {
            return _itemsHost.Children;
        }

        private static void OnOrientationPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((StackPanelEx)d)._itemsHost.Orientation = (Orientation)e.NewValue;
        }

        private static void OnSpacingPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((StackPanelEx)d)._itemsHost.Spacing = (double)e.NewValue;
        }

        private readonly Border _border;
        private readonly ItemsHost _itemsHost;

        private class ItemsHost : SimpleStackPanel
        {
            public ItemsHost(StackPanelEx owner)
            {
                _owner = owner;
            }

            protected override UIElementCollection CreateUIElementCollection(FrameworkElement logicalParent)
            {
                return new UIElementCollection(this, _owner);
            }

            private readonly StackPanelEx _owner;
        }
    }
}
