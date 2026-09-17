using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Windows.Forms;
using System.Runtime.InteropServices;

namespace gInk
{
	public class FormRadialMenu : Form
	{
		private readonly Root Root;

		public enum RadialTarget
		{
			None = -1,
			Center = 99,
			Clear = 0,       // 0° (East): Clear All Ink
			Snapshot = 1,    // 45° (SE): Snapshot
			Erase = 2,       // 90° (South): Eraser
			Undo = 3,        // 135° (SW): Undo / Clear
			Pointer = 4,     // 180° (West): Pointer
			InkVisible = 5,  // 225° (NW): Ink Visible
			Pan = 6,         // 270° (North): Pan
			Draw = 7,        // 315° (NE): Draw
			ColorSwatch = 8  // Outer color arc swatches
		}

		private RadialTarget hoveredTarget = RadialTarget.None;
		private int hoveredPenIndex = -1;

		// Geometry parameters
		private const int FormDim = 420;
		private const float CenterRadius = 50f;
		private const float InnerRingRadius = 58f;
		private const float OuterRingRadius = 136f;
		private const float ColorOrbRadius = 168f;
		private float CenterX => FormDim / 2f;
		private float CenterY => FormDim / 2f;

		private Bitmap renderBitmap;
		private List<int> enabledPens = new List<int>();

		private static readonly Font _fontPenSize = new Font("Segoe UI", 11.5f, FontStyle.Bold);
		private static readonly Font _fontPenResizeHint = new Font("Segoe UI", 7.0f, FontStyle.Bold);
		private static readonly Font _fontCenterTitle = new Font("Segoe UI", 13.0f, FontStyle.Bold);
		private static readonly Font _fontCenterSub = new Font("Segoe UI", 7.5f, FontStyle.Bold);

		protected override CreateParams CreateParams
		{
			get
			{
				CreateParams cp = base.CreateParams;
				cp.ExStyle |= 0x00080000; // WS_EX_LAYERED
				cp.ExStyle |= 0x00000080; // WS_EX_TOOLWINDOW
				cp.ExStyle |= 0x00000008; // WS_EX_TOPMOST
				cp.ExStyle |= 0x08000000; // WS_EX_NOACTIVATE
				return cp;
			}
		}

		protected override void WndProc(ref Message m)
		{
			const int WM_SYSCOMMAND = 0x0112;
			const int SC_KEYMENU = 0xF100;
			if (m.Msg == WM_SYSCOMMAND && ((int)m.WParam & 0xFFF0) == SC_KEYMENU)
			{
				return; // Block Windows from opening the system menu bar on Alt release
			}

			const int WM_MOUSEWHEEL = 0x020A;
			if (m.Msg == WM_MOUSEWHEEL)
			{
				short delta = (short)((m.WParam.ToInt64() >> 16) & 0xFFFF);
				HandleWheelScroll(delta);
				return;
			}

			base.WndProc(ref m);
		}

		public FormRadialMenu(Root root)
		{
			Root = root;

			this.FormBorderStyle = FormBorderStyle.None;
			this.ShowInTaskbar = false;
			this.TopMost = true;
			this.StartPosition = FormStartPosition.Manual;
			this.Size = new Size(FormDim, FormDim);
			this.DoubleBuffered = true;

			renderBitmap = new Bitmap(FormDim, FormDim, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
			UpdateEnabledPens();
		}

		public void UpdateEnabledPens()
		{
			enabledPens.Clear();
			for (int i = 0; i < Root.MaxPenCount; i++)
			{
				enabledPens.Add(i);
			}
			if (enabledPens.Count == 0)
				enabledPens.Add(1);
		}

		public void OpenAtScreenCenter()
		{
			UpdateEnabledPens();

			Rectangle screen = Screen.PrimaryScreen.Bounds;
			int left = screen.Left + (screen.Width - FormDim) / 2;
			int top = screen.Top + (screen.Height - FormDim) / 2;

			this.Location = new Point(left, top);

			hoveredTarget = RadialTarget.Center;
			hoveredPenIndex = -1;

			RenderMenu();
			this.Show();

			// Center the mouse cursor at the wheel center
			Cursor.Position = new Point(screen.Left + screen.Width / 2, screen.Top + screen.Height / 2);

			this.Activate();
			this.Capture = true;
		}

		public void OpenAtCursor(Point? summonPos = null)
		{
			UpdateEnabledPens();

			Point pos = summonPos ?? Cursor.Position;
			int left = pos.X - FormDim / 2;
			int top = pos.Y - FormDim / 2;

			// Clamp inside virtual screen
			Rectangle vs = SystemInformation.VirtualScreen;
			if (left < vs.Left) left = vs.Left;
			if (top < vs.Top) top = vs.Top;
			if (left + FormDim > vs.Right) left = vs.Right - FormDim;
			if (top + FormDim > vs.Bottom) top = vs.Bottom - FormDim;

			this.Location = new Point(left, top);

			hoveredTarget = RadialTarget.Center;
			hoveredPenIndex = -1;

			RenderMenu();
			this.Show();
		}

		public void CloseMenu()
		{
			this.Capture = false;
			this.Hide();
		}

		private PointF GetColorOrbPosition(int index, int totalCount)
		{
			if (totalCount <= 0)
			{
				return new PointF(CenterX + ColorOrbRadius, CenterY);
			}

			float startDeg = -90f; // Start at 12 o'clock (top)
			float stepDeg = 360f / totalCount;
			float angleDeg = startDeg + index * stepDeg;
			double rad = angleDeg * Math.PI / 180.0;
			return new PointF(CenterX + (float)(Math.Cos(rad) * ColorOrbRadius), CenterY + (float)(Math.Sin(rad) * ColorOrbRadius));
		}

		public void UpdateHoverFromPoint(int x, int y)
		{
			float dx = x - CenterX;
			float dy = y - CenterY;
			double r = Math.Sqrt(dx * dx + dy * dy);
			double angle = (Math.Atan2(dy, dx) * 180.0 / Math.PI + 360.0) % 360.0;

			RadialTarget newTarget = RadialTarget.None;
			int newPenIndex = -1;

			int count = enabledPens.Count;

			// 1. Check orbital color orbs: check outer circular ring zone
			if (count > 0 && r >= OuterRingRadius + 10f && r <= FormDim / 2f)
			{
				float startDeg = -90f;
				float relAngle = (float)((angle - startDeg + 360.0) % 360.0);
				int closestIdx = (int)Math.Round(relAngle / (360.0f / count)) % count;
				newTarget = RadialTarget.ColorSwatch;
				newPenIndex = enabledPens[closestIdx];
			}
			else
			{
				for (int i = 0; i < count; i++)
				{
					PointF orbPt = GetColorOrbPosition(i, count);
					float odx = x - orbPt.X;
					float ody = y - orbPt.Y;
					if (odx * odx + ody * ody <= 24 * 24)
					{
						newTarget = RadialTarget.ColorSwatch;
						newPenIndex = enabledPens[i];
						break;
					}
				}
			}

			// 2. Check Center Hub
			if (newTarget == RadialTarget.None)
			{
				if (r < CenterRadius)
				{
					newTarget = RadialTarget.Center;
				}
				// 3. Check Main 8 Floating Sectors
				else if (r >= InnerRingRadius - 6 && r <= OuterRingRadius + 8)
				{
					int sector = (int)Math.Floor(((angle + 22.5) % 360.0) / 45.0);
					newTarget = (RadialTarget)sector;
				}
			}

			if (newTarget != hoveredTarget || newPenIndex != hoveredPenIndex)
			{
				hoveredTarget = newTarget;
				hoveredPenIndex = newPenIndex;
				RenderMenu();
			}
		}

		protected override void OnMouseMove(MouseEventArgs e)
		{
			base.OnMouseMove(e);
			UpdateHoverFromPoint(e.X, e.Y);
		}

		private DateTime lastWheelTime = DateTime.MinValue;
		private int lastWheelWidth = 0;

		public void HandleWheelScroll(int delta)
		{
			if (hoveredTarget == RadialTarget.Undo)
			{
				if (delta > 0)
					Root.RedoInk();
				else
					Root.UndoInk();
				RenderMenu();
				return;
			}

			int curP = Root.CurrentPen >= 0 ? Root.CurrentPen : (Root.LastPen >= 0 ? Root.LastPen : 1);
			float curWidth = Root.PenAttr[curP].Width;

			int step;
			if (curWidth < 120) step = 20;
			else if (curWidth < 350) step = 40;
			else step = 80;

			int change = (delta > 0) ? step : -step;
			Root.AdjustPenWidth(change);

			lastWheelTime = DateTime.Now;
			lastWheelWidth = (int)Root.PenAttr[curP].Width;
			RenderMenu();
		}

		protected override void OnMouseWheel(MouseEventArgs e)
		{
			base.OnMouseWheel(e);
			HandleWheelScroll(e.Delta);
		}

		protected override void OnMouseDown(MouseEventArgs e)
		{
			base.OnMouseDown(e);
			UpdateHoverFromPoint(e.X, e.Y);
			CommitAndClose();
		}

		protected override void OnMouseUp(MouseEventArgs e)
		{
			base.OnMouseUp(e);

			CommitAndClose();
		}

		public void CommitAndClose()
		{
			RadialTarget target = hoveredTarget;
			int pen = hoveredPenIndex;

			CloseMenu();

			if (Root != null && Root.FormCollection != null && !Root.FormCollection.IsDisposed)
			{
				try
				{
					Root.FormCollection.BeginInvoke((MethodInvoker)(() =>
					{
						ExecuteAction(target, pen);
					}));
					return;
				}
				catch { }
			}

			ExecuteAction(target, pen);
		}

		protected override bool ProcessDialogKey(Keys keyData)
		{
			Keys key = keyData & Keys.KeyCode;
			if (key == Keys.Alt || key == Keys.Menu)
			{
				return false;
			}
			return base.ProcessDialogKey(keyData);
		}

		protected override void OnKeyDown(KeyEventArgs e)
		{
			base.OnKeyDown(e);

			if (e.KeyCode == Keys.Escape || e.KeyCode == Keys.Space)
			{
				CloseMenu();
				e.Handled = true;
			}
			else if (e.KeyCode == Keys.Alt || e.KeyCode == Keys.Menu)
			{
				e.Handled = true;
			}
			else if (e.KeyCode >= Keys.D0 && e.KeyCode <= Keys.D9)
			{
				int pen = e.KeyCode - Keys.D0;
				if (pen >= 0 && pen < Root.MaxPenCount && Root.PenEnabled[pen])
				{
					Root.SelectPen(pen);
					CloseMenu();
					e.Handled = true;
				}
			}
			else if (e.KeyCode == Keys.E)
			{
				Root.SelectPen(-1);
				CloseMenu();
				e.Handled = true;
			}
			else if (e.KeyCode == Keys.Z)
			{
				Root.UndoInk();
				CloseMenu();
				e.Handled = true;
			}
			else if (e.KeyCode == Keys.Y)
			{
				Root.RedoInk();
				CloseMenu();
				e.Handled = true;
			}
			else if (e.KeyCode == Keys.D)
			{
				if (Root.Docked) Root.UnDock(); else Root.Dock();
				CloseMenu();
				e.Handled = true;
			}
			else if (e.KeyCode == Keys.X)
			{
				CloseMenu();
				if (Root.FormCollection != null) Root.FormCollection.RetreatAndExit();
				e.Handled = true;
			}
			else if (e.KeyCode == Keys.C)
			{
				Root.ClearInk();
				CloseMenu();
				e.Handled = true;
			}
			else if (e.KeyCode == Keys.S)
			{
				CloseMenu();
				if (Root.FormCollection != null) Root.FormCollection.btSnap_Click(null, null);
				e.Handled = true;
			}
			else if (e.KeyCode == Keys.P)
			{
				Root.SelectPen(-2);
				CloseMenu();
				e.Handled = true;
			}
			else if (e.KeyCode == Keys.V)
			{
				Root.SetInkVisible(!Root.InkVisible);
				CloseMenu();
				e.Handled = true;
			}
			else if (e.KeyCode == Keys.M)
			{
				Root.SelectPen(-3);
				CloseMenu();
				e.Handled = true;
			}
			else if (e.KeyCode == Keys.Oemplus || e.KeyCode == Keys.Add)
			{
				HandleWheelScroll(120);
				e.Handled = true;
			}
			else if (e.KeyCode == Keys.OemMinus || e.KeyCode == Keys.Subtract)
			{
				HandleWheelScroll(-120);
				e.Handled = true;
			}
		}

		protected override void OnKeyUp(KeyEventArgs e)
		{
			base.OnKeyUp(e);
		}

		protected override void OnDeactivate(EventArgs e)
		{
			base.OnDeactivate(e);
			// Do not auto-close on deactivate; lifecycle is controlled by Alt-hold, explicit mouse click, or cancel
		}

		private void ExecuteAction(RadialTarget target, int penIndex)
		{
			switch (target)
			{
				case RadialTarget.Center:
					CloseMenu();
					break;

				case RadialTarget.Draw:
					int curPen = Root.CurrentPen;
					if (curPen < 0) curPen = (Root.LastPen >= 0 ? Root.LastPen : 1);
					if (Root.CurrentPen == curPen)
					{
						Root.TogglePenOrHighlighter(curPen);
					}
					else
					{
						Root.SelectPen(curPen);
					}
					CloseMenu();
					break;

				case RadialTarget.Clear:
					Root.ClearInk();
					CloseMenu();
					break;

				case RadialTarget.ColorSwatch:
					if (penIndex >= 0)
					{
						Root.SelectPen(penIndex);
					}
					CloseMenu();
					break;

				case RadialTarget.Erase:
					Root.SelectPen(-1);
					CloseMenu();
					break;

				case RadialTarget.Pointer:
					Root.SelectPen(-2);
					CloseMenu();
					break;

				case RadialTarget.Pan:
					Root.SelectPen(-3);
					CloseMenu();
					break;

				case RadialTarget.Snapshot:
					CloseMenu();
					if (Root.FormCollection != null)
						Root.FormCollection.btSnap_Click(null, null);
					break;

				case RadialTarget.Undo:
					Root.UndoInk();
					CloseMenu();
					break;

				case RadialTarget.InkVisible:
					Root.SetInkVisible(!Root.InkVisible);
					CloseMenu();
					break;

				case RadialTarget.None:
					CloseMenu();
					break;
			}
		}

		public void RenderMenu()
		{
			using (Graphics g = Graphics.FromImage(renderBitmap))
			{
				g.SmoothingMode = SmoothingMode.AntiAlias;
				g.InterpolationMode = InterpolationMode.HighQualityBicubic;
				g.PixelOffsetMode = PixelOffsetMode.HighQuality;
				g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;

				g.Clear(Color.Transparent);

				// 1. Frosted Obsidian Glass Chassis Backdrop
				using (GraphicsPath baseDisc = new GraphicsPath())
				{
					float chassisR = OuterRingRadius + 9f;
					baseDisc.AddEllipse(CenterX - chassisR, CenterY - chassisR, chassisR * 2, chassisR * 2);
					using (SolidBrush chassisBrush = new SolidBrush(Color.FromArgb(140, 12, 16, 24)))
					{
						g.FillPath(chassisBrush, baseDisc);
					}
					using (Pen chassisBorder = new Pen(Color.FromArgb(32, 255, 255, 255), 1.0f))
					{
						g.DrawPath(chassisBorder, baseDisc);
					}
				}

				// 2. Subtle Ambient Glow around wheel perimeter
				using (GraphicsPath haloPath = new GraphicsPath())
				{
					haloPath.AddEllipse(CenterX - OuterRingRadius - 4, CenterY - OuterRingRadius - 4,
						(OuterRingRadius + 4) * 2, (OuterRingRadius + 4) * 2);
					using (SolidBrush haloBrush = new SolidBrush(Color.FromArgb(16, 0, 240, 160)))
					{
						g.FillPath(haloBrush, haloPath);
					}
				}

				// 3. Draw 8 Floating Segmented Acrylic Pills (with smooth rounded corners)
				for (int i = 0; i < 8; i++)
				{
					RadialTarget target = (RadialTarget)i;
					bool isHovered = (hoveredTarget == target);
					float midAngle = i * 45f;
					float sweepAngle = isHovered ? 41.5f : 40.0f; // clean gap between pills
					float startAngle = midAngle - sweepAngle / 2f;

					float rIn = isHovered ? InnerRingRadius - 3f : InnerRingRadius;
					float rOut = isHovered ? OuterRingRadius + 4f : OuterRingRadius;

					using (GraphicsPath pillPath = CreateRoundedDonutSector(CenterX, CenterY, rIn, rOut, startAngle, sweepAngle, 5.5f))
					{
						if (isHovered)
						{
							// Outer soft glow halo
							using (GraphicsPath haloPill = CreateRoundedDonutSector(CenterX, CenterY, rIn - 2f, rOut + 2f, startAngle - 0.5f, sweepAngle + 1.0f, 7f))
							using (SolidBrush haloPillBrush = new SolidBrush(Color.FromArgb(40, 0, 240, 170)))
							{
								g.FillPath(haloPillBrush, haloPill);
							}

							// Luminous hover gradient wash
							using (SolidBrush hoverBrush = new SolidBrush(Color.FromArgb(235, 8, 52, 44)))
							{
								g.FillPath(hoverBrush, pillPath);
							}
							using (Pen glowPen = new Pen(Color.FromArgb(255, 0, 245, 175), 2.2f))
							{
								glowPen.LineJoin = LineJoin.Round;
								g.DrawPath(glowPen, pillPath);
							}
						}
						else
						{
							// Frosted obsidian glass floating pill
							using (SolidBrush pillBrush = new SolidBrush(Color.FromArgb(220, 20, 26, 36)))
							{
								g.FillPath(pillBrush, pillPath);
							}
							using (Pen borderPen = new Pen(Color.FromArgb(45, 255, 255, 255), 1.0f))
							{
								borderPen.LineJoin = LineJoin.Round;
								g.DrawPath(borderPen, pillPath);
							}

							// 1px subtle specular rim on outer edge
							using (Pen rimPen = new Pen(Color.FromArgb(25, 255, 255, 255), 1.0f))
							{
								g.DrawArc(rimPen, CenterX - rOut + 1f, CenterY - rOut + 1f, (rOut - 1f) * 2, (rOut - 1f) * 2, startAngle + 4f, sweepAngle - 8f);
							}
						}
					}

					// Draw Sector Icon (Centered at radius 97px)
					double rad = midAngle * Math.PI / 180.0;
					float iconDist = (InnerRingRadius + OuterRingRadius) / 2f;
					float ix = CenterX + (float)(Math.Cos(rad) * iconDist);
					float iy = CenterY + (float)(Math.Sin(rad) * iconDist);
					float iconSize = isHovered ? 40f : 35f;

					// Active tool indicator pip on outer rim
					bool isActiveTool = false;
					if (target == RadialTarget.Draw && Root.CurrentPen >= 0 && !Root.EraserMode && !Root.PointerMode && !Root.PanMode)
						isActiveTool = true;
					else if (target == RadialTarget.Erase && Root.EraserMode)
						isActiveTool = true;
					else if (target == RadialTarget.Pointer && Root.PointerMode)
						isActiveTool = true;
					else if (target == RadialTarget.Pan && Root.PanMode)
						isActiveTool = true;

					if (isActiveTool)
					{
						using (Pen activeGlow = new Pen(Color.FromArgb(80, 0, 240, 170), 3.5f))
						{
							activeGlow.StartCap = LineCap.Round;
							activeGlow.EndCap = LineCap.Round;
							g.DrawArc(activeGlow, CenterX - rIn - 1f, CenterY - rIn - 1f, (rIn + 1f) * 2, (rIn + 1f) * 2, startAngle + 7f, sweepAngle - 14f);
						}
						using (Pen activeArc = new Pen(Color.FromArgb(255, 0, 255, 180), 1.8f))
						{
							activeArc.StartCap = LineCap.Round;
							activeArc.EndCap = LineCap.Round;
							g.DrawArc(activeArc, CenterX - rIn - 1f, CenterY - rIn - 1f, (rIn + 1f) * 2, (rIn + 1f) * 2, startAngle + 7f, sweepAngle - 14f);
						}
					}

					DrawSectorIcon(g, target, ix, iy, iconSize, isHovered);
				}

				// 4. Draw Floating Orbital Color Orbs
				DrawOrbitalColorOrbs(g);

				// 5. Center Hub (Dynamic Tool Title & Modern Micro-Badge)
				DrawCenterHub(g);
			}

			UpdateLayeredWindowFromBitmap();
		}

		private void DrawCenterHub(Graphics g)
		{
			bool isCenterHovered = (hoveredTarget == RadialTarget.Center);

			// Center Circle Base (Dark Smoked Acrylic with neon glow)
			using (GraphicsPath centerPath = new GraphicsPath())
			{
				centerPath.AddEllipse(CenterX - CenterRadius, CenterY - CenterRadius, CenterRadius * 2, CenterRadius * 2);

				Color fillCol = isCenterHovered ? Color.FromArgb(248, 16, 44, 38) : Color.FromArgb(245, 15, 20, 28);
				using (SolidBrush centerBrush = new SolidBrush(fillCol))
				{
					g.FillPath(centerBrush, centerPath);
				}

				Color borderCol = isCenterHovered ? Color.FromArgb(255, 0, 255, 180) : Color.FromArgb(190, 0, 230, 160);
				float borderWidth = isCenterHovered ? 2.5f : 1.8f;
				using (Pen centerPen = new Pen(borderCol, borderWidth))
				{
					g.DrawPath(centerPen, centerPath);
				}

				// Inner subtle specular chamfer ring
				using (Pen innerSpecular = new Pen(Color.FromArgb(35, 255, 255, 255), 1.0f))
				{
					g.DrawEllipse(innerSpecular, CenterX - CenterRadius + 3.5f, CenterY - CenterRadius + 3.5f, (CenterRadius - 3.5f) * 2, (CenterRadius - 3.5f) * 2);
				}
			}

			// Determine dynamic content based on hovered target
			string title;
			string subtitle;
			Color subColor;

			switch (hoveredTarget)
			{
				case RadialTarget.Draw:
					int actP = Root.CurrentPen >= 0 ? Root.CurrentPen : (Root.LastPen >= 0 ? Root.LastPen : 1);
					bool isHl = Root.PenAttr[actP].Transparency >= 100;
					if (Root.CurrentPen >= 0)
					{
						title = isHl ? "Highlighter" : "Pen";
						subtitle = isHl ? "Switch to Pen" : "Switch to Highlighter";
						subColor = isHl ? Color.FromArgb(0, 240, 170) : Color.FromArgb(250, 220, 30);
					}
					else
					{
						title = "Draw";
						subtitle = isHl ? "Highlighter" : "Pen " + actP;
						subColor = Color.FromArgb(0, 240, 170);
					}
					break;

				case RadialTarget.Clear:
					title = "Clear";
					subtitle = "Clear All Ink";
					subColor = Color.FromArgb(255, 95, 105);
					break;

				case RadialTarget.Snapshot:
					title = "Snapshot";
					subtitle = "Capture Region";
					subColor = Color.FromArgb(0, 240, 170);
					break;

				case RadialTarget.Erase:
					title = "Eraser";
					subtitle = "Eraser Tool";
					subColor = Color.FromArgb(255, 110, 150);
					break;

				case RadialTarget.Undo:
					title = "Undo";
					subtitle = "Undo (Scroll: Redo)";
					subColor = Color.FromArgb(0, 240, 170);
					break;

				case RadialTarget.Pointer:
					title = "Pointer";
					subtitle = Root.PointerMode ? "Pointer Active" : "Desktop Pass-Through";
					subColor = Color.FromArgb(0, 240, 170);
					break;

				case RadialTarget.InkVisible:
					title = "Ink";
					subtitle = Root.InkVisible ? "Ink Visible" : "Ink Hidden";
					subColor = Root.InkVisible ? Color.FromArgb(0, 240, 170) : Color.FromArgb(255, 95, 105);
					break;

				case RadialTarget.Pan:
					title = "Pan";
					subtitle = "Pan Canvas";
					subColor = Color.FromArgb(0, 240, 170);
					break;

				case RadialTarget.ColorSwatch:
					title = "Color";
					if (hoveredPenIndex >= 0 && hoveredPenIndex < Root.MaxPenCount)
						subtitle = GetColorName(Root.PenAttr[hoveredPenIndex].Color) + " #" + hoveredPenIndex;
					else
						subtitle = "Pen Swatch";
					subColor = Color.FromArgb(0, 240, 170);
					break;

				case RadialTarget.Center:
				case RadialTarget.None:
				default:
					title = "Cancel";
					subtitle = isCenterHovered ? "Release to Cancel" : "Close";
					subColor = isCenterHovered ? Color.FromArgb(0, 240, 170) : Color.FromArgb(145, 170, 190);
					break;
			}

			// Check if live scroll wheel feedback is active
			bool isWheelActive = (DateTime.Now - lastWheelTime).TotalMilliseconds < 1600;
			if (isWheelActive)
			{
				int actPen = Root.CurrentPen >= 0 ? Root.CurrentPen : (Root.LastPen >= 0 ? Root.LastPen : 1);
				int widthVal = lastWheelWidth > 0 ? lastWheelWidth : (int)Root.PenAttr[actPen].Width;
				Color penColor = Root.PenAttr[actPen].Color;
				float dotRadius = Math.Max(2.5f, Math.Min(20f, (widthVal / 10f) * 0.75f));

				// Live expanding/shrinking color dot preview
				using (SolidBrush dotBrush = new SolidBrush(Color.FromArgb(235, penColor.R, penColor.G, penColor.B)))
				using (Pen dotRing = new Pen(Color.White, 1.2f))
				{
					g.FillEllipse(dotBrush, CenterX - dotRadius, CenterY - 14f - dotRadius, dotRadius * 2, dotRadius * 2);
					g.DrawEllipse(dotRing, CenterX - dotRadius, CenterY - 14f - dotRadius, dotRadius * 2, dotRadius * 2);
				}

				using (SolidBrush titleBrush = new SolidBrush(Color.White))
				{
					string sizeStr = (widthVal / 10).ToString() + " px";
					SizeF sz = g.MeasureString(sizeStr, _fontPenSize);
					g.DrawString(sizeStr, _fontPenSize, titleBrush, CenterX - sz.Width / 2f, CenterY + 1f);
				}

				using (SolidBrush subBrush = new SolidBrush(Color.FromArgb(250, 220, 30)))
				{
					string sub = "Scroll to Resize";
					SizeF sz = g.MeasureString(sub, _fontPenResizeHint);
					g.DrawString(sub, _fontPenResizeHint, subBrush, CenterX - sz.Width / 2f, CenterY + 18f);
				}
				return;
			}

			// Main Title
			using (SolidBrush titleBrush = new SolidBrush(Color.White))
			{
				SizeF size = g.MeasureString(title, _fontCenterTitle);
				g.DrawString(title, _fontCenterTitle, titleBrush, CenterX - size.Width / 2f, CenterY - size.Height / 2f - 6f);
			}

			// Clean Subtitle in glowing accent color (no capsule/badge box)
			using (SolidBrush subBrush = new SolidBrush(subColor))
			{
				SizeF size = g.MeasureString(subtitle, _fontCenterSub);
				g.DrawString(subtitle, _fontCenterSub, subBrush, CenterX - size.Width / 2f, CenterY + 10f);
			}
		}

		private void DrawOrbitalColorOrbs(Graphics g)
		{
			int count = enabledPens.Count;
			if (count == 0) return;

			// Sleek glowing orbital guide track (full 360 degree circle)
			using (Pen orbitPen = new Pen(Color.FromArgb(40, 255, 255, 255), 1.2f))
			{
				orbitPen.DashStyle = DashStyle.Dot;
				g.DrawEllipse(orbitPen, CenterX - ColorOrbRadius, CenterY - ColorOrbRadius, ColorOrbRadius * 2, ColorOrbRadius * 2);
			}

			for (int i = 0; i < count; i++)
			{
				int penId = enabledPens[i];
				Color penCol = Root.PenAttr[penId].Color;
				PointF pt = GetColorOrbPosition(i, count);
				bool isHovered = (hoveredTarget == RadialTarget.ColorSwatch && hoveredPenIndex == penId);
				bool isActive = (Root.CurrentPen == penId);

				float r = isHovered ? 17f : (isActive ? 15.5f : 13.5f);

				// Drop shadow
				using (SolidBrush shadowBrush = new SolidBrush(Color.FromArgb(130, 0, 0, 0)))
				{
					g.FillEllipse(shadowBrush, pt.X - r + 1.5f, pt.Y - r + 2.5f, r * 2, r * 2);
				}

				// Glowing colored aura
				if (isHovered || isActive)
				{
					using (SolidBrush auraBrush = new SolidBrush(Color.FromArgb(isHovered ? 120 : 80, penCol.R, penCol.G, penCol.B)))
					{
						g.FillEllipse(auraBrush, pt.X - r - 5f, pt.Y - r - 5f, (r + 5f) * 2, (r + 5f) * 2);
					}
				}

				// Main Orb Color Fill
				using (SolidBrush colBrush = new SolidBrush(penCol))
				{
					g.FillEllipse(colBrush, pt.X - r, pt.Y - r, r * 2, r * 2);
				}

				// Dual Ring Bezel: Outer crisp white ring for active/hovered
				if (isHovered || isActive)
				{
					using (Pen whitePen = new Pen(Color.White, isHovered ? 2.8f : 2.2f))
					{
						g.DrawEllipse(whitePen, pt.X - r, pt.Y - r, r * 2, r * 2);
					}
					// Inner contrast ring
					using (Pen darkRing = new Pen(Color.FromArgb(100, 0, 0, 0), 1.0f))
					{
						g.DrawEllipse(darkRing, pt.X - r + 2.5f, pt.Y - r + 2.5f, (r - 2.5f) * 2, (r - 2.5f) * 2);
					}
				}
				else
				{
					using (Pen borderPen = new Pen(Color.FromArgb(120, 255, 255, 255), 1.2f))
					{
						g.DrawEllipse(borderPen, pt.X - r, pt.Y - r, r * 2, r * 2);
					}
				}

				// 3D Glassy Specular Highlight on top-left of Orb
				using (GraphicsPath shine = new GraphicsPath())
				{
					float shineR = r * 0.65f;
					shine.AddArc(pt.X - shineR - 1f, pt.Y - shineR - 1f, shineR * 2, shineR * 2, 200, 130);
					using (Pen shinePen = new Pen(Color.FromArgb(160, 255, 255, 255), 1.2f))
					{
						g.DrawPath(shinePen, shine);
					}
				}
			}
		}

		private void DrawSectorIcon(Graphics g, RadialTarget target, float ix, float iy, float iconSize, bool isHovered)
		{
			switch (target)
			{
				case RadialTarget.Draw:
					int activeP = Root.CurrentPen >= 0 ? Root.CurrentPen : 1;
					bool isHighlighter = Root.PenAttr[activeP].Transparency >= 100;
					ModernIcons.DrawIcon(g, isHighlighter ? ModernIconType.Highlighter : ModernIconType.Pen, ix, iy, iconSize, isHovered, Root.PenAttr[activeP].Color);
					break;

				case RadialTarget.Clear:
					ModernIcons.DrawIcon(g, ModernIconType.Clear, ix, iy, iconSize, isHovered);
					break;

				case RadialTarget.Snapshot:
					ModernIcons.DrawIcon(g, ModernIconType.Snapshot, ix, iy, iconSize, isHovered);
					break;

				case RadialTarget.Erase:
					ModernIcons.DrawIcon(g, ModernIconType.Eraser, ix, iy, iconSize, isHovered);
					break;

				case RadialTarget.Undo:
					ModernIcons.DrawIcon(g, ModernIconType.Undo, ix, iy, iconSize, isHovered);
					break;

				case RadialTarget.Pointer:
					ModernIcons.DrawIcon(g, ModernIconType.Pointer, ix, iy, iconSize, isHovered);
					break;

				case RadialTarget.InkVisible:
					ModernIcons.DrawIcon(g, Root.InkVisible ? ModernIconType.Visible : ModernIconType.VisibleNot, ix, iy, iconSize, isHovered);
					break;

				case RadialTarget.Pan:
					ModernIcons.DrawIcon(g, ModernIconType.Pan, ix, iy, iconSize, isHovered);
					break;
			}
		}

		private string GetColorName(Color c)
		{
			if (c.R > 220 && c.G > 220 && c.B > 220) return "WHITE";
			if (c.R < 60 && c.G < 60 && c.B < 60) return "BLACK";
			if (Math.Abs(c.R - c.G) < 30 && Math.Abs(c.G - c.B) < 30 && c.R < 160) return "GRAY";
			if (c.R > 200 && c.G < 100 && c.B < 100) return "RED";
			if (c.B > 180 && c.R < 100 && c.G < 150) return "BLUE";
			if (c.G > 150 && c.R < 150 && c.B < 150) return "GREEN";
			if (c.R > 200 && c.G > 160 && c.B < 80) return "YELLOW";
			if (c.R > 220 && c.G > 100 && c.B < 50) return "ORANGE";
			if (c.R > 180 && c.B > 150 && c.G < 160) return "PINK";
			if (c.B > 150 && c.G > 150 && c.R < 100) return "CYAN";
			if (c.R > 120 && c.B > 120) return "PURPLE";
			return "COLOR";
		}

		private static PointF Pol(float cx, float cy, float r, float deg)
		{
			double rad = deg * Math.PI / 180.0;
			return new PointF(cx + (float)(Math.Cos(rad) * r), cy + (float)(Math.Sin(rad) * r));
		}

		private GraphicsPath CreateRoundedDonutSector(float cx, float cy, float rIn, float rOut, float startDeg, float sweepDeg, float cornerRadius)
		{
			GraphicsPath path = new GraphicsPath();

			float cr = cornerRadius;
			float dOut = (float)(cr / rOut * 180.0 / Math.PI);
			float dIn = (float)(cr / rIn * 180.0 / Math.PI);
			float endDeg = startDeg + sweepDeg;

			PointF pOuterStart = Pol(cx, cy, rOut, startDeg + dOut);
			path.AddArc(cx - rOut, cy - rOut, rOut * 2, rOut * 2, startDeg + dOut, sweepDeg - 2 * dOut);

			PointF cornerOuterEnd = Pol(cx, cy, rOut, endDeg);
			PointF pOuterEnd = Pol(cx, cy, rOut, endDeg - dOut);
			PointF pRadEndOuter = Pol(cx, cy, rOut - cr, endDeg);
			path.AddBezier(pOuterEnd, cornerOuterEnd, cornerOuterEnd, pRadEndOuter);

			PointF pRadEndInner = Pol(cx, cy, rIn + cr, endDeg);
			path.AddLine(pRadEndOuter, pRadEndInner);

			PointF cornerInnerEnd = Pol(cx, cy, rIn, endDeg);
			PointF pInnerEnd = Pol(cx, cy, rIn, endDeg - dIn);
			path.AddBezier(pRadEndInner, cornerInnerEnd, cornerInnerEnd, pInnerEnd);

			path.AddArc(cx - rIn, cy - rIn, rIn * 2, rIn * 2, endDeg - dIn, -(sweepDeg - 2 * dIn));

			PointF pInnerStart = Pol(cx, cy, rIn, startDeg + dIn);
			PointF cornerInnerStart = Pol(cx, cy, rIn, startDeg);
			PointF pRadStartInner = Pol(cx, cy, rIn + cr, startDeg);
			path.AddBezier(pInnerStart, cornerInnerStart, cornerInnerStart, pRadStartInner);

			PointF pRadStartOuter = Pol(cx, cy, rOut - cr, startDeg);
			path.AddLine(pRadStartInner, pRadStartOuter);

			PointF cornerOuterStart = Pol(cx, cy, rOut, startDeg);
			path.AddBezier(pRadStartOuter, cornerOuterStart, cornerOuterStart, pOuterStart);

			path.CloseFigure();
			return path;
		}

		private void UpdateLayeredWindowFromBitmap()
		{
			IntPtr screenDc = GetDC(IntPtr.Zero);
			IntPtr memDc = CreateCompatibleDC(screenDc);
			IntPtr hBmp = renderBitmap.GetHbitmap(Color.FromArgb(0));
			IntPtr oldBmp = SelectObject(memDc, hBmp);

			Size size = new Size(FormDim, FormDim);
			Point pointSource = new Point(0, 0);
			Point topPos = new Point(this.Left, this.Top);

			BLENDFUNCTION blend = new BLENDFUNCTION
			{
				BlendOp = 0x00, // AC_SRC_OVER
				BlendFlags = 0,
				SourceConstantAlpha = 255,
				AlphaFormat = 0x01 // AC_SRC_ALPHA
			};

			UpdateLayeredWindow(this.Handle, screenDc, ref topPos, ref size, memDc, ref pointSource, 0, ref blend, 0x02); // ULW_ALPHA

			SelectObject(memDc, oldBmp);
			DeleteObject(hBmp);
			DeleteDC(memDc);
			ReleaseDC(IntPtr.Zero, screenDc);
		}

		protected override void Dispose(bool disposing)
		{
			if (disposing)
			{
				if (renderBitmap != null)
				{
					renderBitmap.Dispose();
					renderBitmap = null;
				}
			}
			base.Dispose(disposing);
		}

		#region Win32 API
		[StructLayout(LayoutKind.Sequential, Pack = 1)]
		private struct BLENDFUNCTION
		{
			public byte BlendOp;
			public byte BlendFlags;
			public byte SourceConstantAlpha;
			public byte AlphaFormat;
		}

		[DllImport("user32.dll", ExactSpelling = true, SetLastError = true)]
		private static extern bool UpdateLayeredWindow(IntPtr hwnd, IntPtr hdcDst, ref Point pptDst, ref Size psize, IntPtr hdcSrc, ref Point pptSrc, uint crKey, [In] ref BLENDFUNCTION pblend, uint dwFlags);

		[DllImport("user32.dll", ExactSpelling = true, SetLastError = true)]
		private static extern IntPtr GetDC(IntPtr hWnd);

		[DllImport("user32.dll", ExactSpelling = true, SetLastError = true)]
		private static extern int ReleaseDC(IntPtr hWnd, IntPtr hDC);

		[DllImport("gdi32.dll", ExactSpelling = true, SetLastError = true)]
		private static extern IntPtr CreateCompatibleDC(IntPtr hDC);

		[DllImport("gdi32.dll", ExactSpelling = true, SetLastError = true)]
		private static extern bool DeleteDC(IntPtr hdc);

		[DllImport("gdi32.dll", ExactSpelling = true, SetLastError = true)]
		private static extern IntPtr SelectObject(IntPtr hDC, IntPtr hObject);

		[DllImport("gdi32.dll", ExactSpelling = true, SetLastError = true)]
		private static extern bool DeleteObject(IntPtr hObject);
		#endregion
	}
}
