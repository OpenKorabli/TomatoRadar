using System.Windows;

namespace TomatoRadar.Models
{
    internal class ListItem : DependencyObject
    {
        public static readonly DependencyProperty ContentProperty = DependencyProperty.RegisterAttached("Content", typeof(object), typeof(ListItem));
        public object Content
        {
            get { return (object)GetValue(ContentProperty); }
            set { SetValue(ContentProperty, value); }
        }
        public string? Value { get; set; }
    }
}
