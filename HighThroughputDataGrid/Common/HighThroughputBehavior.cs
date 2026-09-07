using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using Microsoft.Xaml.Behaviors;
using System.Windows.Media;

namespace HighThroughputDataGrid.Common
{
    public class HighThroughputBehavior : Behavior<ListBox>
    {
        private ScrollViewer _scrollViewer;
        private VirtualizingStackPanel _panel;
        private IList<Suppressible> _onScreenItems = new List<Suppressible>();

        public static readonly DependencyProperty VirtualizingCountProperty =
            DependencyProperty.Register("VirtualizingCount",
                typeof(int), typeof(HighThroughputBehavior),
                      new FrameworkPropertyMetadata(0,
                      FrameworkPropertyMetadataOptions.None));

        public static readonly DependencyProperty VirtualizingCapacityProperty =
            DependencyProperty.Register("VirtualizingCapacity",
                typeof(int), typeof(HighThroughputBehavior),
                      new FrameworkPropertyMetadata(0,
                      FrameworkPropertyMetadataOptions.None));

        public ScrollViewer ScrollViewer { get {return _scrollViewer; } }

        public int VirtualizingCount
        {
            get { return (int)GetValue(VirtualizingCountProperty); }
            set { SetValue(VirtualizingCountProperty, value); }
        }
        public int VirtualizingCapacity
        {
            get { return (int)GetValue(VirtualizingCapacityProperty); }
            set { SetValue(VirtualizingCapacityProperty, value); }
        }

        protected override void OnAttached()
        {
            ListBox listBox = AssociatedObject;
            listBox.Loaded += OnListBoxLoaded;
        }

        protected override void OnDetaching()
        {
            ListBox listBox = AssociatedObject;
            listBox.Loaded -= OnListBoxLoaded;
        }

        private void OnListBoxLoaded(object sender, System.Windows.RoutedEventArgs args)
        {
            ListBox listBox = AssociatedObject;
            _scrollViewer = listBox.FindVisualChild<ScrollViewer>();
            _panel = _scrollViewer.FindVisualChild<VirtualizingStackPanel>();

            OnScrollChanged();
            _scrollViewer.ScrollChanged += (s, e) => OnScrollChanged();
        }

        private void OnScrollChanged()
        {
            VirtualizingCount = _panel.Children.Count;
            VirtualizingCapacity = _panel.Children.Capacity;
            var layout = LayoutInformation.GetLayoutSlot(_panel);
            var children = _panel.Children.Cast<ListBoxItem>();
            var onScreenItems = children
                .Select(item => new { bound = LayoutInformation.GetLayoutSlot(item), item })
                .Where(x => (x.bound.Bottom < layout.Bottom && x.bound.Bottom > layout.Top) || (x.bound.Top > 0 && x.bound.Top < layout.Bottom))
                .Select(x => x.item.DataContext).Cast<Suppressible>().ToList();

            var cachedItems = children.Select(c => c.DataContext).Cast<Suppressible>().ToList();
            foreach (var suppressible in _onScreenItems.Except(onScreenItems))
                suppressible.IsSuppressed = true;
            foreach (var suppressible in onScreenItems)
                suppressible.IsSuppressed = false;

            _onScreenItems = onScreenItems;
        }
    }
}
