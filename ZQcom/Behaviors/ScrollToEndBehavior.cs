using System.Windows;
using System.Windows.Controls;
using System.Windows.Interactivity;

namespace ZQcom.Behaviors
{
    public class ScrollToEndBehavior : Behavior<TextBox>
    {
        protected override void OnAttached()
        {
            base.OnAttached();
            AssociatedObject.TextChanged += OnTextChanged;
        }

        protected override void OnDetaching()
        {
            base.OnDetaching();
            AssociatedObject.TextChanged -= OnTextChanged;
        }

        // 滚到到底部
        private void OnTextChanged(object sender, TextChangedEventArgs e)
        {
            AssociatedObject.ScrollToEnd();
        }
    }
}