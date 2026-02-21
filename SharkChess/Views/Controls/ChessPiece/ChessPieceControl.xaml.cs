using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using SharkChess.Models;
using SharkChess.Services;

// 使用别名避免与 ChessPiece 命名空间冲突
using ChessPieceModel = SharkChess.Models.ChessPiece;

namespace SharkChess.Views.Controls.ChessPiece
{
    /// <summary>
    /// 中国象棋棋子控件
    /// 支持中国风装饰纹样、主题切换、动画效果
    /// </summary>
    public partial class ChessPieceControl : UserControl
    {
        #region 依赖属性
        
        /// <summary>棋子数据</summary>
        public static readonly DependencyProperty PieceProperty =
            DependencyProperty.Register(nameof(Piece), typeof(ChessPieceModel), 
                typeof(ChessPieceControl), new PropertyMetadata(new ChessPieceModel(), OnPieceChanged));
        
        /// <summary>主题配置</summary>
        public static readonly DependencyProperty ThemeConfigProperty =
            DependencyProperty.Register(nameof(ThemeConfig), typeof(ThemeConfig), 
                typeof(ChessPieceControl), new PropertyMetadata(new ThemeConfig(), OnThemeChanged));
        
        /// <summary>是否选中</summary>
        public static readonly DependencyProperty IsSelectedProperty =
            DependencyProperty.Register(nameof(IsSelected), typeof(bool), 
                typeof(ChessPieceControl), new PropertyMetadata(false, OnSelectionChanged));
        
        /// <summary>动画状态</summary>
        public static readonly DependencyProperty AnimationStateProperty =
            DependencyProperty.Register(nameof(AnimationState), typeof(PieceAnimationState), 
                typeof(ChessPieceControl), new PropertyMetadata(PieceAnimationState.Idle, OnAnimationStateChanged));
        
        /// <summary>棋子尺寸（用于动态调整）</summary>
        public static readonly DependencyProperty PieceSizeProperty =
            DependencyProperty.Register(nameof(PieceSize), typeof(double), 
                typeof(ChessPieceControl), new PropertyMetadata(60.0, OnPieceSizeChanged));
        
        #endregion
        
        #region 属性
        
        public ChessPieceModel Piece
        {
            get => (ChessPieceModel)GetValue(PieceProperty);
            set => SetValue(PieceProperty, value);
        }
        
        public ThemeConfig ThemeConfig
        {
            get => (ThemeConfig)GetValue(ThemeConfigProperty);
            set => SetValue(ThemeConfigProperty, value);
        }
        
        public bool IsSelected
        {
            get => (bool)GetValue(IsSelectedProperty);
            set => SetValue(IsSelectedProperty, value);
        }
        
        public PieceAnimationState AnimationState
        {
            get => (PieceAnimationState)GetValue(AnimationStateProperty);
            set => SetValue(AnimationStateProperty, value);
        }
        
        public double PieceSize
        {
            get => (double)GetValue(PieceSizeProperty);
            set => SetValue(PieceSizeProperty, value);
        }
        
        #endregion
        
        #region 私有字段
        
        private Storyboard? _selectAnimation;
        private Storyboard? _deselectAnimation;
        private Storyboard? _captureAnimation;
        private Storyboard? _jumpAnimation;
        private bool _isAnimating = false;
        
        #endregion
        
        #region 构造函数
        
        public ChessPieceControl()
        {
            InitializeComponent();
            InitializeAnimations();
        }
        
        #endregion
        
        #region 尺寸调整
        
        private static void OnPieceSizeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ChessPieceControl control)
            {
                control.Width = (double)e.NewValue;
                control.Height = (double)e.NewValue;
                
                // 调整字体大小
                double fontSize = (double)e.NewValue * 0.47;
                control.PieceText.FontSize = fontSize;
                
                // 调整装饰层大小
                double decorationSize = (double)e.NewValue * 0.77;
                control.DecorationLayer.Width = decorationSize;
                control.DecorationLayer.Height = decorationSize;
            }
        }
        
        #endregion
        
        #region 棋子数据更新
        
        private static void OnPieceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ChessPieceControl control)
            {
                control.UpdatePiece();
            }
        }
        
        private void UpdatePiece()
        {
            if (Piece == null || Piece.IsEmpty)
            {
                Visibility = Visibility.Collapsed;
                return;
            }
            
            Visibility = Visibility.Visible;
            PieceText.Text = Piece.ChineseName;
            
            // 应用主题（确保颜色正确）
            ApplyTheme(ThemeConfig);
        }
        
        #endregion
        
        #region 主题应用
        
        private static void OnThemeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ChessPieceControl control && e.NewValue is ThemeConfig config)
            {
                control.ApplyTheme(config);
            }
        }
        
        /// <summary>
        /// 应用主题配置
        /// </summary>
        public void ApplyTheme(ThemeConfig config)
        {
            if (Piece == null || Piece.IsEmpty || config == null) return;
            
            // 1. 边框颜色
            Color borderColor = Piece.Color == PieceColor.Red
                ? SafeParseColor(config.RedPieceBorder, "#DC143C")
                : SafeParseColor(config.BlackPieceBorder, "#000000");
            
            OuterRing.Stroke = new SolidColorBrush(borderColor);
            InnerRing.Stroke = new SolidColorBrush(borderColor);
            
            // 2. 文字颜色
            Color textColor = Piece.Color == PieceColor.Red
                ? SafeParseColor(config.RedPieceText, "#DC143C")
                : SafeParseColor(config.BlackPieceText, "#000000");
            
            PieceText.Foreground = new SolidColorBrush(textColor);
            
            // 3. 渐变背景（模拟木质/玉石纹理）
            Color bgColor = Piece.Color == PieceColor.Red
                ? SafeParseColor(config.RedPieceBackground, "#FFF5EE")
                : SafeParseColor(config.BlackPieceBackground, "#FFF5EE");
            
            ApplyGradientBackground(bgColor);
            
            // 4. 装饰纹颜色
            Color decorationColor = SafeParseColor(config.DecorationColor, "#B8860B");
            DecorationLayer.Stroke = new SolidColorBrush(decorationColor);
            
            // 5. 字体设置
            if (!string.IsNullOrEmpty(config.PieceFontFamily))
            {
                try
                {
                    PieceText.FontFamily = new FontFamily(config.PieceFontFamily);
                }
                catch
                {
                    PieceText.FontFamily = new FontFamily("KaiTi");
                }
            }
            
            // 6. 装饰纹样式（根据棋子类型）
            ApplyDecorationByPieceType(Piece.Type);
        }
        
        /// <summary>
        /// 应用渐变背景
        /// </summary>
        private void ApplyGradientBackground(Color baseColor)
        {
            // 高光色（更亮）
            GradientLight.Color = LightenColor(baseColor, 25);
            
            // 中间色（原色）
            GradientMid.Color = baseColor;
            
            // 暗部色（更暗）
            GradientDark.Color = DarkenColor(baseColor, 15);
        }
        
        /// <summary>
        /// 根据棋子类型应用装饰纹样
        /// </summary>
        private void ApplyDecorationByPieceType(PieceType pieceType)
        {
            string resourceKey = pieceType switch
            {
                // 将/帅/车：使用回纹装饰
                PieceType.King or PieceType.Rook => "MeanderRing",
                
                // 象/士/马：使用云纹装饰
                PieceType.Bishop or PieceType.Advisor or PieceType.Knight => "CloudSimplified",
                
                // 炮/兵卒：使用简约圈纹
                PieceType.Cannon or PieceType.Pawn => "SimpleRing",
                
                _ => "SimpleRing"
            };
            
            // 应用装饰纹样
            if (Resources[resourceKey] is Geometry geometry)
            {
                DecorationLayer.Data = geometry;
            }
        }
        
        #endregion
        
        #region 动画系统
        
        private void InitializeAnimations()
        {
            // 性能优化：使用静态方法创建动画（减少代码重复）
            // 注意：每个实例仍然需要自己的 Storyboard 实例
            _selectAnimation = CreateSelectAnimation(this);
            _deselectAnimation = CreateDeselectAnimation(this);
            _captureAnimation = CreateCaptureAnimation(this);
            _jumpAnimation = CreateJumpAnimation(this);
        }
        
        /// <summary>
        /// 创建选中动画（静态方法，减少代码重复）
        /// </summary>
        private static Storyboard CreateSelectAnimation(ChessPieceControl control)
        {
            var selectAnimation = new Storyboard();
            
            // 缩放动画（弹跳效果）
            var scaleX = new DoubleAnimationUsingKeyFrames();
            scaleX.KeyFrames.Add(new EasingDoubleKeyFrame(1.0, KeyTime.FromTimeSpan(TimeSpan.Zero)));
            scaleX.KeyFrames.Add(new EasingDoubleKeyFrame(1.12, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(100))) 
                { EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }});
            scaleX.KeyFrames.Add(new EasingDoubleKeyFrame(1.06, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(200))));
            scaleX.KeyFrames.Add(new EasingDoubleKeyFrame(1.1, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(300))));
            scaleX.KeyFrames.Add(new EasingDoubleKeyFrame(1.08, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(400))));
            Storyboard.SetTarget(scaleX, control.ScaleTransform);
            Storyboard.SetTargetProperty(scaleX, new PropertyPath(ScaleTransform.ScaleXProperty));
            
            var scaleY = new DoubleAnimationUsingKeyFrames();
            scaleY.KeyFrames.Add(new EasingDoubleKeyFrame(1.0, KeyTime.FromTimeSpan(TimeSpan.Zero)));
            scaleY.KeyFrames.Add(new EasingDoubleKeyFrame(1.12, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(100))) 
                { EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }});
            scaleY.KeyFrames.Add(new EasingDoubleKeyFrame(1.06, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(200))));
            scaleY.KeyFrames.Add(new EasingDoubleKeyFrame(1.1, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(300))));
            scaleY.KeyFrames.Add(new EasingDoubleKeyFrame(1.08, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(400))));
            Storyboard.SetTarget(scaleY, control.ScaleTransform);
            Storyboard.SetTargetProperty(scaleY, new PropertyPath(ScaleTransform.ScaleYProperty));
            
            // 高光脉冲效果
            var highlightPulse = new DoubleAnimationUsingKeyFrames();
            highlightPulse.KeyFrames.Add(new EasingDoubleKeyFrame(0.3, KeyTime.FromTimeSpan(TimeSpan.Zero)));
            highlightPulse.KeyFrames.Add(new EasingDoubleKeyFrame(0.6, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(250))));
            highlightPulse.KeyFrames.Add(new EasingDoubleKeyFrame(0.3, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(500))));
            highlightPulse.KeyFrames.Add(new EasingDoubleKeyFrame(0.5, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(750))));
            highlightPulse.KeyFrames.Add(new EasingDoubleKeyFrame(0.3, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(1000))));
            Storyboard.SetTarget(highlightPulse, control.HighlightLayer);
            Storyboard.SetTargetProperty(highlightPulse, new PropertyPath(OpacityProperty));
            
            selectAnimation.Children.Add(scaleX);
            selectAnimation.Children.Add(scaleY);
            selectAnimation.Children.Add(highlightPulse);
            selectAnimation.RepeatBehavior = RepeatBehavior.Forever;
            
            return selectAnimation;
        }
        
        /// <summary>
        /// 创建取消选中动画（静态方法，减少代码重复）
        /// </summary>
        private static Storyboard CreateDeselectAnimation(ChessPieceControl control)
        {
            var deselectAnimation = new Storyboard();
            
            var resetScaleX = new DoubleAnimation(1.0, TimeSpan.FromMilliseconds(150))
            {
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
            };
            Storyboard.SetTarget(resetScaleX, control.ScaleTransform);
            Storyboard.SetTargetProperty(resetScaleX, new PropertyPath(ScaleTransform.ScaleXProperty));
            
            var resetScaleY = new DoubleAnimation(1.0, TimeSpan.FromMilliseconds(150))
            {
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
            };
            Storyboard.SetTarget(resetScaleY, control.ScaleTransform);
            Storyboard.SetTargetProperty(resetScaleY, new PropertyPath(ScaleTransform.ScaleYProperty));
            
            var resetHighlight = new DoubleAnimation(0.3, TimeSpan.FromMilliseconds(150));
            Storyboard.SetTarget(resetHighlight, control.HighlightLayer);
            Storyboard.SetTargetProperty(resetHighlight, new PropertyPath(OpacityProperty));
            
            deselectAnimation.Children.Add(resetScaleX);
            deselectAnimation.Children.Add(resetScaleY);
            deselectAnimation.Children.Add(resetHighlight);
            
            return deselectAnimation;
        }
        
        /// <summary>
        /// 创建吃子动画（静态方法，减少代码重复）
        /// </summary>
        private static Storyboard CreateCaptureAnimation(ChessPieceControl control)
        {
            var captureAnimation = new Storyboard();
            
            // 震动效果
            var shake = new DoubleAnimationUsingKeyFrames();
            shake.KeyFrames.Add(new DiscreteDoubleKeyFrame(0, KeyTime.FromTimeSpan(TimeSpan.Zero)));
            shake.KeyFrames.Add(new DiscreteDoubleKeyFrame(5, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(40))));
            shake.KeyFrames.Add(new DiscreteDoubleKeyFrame(-5, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(80))));
            shake.KeyFrames.Add(new DiscreteDoubleKeyFrame(4, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(120))));
            shake.KeyFrames.Add(new DiscreteDoubleKeyFrame(-4, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(160))));
            shake.KeyFrames.Add(new DiscreteDoubleKeyFrame(3, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(200))));
            shake.KeyFrames.Add(new DiscreteDoubleKeyFrame(-2, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(240))));
            shake.KeyFrames.Add(new DiscreteDoubleKeyFrame(0, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(280))));
            Storyboard.SetTarget(shake, control.TranslateTransform);
            Storyboard.SetTargetProperty(shake, new PropertyPath(TranslateTransform.XProperty));
            
            // 淡出
            var fadeOut = new DoubleAnimation(1.0, 0.0, TimeSpan.FromMilliseconds(250))
            {
                BeginTime = TimeSpan.FromMilliseconds(200)
            };
            Storyboard.SetTarget(fadeOut, control.PieceRoot);
            Storyboard.SetTargetProperty(fadeOut, new PropertyPath(OpacityProperty));
            
            // 缩小
            var shrink = new DoubleAnimation(1.0, 0.3, TimeSpan.FromMilliseconds(250))
            {
                BeginTime = TimeSpan.FromMilliseconds(200),
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn }
            };
            Storyboard.SetTarget(shrink, control.ScaleTransform);
            Storyboard.SetTargetProperty(shrink, new PropertyPath(ScaleTransform.ScaleXProperty));
            
            var shrinkY = new DoubleAnimation(1.0, 0.3, TimeSpan.FromMilliseconds(250))
            {
                BeginTime = TimeSpan.FromMilliseconds(200),
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn }
            };
            Storyboard.SetTarget(shrinkY, control.ScaleTransform);
            Storyboard.SetTargetProperty(shrinkY, new PropertyPath(ScaleTransform.ScaleYProperty));
            
            captureAnimation.Children.Add(shake);
            captureAnimation.Children.Add(fadeOut);
            captureAnimation.Children.Add(shrink);
            captureAnimation.Children.Add(shrinkY);
            
            return captureAnimation;
        }
        
        /// <summary>
        /// 创建落子跳动动画（静态方法，减少代码重复）
        /// </summary>
        private static Storyboard CreateJumpAnimation(ChessPieceControl control)
        {
            var jumpAnimation = new Storyboard();
            
            var jumpScale = new DoubleAnimationUsingKeyFrames();
            jumpScale.KeyFrames.Add(new EasingDoubleKeyFrame(1.0, KeyTime.FromTimeSpan(TimeSpan.Zero)));
            jumpScale.KeyFrames.Add(new EasingDoubleKeyFrame(1.15, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(80))));
            jumpScale.KeyFrames.Add(new EasingDoubleKeyFrame(0.95, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(160))));
            jumpScale.KeyFrames.Add(new EasingDoubleKeyFrame(1.05, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(240))));
            jumpScale.KeyFrames.Add(new EasingDoubleKeyFrame(1.0, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(320))));
            Storyboard.SetTarget(jumpScale, control.ScaleTransform);
            Storyboard.SetTargetProperty(jumpScale, new PropertyPath(ScaleTransform.ScaleXProperty));
            
            var jumpScaleY = new DoubleAnimationUsingKeyFrames();
            jumpScaleY.KeyFrames.Add(new EasingDoubleKeyFrame(1.0, KeyTime.FromTimeSpan(TimeSpan.Zero)));
            jumpScaleY.KeyFrames.Add(new EasingDoubleKeyFrame(1.15, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(80))));
            jumpScaleY.KeyFrames.Add(new EasingDoubleKeyFrame(0.95, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(160))));
            jumpScaleY.KeyFrames.Add(new EasingDoubleKeyFrame(1.05, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(240))));
            jumpScaleY.KeyFrames.Add(new EasingDoubleKeyFrame(1.0, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(320))));
            Storyboard.SetTarget(jumpScaleY, control.ScaleTransform);
            Storyboard.SetTargetProperty(jumpScaleY, new PropertyPath(ScaleTransform.ScaleYProperty));
            
            jumpAnimation.Children.Add(jumpScale);
            jumpAnimation.Children.Add(jumpScaleY);
            
            return jumpAnimation;
        }
        
        #endregion
        
        private static void OnSelectionChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ChessPieceControl control)
            {
                if ((bool)e.NewValue)
                    control.PlaySelectAnimation();
                else
                    control.PlayDeselectAnimation();
            }
        }
        
        private static void OnAnimationStateChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ChessPieceControl control)
            {
                control.PlayAnimation((PieceAnimationState)e.NewValue);
            }
        }
        
        /// <summary>播放选中动画</summary>
        public void PlaySelectAnimation()
        {
            if (_isAnimating) return;
            _selectAnimation?.Begin(this, true);
        }
        
        /// <summary>播放取消选中动画</summary>
        public void PlayDeselectAnimation()
        {
            _selectAnimation?.Stop(this);
            _deselectAnimation?.Begin(this);
        }
        
        /// <summary>播放落子动画（跳动效果）</summary>
        public void PlayLandAnimation()
        {
            _jumpAnimation?.Begin(this);
        }
        
        /// <summary>播放吃子动画</summary>
        public void PlayCaptureAnimation(Action? onComplete = null)
        {
            if (_captureAnimation == null) return;
            
            if (onComplete != null)
            {
                _captureAnimation.Completed += (s, e) => onComplete();
            }
            _captureAnimation.Begin(this);
        }
        
        /// <summary>播放移动动画</summary>
        public void PlayMoveAnimation(Point from, Point to, Action? onComplete = null)
        {
            // 计算移动距离和时间
            double distance = Math.Sqrt(Math.Pow(to.X - from.X, 2) + Math.Pow(to.Y - from.Y, 2));
            TimeSpan duration = TimeSpan.FromMilliseconds(Math.Min(400, 150 + distance * 0.6));
            
            // 创建位移动画
            var moveStoryboard = new Storyboard();
            
            var translateX = new DoubleAnimation(from.X, to.X, duration)
            {
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };
            Storyboard.SetTarget(translateX, PieceRoot);
            Storyboard.SetTargetProperty(translateX, new PropertyPath("(Canvas.Left)"));
            
            var translateY = new DoubleAnimation(from.Y, to.Y, duration)
            {
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };
            Storyboard.SetTarget(translateY, PieceRoot);
            Storyboard.SetTargetProperty(translateY, new PropertyPath("(Canvas.Top)"));
            
            // 缩放效果（模拟抛物线高度）
            var scale = new DoubleAnimationUsingKeyFrames();
            double jumpHeight = Math.Min(0.2, distance * 0.001);
            scale.KeyFrames.Add(new EasingDoubleKeyFrame(1.0, KeyTime.FromTimeSpan(TimeSpan.Zero)));
            scale.KeyFrames.Add(new EasingDoubleKeyFrame(1.0 + jumpHeight, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(duration.TotalMilliseconds / 2))));
            scale.KeyFrames.Add(new EasingDoubleKeyFrame(1.0, KeyTime.FromTimeSpan(duration)));
            Storyboard.SetTarget(scale, ScaleTransform);
            Storyboard.SetTargetProperty(scale, new PropertyPath(ScaleTransform.ScaleXProperty));
            
            moveStoryboard.Children.Add(translateX);
            moveStoryboard.Children.Add(translateY);
            moveStoryboard.Children.Add(scale);
            
            if (onComplete != null)
            {
                moveStoryboard.Completed += (s, e) => onComplete();
            }
            
            moveStoryboard.Begin();
        }
        
        /// <summary>播放指定状态的动画</summary>
        public void PlayAnimation(PieceAnimationState state)
        {
            switch (state)
            {
                case PieceAnimationState.Selected:
                    PlaySelectAnimation();
                    break;
                case PieceAnimationState.Deselected:
                    PlayDeselectAnimation();
                    break;
                case PieceAnimationState.Landing:
                    PlayLandAnimation();
                    break;
                case PieceAnimationState.Captured:
                    PlayCaptureAnimation();
                    break;
                case PieceAnimationState.Idle:
                default:
                    // 重置状态
                    _selectAnimation?.Stop(this);
                    ScaleTransform.ScaleX = 1.0;
                    ScaleTransform.ScaleY = 1.0;
                    HighlightLayer.Opacity = 0.3;
                    break;
            }
        }
        
        /// <summary>重置动画状态</summary>
        public void ResetAnimation()
        {
            _selectAnimation?.Stop(this);
            _captureAnimation?.Stop(this);
            _jumpAnimation?.Stop(this);
            
            ScaleTransform.ScaleX = 1.0;
            ScaleTransform.ScaleY = 1.0;
            TranslateTransform.X = 0;
            TranslateTransform.Y = 0;
            Opacity = 1.0;
            HighlightLayer.Opacity = 0.3;
        }

        #region 颜色辅助方法
        
        /// <summary>安全解析颜色字符串（带异常处理）</summary>
        /// <param name="colorString">颜色字符串</param>
        /// <param name="fallback">回退颜色字符串</param>
        /// <returns>解析后的颜色或回退颜色</returns>
        private static Color SafeParseColor(string colorString, string fallback)
        {
            if (string.IsNullOrWhiteSpace(colorString))
            {
                Debug.WriteLine($"[ChessPieceControl] 颜色字符串为空，使用默认值：{fallback}");
                return SafeParseColor(fallback, "#808080"); // 如果 fallback 也无效，使用灰色
            }

            try
            {
                return (Color)ColorConverter.ConvertFromString(colorString);
            }
            catch (FormatException)
            {
                Debug.WriteLine($"[ChessPieceControl] 颜色格式错误：{colorString}，使用默认值：{fallback}");
                return SafeParseColor(fallback, "#808080");
            }
            catch (ArgumentException)
            {
                Debug.WriteLine($"[ChessPieceControl] 颜色参数错误：{colorString}，使用默认值：{fallback}");
                return SafeParseColor(fallback, "#808080");
            }
        }
        
        /// <summary>调亮颜色</summary>
        private static Color LightenColor(Color color, int amount)
        {
            return Color.FromArgb(
                color.A,
                (byte)Math.Min(255, color.R + amount),
                (byte)Math.Min(255, color.G + amount),
                (byte)Math.Min(255, color.B + amount)
            );
        }
        
        /// <summary>调暗颜色</summary>
        private static Color DarkenColor(Color color, int amount)
        {
            return Color.FromArgb(
                color.A,
                (byte)Math.Max(0, color.R - amount),
                (byte)Math.Max(0, color.G - amount),
                (byte)Math.Max(0, color.B - amount)
            );
        }
        
        #endregion
    }
    
    /// <summary>
    /// 棋子动画状态枚举
    /// </summary>
    public enum PieceAnimationState
    {
        /// <summary>空闲</summary>
        Idle,
        
        /// <summary>选中</summary>
        Selected,
        
        /// <summary>取消选中</summary>
        Deselected,
        
        /// <summary>移动中</summary>
        Moving,
        
        /// <summary>落子</summary>
        Landing,
        
        /// <summary>被吃</summary>
        Captured,
        
        /// <summary>拖拽中</summary>
        Dragging
    }
}
