using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace net.adamec.dev.vs.extension.radprojects.ui.updown
{
    /// <inheritdoc />
    /// <summary>
    /// WPF TextBox extended with a "shrinked" scrollbar allowing to increase/decrease the integer value
    /// The int value is accessible through <see cref="P:net.adamec.dev.vs.extension.radprojects.ui.updown.IntegerUpDown.Value" /> property (bound to <see cref="P:System.Windows.Controls.Primitives.RangeBase.Value" />, 
    /// the text  value (even invalid) is accessible through <see cref="P:System.Windows.Controls.TextBox.Text" /> property.
    /// Visual tree defined in IntegerUpDownResourceDictionary.xaml
    /// </summary>
    public class IntegerUpDown : TextBox
    {
        //CTOR - use the style from IntegerUpDownResourceDictionary.xaml that is bound to IntegerUpDown type
        static IntegerUpDown()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(IntegerUpDown), new FrameworkPropertyMetadata(typeof(IntegerUpDown)));
        }

        /// <summary>
        /// Dependency property for <see cref="IsRequired"/>
        /// </summary>
        private static readonly DependencyProperty IsRequiredProperty =
            DependencyProperty.Register(nameof(IsRequired), typeof(bool), typeof(IntegerUpDown),
                new PropertyMetadata(false));
        /// <summary>
        /// Flag whether the control must have a value
        /// </summary>
        [Category("Common")]
        [Description("Flag whether the control must have a value to be valid")]
        public bool IsRequired
        {
            get => (bool)GetValue(IsRequiredProperty);
            set => SetValue(IsRequiredProperty, value);
        }

        /// <summary>
        /// Dependency property key for <see cref="IsValidProperty"/>
        /// </summary>
        internal static readonly DependencyPropertyKey IsValidPropertyKey =
            DependencyProperty.RegisterReadOnly(nameof(IsValid), typeof(bool), typeof(IntegerUpDown),
                new PropertyMetadata(false));

        /// <summary>
        /// Dependency property for <see cref="IsValid"/>
        /// </summary>
        private static readonly DependencyProperty IsValidProperty = IsValidPropertyKey.DependencyProperty;

        /// <summary>
        /// Flag whether the <see cref="IntegerUpDown.Text"/>  entered is valid,
        /// so the backing <see cref="Value"/> have corresponding value or
        /// the Text is empty if not <see cref="IsRequired"/>
        /// </summary>
        [Category("Common")]
        [Description("Flag whether the control must have a value to be valid")]
        public bool IsValid
        {
            get => (bool)GetValue(IsValidProperty);
            private set => SetValue(IsValidPropertyKey, value);
        }

        /// <summary>
        /// Dependency property for <see cref="Value"/>
        /// </summary>
        private static readonly DependencyProperty ValueProperty =
            DependencyProperty.Register(nameof(Value), typeof(int), typeof(IntegerUpDown),
                new FrameworkPropertyMetadata(0) { BindsTwoWayByDefault = true });
        /// <summary>
        /// The integer value of content
        /// </summary>
        [Category("Common")]
        [Description("The integer value of content")]
        public int Value
        {
            get => (int)GetValue(ValueProperty);
            set => SetValue(ValueProperty, value);
        }

        /// <summary>
        /// Dependency property for <see cref="Minimum"/>
        /// </summary>
        private static readonly DependencyProperty MinimumProperty =
            DependencyProperty.Register(nameof(Minimum), typeof(int), typeof(IntegerUpDown),
                new PropertyMetadata(0));
        /// <summary>
        /// Minimal allowed <see cref="Value"/>
        /// </summary>
        [Category("Common")]
        [Description("Minimal allowed value")]
        public int Minimum
        {
            get => (int)GetValue(MinimumProperty);
            set => SetValue(MinimumProperty, value);
        }

        /// <summary>
        /// Dependency property for <see cref="Maximum"/>
        /// </summary>
        private static readonly DependencyProperty MaximumProperty =
            DependencyProperty.Register(nameof(Maximum), typeof(int), typeof(IntegerUpDown),
                new PropertyMetadata(100));
        /// <summary>
        /// Maximal allowed <see cref="Value"/>
        /// </summary>
        [Category("Common")]
        [Description("Maximal allowed value")]
        public int Maximum
        {
            get => (int)GetValue(MaximumProperty);
            set => SetValue(MaximumProperty, value);
        }

        /// <summary>
        /// Attached scrollbar
        /// </summary>
        private ScrollBar Scroll { get; set; }

        /// <inheritdoc />
        /// <summary>
        /// Get reference to attached <see cref="Scroll"/> and register the <see cref="ScrollBar.ValueChanged"/> event handler
        /// </summary>
        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            Scroll = Template.FindName("PART_Scroll", this) as ScrollBar;
            if (Scroll == null) return;

            Scroll.ValueChanged += ScrollOnValueChanged;
            if (IsValid) Scroll.Value = int.Parse(Text);
        }

        /// <summary>
        /// <see cref="ScrollBar.ValueChanged"/> event handler - update the <see cref="IntegerUpDown.Text"/>
        /// when the scroll bar buttons are used
        /// </summary>
        /// <param name="sender">Event sender</param>
        /// <param name="e">Event data</param>
        private void ScrollOnValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            Text = ((int)Scroll.Value).ToString();
        }

        /// <inheritdoc />
        /// <summary>
        /// Validates the input text when <see cref="P:System.Windows.Controls.TextBox.Text" /> is changed
        /// Sets <see cref="P:net.adamec.dev.vs.extension.radprojects.ui.updown.IntegerUpDown.IsValid" /> property
        /// Don't forget call base.OnTextChanged(e); to raise <see cref="TextBoxBase.TextChanged"/> event!
        /// </summary>
        /// <param name="e">Event data prepared for <see cref="TextBoxBase.TextChanged"/> event</param>
        protected override void OnTextChanged(TextChangedEventArgs e)
        {
            var newValue = Text;
            var isValid = !(string.IsNullOrEmpty(newValue) && IsRequired);
            if (!int.TryParse(newValue, out var newValueInt) || newValueInt < Minimum || newValueInt > Maximum) isValid = false;

            IsValid = isValid;
            if (isValid && Scroll != null && Math.Abs(Scroll.Value - newValueInt) > 0.00001)
                Scroll.Value = newValueInt;

            base.OnTextChanged(e);
        }
    }
}
