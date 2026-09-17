using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace gInk
{
	public enum ModernIconType
	{
		Pen,
		Highlighter,
		Eraser,
		Pan,
		Pointer,
		Snapshot,
		Undo,
		Clear,
		Visible,
		VisibleNot,
		PenWidth,
		Dock,
		DockBack,
		Exit,
		Color
	}

	public static class ModernIcons
	{
		// --------------------------------------------------------------------
		// Palette
		// --------------------------------------------------------------------

		public static Color ColorInactive = Color.FromArgb(216, 222, 230);
		public static Color ColorActive   = Color.FromArgb(82, 216, 177);
		public static Color ColorDanger   = Color.FromArgb(240, 105, 115);

		public static void SetPalette(Color inactive, Color active, Color danger)
		{
			ColorInactive = inactive;
			ColorActive = active;
			ColorDanger = danger;
		}

		private const float IconGrid = 24f;
		private const float StrokeWidth = 1.75f;

		// --------------------------------------------------------------------
		// Public API
		// --------------------------------------------------------------------

		public static Bitmap CreateIcon(
			ModernIconType type,
			int width,
			int height,
			bool active,
			Color? customColor = null,
			float scaleRatio = 0.48f)
		{
			if (width <= 0)
				width = 24;

			if (height <= 0)
				height = 24;

			Bitmap bmp = new Bitmap(
				width,
				height,
				PixelFormat.Format32bppArgb);

			using (Graphics g = Graphics.FromImage(bmp))
			{
				g.SmoothingMode = SmoothingMode.AntiAlias;
				g.PixelOffsetMode = PixelOffsetMode.HighQuality;
				g.InterpolationMode = InterpolationMode.HighQualityBicubic;
				g.CompositingQuality = CompositingQuality.HighQuality;

				g.Clear(Color.Transparent);

				float cx = width / 2f;
				float cy = height / 2f;
				float size = Math.Min(width, height) * scaleRatio;

				DrawIcon(g, type, cx, cy, size, active, customColor);
			}

			return bmp;
		}

		public static void DrawIcon(
			Graphics g,
			ModernIconType type,
			float cx,
			float cy,
			float size,
			bool active,
			Color? customColor = null)
		{
			switch (type)
			{
				case ModernIconType.Pen:
					DrawPen(g, cx, cy, size, active, customColor);
					break;

				case ModernIconType.Highlighter:
					DrawHighlighter(g, cx, cy, size, active, customColor);
					break;

				case ModernIconType.Eraser:
					DrawEraser(g, cx, cy, size, active);
					break;

				case ModernIconType.Pan:
					DrawPan(g, cx, cy, size, active);
					break;

				case ModernIconType.Pointer:
					DrawPointer(g, cx, cy, size, active);
					break;

				case ModernIconType.Snapshot:
					DrawSnapshot(g, cx, cy, size, active);
					break;

				case ModernIconType.Undo:
					DrawUndo(g, cx, cy, size, active);
					break;

				case ModernIconType.Clear:
					DrawClear(g, cx, cy, size, active);
					break;

				case ModernIconType.Visible:
					DrawEye(g, cx, cy, size, active, true);
					break;

				case ModernIconType.VisibleNot:
					DrawEye(g, cx, cy, size, active, false);
					break;

				case ModernIconType.PenWidth:
					DrawPenWidth(g, cx, cy, size, active);
					break;

				case ModernIconType.Dock:
					DrawChevron(g, cx, cy, size, active, false);
					break;

				case ModernIconType.DockBack:
					DrawChevron(g, cx, cy, size, active, true);
					break;

				case ModernIconType.Exit:
					DrawExit(g, cx, cy, size, active);
					break;

				case ModernIconType.Color:
					DrawPalette(g, cx, cy, size, active);
					break;
			}
		}

		// --------------------------------------------------------------------
		// Shared helpers
		// --------------------------------------------------------------------

		private static GraphicsState BeginIconSpace(
			Graphics g,
			float cx,
			float cy,
			float size)
		{
			GraphicsState state = g.Save();

			float scale = size / IconGrid;

			g.TranslateTransform(cx, cy);
			g.ScaleTransform(scale, scale);

			return state;
		}

		private static Pen CreateStroke(Color color)
		{
			Pen p = new Pen(color, StrokeWidth)
			{
				StartCap = LineCap.Round,
				EndCap = LineCap.Round,
				LineJoin = LineJoin.Round
			};

			return p;
		}

		private static Color ResolveColor(bool active)
		{
			return active ? ColorActive : ColorInactive;
		}

		private static void DrawCircle(
			Graphics g,
			Pen p,
			float x,
			float y,
			float radius)
		{
			g.DrawEllipse(
				p,
				x - radius,
				y - radius,
				radius * 2f,
				radius * 2f);
		}

		// --------------------------------------------------------------------
		// Pen
		// --------------------------------------------------------------------

		private static void DrawPen(
			Graphics g,
			float cx,
			float cy,
			float size,
			bool active,
			Color? penColor)
		{
			GraphicsState state = BeginIconSpace(g, cx, cy, size);

			Color color = ResolveColor(active);

			using (Pen p = CreateStroke(color))
			{
				using (GraphicsPath path = new GraphicsPath())
				{
					path.StartFigure();

					path.AddLine(-6.5f, 4.0f, 4.4f, -6.9f);

					path.AddBezier(
						4.4f, -6.9f,
						5.7f, -8.2f,
						6.8f, -8.5f,
						8.0f, -7.3f);

					path.AddBezier(
						8.0f, -7.3f,
						9.1f, -6.2f,
						8.8f, -5.1f,
						7.5f, -3.8f);

					path.AddLine(7.5f, -3.8f, -3.5f, 7.2f);
					path.AddLine(-3.5f, 7.2f, -8.2f, 8.6f);
					path.AddLine(-8.2f, 8.6f, -6.5f, 4.0f);

					path.CloseFigure();

					g.DrawPath(p, path);
				}

				// Ferrule
				g.DrawLine(
					p,
					2.6f, -5.1f,
					5.5f, -2.2f);

				// Tip separator
				g.DrawLine(
					p,
					-7.0f, 4.5f,
					-4.1f, 7.4f);

				// Small meaningful color accent.
				if (penColor.HasValue)
				{
					using (SolidBrush brush = new SolidBrush(penColor.Value))
					{
						PointF[] tip =
						{
							new PointF(-8.2f, 8.6f),
							new PointF(-6.9f, 5.0f),
							new PointF(-4.5f, 7.4f)
						};

						g.FillPolygon(brush, tip);
					}
				}
			}

			g.Restore(state);
		}

		// --------------------------------------------------------------------
		// Highlighter
		// --------------------------------------------------------------------

		private static void DrawHighlighter(
			Graphics g,
			float cx,
			float cy,
			float size,
			bool active,
			Color? markerColor)
		{
			GraphicsState state = BeginIconSpace(g, cx, cy, size);

			g.RotateTransform(-45f);

			Color color = ResolveColor(active);

			using (Pen p = CreateStroke(color))
			{
				using (GraphicsPath body = new GraphicsPath())
				{
					body.StartFigure();

					body.AddLine(-5.5f, -7.0f, 4.5f, -7.0f);
					body.AddLine(4.5f, -7.0f, 5.8f, -5.5f);
					body.AddLine(5.8f, -5.5f, 5.8f, 4.5f);
					body.AddLine(5.8f, 4.5f, -5.5f, 4.5f);
					body.CloseFigure();

					g.DrawPath(p, body);
				}

				// Chisel tip
				PointF[] tip =
				{
					new PointF(-5.5f, 4.5f),
					new PointF(5.8f, 4.5f),
					new PointF(4.0f, 7.8f),
					new PointF(-3.7f, 7.8f)
				};

				g.DrawPolygon(p, tip);

				if (markerColor.HasValue)
				{
					using (SolidBrush brush =
						new SolidBrush(markerColor.Value))
					{
						g.FillPolygon(brush, tip);
					}
				}

				// Grip separator
				g.DrawLine(
					p,
					-5.0f, -3.5f,
					5.3f, -3.5f);
			}

			g.Restore(state);
		}

		// --------------------------------------------------------------------
		// Eraser
		// --------------------------------------------------------------------

		private static void DrawEraser(
			Graphics g,
			float cx,
			float cy,
			float size,
			bool active)
		{
			GraphicsState state = BeginIconSpace(g, cx, cy, size);

			Color color = ResolveColor(active);

			using (Pen p = CreateStroke(color))
			{
				// Ground line (horizontal, stable)
				g.DrawLine(p, -7.0f, 7.5f, 7.0f, 7.5f);

				// Angled eraser body
				GraphicsState rotState = g.Save();
				g.TranslateTransform(0f, -1.0f);
				g.RotateTransform(-35f);

				PointF[] body =
				{
					new PointF(-5.5f, -6.0f),
					new PointF(5.5f, -6.0f),
					new PointF(5.5f, 2.0f),
					new PointF(2.5f, 5.0f),
					new PointF(-5.5f, 5.0f),
					new PointF(-5.5f, -6.0f)
				};

				g.DrawLines(p, body);

				// Rubber sleeve separator
				g.DrawLine(
					p,
					-1.0f, -6.0f,
					-1.0f, 5.0f);

				g.Restore(rotState);
			}

			g.Restore(state);
		}

		// --------------------------------------------------------------------
		// Pan
		// --------------------------------------------------------------------

		private static void DrawPan(
			Graphics g,
			float cx,
			float cy,
			float size,
			bool active)
		{
			GraphicsState state = BeginIconSpace(g, cx, cy, size);

			Color color = ResolveColor(active);

			using (Pen p = CreateStroke(color))
			{
				float left = -7f;
				float right = 7f;
				float top = -7f;
				float bottom = 7f;

				// Axis
				g.DrawLine(p, left, 0, right, 0);
				g.DrawLine(p, 0, top, 0, bottom);

				// Left
				g.DrawLines(
					p,
					new[]
					{
						new PointF(-4.8f, -2.0f),
						new PointF(-7.0f, 0f),
						new PointF(-4.8f, 2.0f)
					});

				// Right
				g.DrawLines(
					p,
					new[]
					{
						new PointF(4.8f, -2.0f),
						new PointF(7.0f, 0f),
						new PointF(4.8f, 2.0f)
					});

				// Up
				g.DrawLines(
					p,
					new[]
					{
						new PointF(-2.0f, -4.8f),
						new PointF(0f, -7.0f),
						new PointF(2.0f, -4.8f)
					});

				// Down
				g.DrawLines(
					p,
					new[]
					{
						new PointF(-2.0f, 4.8f),
						new PointF(0f, 7.0f),
						new PointF(2.0f, 4.8f)
					});
			}

			g.Restore(state);
		}

		// --------------------------------------------------------------------
		// Pointer
		// --------------------------------------------------------------------

		private static void DrawPointer(
			Graphics g,
			float cx,
			float cy,
			float size,
			bool active)
		{
			GraphicsState state = BeginIconSpace(g, cx, cy, size);

			Color color = ResolveColor(active);

			using (Pen p = CreateStroke(color))
			{
				// Modern Lucide MousePointer: Clean 45-degree diagonal arrow
				PointF[] cursor =
				{
					new PointF(-7.5f, -7.5f), // Tip
					new PointF(-1.5f, 7.5f),  // Bottom barb
					new PointF(1.0f, 1.0f),   // Inner notch
					new PointF(7.5f, -1.5f),  // Right barb
					new PointF(-7.5f, -7.5f)  // Back to tip
				};

				g.DrawLines(p, cursor);

				// Shaft
				g.DrawLine(p, 1.0f, 1.0f, 7.5f, 7.5f);
			}

			g.Restore(state);
		}

		// --------------------------------------------------------------------
		// Snapshot
		// --------------------------------------------------------------------

		private static void DrawSnapshot(
			Graphics g,
			float cx,
			float cy,
			float size,
			bool active)
		{
			GraphicsState state = BeginIconSpace(g, cx, cy, size);

			Color color = ResolveColor(active);

			using (Pen p = CreateStroke(color))
			{
				using (GraphicsPath camera = new GraphicsPath())
				{
					camera.AddArc(
						-9f, -5.5f,
						3f, 3f,
						180f, 90f);

					camera.AddLine(
						-6f, -5.5f,
						-3.5f, -7f);

					camera.AddLine(
						-3.5f, -7f,
						3.5f, -7f);

					camera.AddLine(
						3.5f, -7f,
						6f, -5.5f);

					camera.AddArc(
						6f, -5.5f,
						3f, 3f,
						270f, 90f);

					camera.AddLine(
						9f, -2.5f,
						9f, 5.5f);

					camera.AddArc(
						6f, 2.5f,
						3f, 3f,
						0f, 90f);

					camera.AddLine(
						6f, 5.5f,
						-6f, 5.5f);

					camera.AddArc(
						-9f, 2.5f,
						3f, 3f,
						90f, 90f);

					camera.CloseFigure();

					g.DrawPath(p, camera);
				}

				DrawCircle(g, p, 0f, 0f, 3.2f);

				// Viewfinder detail
				g.DrawLine(
					p,
					5.5f, -3.5f,
					6.6f, -3.5f);
			}

			g.Restore(state);
		}

		// --------------------------------------------------------------------
		// Undo
		// --------------------------------------------------------------------

		private static void DrawUndo(
			Graphics g,
			float cx,
			float cy,
			float size,
			bool active)
		{
			GraphicsState state = BeginIconSpace(g, cx, cy, size);

			Color color = ResolveColor(active);

			using (Pen p = CreateStroke(color))
			{
				// Shaft with 90-degree smooth curve down
				using (GraphicsPath path = new GraphicsPath())
				{
					path.StartFigure();
					path.AddLine(-7.5f, -2.5f, 2.5f, -2.5f);
					path.AddArc(-2.5f, -2.5f, 10f, 10f, 270f, 90f);
					path.AddLine(7.5f, 2.5f, 7.5f, 7.5f);
					g.DrawPath(p, path);
				}

				// Arrowhead pointing left
				PointF[] head =
				{
					new PointF(-2.5f, -7.5f),
					new PointF(-7.5f, -2.5f),
					new PointF(-2.5f, 2.5f)
				};
				g.DrawLines(p, head);
			}

			g.Restore(state);
		}

		// --------------------------------------------------------------------
		// Clear / Trash
		// --------------------------------------------------------------------

		private static void DrawClear(
			Graphics g,
			float cx,
			float cy,
			float size,
			bool active)
		{
			GraphicsState state = BeginIconSpace(g, cx, cy, size);

			Color color = active ? ColorDanger : ColorInactive;

			using (Pen p = CreateStroke(color))
			{
				// Lid
				g.DrawLine(
					p,
					-8f, -6.5f,
					8f, -6.5f);

				// Handle
				g.DrawLine(
					p,
					-3f, -9f,
					3f, -9f);

				g.DrawLine(
					p,
					-3f, -9f,
					-3f, -6.5f);

				g.DrawLine(
					p,
					3f, -9f,
					3f, -6.5f);

				// Body
				using (GraphicsPath body = new GraphicsPath())
				{
					body.StartFigure();

					body.AddLine(-6.5f, -5.0f, -5f, 8f);
					body.AddLine(5f, 8f, 6.5f, -5f);

					g.DrawPath(p, body);
				}

				// Minimal inner bars
				g.DrawLine(
					p,
					-2.2f, -2.5f,
					-1.7f, 4.5f);

				g.DrawLine(
					p,
					2.2f, -2.5f,
					1.7f, 4.5f);
			}

			g.Restore(state);
		}

		// --------------------------------------------------------------------
		// Eye
		// --------------------------------------------------------------------

		private static void DrawEye(
			Graphics g,
			float cx,
			float cy,
			float size,
			bool active,
			bool visible)
		{
			GraphicsState state = BeginIconSpace(g, cx, cy, size);

			Color color = active
				? (visible ? ColorActive : ColorDanger)
				: ColorInactive;

			using (Pen p = CreateStroke(color))
			{
				using (GraphicsPath eye = new GraphicsPath())
				{
					eye.StartFigure();

					eye.AddBezier(
						-9f, 0f,
						-6f, -4.8f,
						-2.5f, -6.3f,
						0f, -6.3f);

					eye.AddBezier(
						0f, -6.3f,
						2.5f, -6.3f,
						6f, -4.8f,
						9f, 0f);

					eye.AddBezier(
						9f, 0f,
						6f, 4.8f,
						2.5f, 6.3f,
						0f, 6.3f);

					eye.AddBezier(
						0f, 6.3f,
						-2.5f, 6.3f,
						-6f, 4.8f,
						-9f, 0f);

					g.DrawPath(p, eye);
				}

				DrawCircle(g, p, 0f, 0f, 2.2f);

				if (!visible)
				{
					using (Pen slash = CreateStroke(ColorDanger))
					{
						slash.Width = 2.0f;

						g.DrawLine(
							slash,
							-8f, -7f,
							8f, 7f);
					}
				}
			}

			g.Restore(state);
		}

		// --------------------------------------------------------------------
		// Pen width
		// --------------------------------------------------------------------

		private static void DrawPenWidth(
			Graphics g,
			float cx,
			float cy,
			float size,
			bool active)
		{
			GraphicsState state = BeginIconSpace(g, cx, cy, size);

			Color color = ResolveColor(active);

			using (Pen p = CreateStroke(color))
			{
				p.Width = 1.35f;

				g.DrawLine(
					p,
					-7f, -5f,
					7f, -5f);

				p.Width = 1.75f;

				g.DrawLine(
					p,
					-7f, 0f,
					7f, 0f);

				p.Width = 2.45f;

				g.DrawLine(
					p,
					-7f, 5f,
					7f, 5f);
			}

			g.Restore(state);
		}

		// --------------------------------------------------------------------
		// Dock chevrons
		// --------------------------------------------------------------------

		private static void DrawChevron(
			Graphics g,
			float cx,
			float cy,
			float size,
			bool active,
			bool isBack)
		{
			GraphicsState state = BeginIconSpace(g, cx, cy, size);

			Color color = ResolveColor(active);

			using (Pen p = CreateStroke(color))
			{
				float direction = isBack ? -1f : 1f;

				float x1 = direction * -4.5f;
				float x2 = direction * 1.5f;

				g.DrawLines(
					p,
					new[]
					{
						new PointF(x1 - direction * 2.2f, -4.2f),
						new PointF(x1 + direction * 2.2f, 0f),
						new PointF(x1 - direction * 2.2f, 4.2f)
					});

				g.DrawLines(
					p,
					new[]
					{
						new PointF(x2 - direction * 2.2f, -4.2f),
						new PointF(x2 + direction * 2.2f, 0f),
						new PointF(x2 - direction * 2.2f, 4.2f)
					});
			}

			g.Restore(state);
		}

		// --------------------------------------------------------------------
		// Exit / Logout
		// --------------------------------------------------------------------

		private static void DrawExit(
			Graphics g,
			float cx,
			float cy,
			float size,
			bool active)
		{
			GraphicsState state = BeginIconSpace(g, cx, cy, size);

			Color color = active ? ColorDanger : ColorInactive;

			using (Pen p = CreateStroke(color))
			{
				// Door frame
				g.DrawLine(
					p,
					-5f, -8f,
					-5f, 8f);

				g.DrawLine(
					p,
					-5f, -8f,
					0f, -8f);

				g.DrawLine(
					p,
					-5f, 8f,
					0f, 8f);

				// Exit arrow
				g.DrawLine(
					p,
					-1f, 0f,
					8f, 0f);

				g.DrawLine(
					p,
					4.5f, -3.5f,
					8f, 0f);

				g.DrawLine(
					p,
					4.5f, 3.5f,
					8f, 0f);
			}

			g.Restore(state);
		}

		// --------------------------------------------------------------------
		// Palette
		// --------------------------------------------------------------------

		private static void DrawPalette(
			Graphics g,
			float cx,
			float cy,
			float size,
			bool active)
		{
			GraphicsState state = BeginIconSpace(g, cx, cy, size);

			Color color = ResolveColor(active);

			using (Pen p = CreateStroke(color))
			{
				using (GraphicsPath palette = new GraphicsPath())
				{
					palette.StartFigure();

					palette.AddBezier(
						-7.5f, -1.2f,
						-8.5f, -6.2f,
						-4.2f, -9f,
						0.5f, -9f);

					palette.AddBezier(
						0.5f, -9f,
						5.5f, -9f,
						9f, -5.5f,
						9f, -1f);

					palette.AddBezier(
						9f, -1f,
						9f, 2.8f,
						6.2f, 7.8f,
						2.0f, 7.8f);

					palette.AddBezier(
						2.0f, 7.8f,
						0.4f, 7.8f,
						0.4f, 5.3f,
						-1.5f, 5.3f);

					palette.AddBezier(
						-1.5f, 5.3f,
						-3.2f, 5.3f,
						-4.2f, 7.2f,
						-6.0f, 6.0f);

					palette.AddBezier(
						-6.0f, 6.0f,
						-7.7f, 4.8f,
						-8.5f, 2.0f,
						-7.5f, -1.2f);

					g.DrawPath(p, palette);
				}

				// Four restrained palette dots
				using (SolidBrush b = new SolidBrush(color))
				{
					g.FillEllipse(b, -5.0f, -4.6f, 2.1f, 2.1f);
					g.FillEllipse(b, -1.1f, -6.0f, 2.1f, 2.1f);
					g.FillEllipse(b, 2.6f, -4.3f, 2.1f, 2.1f);
					g.FillEllipse(b, 4.1f, -0.5f, 2.1f, 2.1f);
				}
			}

			g.Restore(state);
		}

		// --------------------------------------------------------------------
		// Optional standalone swatch
		// --------------------------------------------------------------------

		public static void DrawColorSwatch(
			Graphics g,
			float cx,
			float cy,
			float size,
			Color swatchColor)
		{
			float radius = size * 0.36f;

			using (Pen border = new Pen(
				Color.FromArgb(160, 255, 255, 255),
				1.2f))
			using (SolidBrush brush = new SolidBrush(swatchColor))
			{
				g.FillEllipse(
					brush,
					cx - radius,
					cy - radius,
					radius * 2f,
					radius * 2f);

				g.DrawEllipse(
					border,
					cx - radius,
					cy - radius,
					radius * 2f,
					radius * 2f);
			}
		}

		// --------------------------------------------------------------------
		// Dynamic Cursor Generation
		// --------------------------------------------------------------------

		[StructLayout(LayoutKind.Sequential)]
		private struct ICONINFO
		{
			public bool fIcon;
			public int xHotspot;
			public int yHotspot;
			public IntPtr hbmMask;
			public IntPtr hbmColor;
		}

		[DllImport("user32.dll", SetLastError = true)]
		private static extern IntPtr CreateIconIndirect(
			ref ICONINFO icon);

		[DllImport("user32.dll", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		private static extern bool GetIconInfo(
			IntPtr hIcon,
			out ICONINFO piconinfo);

		[DllImport("user32.dll", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool DestroyIcon(
			IntPtr hIcon);

		[DllImport("gdi32.dll", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		private static extern bool DeleteObject(
			IntPtr hObject);

		public static Cursor CreateCursorFromBitmap(
			Bitmap bmp,
			int xHotspot,
			int yHotspot)
		{
			IntPtr hIcon = bmp.GetHicon();

			ICONINFO iconInfo = new ICONINFO();

			if (!GetIconInfo(hIcon, out iconInfo))
			{
				DestroyIcon(hIcon);
				return Cursors.Default;
			}

			iconInfo.xHotspot = xHotspot;
			iconInfo.yHotspot = yHotspot;
			iconInfo.fIcon = false;

			IntPtr hCursor = CreateIconIndirect(ref iconInfo);

			DestroyIcon(hIcon);

			if (iconInfo.hbmColor != IntPtr.Zero)
				DeleteObject(iconInfo.hbmColor);

			if (iconInfo.hbmMask != IntPtr.Zero)
				DeleteObject(iconInfo.hbmMask);

			if (hCursor == IntPtr.Zero)
				return Cursors.Default;

			return new Cursor(hCursor);
		}

		public static Cursor CreateColoringCrossCursor(
			Color dotColor,
			int dotDiameter,
			bool isEraser = false,
			int opacity = 255)
		{
			int clampedDia = Math.Max(3, Math.Min(250, dotDiameter));
			int bmpSize = clampedDia + 8;
			if (bmpSize % 2 == 0)
				bmpSize += 1;

			int cx = bmpSize / 2;
			int cy = bmpSize / 2;

			using (Bitmap bmp = new Bitmap(
				bmpSize,
				bmpSize,
				PixelFormat.Format32bppArgb))
			{
				using (Graphics g = Graphics.FromImage(bmp))
				{
					g.SmoothingMode = SmoothingMode.AntiAlias;
					g.PixelOffsetMode = PixelOffsetMode.HighQuality;
					g.InterpolationMode = InterpolationMode.HighQualityBicubic;

					g.Clear(Color.Transparent);

					float radius = clampedDia / 2f;

					if (isEraser)
					{
						float erDia = Math.Max(clampedDia, 12);
						float erR = erDia / 2f;

						using (SolidBrush shadowBrush =
							new SolidBrush(
								Color.FromArgb(70, 0, 0, 0)))
						{
							g.FillEllipse(
								shadowBrush,
								cx - erR + 1f,
								cy - erR + 1f,
								erDia,
								erDia);
						}

						using (SolidBrush fill =
							new SolidBrush(
								Color.FromArgb(
									240,
									240,
									245)))
						using (Pen outline =
							new Pen(
								Color.FromArgb(
									210,
									40,
									40,
									45),
								1.5f))
						{
							g.FillEllipse(
								fill,
								cx - erR,
								cy - erR,
								erDia,
								erDia);

							g.DrawEllipse(
								outline,
								cx - erR,
								cy - erR,
								erDia,
								erDia);
						}
					}
					else
					{
						int alpha =
							Math.Max(
								25,
								Math.Min(255, opacity));

						Color main =
							Color.FromArgb(
								alpha,
								dotColor.R,
								dotColor.G,
								dotColor.B);

						using (SolidBrush shadowBrush =
							new SolidBrush(
								Color.FromArgb(
									Math.Min(90, alpha),
									0,
									0,
									0)))
						{
							g.FillEllipse(
								shadowBrush,
								cx - radius + 1f,
								cy - radius + 1f,
								clampedDia,
								clampedDia);
						}

						using (SolidBrush dotBrush =
							new SolidBrush(main))
						{
							g.FillEllipse(
								dotBrush,
								cx - radius,
								cy - radius,
								clampedDia,
								clampedDia);
						}

						int brightness =
							(dotColor.R * 299 +
							 dotColor.G * 587 +
							 dotColor.B * 114) / 1000;

						int borderAlpha =
							Math.Max(140, alpha);

						Color borderColor =
							brightness > 170
								? Color.FromArgb(
									borderAlpha,
									25,
									25,
									30)
								: Color.FromArgb(
									borderAlpha,
									245,
									245,
									250);

						using (Pen border =
							new Pen(borderColor, 1f))
						{
							g.DrawEllipse(
								border,
								cx - radius,
								cy - radius,
								clampedDia,
								clampedDia);
						}
					}
				}

				return CreateCursorFromBitmap(
					bmp,
					cx,
					cy);
			}
		}

		public static Cursor CreateColoringDotCursor(
			Color dotColor,
			int dotDiameter,
			bool isEraser = false,
			int opacity = 255)
		{
			return CreateColoringCrossCursor(
				dotColor,
				dotDiameter,
				isEraser,
				opacity);
		}
	}
}