using System;
using System.Drawing;

namespace gInk
{
	public enum UIThemeId
	{
		ObsidianDark = 0,
		WindowsNative = 1,
		MidnightNavy = 2,
		GraphiteSlate = 3
	}

	public class UITheme
	{
		public UIThemeId Id { get; set; }
		public string Name { get; set; }
		public bool IsDark { get; set; }

		// Bottom Toolbar Palette
		public Color ToolbarBg { get; set; }
		public Color ToolbarBorder { get; set; }
		public Color SpecularRim { get; set; }
		public Color ButtonHoverBg { get; set; }
		public Color ButtonDownBg { get; set; }
		public Color PenNormalBorder { get; set; }
		public Color PenActiveBorder { get; set; }
		public Color PenWidthPanelBg { get; set; }
		public Color PenWidthTrackColor { get; set; }
		public Color PenWidthIndicatorColor { get; set; }

		// Radial Menu Palette
		public Color RadialChassisBg { get; set; }
		public Color RadialChassisBorder { get; set; }
		public Color RadialAmbientGlow { get; set; }
		public Color PillNormalBg { get; set; }
		public Color PillNormalBorder { get; set; }
		public Color PillNormalRim { get; set; }
		public Color PillHoverBg { get; set; }
		public Color PillHoverBorder { get; set; }
		public Color PillHoverGlow { get; set; }
		public Color CenterHubBg { get; set; }
		public Color CenterHubBorder { get; set; }
		public Color CenterHubHoverBg { get; set; }
		public Color CenterHubHoverBorder { get; set; }
		public Color CenterTitleColor { get; set; }
		public Color CenterSubAccent { get; set; }
		public Color CenterSubNeutral { get; set; }
		public Color ActiveToolIndicatorColor { get; set; }
		public Color ActiveToolGlowColor { get; set; }
		public Color OrbitalGuideTrackColor { get; set; }

		// Vector Icon Palette
		public Color IconInactive { get; set; }
		public Color IconActive { get; set; }
		public Color IconDanger { get; set; }

		public static readonly UITheme[] Themes = new UITheme[]
		{
			// 0: Obsidian Dark (Default unified dark frosted style)
			new UITheme
			{
				Id = UIThemeId.ObsidianDark,
				Name = "Obsidian Dark",
				IsDark = true,

				ToolbarBg = Color.FromArgb(20, 26, 36),
				ToolbarBorder = Color.FromArgb(50, 255, 255, 255),
				SpecularRim = Color.FromArgb(40, 255, 255, 255),
				ButtonHoverBg = Color.FromArgb(36, 48, 66),
				ButtonDownBg = Color.FromArgb(28, 38, 54),
				PenNormalBorder = Color.FromArgb(70, 255, 255, 255),
				PenActiveBorder = Color.FromArgb(255, 255, 255),
				PenWidthPanelBg = Color.FromArgb(20, 26, 36),
				PenWidthTrackColor = Color.FromArgb(60, 255, 255, 255),
				PenWidthIndicatorColor = Color.FromArgb(82, 216, 177),

				RadialChassisBg = Color.FromArgb(140, 12, 16, 24),
				RadialChassisBorder = Color.FromArgb(32, 255, 255, 255),
				RadialAmbientGlow = Color.FromArgb(16, 0, 240, 160),
				PillNormalBg = Color.FromArgb(220, 20, 26, 36),
				PillNormalBorder = Color.FromArgb(45, 255, 255, 255),
				PillNormalRim = Color.FromArgb(25, 255, 255, 255),
				PillHoverBg = Color.FromArgb(235, 8, 52, 44),
				PillHoverBorder = Color.FromArgb(255, 0, 245, 175),
				PillHoverGlow = Color.FromArgb(40, 0, 240, 170),
				CenterHubBg = Color.FromArgb(245, 15, 20, 28),
				CenterHubBorder = Color.FromArgb(190, 0, 230, 160),
				CenterHubHoverBg = Color.FromArgb(248, 16, 44, 38),
				CenterHubHoverBorder = Color.FromArgb(255, 0, 255, 180),
				CenterTitleColor = Color.White,
				CenterSubAccent = Color.FromArgb(0, 240, 170),
				CenterSubNeutral = Color.FromArgb(145, 170, 190),
				ActiveToolIndicatorColor = Color.FromArgb(255, 0, 255, 180),
				ActiveToolGlowColor = Color.FromArgb(80, 0, 240, 170),
				OrbitalGuideTrackColor = Color.FromArgb(40, 255, 255, 255),

				IconInactive = Color.FromArgb(216, 222, 230),
				IconActive = Color.FromArgb(82, 216, 177),
				IconDanger = Color.FromArgb(240, 105, 115)
			},

			// 1: Windows Native (Windows 11 Fluent Light desktop style)
			new UITheme
			{
				Id = UIThemeId.WindowsNative,
				Name = "Windows Native",
				IsDark = false,

				ToolbarBg = Color.FromArgb(246, 248, 251),
				ToolbarBorder = Color.FromArgb(218, 222, 230),
				SpecularRim = Color.FromArgb(120, 255, 255, 255),
				ButtonHoverBg = Color.FromArgb(232, 240, 250),
				ButtonDownBg = Color.FromArgb(215, 228, 245),
				PenNormalBorder = Color.FromArgb(210, 215, 225),
				PenActiveBorder = Color.FromArgb(0, 103, 192),
				PenWidthPanelBg = Color.FromArgb(246, 248, 251),
				PenWidthTrackColor = Color.FromArgb(190, 198, 210),
				PenWidthIndicatorColor = Color.FromArgb(0, 103, 192),

				RadialChassisBg = Color.FromArgb(215, 240, 243, 248),
				RadialChassisBorder = Color.FromArgb(210, 216, 226),
				RadialAmbientGlow = Color.FromArgb(18, 0, 103, 192),
				PillNormalBg = Color.FromArgb(245, 255, 255, 255),
				PillNormalBorder = Color.FromArgb(215, 220, 230),
				PillNormalRim = Color.FromArgb(160, 255, 255, 255),
				PillHoverBg = Color.FromArgb(250, 234, 242, 253),
				PillHoverBorder = Color.FromArgb(0, 103, 192),
				PillHoverGlow = Color.FromArgb(40, 0, 103, 192),
				CenterHubBg = Color.FromArgb(250, 255, 255, 255),
				CenterHubBorder = Color.FromArgb(0, 103, 192),
				CenterHubHoverBg = Color.FromArgb(250, 234, 242, 253),
				CenterHubHoverBorder = Color.FromArgb(0, 90, 175),
				CenterTitleColor = Color.FromArgb(28, 28, 28),
				CenterSubAccent = Color.FromArgb(0, 103, 192),
				CenterSubNeutral = Color.FromArgb(110, 115, 125),
				ActiveToolIndicatorColor = Color.FromArgb(0, 103, 192),
				ActiveToolGlowColor = Color.FromArgb(60, 0, 103, 192),
				OrbitalGuideTrackColor = Color.FromArgb(160, 180, 198),

				IconInactive = Color.FromArgb(42, 45, 52),
				IconActive = Color.FromArgb(0, 103, 192),
				IconDanger = Color.FromArgb(218, 59, 1)
			},

			// 2: Midnight Navy (Deep Nordic ocean navy with ice cyan glow)
			new UITheme
			{
				Id = UIThemeId.MidnightNavy,
				Name = "Midnight Navy",
				IsDark = true,

				ToolbarBg = Color.FromArgb(18, 26, 48),
				ToolbarBorder = Color.FromArgb(50, 80, 130),
				SpecularRim = Color.FromArgb(50, 140, 210, 255),
				ButtonHoverBg = Color.FromArgb(28, 44, 76),
				ButtonDownBg = Color.FromArgb(36, 56, 96),
				PenNormalBorder = Color.FromArgb(60, 95, 150),
				PenActiveBorder = Color.FromArgb(80, 210, 255),
				PenWidthPanelBg = Color.FromArgb(18, 26, 48),
				PenWidthTrackColor = Color.FromArgb(50, 80, 130),
				PenWidthIndicatorColor = Color.FromArgb(80, 210, 255),

				RadialChassisBg = Color.FromArgb(180, 12, 18, 34),
				RadialChassisBorder = Color.FromArgb(60, 90, 145),
				RadialAmbientGlow = Color.FromArgb(25, 80, 190, 255),
				PillNormalBg = Color.FromArgb(230, 20, 30, 52),
				PillNormalBorder = Color.FromArgb(55, 85, 135),
				PillNormalRim = Color.FromArgb(40, 120, 190, 255),
				PillHoverBg = Color.FromArgb(245, 18, 55, 95),
				PillHoverBorder = Color.FromArgb(80, 210, 255),
				PillHoverGlow = Color.FromArgb(50, 80, 210, 255),
				CenterHubBg = Color.FromArgb(250, 16, 24, 44),
				CenterHubBorder = Color.FromArgb(80, 210, 255),
				CenterHubHoverBg = Color.FromArgb(250, 22, 45, 78),
				CenterHubHoverBorder = Color.FromArgb(120, 230, 255),
				CenterTitleColor = Color.White,
				CenterSubAccent = Color.FromArgb(80, 210, 255),
				CenterSubNeutral = Color.FromArgb(135, 165, 195),
				ActiveToolIndicatorColor = Color.FromArgb(80, 210, 255),
				ActiveToolGlowColor = Color.FromArgb(70, 80, 210, 255),
				OrbitalGuideTrackColor = Color.FromArgb(65, 120, 190),

				IconInactive = Color.FromArgb(168, 194, 224),
				IconActive = Color.FromArgb(80, 210, 255),
				IconDanger = Color.FromArgb(245, 95, 115)
			},

			// 3: Graphite Slate (Professional neutral matte charcoal dark / titanium)
			new UITheme
			{
				Id = UIThemeId.GraphiteSlate,
				Name = "Graphite Slate",
				IsDark = true,

				ToolbarBg = Color.FromArgb(28, 30, 34),
				ToolbarBorder = Color.FromArgb(58, 63, 74),
				SpecularRim = Color.FromArgb(40, 255, 255, 255),
				ButtonHoverBg = Color.FromArgb(44, 48, 56),
				ButtonDownBg = Color.FromArgb(36, 40, 48),
				PenNormalBorder = Color.FromArgb(70, 75, 88),
				PenActiveBorder = Color.FromArgb(240, 245, 255),
				PenWidthPanelBg = Color.FromArgb(28, 30, 34),
				PenWidthTrackColor = Color.FromArgb(60, 65, 76),
				PenWidthIndicatorColor = Color.FromArgb(226, 232, 240),

				RadialChassisBg = Color.FromArgb(180, 22, 24, 28),
				RadialChassisBorder = Color.FromArgb(50, 55, 65),
				RadialAmbientGlow = Color.FromArgb(15, 255, 255, 255),
				PillNormalBg = Color.FromArgb(235, 32, 35, 42),
				PillNormalBorder = Color.FromArgb(58, 63, 74),
				PillNormalRim = Color.FromArgb(25, 255, 255, 255),
				PillHoverBg = Color.FromArgb(245, 48, 54, 65),
				PillHoverBorder = Color.FromArgb(226, 232, 240),
				PillHoverGlow = Color.FromArgb(35, 255, 255, 255),
				CenterHubBg = Color.FromArgb(250, 25, 27, 32),
				CenterHubBorder = Color.FromArgb(180, 190, 205),
				CenterHubHoverBg = Color.FromArgb(250, 38, 42, 50),
				CenterHubHoverBorder = Color.FromArgb(240, 245, 255),
				CenterTitleColor = Color.White,
				CenterSubAccent = Color.FromArgb(226, 232, 240),
				CenterSubNeutral = Color.FromArgb(150, 158, 172),
				ActiveToolIndicatorColor = Color.FromArgb(240, 245, 255),
				ActiveToolGlowColor = Color.FromArgb(60, 255, 255, 255),
				OrbitalGuideTrackColor = Color.FromArgb(65, 70, 82),

				IconInactive = Color.FromArgb(200, 208, 220),
				IconActive = Color.FromArgb(245, 248, 255),
				IconDanger = Color.FromArgb(240, 100, 110)
			}
		};

		public static UITheme Get(int index)
		{
			if (index < 0 || index >= Themes.Length)
				return Themes[0];
			return Themes[index];
		}

		public static UITheme Get(UIThemeId id)
		{
			return Get((int)id);
		}
	}
}
