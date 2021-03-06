﻿using System.Windows;
using System.Windows.Controls;

namespace Liberfy
{
    /// <summary>
    /// BusyIndicator.xaml の相互作用ロジック
    /// </summary>
    public class BusyIndicator : Control
    {
        static BusyIndicator()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(BusyIndicator), new FrameworkPropertyMetadata(typeof(BusyIndicator)));
        }

        /// <summary>
        /// <see cref="BusyIndicator"/>を生成する。
        /// </summary>
        public BusyIndicator() : base()
        {
        }

        /// <summary>
        /// アニメーションの表示状態を取得または設定する。
        /// </summary>
        public bool IsBusy
        {
            get => (bool)this.GetValue(IsBusyProperty);
            set => this.SetValue(IsBusyProperty, value);
        }

        /// <summary>
        /// アニメーション表示状態のプロパティ
        /// </summary>
        public static readonly DependencyProperty IsBusyProperty =
            DependencyProperty.Register(nameof(IsBusy), typeof(bool), typeof(BusyIndicator), new(false));
    }
}
