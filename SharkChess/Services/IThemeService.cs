using SharkChess.Models;

namespace SharkChess.Services
{
    /// <summary>
    /// 主题服务接口
    /// </summary>
    public interface IThemeService
    {
        /// <summary>
        /// 当前主题
        /// </summary>
        BoardTheme CurrentTheme { get; }

        /// <summary>
        /// 保存的主题（启动时自动加载）
        /// </summary>
        BoardTheme SavedTheme { get; }

        /// <summary>
        /// 主题变更事件
        /// </summary>
        event EventHandler<ThemeChangedEventArgs>? ThemeChanged;

        /// <summary>
        /// 设置主题
        /// </summary>
        void SetTheme(BoardTheme theme);

        /// <summary>
        /// 获取主题配置
        /// </summary>
        ThemeConfig GetThemeConfig(BoardTheme theme);

        /// <summary>
        /// 获取当前主题配置
        /// </summary>
        ThemeConfig GetCurrentConfig();

        /// <summary>
        /// 保存当前主题到配置文件
        /// </summary>
        void SaveCurrentTheme();

        /// <summary>
        /// 从配置文件加载保存的主题
        /// </summary>
        void LoadSavedTheme();
    }

    /// <summary>
    /// 主题配置
    /// </summary>
    public class ThemeConfig
    {
        /// <summary>
        /// 主题名称
        /// </summary>
        public string Name { get; init; } = "";

        /// <summary>
        /// 棋盘背景色
        /// </summary>
        public string BoardBackground { get; init; } = "#DEB887";

        /// <summary>
        /// 棋盘线条颜色
        /// </summary>
        public string LineColor { get; init; } = "#8B4513";

        /// <summary>
        /// 线条粗细
        /// </summary>
        public double LineThickness { get; init; } = 1.5;

        /// <summary>
        /// 楚河汉界文字颜色
        /// </summary>
        public string RiverTextColor { get; init; } = "#8B4513";

        /// <summary>
        /// 楚河汉界字体大小
        /// </summary>
        public double RiverTextSize { get; init; } = 24;

        /// <summary>
        /// 坐标文字颜色
        /// </summary>
        public string CoordinateColor { get; init; } = "#8B4513";

        /// <summary>
        /// 是否显示坐标
        /// </summary>
        public bool ShowCoordinates { get; init; } = true;

        /// <summary>
        /// 九宫格斜线颜色
        /// </summary>
        public string PalaceLineColor { get; init; } = "#8B4513";

        /// <summary>
        /// 选中格子高亮色
        /// </summary>
        public string SelectedHighlight { get; init; } = "#FF6B6B";

        /// <summary>
        /// 合法走法提示色
        /// </summary>
        public string LegalMoveHint { get; init; } = "#90EE90";

        /// <summary>
        /// 红方棋子背景色
        /// </summary>
        public string RedPieceBackground { get; init; } = "#FFF5EE";

        /// <summary>
        /// 红方棋子边框色
        /// </summary>
        public string RedPieceBorder { get; init; } = "#DC143C";

        /// <summary>
        /// 红方棋子文字颜色
        /// </summary>
        public string RedPieceText { get; init; } = "#DC143C";

        /// <summary>
        /// 黑方棋子背景色
        /// </summary>
        public string BlackPieceBackground { get; init; } = "#FFF5EE";

        /// <summary>
        /// 黑方棋子边框色
        /// </summary>
        public string BlackPieceBorder { get; init; } = "#000000";

        /// <summary>
        /// 黑方棋子文字颜色
        /// </summary>
        public string BlackPieceText { get; init; } = "#000000";

        /// <summary>
        /// 棋子字体大小
        /// </summary>
        public double PieceFontSize { get; init; } = 28;

        /// <summary>
        /// 特殊区域高亮（教学版用）
        /// </summary>
        public bool HighlightSpecialAreas { get; init; } = false;

        /// <summary>
        /// 落子点标记颜色
        /// </summary>
        public string IntersectionMarkColor { get; init; } = "#888888";

        /// <summary>
        /// 是否显示边缘路数/线数标注
        /// </summary>
        public bool ShowEdgeCoordinates { get; init; } = false;

        /// <summary>
        /// 是否高亮楚河汉界区域
        /// </summary>
        public bool HighlightRiverArea { get; init; } = false;

        /// <summary>
        /// 是否高亮九宫格区域
        /// </summary>
        public bool HighlightPalaceArea { get; init; } = false;

        /// <summary>
        /// 楚河汉界区域高亮色
        /// </summary>
        public string RiverAreaHighlightColor { get; init; } = "#87CEEB40";

        /// <summary>
        /// 九宫格区域高亮色
        /// </summary>
        public string PalaceAreaHighlightColor { get; init; } = "#FFE4B580";

        /// <summary>
        /// 楚河汉界字体名称
        /// </summary>
        public string RiverFontFamily { get; init; } = "KaiTi";

        /// <summary>
        /// 是否显示宣纸纹理效果
        /// </summary>
        public bool ShowPaperTexture { get; init; } = false;

        /// <summary>
        /// 是否显示所有落子点圆点标记
        /// </summary>
        public bool ShowIntersectionDots { get; init; } = false;

        /// <summary>
        /// 落子点圆点颜色
        /// </summary>
        public string IntersectionDotColor { get; init; } = "#CCCCCC";

        // ========== 棋子装饰配置（新增） ==========

        /// <summary>
        /// 棋子装饰纹颜色
        /// </summary>
        public string DecorationColor { get; init; } = "#B8860B";

        /// <summary>
        /// 棋子外圈宽度
        /// </summary>
        public double OuterRingThickness { get; init; } = 3.0;

        /// <summary>
        /// 棋子内圈宽度
        /// </summary>
        public double InnerRingThickness { get; init; } = 1.0;

        /// <summary>
        /// 棋子阴影深度
        /// </summary>
        public double ShadowDepth { get; init; } = 4.0;

        /// <summary>
        /// 棋子高光强度 (0-1)
        /// </summary>
        public double HighlightIntensity { get; init; } = 0.3;

        /// <summary>
        /// 棋子字体（支持书法字体如华文行楷）
        /// </summary>
        public string PieceFontFamily { get; init; } = "KaiTi";

        /// <summary>
        /// 选中光晕颜色
        /// </summary>
        public string SelectionGlowColor { get; init; } = "#FFD700";

        /// <summary>
        /// 是否启用棋子阴影
        /// </summary>
        public bool EnablePieceShadow { get; init; } = true;

        /// <summary>
        /// 是否启用棋子高光
        /// </summary>
        public bool EnablePieceHighlight { get; init; } = true;

        /// <summary>
        /// 是否启用棋子装饰纹
        /// </summary>
        public bool EnablePieceDecoration { get; init; } = true;
    }

    /// <summary>
    /// 主题变更事件参数
    /// </summary>
    public class ThemeChangedEventArgs : EventArgs
    {
        public BoardTheme NewTheme { get; init; }
        public ThemeConfig Config { get; init; } = new ThemeConfig();
    }
}
