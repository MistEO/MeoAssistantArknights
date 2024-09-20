// <copyright file="ScrollViewerBinding.cs" company="MaaAssistantArknights">
// MaaWpfGui - A part of the MaaCoreArknights project
// Copyright (C) 2021 MistEO and Contributors
//
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU Affero General Public License v3.0 only as published by
// the Free Software Foundation, either version 3 of the License, or
// any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY
// </copyright>

using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using HandyControl.Controls;
using ScrollViewer = System.Windows.Controls.ScrollViewer;

namespace MaaWpfGui.Styles.Properties
{
    /// <summary>
    /// The scroll viewer properties.
    /// </summary>
    public static class ScrollViewerBinding
    {
        #region VerticalOffset attached property

        /// <summary>
        /// VerticalOffset attached property.
        /// </summary>
        public static readonly DependencyProperty VerticalOffsetProperty =
            DependencyProperty.RegisterAttached("VerticalOffset", typeof(double),
            typeof(ScrollViewerBinding), new FrameworkPropertyMetadata(double.NaN,
                FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                OnVerticalOffsetPropertyChanged));

        /// <summary>
        /// Just a flag that the binding has been applied.
        /// </summary>
        private static readonly DependencyProperty _verticalOffsetBindingProperty =
            DependencyProperty.RegisterAttached("_verticalOffsetBinding", typeof(bool?), typeof(ScrollViewerBinding));

        /// <summary>
        /// Gets vertical offset property.
        /// </summary>
        /// <param name="depObj">The <see cref="DependencyObject"/> instance.</param>
        /// <returns>The property value.</returns>
        // ReSharper disable once UnusedMember.Global
        public static double GetVerticalOffset(DependencyObject depObj)
        {
            if (depObj is not ScrollViewer)
            {
                return 0;
            }

            return (double)depObj.GetValue(VerticalOffsetProperty);
        }

        /// <summary>
        /// Sets vertical offset property.
        /// </summary>
        /// <param name="depObj">The <see cref="DependencyObject"/> instance.</param>
        /// <param name="value">The new property value.</param>
        public static void SetVerticalOffset(DependencyObject depObj, double value)
        {
            if (depObj is not ScrollViewer)
            {
                return;
            }

            depObj.SetValue(VerticalOffsetProperty, value);
        }

        private static void OnVerticalOffsetPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not ScrollViewer scrollViewer)
            {
                return;
            }

            BindVerticalOffset(scrollViewer);
            scrollViewer.ScrollToVerticalOffset((double)e.NewValue);
        }

        private static void BindVerticalOffset(ScrollViewer scrollViewer)
        {
            if (scrollViewer.GetValue(_verticalOffsetBindingProperty) != null)
            {
                return;
            }

            scrollViewer.SetValue(_verticalOffsetBindingProperty, true);
            scrollViewer.ScrollChanged += (s, se) =>
            {
                if (se.VerticalChange == 0)
                {
                    return;
                }

                SetVerticalOffset(scrollViewer, se.VerticalOffset);
            };
        }

        #endregion VerticalOffset attached property

        #region ViewportHeight attached property

        /// <summary>
        /// The viewport height property.
        /// </summary>
        public static readonly DependencyProperty ViewportHeightProperty =
            DependencyProperty.RegisterAttached("ViewportHeight", typeof(double),
            typeof(ScrollViewerBinding), new FrameworkPropertyMetadata(double.NaN,
                FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                OnViewportHeightPropertyChanged));

        private static readonly DependencyProperty _viewportHeightBindingProperty =
            DependencyProperty.RegisterAttached("_viewportHeightBinding", typeof(bool?), typeof(ScrollViewerBinding));

        /// <summary>
        /// Gets viewport height property.
        /// </summary>
        /// <param name="depObj">The <see cref="DependencyObject"/> instance.</param>
        /// <returns>The property value.</returns>
        // ReSharper disable once UnusedMember.Global
        public static double GetViewportHeight(DependencyObject depObj)
        {
            return depObj is not ScrollViewer scrollViewer ? double.NaN : scrollViewer.ViewportHeight;
        }

        /// <summary>
        /// Sets viewport height property.
        /// </summary>
        /// <param name="depObj">The <see cref="DependencyObject"/> instance.</param>
        /// <param name="value">The new property value.</param>
        public static void SetViewportHeight(DependencyObject depObj, double value)
        {
            if (depObj is not ScrollViewer)
            {
                return;
            }

            depObj.SetValue(ViewportHeightProperty, value);
        }

        private static void OnViewportHeightPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not ScrollViewer scrollViewer)
            {
                return;
            }

            BindViewportHeight(scrollViewer);
        }

        private static void BindViewportHeight(ScrollViewer scrollViewer)
        {
            if (scrollViewer.GetValue(_viewportHeightBindingProperty) != null)
            {
                return;
            }

            scrollViewer.SetValue(_viewportHeightBindingProperty, true);

            scrollViewer.Loaded += (s, se) =>
            {
                SetViewportHeight(scrollViewer, scrollViewer.ViewportHeight);
            };

            scrollViewer.ScrollChanged += (s, se) =>
            {
                SetViewportHeight(scrollViewer, se.ViewportHeight);
            };
        }

        #endregion ViewportHeight attached property

        #region ExtentHeight attached property

        /// <summary>
        /// The extent height property.
        /// </summary>
        public static readonly DependencyProperty ExtentHeightProperty =
            DependencyProperty.RegisterAttached("ExtentHeight", typeof(double),
            typeof(ScrollViewerBinding), new FrameworkPropertyMetadata(double.NaN,
                FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                OnExtentHeightPropertyChanged));

        private static readonly DependencyProperty _extentHeightBindingProperty =
            DependencyProperty.RegisterAttached("_extentHeightBinding", typeof(bool?), typeof(ScrollViewerBinding));

        /// <summary>
        /// Gets extent height property.
        /// </summary>
        /// <param name="depObj">The <see cref="DependencyObject"/> instance.</param>
        /// <returns>The property value.</returns>
        // ReSharper disable once UnusedMember.Global
        public static double GetExtentHeight(DependencyObject depObj)
        {
            return depObj is not ScrollViewer scrollViewer ? double.NaN : scrollViewer.ExtentHeight;
        }

        /// <summary>
        /// Sets extent height property.
        /// </summary>
        /// <param name="depObj">The <see cref="DependencyObject"/> instance.</param>
        /// <param name="value">The new property value.</param>
        public static void SetExtentHeight(DependencyObject depObj, double value)
        {
            if (depObj is not ScrollViewer)
            {
                return;
            }

            depObj.SetValue(ExtentHeightProperty, value);
        }

        private static void OnExtentHeightPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not ScrollViewer scrollViewer)
            {
                return;
            }

            BindExtentHeight(scrollViewer);
        }

        private static void BindExtentHeight(ScrollViewer scrollViewer)
        {
            if (scrollViewer.GetValue(_extentHeightBindingProperty) != null)
            {
                return;
            }

            scrollViewer.SetValue(_extentHeightBindingProperty, true);

            scrollViewer.Loaded += (s, se) =>
            {
                SetExtentHeight(scrollViewer, scrollViewer.ExtentHeight);
            };
        }

        #endregion ExtentHeight attached property

        #region DividerVerticalOffsetList attached property

        /// <summary>
        /// DividerIndex attached property.
        /// </summary>
        public static readonly DependencyProperty DividerVerticalOffsetListProperty =
            DependencyProperty.RegisterAttached("DividerVerticalOffsetList",
                typeof(List<double>),
                typeof(ScrollViewerBinding),
                new FrameworkPropertyMetadata(new List<double>(),
                    FrameworkPropertyMetadataOptions.AffectsRender |
                    FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                    OnDividerVerticalOffsetListPropertyChanged));

        /// <summary>
        /// Just a flag that the binding has been applied.
        /// </summary>
        private static readonly DependencyProperty _dividerVerticalOffsetListBindingProperty =
            DependencyProperty.RegisterAttached("_dividerVerticalOffsetListBinding", typeof(bool?), typeof(ScrollViewerBinding));

        /// <summary>
        /// Gets divider vertical offset property.
        /// </summary>
        /// <param name="depObj">The <see cref="DependencyObject"/> instance.</param>
        /// <returns>The property value.</returns>
        // ReSharper disable once UnusedMember.Global
        public static List<double> GetDividerVerticalOffsetList(DependencyObject depObj)
        {
            return (List<double>)depObj.GetValue(DividerVerticalOffsetListProperty);
        }

        /// <summary>
        /// Sets divider vertical offset property.
        /// </summary>
        /// <param name="depObj">The <see cref="DependencyObject"/> instance.</param>
        /// <param name="value">The new property value.</param>
        public static void SetDividerVerticalOffsetList(DependencyObject depObj, List<double> value)
        {
            if (depObj is not ScrollViewer)
            {
                return;
            }

            depObj.SetValue(DividerVerticalOffsetListProperty, value);
        }

        private static void OnDividerVerticalOffsetListPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not ScrollViewer scrollViewer)
            {
                return;
            }

            BindDividerVerticalOffsetList(scrollViewer);
        }

        private static void BindDividerVerticalOffsetList(ScrollViewer scrollViewer)
        {
            if (scrollViewer.GetValue(_dividerVerticalOffsetListBindingProperty) != null)
            {
                return;
            }

            scrollViewer.SetValue(_dividerVerticalOffsetListBindingProperty, true);

            // 当滚动条载入时，遍历 StackPanel 中的所有 Divider 子元素对应位置
            scrollViewer.Loaded += (s, se) =>
            {
                if (!scrollViewer.HasContent || scrollViewer.Content is not StackPanel stackPanel)
                {
                    return;
                }

                var point = new Point(10, scrollViewer.VerticalOffset);

                var dividerOffsetList = stackPanel.Children.OfType<Divider>()
                    .Select(child => child.TransformToVisual(scrollViewer)
                        .Transform(point))
                    .Select(targetPosition => targetPosition.Y)
                    .ToList();

                if (dividerOffsetList.Count > 0)
                {
                    SetDividerVerticalOffsetList(scrollViewer, dividerOffsetList);
                }
            };
        }

        #endregion DividerVerticalOffsetList attached property
    }
}
