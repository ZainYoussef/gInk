using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.Threading;
//using System.Windows.Input;
using Microsoft.Ink;

namespace gInk
{
	public partial class FormCollection : Form
	{
		public Root Root;
		public InkOverlay IC;

		public Button[] btPen;
		public Bitmap image_exit, image_clear, image_undo, image_snap, image_penwidth;
		public Bitmap image_dock, image_dockback;
		public Bitmap image_pencil, image_highlighter, image_pencil_act, image_highlighter_act;
		public Bitmap image_pointer, image_pointer_act;
		public Bitmap[] image_pen;
		public Bitmap[] image_pen_act;
		public Bitmap image_eraser_act, image_eraser;
		public Bitmap image_pan_act, image_pan;
		public Bitmap image_visible_not, image_visible;
		private System.Windows.Forms.Cursor dynamicCursor = null;

		public int ButtonsEntering = 0;  // -1 = exiting
		public int gpButtonsLeft, gpButtonsTop, gpButtonsWidth, gpButtonsHeight; // the default location, fixed

		public bool gpPenWidth_MouseOn = false;

		public int PrimaryLeft, PrimaryTop;

		// http://www.csharp411.com/hide-form-from-alttab/
		protected override CreateParams CreateParams
		{
			get
			{
				CreateParams cp = base.CreateParams;
				// turn on WS_EX_TOOLWINDOW style bit
				cp.ExStyle |= 0x80;
				return cp;
			}
		}

		private delegate IntPtr LowLevelMouseProc(int nCode, IntPtr wParam, IntPtr lParam);
		private LowLevelMouseProc _mouseHookCallback;
		private IntPtr _mouseHookHandle = IntPtr.Zero;
		private bool _swallowNextLButtonUp = false;
		public bool SuppressStrokeFromRadialMenu = false;
		private byte[] _keyStateBuffer = new byte[256];

		private const int WH_MOUSE_LL = 14;
		private const int WM_MOUSEMOVE = 0x0200;
		private const int WM_LBUTTONDOWN = 0x0201;
		private const int WM_LBUTTONUP = 0x0202;
		private const int WM_RBUTTONDOWN = 0x0204;
		private const int WM_RBUTTONUP = 0x0205;
		private const int WM_RBUTTONDBLCLK = 0x0206;

		[DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
		private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelMouseProc lpfn, IntPtr hMod, uint dwThreadId);

		[DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		private static extern bool UnhookWindowsHookEx(IntPtr hhk);

		[DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
		private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

		[DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
		private static extern IntPtr GetModuleHandle(string lpModuleName);

		public void InstallMouseHook()
		{
			if (_mouseHookHandle != IntPtr.Zero)
				return;

			_mouseHookCallback = MouseHookCallback;
			using (System.Diagnostics.Process curProcess = System.Diagnostics.Process.GetCurrentProcess())
			using (System.Diagnostics.ProcessModule curModule = curProcess.MainModule)
			{
				_mouseHookHandle = SetWindowsHookEx(WH_MOUSE_LL, _mouseHookCallback, GetModuleHandle(curModule.ModuleName), 0);
			}
		}

		public void UninstallMouseHook()
		{
			if (_mouseHookHandle != IntPtr.Zero)
			{
				UnhookWindowsHookEx(_mouseHookHandle);
				_mouseHookHandle = IntPtr.Zero;
			}
		}

		private IntPtr MouseHookCallback(int nCode, IntPtr wParam, IntPtr lParam)
		{
			if (nCode >= 0)
			{
				int msg = wParam.ToInt32();

				if (msg == WM_LBUTTONDOWN)
				{
					if (Root != null && Root.FormRadialMenu != null && Root.FormRadialMenu.Visible)
					{
						Point screenPt = System.Windows.Forms.Cursor.Position;
						Point clientPt = Root.FormRadialMenu.PointToClient(screenPt);
						Root.FormRadialMenu.UpdateHoverFromPoint(clientPt.X, clientPt.Y);
						Root.FormRadialMenu.CommitAndClose();
						_swallowNextLButtonUp = true;
						return (IntPtr)1; // Drop WM_LBUTTONDOWN from OS! InkOverlay will NEVER see it or draw a dot!
					}
				}
				else if (msg == WM_LBUTTONUP)
				{
					if (_swallowNextLButtonUp)
					{
						_swallowNextLButtonUp = false;
						return (IntPtr)1; // Drop WM_LBUTTONUP from OS!
					}
				}
				else if (msg == WM_RBUTTONDOWN)
				{
					if (Root != null && !Root.PointerMode && Root.Snapping <= 0 && this.Visible && !this.IsDisposed)
					{
						Point screenPt = System.Windows.Forms.Cursor.Position;
						Point clientPt = PointToClient(screenPt);

						bool onToolbar = (gpButtons != null && gpButtons.Visible && gpButtons.Bounds.Contains(clientPt)) ||
						                 (gpPenWidth != null && gpPenWidth.Visible && gpPenWidth.Bounds.Contains(clientPt));

						if (!onToolbar && !Root.FingerInAction)
						{
							Root.OpenRadialMenu(screenPt);
							return (IntPtr)1; // Drop WM_RBUTTONDOWN from OS! InkOverlay will NEVER see it!
						}
						else
						{
							return (IntPtr)1;
						}
					}
				}
				else if (msg == WM_MOUSEMOVE)
				{
					if (Root != null && Root.FormRadialMenu != null && Root.FormRadialMenu.Visible)
					{
						Point cur = System.Windows.Forms.Cursor.Position;
						Point clientPt = Root.FormRadialMenu.PointToClient(cur);
						Root.FormRadialMenu.UpdateHoverFromPoint(clientPt.X, clientPt.Y);
					}
				}
				else if (msg == WM_RBUTTONUP)
				{
					if (Root != null && Root.FormRadialMenu != null && Root.FormRadialMenu.Visible)
					{
						Root.FormRadialMenu.CommitAndClose();
						return (IntPtr)1; // Drop WM_RBUTTONUP from OS!
					}
					else if (Root != null && !Root.PointerMode && Root.Snapping <= 0 && this.Visible && !this.IsDisposed)
					{
						return (IntPtr)1;
					}
				}
			}

			return CallNextHookEx(_mouseHookHandle, nCode, wParam, lParam);
		}

		protected override void WndProc(ref Message m)
		{
			const int WM_SYSCOMMAND = 0x0112;
			const int SC_KEYMENU = 0xF100;
			if (m.Msg == WM_SYSCOMMAND && ((int)m.WParam & 0xFFF0) == SC_KEYMENU)
			{
				return; // Don't let Windows activate system menu bar on Alt release!
			}

			const int WM_MOUSEWHEEL = 0x020A;
			if (m.Msg == WM_MOUSEWHEEL)
			{
				if (Root.FormRadialMenu == null || !Root.FormRadialMenu.Visible)
				{
					if (!Root.PointerMode && Root.Snapping <= 0 && !Root.FingerInAction)
					{
						short delta = (short)((m.WParam.ToInt64() >> 16) & 0xFFFF);
						int curP = Root.CurrentPen >= 0 ? Root.CurrentPen : (Root.LastPen >= 0 ? Root.LastPen : 1);
						float curWidth = Root.PenAttr[curP].Width;

						int step;
						if (curWidth < 120) step = 20;
						else if (curWidth < 350) step = 40;
						else step = 80;

						int change = (delta > 0) ? step : -step;
						AdjustPenWidth(change);
						return;
					}
				}
			}

			const int WM_LBUTTONDOWN = 0x0201;
			const int WM_LBUTTONUP = 0x0202;
			const int WM_RBUTTONDOWN = 0x0204;
			const int WM_RBUTTONUP = 0x0205;
			const int WM_RBUTTONDBLCLK = 0x0206;

			if (m.Msg == WM_LBUTTONDOWN || m.Msg == WM_LBUTTONUP)
			{
				if (Root != null && Root.FormRadialMenu != null && Root.FormRadialMenu.Visible)
				{
					if (m.Msg == WM_LBUTTONDOWN)
					{
						Point screenPt = System.Windows.Forms.Cursor.Position;
						Point clientPt = Root.FormRadialMenu.PointToClient(screenPt);
						Root.FormRadialMenu.UpdateHoverFromPoint(clientPt.X, clientPt.Y);
						Root.FormRadialMenu.CommitAndClose();
					}
					return; // Never forward left clicks on radial menu to base or InkOverlay
				}
			}

			if (m.Msg == WM_RBUTTONDOWN || m.Msg == WM_RBUTTONUP || m.Msg == WM_RBUTTONDBLCLK)
			{
				if (Root != null && !Root.PointerMode && Root.Snapping <= 0)
				{
					if (m.Msg == WM_RBUTTONDOWN)
					{
						if (Root.FormRadialMenu == null || !Root.FormRadialMenu.Visible)
						{
							Root.OpenRadialMenu(System.Windows.Forms.Cursor.Position);
						}
					}
					else if (m.Msg == WM_RBUTTONUP)
					{
						if (Root.FormRadialMenu != null && Root.FormRadialMenu.Visible)
						{
							Root.FormRadialMenu.CommitAndClose();
						}
					}
					return; // Never forward right clicks to base or InkOverlay
				}
			}

			base.WndProc(ref m);
		}

		protected override void OnMouseWheel(MouseEventArgs e)
		{
			base.OnMouseWheel(e);

			if (Root.FormRadialMenu == null || !Root.FormRadialMenu.Visible)
			{
				if (!Root.PointerMode && Root.Snapping <= 0 && !Root.FingerInAction)
				{
					int curP = Root.CurrentPen >= 0 ? Root.CurrentPen : (Root.LastPen >= 0 ? Root.LastPen : 1);
					float curWidth = Root.PenAttr[curP].Width;

					int step;
					if (curWidth < 120) step = 20;
					else if (curWidth < 350) step = 40;
					else step = 80;

					int change = (e.Delta > 0) ? step : -step;
					AdjustPenWidth(change);
				}
			}
		}

		public FormCollection(Root root)
		{
			Root = root;
			InitializeComponent();

			PrimaryLeft = Screen.PrimaryScreen.Bounds.Left - SystemInformation.VirtualScreen.Left;
			PrimaryTop = Screen.PrimaryScreen.Bounds.Top - SystemInformation.VirtualScreen.Top;

			gpButtons.Height = (int)(Screen.PrimaryScreen.Bounds.Height * Root.ToolbarHeight);
			btClear.Height = (int)(gpButtons.Height * 0.88);
			btClear.Width = btClear.Height;
			btClear.Top = (int)(gpButtons.Height * 0.07);
			btDock.Height = (int)(gpButtons.Height * 0.88);
			btDock.Width = (int)(btDock.Height * 0.75);
			btDock.Top = (int)(gpButtons.Height * 0.07);
			btEraser.Height = (int)(gpButtons.Height * 0.88);
			btEraser.Width = btEraser.Height;
			btEraser.Top = (int)(gpButtons.Height * 0.07);
			btInkVisible.Height = (int)(gpButtons.Height * 0.88);
			btInkVisible.Width = btInkVisible.Height;
			btInkVisible.Top = (int)(gpButtons.Height * 0.07);
			btPan.Height = (int)(gpButtons.Height * 0.88);
			btPan.Width = btPan.Height;
			btPan.Top = (int)(gpButtons.Height * 0.07);
			btPointer.Height = (int)(gpButtons.Height * 0.88);
			btPointer.Width = btPointer.Height;
			btPointer.Top = (int)(gpButtons.Height * 0.07);
			btSnap.Height = (int)(gpButtons.Height * 0.88);
			btSnap.Width = btSnap.Height;
			btSnap.Top = (int)(gpButtons.Height * 0.07);
			btStop.Height = (int)(gpButtons.Height * 0.88);
			btStop.Width = btStop.Height;
			btStop.Top = (int)(gpButtons.Height * 0.07);
			btUndo.Height = (int)(gpButtons.Height * 0.88);
			btUndo.Width = btUndo.Height;
			btUndo.Top = (int)(gpButtons.Height * 0.07);

			btPen = new Button[Root.MaxPenCount];

			int cumulatedleft = (int)(btStop.Width * 1.2);
			for (int b = 0; b < Root.MaxPenCount; b++)
			{
				btPen[b] = new Button();
				btPen[b].Width = (int)(gpButtons.Height * 0.88);
				btPen[b].Height = (int)(gpButtons.Height * 0.88);
				btPen[b].Top = (int)(gpButtons.Height * 0.08);
				btPen[b].FlatAppearance.BorderColor = System.Drawing.Color.WhiteSmoke;
				btPen[b].FlatAppearance.BorderSize = 3;
				btPen[b].FlatAppearance.MouseOverBackColor = System.Drawing.Color.FromArgb(250, 50, 50);
				btPen[b].FlatStyle = System.Windows.Forms.FlatStyle.Flat;
				btPen[b].ForeColor = System.Drawing.Color.Transparent;
				//btPen[b].Name = "btPen" + b.ToString();
				btPen[b].UseVisualStyleBackColor = false;
				btPen[b].Click += new System.EventHandler(this.btColor_Click);
				btPen[b].BackColor = Root.PenAttr[b].Color;
				btPen[b].FlatAppearance.MouseDownBackColor = Root.PenAttr[b].Color;
				btPen[b].FlatAppearance.MouseOverBackColor = Root.PenAttr[b].Color;

				this.toolTip.SetToolTip(this.btPen[b], Root.Local.ButtonNamePen[b] + " (" + Root.Hotkey_Pens[b].ToString() + ")");

				btPen[b].MouseDown += gpButtons_MouseDown;
				btPen[b].MouseMove += gpButtons_MouseMove;
				btPen[b].MouseUp += gpButtons_MouseUp;

				gpButtons.Controls.Add(btPen[b]);

				if (Root.PenEnabled[b])
				{
					btPen[b].Visible = true;
					btPen[b].Left = cumulatedleft;
					cumulatedleft += (int)(btPen[b].Width * 1.1);
				}
				else
				{
					btPen[b].Visible = false;
				}
			}
			cumulatedleft += (int)(btStop.Width * 0.8);
			if (Root.EraserEnabled)
			{
				btEraser.Visible = true;
				btEraser.Left = cumulatedleft;
				cumulatedleft += (int)(btEraser.Width * 1.1);
			}
			else
			{
				btEraser.Visible = false;
			}
			if (Root.PanEnabled)
			{
				btPan.Visible = true;
				btPan.Left = cumulatedleft;
				cumulatedleft += (int)(btPan.Width * 1.1);
			}
			else
			{
				btPan.Visible = false;
			}
			if (Root.PointerEnabled)
			{
				btPointer.Visible = true;
				btPointer.Left = cumulatedleft;
				cumulatedleft += (int)(btPointer.Width * 1.1);
			}
			else
			{
				btPointer.Visible = false;
			}
			cumulatedleft += (int)(btStop.Width * 0.8);
			if (Root.PenWidthEnabled)
			{
				btPenWidth.Visible = true;
				btPenWidth.Left = cumulatedleft;
				cumulatedleft += (int)(btPenWidth.Width * 1.1);
			}
			else
			{
				btPenWidth.Visible = false;
			}
			if (Root.InkVisibleEnabled)
			{
				btInkVisible.Visible = true;
				btInkVisible.Left = cumulatedleft;
				cumulatedleft += (int)(btInkVisible.Width * 1.1);
			}
			else
			{
				btInkVisible.Visible = false;
			}
			if (Root.SnapEnabled)
			{
				btSnap.Visible = true;
				btSnap.Left = cumulatedleft;
				cumulatedleft += (int)(btSnap.Width * 1.1);
			}
			else
			{
				btSnap.Visible = false;
			}
			if (Root.UndoEnabled)
			{
				btUndo.Visible = true;
				btUndo.Left = cumulatedleft;
				cumulatedleft += (int)(btUndo.Width * 1.1);
			}
			else
			{
				btUndo.Visible = false;
			}
			if (Root.ClearEnabled)
			{
				btClear.Visible = true;
				btClear.Left = cumulatedleft;
				cumulatedleft += (int)(btClear.Width * 1.1);
			}
			else
			{
				btClear.Visible = false;
			}
			cumulatedleft += (int)(btStop.Width * 0.8);
			btStop.Left = cumulatedleft;
			gpButtons.Width = btStop.Right + (int)(btStop.Width * 0.5);
			

			this.Left = SystemInformation.VirtualScreen.Left;
			this.Top = SystemInformation.VirtualScreen.Top;
			//int targetbottom = 0;
			//foreach (Screen screen in Screen.AllScreens)
			//{
			//	if (screen.WorkingArea.Bottom > targetbottom)
			//		targetbottom = screen.WorkingArea.Bottom;
			//}
			//int virwidth = SystemInformation.VirtualScreen.Width;
			//this.Width = virwidth;
			//this.Height = targetbottom - this.Top;
			this.Width = SystemInformation.VirtualScreen.Width;
			this.Height = SystemInformation.VirtualScreen.Height - 2;
			this.DoubleBuffered = true;

			gpButtonsWidth = gpButtons.Width;
			gpButtonsHeight = gpButtons.Height;
			if (true || Root.AllowDraggingToolbar)
			{
				gpButtonsLeft = Root.gpButtonsLeft;
				gpButtonsTop = Root.gpButtonsTop;
				if
				(
					!(IsInsideVisibleScreen(gpButtonsLeft, gpButtonsTop) &&
					IsInsideVisibleScreen(gpButtonsLeft + gpButtonsWidth, gpButtonsTop) &&
					IsInsideVisibleScreen(gpButtonsLeft, gpButtonsTop + gpButtonsHeight) &&
					IsInsideVisibleScreen(gpButtonsLeft + gpButtonsWidth, gpButtonsTop + gpButtonsHeight))
					||
					(gpButtonsLeft == 0 && gpButtonsTop == 0)
				)
				{
					gpButtonsLeft = Screen.PrimaryScreen.WorkingArea.Right - gpButtons.Width + PrimaryLeft;
					gpButtonsTop = Screen.PrimaryScreen.WorkingArea.Bottom - gpButtons.Height - 15 + PrimaryTop;
				}
			}
			else
			{
				gpButtonsLeft = Screen.PrimaryScreen.WorkingArea.Right - gpButtons.Width + PrimaryLeft;
				gpButtonsTop = Screen.PrimaryScreen.WorkingArea.Bottom - gpButtons.Height - 15 + PrimaryTop;
			}

			gpButtons.Left = gpButtonsLeft + gpButtons.Width;
			gpButtons.Top = gpButtonsTop;
			gpPenWidth.Left = gpButtonsLeft + btPenWidth.Left - gpPenWidth.Width / 2 + btPenWidth.Width / 2;
			gpPenWidth.Top = gpButtonsTop - gpPenWidth.Height - 10;

			pboxPenWidthIndicator.Top = 0;
			pboxPenWidthIndicator.Left = (int)Math.Sqrt(Root.GlobalPenWidth * 30);
			gpPenWidth.Controls.Add(pboxPenWidthIndicator);

			gpButtons.Paint += gpButtons_Paint;
			gpButtons.Resize += (s, e) => UpdateChassisRegion();
			gpPenWidth.Paint += gpPenWidth_Paint;
			gpPenWidth.Resize += (s, e) => UpdateChassisRegion();

			if (!Root.ShowBottomToolbar || Root.AlwaysHideToolbar)
			{
				gpButtons.Visible = false;
				gpPenWidth.Visible = false;
			}

			IC = new InkOverlay(this.Handle);
			IC.CollectionMode = CollectionMode.InkOnly;
			IC.AutoRedraw = false;
			IC.DynamicRendering = false;
			IC.EraserMode = InkOverlayEraserMode.StrokeErase;
			IC.CursorInRange += IC_CursorInRange;
			IC.MouseDown += IC_MouseDown;
			IC.MouseMove += IC_MouseMove;
			IC.MouseUp += IC_MouseUp;
			IC.CursorDown += IC_CursorDown;
			IC.Stroke += IC_Stroke;
			IC.DefaultDrawingAttributes.Width = 80;
			IC.DefaultDrawingAttributes.Transparency = 30;
			IC.DefaultDrawingAttributes.AntiAliased = true;

			IC.Enabled = true;
			InstallMouseHook();

			ApplyTheme(Root.CurrentTheme);

			LastTickTime = DateTime.Parse("1987-01-01");
			tiSlide.Enabled = true;

			ToTransparent();
			ToTopMost();

			this.toolTip.SetToolTip(this.btDock, Root.Local.ButtonNameDock);
			this.toolTip.SetToolTip(this.btPenWidth, Root.Local.ButtonNamePenwidth);
			this.toolTip.SetToolTip(this.btEraser, Root.Local.ButtonNameErasor + " (" + Root.Hotkey_Eraser.ToString() + ")");
			this.toolTip.SetToolTip(this.btPan, Root.Local.ButtonNamePan + " (" + Root.Hotkey_Pan.ToString() + ")");
			this.toolTip.SetToolTip(this.btPointer, Root.Local.ButtonNameMousePointer + " (" + Root.Hotkey_Pointer.ToString() + ")");
			this.toolTip.SetToolTip(this.btInkVisible, Root.Local.ButtonNameInkVisible + " (" + Root.Hotkey_InkVisible.ToString() + ")");
			this.toolTip.SetToolTip(this.btSnap, Root.Local.ButtonNameSnapshot + " (" + Root.Hotkey_Snap.ToString() + ")");
			this.toolTip.SetToolTip(this.btUndo, Root.Local.ButtonNameUndo + " (" + Root.Hotkey_Undo.ToString() + ")");
			this.toolTip.SetToolTip(this.btClear, Root.Local.ButtonNameClear + " (" + Root.Hotkey_Clear.ToString() + ")");
			this.toolTip.SetToolTip(this.btStop, Root.Local.ButtonNameExit + " (ESC)");
		}

		private void IC_Stroke(object sender, InkCollectorStrokeEventArgs e)
		{
			if (Root.PanMode || Root.Snapping > 0 || SuppressStrokeFromRadialMenu || (GetAsyncKeyState(0x02) & 0x8000) != 0 || (Root.FormRadialMenu != null && Root.FormRadialMenu.Visible))
			{
				SuppressStrokeFromRadialMenu = false;
				e.Cancel = true;
				try
				{
					if (e.Stroke != null && !e.Stroke.Deleted)
					{
						IC.Ink.DeleteStroke(e.Stroke);
					}
				}
				catch { }
				if (Root.FormDisplay != null)
				{
					Root.FormDisplay.ClearCanvus();
					Root.FormDisplay.DrawStrokes();
					Root.FormDisplay.DrawButtons(false);
					Root.FormDisplay.UpdateFormDisplay(true);
				}
				return;
			}

			SaveUndoStrokes();
		}

		private void SaveUndoStrokes()
		{
			Root.RedoDepth = 0;
			if (Root.UndoDepth < Root.UndoStrokes.GetLength(0) - 1)
				Root.UndoDepth++;

			Root.UndoP++;
			if (Root.UndoP >= Root.UndoStrokes.GetLength(0))
				Root.UndoP = 0;

			if (Root.UndoStrokes[Root.UndoP] == null)
				Root.UndoStrokes[Root.UndoP] = new Ink();
			Root.UndoStrokes[Root.UndoP].DeleteStrokes();
			if (IC.Ink.Strokes.Count > 0)
				Root.UndoStrokes[Root.UndoP].AddStrokesAtRectangle(IC.Ink.Strokes, IC.Ink.Strokes.GetBoundingBox());
		}

		private void IC_CursorDown(object sender, InkCollectorCursorDownEventArgs e)
		{
			if (SuppressStrokeFromRadialMenu || (GetAsyncKeyState(0x02) & 0x8000) != 0 || (Root.FormRadialMenu != null && Root.FormRadialMenu.Visible))
			{
				return;
			}

			if (!Root.InkVisible && Root.Snapping <= 0)
			{
				Root.SetInkVisible(true);
			}

			Root.FormDisplay.ClearCanvus(Root.FormDisplay.gOneStrokeCanvus);
			Root.FormDisplay.DrawStrokes(Root.FormDisplay.gOneStrokeCanvus);
			Root.FormDisplay.DrawButtons(Root.FormDisplay.gOneStrokeCanvus, false);
		}

		private void IC_MouseDown(object sender, CancelMouseEventArgs e)
		{
			if (Root.FormRadialMenu != null && Root.FormRadialMenu.Visible)
			{
				SuppressStrokeFromRadialMenu = true;
				Root.FormRadialMenu.CommitAndClose();
				e.Cancel = true;
				return;
			}

			if (Root.gpPenWidthVisible)
			{
				Root.gpPenWidthVisible = false;
				Root.UponSubPanelUpdate = true;
			}

			if (e.Button == MouseButtons.Right || (GetAsyncKeyState(0x02) & 0x8000) != 0)
			{
				e.Cancel = true;
				return;
			}

			Root.FingerInAction = true;
			if (Root.Snapping == 1)
			{
				Root.SnappingX = e.X;
				Root.SnappingY = e.Y;
				Root.SnappingRect = new Rectangle(e.X, e.Y, 0, 0);
				Root.Snapping = 2;
			}

			if (!Root.InkVisible && Root.Snapping <= 0)
			{
				Root.SetInkVisible(true);
			}

			LasteXY.X = e.X;
			LasteXY.Y = e.Y;
			IC.Renderer.PixelToInkSpace(Root.FormDisplay.gOneStrokeCanvus, ref LasteXY);
		}

		public Point LasteXY;
		private void IC_MouseMove(object sender, CancelMouseEventArgs e)
		{
			if (LasteXY.X == 0 && LasteXY.Y == 0)
			{
				LasteXY.X = e.X;
				LasteXY.Y = e.Y;
				IC.Renderer.PixelToInkSpace(Root.FormDisplay.gOneStrokeCanvus, ref LasteXY);
			}
			Point currentxy = new Point(e.X, e.Y);
			IC.Renderer.PixelToInkSpace(Root.FormDisplay.gOneStrokeCanvus, ref currentxy);

			if (Root.Snapping == 2)
			{
				int left = Math.Min(Root.SnappingX, e.X);
				int top = Math.Min(Root.SnappingY, e.Y);
				int width = Math.Abs(Root.SnappingX - e.X);
				int height = Math.Abs(Root.SnappingY - e.Y);
				Root.SnappingRect = new Rectangle(left, top, width, height);

				if (LasteXY != currentxy)
					Root.MouseMovedUnderSnapshotDragging = true;
			}
			else if (Root.PanMode && Root.FingerInAction)
			{
				Root.Pan(currentxy.X - LasteXY.X, currentxy.Y - LasteXY.Y);			
			}

			LasteXY = currentxy;
		}

		private void IC_MouseUp(object sender, CancelMouseEventArgs e)
		{
			if (e.Button == MouseButtons.Right)
			{
				e.Cancel = true;
				return;
			}

			Root.FingerInAction = false;
			if (Root.Snapping == 2)
			{
				int left = Math.Min(Root.SnappingX, e.X);
				int top = Math.Min(Root.SnappingY, e.Y);
				int width = Math.Abs(Root.SnappingX - e.X);
				int height = Math.Abs(Root.SnappingY - e.Y);
				if (width < 5 || height < 5)
				{
					left = 0;
					top = 0;
					width = this.Width;
					height = this.Height;
				}
				Root.SnappingRect = new Rectangle(left + this.Left, top + this.Top, width, height);
				Root.UponTakingSnap = true;
				ExitSnapping();
			}
			else if (Root.PanMode)
			{
				SaveUndoStrokes();
			}
			else
			{
				Root.UponAllDrawingUpdate = true;
			}
		}

		private void IC_CursorInRange(object sender, InkCollectorCursorInRangeEventArgs e)
		{
			if (e.Cursor.Inverted && Root.CurrentPen != -1)
			{
				EnterEraserMode(true);
				/*
				// temperary eraser icon light
				if (btEraser.Image == image_eraser)
				{
					btEraser.Image = image_eraser_act;
					Root.FormDisplay.DrawButtons(true);
					Root.FormDisplay.UpdateFormDisplay();
				}
				*/
			}
			else if (!e.Cursor.Inverted && Root.CurrentPen != -1)
			{
				EnterEraserMode(false);
				/*
				if (btEraser.Image == image_eraser_act)
				{
					btEraser.Image = image_eraser;
					Root.FormDisplay.DrawButtons(true);
					Root.FormDisplay.UpdateFormDisplay();
				}
				*/
			}
		}

		public void ToTransparent()
		{
			UInt32 dwExStyle = GetWindowLong(this.Handle, -20);
			SetWindowLong(this.Handle, -20, dwExStyle | 0x00080000);
			SetLayeredWindowAttributes(this.Handle, 0x00FFFFFF, 1, 0x2);
		}

		public void ToTopMost()
		{
			SetWindowPos(this.Handle, (IntPtr)(-1), 0, 0, 0, 0, 0x0002 | 0x0001 | 0x0020);
		}

		public void ToThrough()
		{
			UInt32 dwExStyle = GetWindowLong(this.Handle, -20);
			//SetWindowLong(this.Handle, -20, dwExStyle | 0x00080000);
			//SetWindowPos(this.Handle, (IntPtr)0, 0, 0, 0, 0, 0x0002 | 0x0001 | 0x0004 | 0x0010 | 0x0020);
			//SetLayeredWindowAttributes(this.Handle, 0x00FFFFFF, 1, 0x2);
			SetWindowLong(this.Handle, -20, dwExStyle | 0x00080000 | 0x00000020);
			//SetWindowPos(this.Handle, (IntPtr)(1), 0, 0, 0, 0, 0x0002 | 0x0001 | 0x0010 | 0x0020);
		}

		public void ToUnThrough()
		{
			UInt32 dwExStyle = GetWindowLong(this.Handle, -20);
			//SetWindowLong(this.Handle, -20, (uint)(dwExStyle & ~0x00080000 & ~0x0020));
			SetWindowLong(this.Handle, -20, (uint)(dwExStyle & ~0x0020));
			//SetWindowPos(this.Handle, (IntPtr)(-2), 0, 0, 0, 0, 0x0002 | 0x0001 | 0x0010 | 0x0020);

			//dwExStyle = GetWindowLong(this.Handle, -20);
			//SetWindowLong(this.Handle, -20, dwExStyle | 0x00080000);
			//SetLayeredWindowAttributes(this.Handle, 0x00FFFFFF, 1, 0x2);
			//SetWindowPos(this.Handle, (IntPtr)(-1), 0, 0, 0, 0, 0x0002 | 0x0001 | 0x0020);
		}

		public void EnterEraserMode(bool enter)
		{
			try
			{
				if (enter)
				{
					if (IC.CollectingInk)
					{
						IC.Enabled = false;
						IC.EditingMode = InkOverlayEditingMode.Delete;
						IC.Enabled = true;
					}
					else
					{
						IC.EditingMode = InkOverlayEditingMode.Delete;
					}
					Root.EraserMode = true;
				}
				else
				{
					if (IC.CollectingInk)
					{
						IC.Enabled = false;
						IC.EditingMode = InkOverlayEditingMode.Ink;
						IC.Enabled = true;
					}
					else
					{
						IC.EditingMode = InkOverlayEditingMode.Ink;
					}
					Root.EraserMode = false;
				}
			}
			catch
			{
				try
				{
					IC.Enabled = false;
					IC.EditingMode = enter ? InkOverlayEditingMode.Delete : InkOverlayEditingMode.Ink;
					IC.Enabled = true;
					Root.EraserMode = enter;
				}
				catch { }
			}
		}

		public void SelectPen(int pen)
		{
			// -3=pan, -2=pointer, -1=erasor, 0+=pens
			if (pen == -3)
			{
				for (int b = 0; b < Root.MaxPenCount; b++)
					btPen[b].Image = image_pen[b];
				btEraser.Image = image_eraser;
				btPointer.Image = image_pointer;
				btPan.Image = image_pan_act;
				EnterEraserMode(false);
				Root.UnPointer();
				Root.PanMode = true;

				UpdateCursor();

				try
				{
					IC.SetWindowInputRectangle(new Rectangle(0, 0, 1, 1));
				}
				catch
				{
					try
					{
						Thread.Sleep(1);
						IC.SetWindowInputRectangle(new Rectangle(0, 0, 1, 1));
					}
					catch { }
				}
			}
			else if (pen == -2)
			{
				for (int b = 0; b < Root.MaxPenCount; b++)
					btPen[b].Image = image_pen[b];
				btEraser.Image = image_eraser;
				btPointer.Image = image_pointer_act;
				btPan.Image = image_pan;
				EnterEraserMode(false);
				Root.Pointer();
				Root.PanMode = false;
				UpdateCursor();
			}
			else if (pen == -1)
			{
				if (this.Cursor != System.Windows.Forms.Cursors.Default)
					this.Cursor = System.Windows.Forms.Cursors.Default;

				for (int b = 0; b < Root.MaxPenCount; b++)
					btPen[b].Image = image_pen[b];
				btEraser.Image = image_eraser_act;
				btPointer.Image = image_pointer;
				btPan.Image = image_pan;
				EnterEraserMode(true);
				Root.UnPointer();
				Root.PanMode = false;

				UpdateCursor();

				try
				{
					IC.SetWindowInputRectangle(new Rectangle(0, 0, this.Width, this.Height));
				}
				catch
				{
					//Thread.Sleep(1);
					//IC.SetWindowInputRectangle(new Rectangle(0, 0, this.Width, this.Height));
				}
			}
			else if (pen >= 0)
			{
				if (this.Cursor != System.Windows.Forms.Cursors.Default)
					this.Cursor = System.Windows.Forms.Cursors.Default;

				IC.DefaultDrawingAttributes = Root.PenAttr[pen].Clone();
				if (Root.PenWidthEnabled)
				{
					IC.DefaultDrawingAttributes.Width = Root.GlobalPenWidth;
				}
				else
				{
					Root.GlobalPenWidth = (int)Root.PenAttr[pen].Width;
				}
				for (int b = 0; b < Root.MaxPenCount; b++)
					btPen[b].Image = image_pen[b];
				btPen[pen].Image = image_pen_act[pen];
				btEraser.Image = image_eraser;
				btPointer.Image = image_pointer;
				btPan.Image = image_pan;
				EnterEraserMode(false);
				Root.UnPointer();
				Root.PanMode = false;

				UpdateCursor();

				try
				{
					IC.SetWindowInputRectangle(new Rectangle(0, 0, this.Width, this.Height));
				}
				catch
				{
					//Thread.Sleep(1);
					//IC.SetWindowInputRectangle(new Rectangle(0, 0, this.Width, this.Height));
				}
			}
			Root.CurrentPen = pen;
			UpdatePenBorders(pen);
			if (Root.gpPenWidthVisible)
			{
				Root.gpPenWidthVisible = false;
				Root.UponSubPanelUpdate = true;
			}
			else
				Root.UponButtonsUpdate |= 0x2;

			if (pen != -2)
				Root.LastPen = pen;
		}

		public void UpdatePenBorders(int activePen)
		{
			UITheme theme = Root.CurrentTheme;
			if (theme == null || btPen == null) return;

			for (int b = 0; b < Root.MaxPenCount; b++)
			{
				if (btPen[b] != null)
				{
					bool isSelected = (activePen == b && !Root.EraserMode && !Root.PointerMode && !Root.PanMode);
					btPen[b].FlatAppearance.BorderSize = isSelected ? 3 : 1;
					btPen[b].FlatAppearance.BorderColor = isSelected ? theme.PenActiveBorder : theme.PenNormalBorder;
				}
			}
		}

		public void ApplyTheme(UITheme theme)
		{
			if (theme == null) return;

			ModernIcons.SetPalette(theme.IconInactive, theme.IconActive, theme.IconDanger);

			DisposeBitmap(ref image_exit);
			DisposeBitmap(ref image_clear);
			DisposeBitmap(ref image_undo);
			DisposeBitmap(ref image_eraser);
			DisposeBitmap(ref image_eraser_act);
			DisposeBitmap(ref image_pan);
			DisposeBitmap(ref image_pan_act);
			DisposeBitmap(ref image_visible);
			DisposeBitmap(ref image_visible_not);
			DisposeBitmap(ref image_snap);
			DisposeBitmap(ref image_penwidth);
			DisposeBitmap(ref image_dock);
			DisposeBitmap(ref image_dockback);
			DisposeBitmap(ref image_pointer);
			DisposeBitmap(ref image_pointer_act);
			DisposeBitmap(ref image_pencil);
			DisposeBitmap(ref image_pencil_act);
			DisposeBitmap(ref image_highlighter);
			DisposeBitmap(ref image_highlighter_act);

			if (image_pen != null)
			{
				for (int i = 0; i < image_pen.Length; i++)
					DisposeBitmap(ref image_pen[i]);
			}
			if (image_pen_act != null)
			{
				for (int i = 0; i < image_pen_act.Length; i++)
					DisposeBitmap(ref image_pen_act[i]);
			}

			// Recreate vector icons
			image_exit = ModernIcons.CreateIcon(ModernIconType.Exit, btStop.Width, btStop.Height, false);
			image_clear = ModernIcons.CreateIcon(ModernIconType.Clear, btClear.Width, btClear.Height, false);
			image_undo = ModernIcons.CreateIcon(ModernIconType.Undo, btUndo.Width, btUndo.Height, false);
			image_eraser = ModernIcons.CreateIcon(ModernIconType.Eraser, btEraser.Width, btEraser.Height, false);
			image_eraser_act = ModernIcons.CreateIcon(ModernIconType.Eraser, btEraser.Width, btEraser.Height, true);
			image_pan = ModernIcons.CreateIcon(ModernIconType.Pan, btPan.Width, btPan.Height, false);
			image_pan_act = ModernIcons.CreateIcon(ModernIconType.Pan, btPan.Width, btPan.Height, true);
			image_visible = ModernIcons.CreateIcon(ModernIconType.Visible, btInkVisible.Width, btInkVisible.Height, false);
			image_visible_not = ModernIcons.CreateIcon(ModernIconType.VisibleNot, btInkVisible.Width, btInkVisible.Height, true);
			image_snap = ModernIcons.CreateIcon(ModernIconType.Snapshot, btSnap.Width, btSnap.Height, false);
			image_penwidth = ModernIcons.CreateIcon(ModernIconType.PenWidth, btPenWidth.Width, btPenWidth.Height, false);
			image_dock = ModernIcons.CreateIcon(ModernIconType.Dock, btDock.Width, btDock.Height, false);
			image_dockback = ModernIcons.CreateIcon(ModernIconType.DockBack, btDock.Width, btDock.Height, true);
			image_pointer = ModernIcons.CreateIcon(ModernIconType.Pointer, btPointer.Width, btPointer.Height, false);
			image_pointer_act = ModernIcons.CreateIcon(ModernIconType.Pointer, btPointer.Width, btPointer.Height, true);
			image_pencil = ModernIcons.CreateIcon(ModernIconType.Pen, btPen[2].Width, btPen[2].Height, false);
			image_pencil_act = ModernIcons.CreateIcon(ModernIconType.Pen, btPen[2].Width, btPen[2].Height, true);
			image_highlighter = ModernIcons.CreateIcon(ModernIconType.Highlighter, btPen[2].Width, btPen[2].Height, false);
			image_highlighter_act = ModernIcons.CreateIcon(ModernIconType.Highlighter, btPen[2].Width, btPen[2].Height, true);

			image_pen = new Bitmap[Root.MaxPenCount];
			image_pen_act = new Bitmap[Root.MaxPenCount];
			for (int b = 0; b < Root.MaxPenCount; b++)
			{
				bool isHighlighter = Root.PenAttr[b].Transparency >= 100;
				ModernIconType penType = isHighlighter ? ModernIconType.Highlighter : ModernIconType.Pen;
				Color penCol = Root.PenAttr[b].Color;

				image_pen[b] = ModernIcons.CreateIcon(penType, btPen[b].Width, btPen[b].Height, false, penCol);
				image_pen_act[b] = ModernIcons.CreateIcon(penType, btPen[b].Width, btPen[b].Height, true, penCol);
			}

			// Style Bottom Panel Chassis
			gpButtons.BackColor = theme.ToolbarBg;

			// Style Tool Buttons
			Button[] toolButtons = new Button[]
			{
				btDock, btPenWidth, btEraser, btPan, btPointer, btInkVisible, btSnap, btUndo, btClear, btStop
			};
			foreach (Button btn in toolButtons)
			{
				if (btn != null)
				{
					btn.BackColor = theme.ToolbarBg;
					btn.FlatAppearance.MouseOverBackColor = theme.ButtonHoverBg;
					btn.FlatAppearance.MouseDownBackColor = theme.ButtonDownBg;
				}
			}

			// Style Pen Buttons
			for (int b = 0; b < Root.MaxPenCount; b++)
			{
				if (btPen[b] != null)
				{
					btPen[b].BackColor = Root.PenAttr[b].Color;
					btPen[b].FlatAppearance.MouseDownBackColor = Root.PenAttr[b].Color;
					btPen[b].FlatAppearance.MouseOverBackColor = Root.PenAttr[b].Color;
				}
			}
			UpdatePenBorders(Root.CurrentPen);

			// Refresh active button images
			btStop.Image = image_exit;
			btClear.Image = image_clear;
			btUndo.Image = image_undo;
			btSnap.Image = image_snap;
			btPenWidth.Image = image_penwidth;
			btDock.Image = Root.Docked ? image_dockback : image_dock;
			btInkVisible.Image = Root.InkVisible ? image_visible : image_visible_not;

			if (Root.PanMode)
				SelectPen(-3);
			else if (Root.PointerMode)
				SelectPen(-2);
			else if (Root.EraserMode)
				SelectPen(-1);
			else if (Root.CurrentPen >= 0)
				SelectPen(Root.CurrentPen);

			// Style Pen Width Panel
			gpPenWidth.BackColor = theme.PenWidthPanelBg;
			pboxPenWidthIndicator.BackColor = theme.PenWidthIndicatorColor;

			UpdateChassisRegion();
			gpButtons.Invalidate();
			gpPenWidth.Invalidate();
		}

		private void UpdateChassisRegion()
		{
			if (gpButtons.Width > 0 && gpButtons.Height > 0)
			{
				int r = Math.Min(14, gpButtons.Height / 3);
				using (GraphicsPath path = CreateRoundedRectanglePath(0, 0, gpButtons.Width, gpButtons.Height, r))
				{
					gpButtons.Region = new Region(path);
				}
			}
			if (gpPenWidth.Width > 0 && gpPenWidth.Height > 0)
			{
				int r = Math.Min(12, gpPenWidth.Height / 3);
				using (GraphicsPath path = CreateRoundedRectanglePath(0, 0, gpPenWidth.Width, gpPenWidth.Height, r))
				{
					gpPenWidth.Region = new Region(path);
				}
			}
		}

		private static GraphicsPath CreateRoundedRectanglePath(float x, float y, float width, float height, float radius)
		{
			GraphicsPath path = new GraphicsPath();
			float d = radius * 2f;
			path.AddArc(x, y, d, d, 180, 90);
			path.AddArc(x + width - d, y, d, d, 270, 90);
			path.AddArc(x + width - d, y + height - d, d, d, 0, 90);
			path.AddArc(x, y + height - d, d, d, 90, 90);
			path.CloseFigure();
			return path;
		}

		private void gpButtons_Paint(object sender, PaintEventArgs e)
		{
			UITheme theme = Root.CurrentTheme;
			if (theme == null) return;

			Graphics g = e.Graphics;
			g.SmoothingMode = SmoothingMode.AntiAlias;

			int w = gpButtons.Width;
			int h = gpButtons.Height;
			int r = Math.Min(14, h / 3);

			// 1px outer border
			using (GraphicsPath borderPath = CreateRoundedRectanglePath(0.5f, 0.5f, w - 1f, h - 1f, r))
			using (Pen borderPen = new Pen(theme.ToolbarBorder, 1.2f))
			{
				g.DrawPath(borderPen, borderPath);
			}

			// 1px top specular rim highlight
			using (Pen rimPen = new Pen(theme.SpecularRim, 1.0f))
			{
				g.DrawLine(rimPen, r, 1.5f, w - r, 1.5f);
			}
		}

		private void gpPenWidth_Paint(object sender, PaintEventArgs e)
		{
			UITheme theme = Root.CurrentTheme;
			if (theme == null) return;

			Graphics g = e.Graphics;
			g.SmoothingMode = SmoothingMode.AntiAlias;

			int w = gpPenWidth.Width;
			int h = gpPenWidth.Height;
			int r = Math.Min(12, h / 3);

			// 1px outer border
			using (GraphicsPath borderPath = CreateRoundedRectanglePath(0.5f, 0.5f, w - 1f, h - 1f, r))
			using (Pen borderPen = new Pen(theme.ToolbarBorder, 1.2f))
			{
				g.DrawPath(borderPen, borderPath);
			}

			// Center slider track line
			using (Pen trackPen = new Pen(theme.PenWidthTrackColor, 2.0f))
			{
				g.DrawLine(trackPen, 15, h / 2, w - 15, h / 2);
			}
		}

		public void TogglePenOrHighlighter(int penIndex)
		{
			if (penIndex < 0 || penIndex >= Root.MaxPenCount)
				return;

			bool currentlyHighlighter = Root.PenAttr[penIndex].Transparency >= 100;

			if (currentlyHighlighter)
			{
				// Switch to Pencil / Pen (opaque)
				Root.PenAttr[penIndex].Transparency = 0;
				if (Root.PenAttr[penIndex].Width >= 300)
					Root.PenAttr[penIndex].Width = 80;
			}
			else
			{
				// Switch to Highlighter (translucent)
				Root.PenAttr[penIndex].Transparency = 175;
				if (Root.PenAttr[penIndex].Width < 300)
					Root.PenAttr[penIndex].Width = 500;
			}

			// Update icons for this pen
			bool isHighlighter = Root.PenAttr[penIndex].Transparency >= 100;
			ModernIconType penType = isHighlighter ? ModernIconType.Highlighter : ModernIconType.Pen;
			Color penCol = Root.PenAttr[penIndex].Color;

			if (image_pen != null && image_pen[penIndex] != null)
			{
				try { image_pen[penIndex].Dispose(); } catch { }
				image_pen[penIndex] = ModernIcons.CreateIcon(penType, btPen[penIndex].Width, btPen[penIndex].Height, false, penCol);
			}
			if (image_pen_act != null && image_pen_act[penIndex] != null)
			{
				try { image_pen_act[penIndex].Dispose(); } catch { }
				image_pen_act[penIndex] = ModernIcons.CreateIcon(penType, btPen[penIndex].Width, btPen[penIndex].Height, true, penCol);
			}

			// Update tooltip
			string penName = isHighlighter ? Root.Local.OptionsPensHighlighter : Root.Local.ButtonNamePen[penIndex];
			this.toolTip.SetToolTip(this.btPen[penIndex], penName + " (" + Root.Hotkey_Pens[penIndex].ToString() + ")");

			// Re-select pen so IC, GlobalPenWidth, and cursor update immediately
			SelectPen(penIndex);
			Root.UponButtonsUpdate |= 0x2;
		}

		public void AdjustPenWidth(int delta)
		{
			int targetPen = Root.CurrentPen >= 0 ? Root.CurrentPen : (Root.LastPen >= 0 ? Root.LastPen : 1);
			int currentWidth = (int)Root.PenAttr[targetPen].Width;
			int newWidth = Math.Max(20, Math.Min(2500, currentWidth + delta));
			Root.PenAttr[targetPen].Width = newWidth;
			Root.GlobalPenWidth = newWidth;
			if (IC != null && IC.DefaultDrawingAttributes != null)
			{
				IC.DefaultDrawingAttributes.Width = newWidth;
			}
			UpdateCursor();
			Root.UponButtonsUpdate |= 0x2;
		}

		public void RetreatAndExit()
		{
			UninstallMouseHook();
			ToThrough();
			Root.ClearInk();
			SaveUndoStrokes();
			Root.SaveOptions("config.ini");
			Root.gpPenWidthVisible = false;

			LastTickTime = DateTime.Now;
			ButtonsEntering = -9;
		}

		private void Form1_Load(object sender, EventArgs e)
		{
		}

		public void btDock_Click(object sender, EventArgs e)
		{
			if (ToolbarMoved)
			{
				ToolbarMoved = false;
				return;
			}

			LastTickTime = DateTime.Now;
			if (!Root.Docked)
			{
				Root.Dock();
			}
			else
			{
				Root.UnDock();
			}
		}

		public void btPointer_Click(object sender, EventArgs e)
		{
			if (ToolbarMoved)
			{
				ToolbarMoved = false;
				return;
			}

			SelectPen(-2);
		}


		private void btPenWidth_Click(object sender, EventArgs e)
		{
			if (ToolbarMoved)
			{
				ToolbarMoved = false;
				return;
			}

			if (Root.PointerMode)
				return;

			Root.gpPenWidthVisible = !Root.gpPenWidthVisible;
			if (Root.gpPenWidthVisible)
			{
				if (Root.CurrentPen >= 0)
				{
					Root.GlobalPenWidth = (int)Root.PenAttr[Root.CurrentPen].Width;
					pboxPenWidthIndicator.Left = Math.Max(10, Math.Min(gpPenWidth.Width - 10, (int)Math.Sqrt(Root.GlobalPenWidth * 30))) - pboxPenWidthIndicator.Width / 2;
				}
				Root.UponButtonsUpdate |= 0x2;
			}
			else
			{
				Root.UponSubPanelUpdate = true;
			}
		}

		public void btSnap_Click(object sender, EventArgs e)
		{
			if (ToolbarMoved)
			{
				ToolbarMoved = false;
				return;
			}

			if (Root.Snapping > 0)
				return;

			Root.gpPenWidthVisible = false;

			try
			{
				IC.SetWindowInputRectangle(new Rectangle(0, 0, this.Width, this.Height));
			}
			catch
			{
				try
				{
					Thread.Sleep(1);
					IC.SetWindowInputRectangle(new Rectangle(0, 0, this.Width, this.Height));
				}
				catch { }
			}

			this.Cursor = System.Windows.Forms.Cursors.Cross;
			IC.Cursor = System.Windows.Forms.Cursors.Cross;
			System.Windows.Forms.Cursor.Current = System.Windows.Forms.Cursors.Cross;

			Root.Snapping = 1;
			ButtonsEntering = -1;
			Root.UponButtonsUpdate |= 0x2;
		}

		public void ExitSnapping()
		{
			try
			{
				IC.SetWindowInputRectangle(new Rectangle(0, 0, this.Width, this.Height));
			}
			catch
			{
				try
				{
					Thread.Sleep(1);
					IC.SetWindowInputRectangle(new Rectangle(0, 0, this.Width, this.Height));
				}
				catch { }
			}
			Root.SnappingX = -1;
			Root.SnappingY = -1;
			Root.Snapping = -60;
			ButtonsEntering = 1;
			Root.SelectPen(Root.CurrentPen);

			UpdateCursor();
		}

		public void btStop_Click(object sender, EventArgs e)
		{
			if (ToolbarMoved)
			{
				ToolbarMoved = false;
				return;
			}

			RetreatAndExit();
		}

		DateTime LastTickTime;
		bool[] LastPenStatus = new bool[10];
		bool LastEraserStatus = false;
		bool LastVisibleStatus = false;
		bool LastPointerStatus = false;
		bool LastPanStatus = false;
		bool LastUndoStatus = false;
		bool LastRedoStatus = false;
		bool LastSnapStatus = false;
		bool LastClearStatus = false;

		private void ApplyPenWidthFromX(int x)
		{
			if (x < 10 || gpPenWidth.Width - x < 10)
				return;

			Root.GlobalPenWidth = Math.Max(30, Math.Min(3000, x * x / 30));
			pboxPenWidthIndicator.Left = x - pboxPenWidthIndicator.Width / 2;
			IC.DefaultDrawingAttributes.Width = Root.GlobalPenWidth;
			if (Root.CurrentPen >= 0)
			{
				Root.PenAttr[Root.CurrentPen].Width = Root.GlobalPenWidth;
			}
			UpdateCursor();
			Root.UponButtonsUpdate |= 0x2;
		}

		private void gpPenWidth_MouseDown(object sender, MouseEventArgs e)
		{
			gpPenWidth_MouseOn = true;
			ApplyPenWidthFromX(e.X);
		}

		private void gpPenWidth_MouseMove(object sender, MouseEventArgs e)
		{
			if (gpPenWidth_MouseOn)
			{
				ApplyPenWidthFromX(e.X);
			}
		}

		private void gpPenWidth_MouseUp(object sender, MouseEventArgs e)
		{
			ApplyPenWidthFromX(e.X);
			UpdateCursor();

			Root.gpPenWidthVisible = false;
			Root.UponSubPanelUpdate = true;
			gpPenWidth_MouseOn = false;
		}

		private void pboxPenWidthIndicator_MouseDown(object sender, MouseEventArgs e)
		{
			gpPenWidth_MouseOn = true;
			int x = e.X + pboxPenWidthIndicator.Left;
			ApplyPenWidthFromX(x);
		}

		private void pboxPenWidthIndicator_MouseMove(object sender, MouseEventArgs e)
		{
			if (gpPenWidth_MouseOn)
			{
				int x = e.X + pboxPenWidthIndicator.Left;
				ApplyPenWidthFromX(x);
			}
		}

		private void pboxPenWidthIndicator_MouseUp(object sender, MouseEventArgs e)
		{
			int x = e.X + pboxPenWidthIndicator.Left;
			ApplyPenWidthFromX(x);
			UpdateCursor();

			Root.gpPenWidthVisible = false;
			Root.UponSubPanelUpdate = true;
			gpPenWidth_MouseOn = false;
		}

		public void UpdateCursor()
		{
			if (Root.PointerMode)
			{
				if (this.Cursor != System.Windows.Forms.Cursors.Default)
					this.Cursor = System.Windows.Forms.Cursors.Default;
				return;
			}

			if (Root.PanMode)
			{
				this.Cursor = System.Windows.Forms.Cursors.SizeAll;
				IC.Cursor = System.Windows.Forms.Cursors.SizeAll;
				System.Windows.Forms.Cursor.Current = System.Windows.Forms.Cursors.SizeAll;
				return;
			}

			if (this.Cursor != System.Windows.Forms.Cursors.Default)
				this.Cursor = System.Windows.Forms.Cursors.Default;

			if (Root.CanvasCursor == 1)
			{
				// Normal Windows pointer (Arrow)
				IC.Cursor = System.Windows.Forms.Cursors.Arrow;
				this.Cursor = System.Windows.Forms.Cursors.Arrow;
				System.Windows.Forms.Cursor.Current = System.Windows.Forms.Cursors.Arrow;
			}
			else
			{
				// Coloring Dot
				SetColoringCrossCursor();
			}
		}

		private void SetColoringCrossCursor()
		{
			System.Windows.Forms.Cursor oldCursor = dynamicCursor;
			Color dotColor;
			int dotDia;
			int alpha = 255;
			bool isEraser = Root.EraserMode;

			if (!isEraser)
			{
				dotColor = IC.DefaultDrawingAttributes.Color;
				alpha = Math.Max(25, 255 - IC.DefaultDrawingAttributes.Transparency);

				Point widt = new Point((int)IC.DefaultDrawingAttributes.Width, 0);
				try
				{
					IC.Renderer.InkSpaceToPixel(IC.Handle, ref widt);
				}
				catch { }

				IntPtr screenDc = GetDC(IntPtr.Zero);
				const int VERTRES = 10;
				const int DESKTOPVERTRES = 117;
				int logicalScreenHeight = GetDeviceCaps(screenDc, VERTRES);
				int physicalScreenHeight = GetDeviceCaps(screenDc, DESKTOPVERTRES);
				float screenScalingFactor = logicalScreenHeight > 0 ? (float)physicalScreenHeight / (float)logicalScreenHeight : 1.0f;
				ReleaseDC(IntPtr.Zero, screenDc);

				dotDia = Math.Max(3, Math.Min(250, (int)Math.Round(widt.X * screenScalingFactor)));
			}
			else
			{
				dotColor = Color.FromArgb(240, 240, 245);
				dotDia = 24;
				alpha = 255;
			}

			dynamicCursor = ModernIcons.CreateColoringCrossCursor(dotColor, dotDia, isEraser, alpha);
			IC.Cursor = dynamicCursor;
			this.Cursor = dynamicCursor;
			System.Windows.Forms.Cursor.Current = dynamicCursor;

			if (oldCursor != null)
			{
				try
				{
					IntPtr hOld = oldCursor.Handle;
					oldCursor.Dispose();
					if (hOld != IntPtr.Zero)
						ModernIcons.DestroyIcon(hOld);
				}
				catch { }
			}
		}

		short LastESCStatus = 0;
		private void tiSlide_Tick(object sender, EventArgs e)
		{
			// ignore the first tick
			if (LastTickTime.Year == 1987)
			{
				LastTickTime = DateTime.Now;
				return;
			}

			int aimedleft = gpButtonsLeft;
			if (ButtonsEntering == -9)
			{
				aimedleft = gpButtonsLeft + gpButtonsWidth;
			}
			else if (ButtonsEntering < 0)
			{
				if (Root.Snapping > 0)
					aimedleft = gpButtonsLeft + gpButtonsWidth + 0;
				else if (Root.Docked)
					aimedleft = gpButtonsLeft + gpButtonsWidth - btDock.Right;
			}
			else if (ButtonsEntering > 0)
			{
				if (Root.Docked)
					aimedleft = gpButtonsLeft + gpButtonsWidth - btDock.Right;
				else
					aimedleft = gpButtonsLeft;
			}
			else if (ButtonsEntering == 0)
			{
				aimedleft = gpButtons.Left; // stay at current location
			}

			if (ButtonsEntering != 0 || gpButtons.Left != aimedleft)
			{
				if (gpButtons.Left > aimedleft)
				{
					float dleft = gpButtons.Left - aimedleft;
					dleft /= 70;
					if (dleft > 8) dleft = 8;
					dleft *= (float)(DateTime.Now - LastTickTime).TotalMilliseconds;
					if (dleft > 120) dleft = 230;
					if (dleft < 1) dleft = 1;
					gpButtons.Left -= (int)dleft;
					LastTickTime = DateTime.Now;
					if (gpButtons.Left < aimedleft)
					{
						gpButtons.Left = aimedleft;
					}
					gpButtons.Width = Math.Max(gpButtonsWidth - (gpButtons.Left - gpButtonsLeft), btDock.Width);
					Root.UponButtonsUpdate |= 0x1;
				}
				else if (gpButtons.Left < aimedleft)
				{
					float dleft = aimedleft - gpButtons.Left;
					dleft /= 70;
					if (dleft > 8) dleft = 8;
					// fast exiting when not docked
					if (ButtonsEntering == -9 && !Root.Docked)
						dleft = 8;
					dleft *= (float)(DateTime.Now - LastTickTime).TotalMilliseconds;
					if (dleft > 120) dleft = 120;
					if (dleft < 1) dleft = 1;
					// fast exiting when docked
					if (ButtonsEntering == -9 && dleft == 1)
						dleft = 2;
					gpButtons.Left += (int)dleft;
					LastTickTime = DateTime.Now;
					if (gpButtons.Left > aimedleft)
					{
						gpButtons.Left = aimedleft;
					}
					gpButtons.Width = Math.Max(gpButtonsWidth - (gpButtons.Left - gpButtonsLeft), btDock.Width);
					Root.UponButtonsUpdate |= 0x1;
					Root.UponButtonsUpdate |= 0x4;
				}

				if (ButtonsEntering == -9 && gpButtons.Left == aimedleft)
				{
					tiSlide.Enabled = false;
					Root.StopInk();
					return;
				}
				else if (ButtonsEntering < 0)
				{
					Root.UponAllDrawingUpdate = true;
					Root.UponButtonsUpdate = 0;
				}
				if (gpButtons.Left == aimedleft)
				{
					ButtonsEntering = 0;
				}
			}



			if (!Root.PointerMode && !this.TopMost)
				ToTopMost();

			// gpPenWidth status

			if (Root.gpPenWidthVisible != gpPenWidth.Visible)
				gpPenWidth.Visible = Root.gpPenWidthVisible;

			// hotkeys

			const int VK_LCONTROL = 0xA2;
			const int VK_RCONTROL = 0xA3;
			const int VK_LSHIFT = 0xA0;
			const int VK_RSHIFT = 0xA1;
			const int VK_LMENU = 0xA4;
			const int VK_RMENU = 0xA5;
			const int VK_LWIN = 0x5B;
			const int VK_RWIN = 0x5C;
			bool pressed;

			if (!Root.PointerMode)
			{
				// ESC key : Exit
				short retVal;
				retVal = GetKeyState(27);
				if ((retVal & 0x8000) == 0x8000 && (LastESCStatus & 0x8000) == 0x0000)
				{
					if (Root.Snapping > 0)
					{
						ExitSnapping();
					}
					else if (Root.gpPenWidthVisible)
					{
						Root.gpPenWidthVisible = false;
						Root.UponSubPanelUpdate = true;
					}
					else if (Root.Snapping == 0)
						RetreatAndExit();
				}
				LastESCStatus = retVal;
			}



			if (!Root.FingerInAction && (!Root.PointerMode || Root.AllowHotkeyInPointerMode) && Root.Snapping <= 0)
			{
				if (GetKeyboardState(_keyStateBuffer))
				{
					bool control = ((_keyStateBuffer[VK_LCONTROL] | _keyStateBuffer[VK_RCONTROL]) & 0x80) != 0;
					bool alt = ((_keyStateBuffer[VK_LMENU] | _keyStateBuffer[VK_RMENU]) & 0x80) != 0;
					bool shift = ((_keyStateBuffer[VK_LSHIFT] | _keyStateBuffer[VK_RSHIFT]) & 0x80) != 0;
					bool win = ((_keyStateBuffer[VK_LWIN] | _keyStateBuffer[VK_RWIN]) & 0x80) != 0;

					for (int p = 0; p < Root.MaxPenCount; p++)
					{
						int k = Root.Hotkey_Pens[p].Key;
						pressed = (k >= 0 && k < 256) && (_keyStateBuffer[k] & 0x80) != 0;
						if (pressed && !LastPenStatus[p] && Root.Hotkey_Pens[p].ModifierMatch(control, alt, shift, win))
						{
							SelectPen(p);
						}
						LastPenStatus[p] = pressed;
					}

					int kEraser = Root.Hotkey_Eraser.Key;
					pressed = (kEraser >= 0 && kEraser < 256) && (_keyStateBuffer[kEraser] & 0x80) != 0;
					if (pressed && !LastEraserStatus && Root.Hotkey_Eraser.ModifierMatch(control, alt, shift, win))
					{
						SelectPen(-1);
					}
					LastEraserStatus = pressed;

					int kVisible = Root.Hotkey_InkVisible.Key;
					pressed = (kVisible >= 0 && kVisible < 256) && (_keyStateBuffer[kVisible] & 0x80) != 0;
					if (pressed && !LastVisibleStatus && Root.Hotkey_InkVisible.ModifierMatch(control, alt, shift, win))
					{
						btInkVisible_Click(null, null);
					}
					LastVisibleStatus = pressed;

					int kUndo = Root.Hotkey_Undo.Key;
					pressed = (kUndo >= 0 && kUndo < 256) && (_keyStateBuffer[kUndo] & 0x80) != 0;
					if (pressed && !LastUndoStatus && Root.Hotkey_Undo.ModifierMatch(control, alt, shift, win))
					{
						if (!Root.InkVisible)
							Root.SetInkVisible(true);

						Root.UndoInk();
					}
					LastUndoStatus = pressed;

					int kRedo = Root.Hotkey_Redo.Key;
					pressed = (kRedo >= 0 && kRedo < 256) && (_keyStateBuffer[kRedo] & 0x80) != 0;
					if (pressed && !LastRedoStatus && Root.Hotkey_Redo.ModifierMatch(control, alt, shift, win))
					{
						Root.RedoInk();
					}
					LastRedoStatus = pressed;

					int kPointer = Root.Hotkey_Pointer.Key;
					pressed = (kPointer >= 0 && kPointer < 256) && (_keyStateBuffer[kPointer] & 0x80) != 0;
					if (pressed && !LastPointerStatus && Root.Hotkey_Pointer.ModifierMatch(control, alt, shift, win))
					{
						SelectPen(-2);
					}
					LastPointerStatus = pressed;

					int kPan = Root.Hotkey_Pan.Key;
					pressed = (kPan >= 0 && kPan < 256) && (_keyStateBuffer[kPan] & 0x80) != 0;
					if (pressed && !LastPanStatus && Root.Hotkey_Pan.ModifierMatch(control, alt, shift, win))
					{
						SelectPen(-3);
					}
					LastPanStatus = pressed;

					int kClear = Root.Hotkey_Clear.Key;
					pressed = (kClear >= 0 && kClear < 256) && (_keyStateBuffer[kClear] & 0x80) != 0;
					if (pressed && !LastClearStatus && Root.Hotkey_Clear.ModifierMatch(control, alt, shift, win))
					{
						btClear_Click(null, null);
					}
					LastClearStatus = pressed;

					int kSnap = Root.Hotkey_Snap.Key;
					pressed = (kSnap >= 0 && kSnap < 256) && (_keyStateBuffer[kSnap] & 0x80) != 0;
					if (pressed && !LastSnapStatus && Root.Hotkey_Snap.ModifierMatch(control, alt, shift, win))
					{
						btSnap_Click(null, null);
					}
					LastSnapStatus = pressed;
				}
			}

			if (Root.Snapping < 0)
				Root.Snapping++;
		}

		private bool IsInsideVisibleScreen(int x, int y)
		{
			x -= PrimaryLeft;
			y -= PrimaryTop;
			//foreach (Screen s in Screen.AllScreens)
			//	Console.WriteLine(s.Bounds);
			//Console.WriteLine(x.ToString() + ", " + y.ToString());

			foreach (Screen s in Screen.AllScreens)
				if (s.Bounds.Contains(x, y))
					return true;
			return false;
		}

		int IsMovingToolbar = 0;
		Point HitMovingToolbareXY = new Point();
		bool ToolbarMoved = false;
		private void gpButtons_MouseDown(object sender, MouseEventArgs e)
		{
			if (!Root.AllowDraggingToolbar)
				return;
			if (ButtonsEntering != 0)
				return;

			ToolbarMoved = false;
			IsMovingToolbar = 1;
			HitMovingToolbareXY.X = e.X;
			HitMovingToolbareXY.Y = e.Y;
		}

		private void gpButtons_MouseMove(object sender, MouseEventArgs e)
		{
			if (IsMovingToolbar == 1)
			{
				if (Math.Abs(e.X - HitMovingToolbareXY.X) > 20 || Math.Abs(e.Y - HitMovingToolbareXY.Y) > 20)
					IsMovingToolbar = 2;
			}
			if (IsMovingToolbar == 2)
			{
				if (e.X != HitMovingToolbareXY.X || e.Y != HitMovingToolbareXY.Y)
				{
					/*
					gpButtonsLeft += e.X - HitMovingToolbareXY.X;
					gpButtonsTop += e.Y - HitMovingToolbareXY.Y;
					
					if (gpButtonsLeft + gpButtonsWidth > SystemInformation.VirtualScreen.Right)
						gpButtonsLeft = SystemInformation.VirtualScreen.Right - gpButtonsWidth;
					if (gpButtonsLeft < SystemInformation.VirtualScreen.Left)
						gpButtonsLeft = SystemInformation.VirtualScreen.Left;
					if (gpButtonsTop + gpButtonsHeight > SystemInformation.VirtualScreen.Bottom)
						gpButtonsTop = SystemInformation.VirtualScreen.Bottom - gpButtonsHeight;
					if (gpButtonsTop < SystemInformation.VirtualScreen.Top)
						gpButtonsTop = SystemInformation.VirtualScreen.Top;
					*/
					int newleft = gpButtonsLeft + e.X - HitMovingToolbareXY.X;
					int newtop = gpButtonsTop + e.Y - HitMovingToolbareXY.Y;

					bool continuemoving;
					bool toolbarmovedthisframe = false;
					int dleft = 0, dtop = 0;
					if
					(
						IsInsideVisibleScreen(newleft, newtop) &&
						IsInsideVisibleScreen(newleft + gpButtonsWidth, newtop) &&
						IsInsideVisibleScreen(newleft, newtop + gpButtonsHeight) &&
						IsInsideVisibleScreen(newleft + gpButtonsWidth, newtop + gpButtonsHeight)
					)
					{
						continuemoving = true;
						ToolbarMoved = true;
						toolbarmovedthisframe = true;
						dleft = newleft - gpButtonsLeft;
						dtop = newtop - gpButtonsTop;
					}
					else
					{
						do
						{
							if (dleft != newleft - gpButtonsLeft)
								dleft += Math.Sign(newleft - gpButtonsLeft);
							else
								break;
							if
							(
								IsInsideVisibleScreen(gpButtonsLeft + dleft, gpButtonsTop + dtop) &&
								IsInsideVisibleScreen(gpButtonsLeft + gpButtonsWidth + dleft, gpButtonsTop + dtop) &&
								IsInsideVisibleScreen(gpButtonsLeft + dleft, gpButtonsTop + gpButtonsHeight + dtop) &&
								IsInsideVisibleScreen(gpButtonsLeft + gpButtonsWidth + dleft, gpButtonsTop + gpButtonsHeight + dtop)
							)
							{
								continuemoving = true;
								ToolbarMoved = true;
								toolbarmovedthisframe = true;
							}
							else
							{
								continuemoving = false;
								dleft -= Math.Sign(newleft - gpButtonsLeft);
							}
						}
						while (continuemoving);
						do
						{
							if (dtop != newtop - gpButtonsTop)
								dtop += Math.Sign(newtop - gpButtonsTop);
							else
								break;
							if
							(
								IsInsideVisibleScreen(gpButtonsLeft + dleft, gpButtonsTop + dtop) &&
								IsInsideVisibleScreen(gpButtonsLeft + gpButtonsWidth + dleft, gpButtonsTop + dtop) &&
								IsInsideVisibleScreen(gpButtonsLeft + dleft, gpButtonsTop + gpButtonsHeight + dtop) &&
								IsInsideVisibleScreen(gpButtonsLeft + gpButtonsWidth + dleft, gpButtonsTop + gpButtonsHeight + dtop)
							)
							{
								continuemoving = true;
								ToolbarMoved = true;
								toolbarmovedthisframe = true;
							}
							else
							{
								continuemoving = false;
								dtop -= Math.Sign(newtop - gpButtonsTop);
							}
						}
						while (continuemoving);
					}

					if (toolbarmovedthisframe)
					{
						gpButtonsLeft += dleft;
						gpButtonsTop += dtop;
						Root.gpButtonsLeft = gpButtonsLeft;
						Root.gpButtonsTop = gpButtonsTop;
						if (Root.Docked)
							gpButtons.Left = gpButtonsLeft + gpButtonsWidth - btDock.Right;
						else
							gpButtons.Left = gpButtonsLeft;
						gpPenWidth.Left = gpButtonsLeft + btPenWidth.Left - gpPenWidth.Width / 2 + btPenWidth.Width / 2;
						gpPenWidth.Top = gpButtonsTop - gpPenWidth.Height - 10;
						gpButtons.Top = gpButtonsTop;
						Root.UponAllDrawingUpdate = true;
					}
				}
			}
		}

		private void gpButtons_MouseUp(object sender, MouseEventArgs e)
		{
			IsMovingToolbar = 0;
		}

		private void FormCollection_FormClosed(object sender, FormClosedEventArgs e)
		{
			CleanupResources();
		}

		private void btInkVisible_Click(object sender, EventArgs e)
		{
			if (ToolbarMoved)
			{
				ToolbarMoved = false;
				return;
			}

			Root.SetInkVisible(!Root.InkVisible);
		}

		public void btClear_Click(object sender, EventArgs e)
		{
			if (ToolbarMoved)
			{
				ToolbarMoved = false;
				return;
			}

			Root.ClearInk();
			SaveUndoStrokes();
		}

		private void btUndo_Click(object sender, EventArgs e)
		{
			if (ToolbarMoved)
			{
				ToolbarMoved = false;
				return;
			}

			if (!Root.InkVisible)
				Root.SetInkVisible(true);

			Root.UndoInk();
		}

		public void btColor_Click(object sender, EventArgs e)
		{
			if (ToolbarMoved)
			{
				ToolbarMoved = false;
				return;
			}

			for (int b = 0; b < Root.MaxPenCount; b++)
			{
				if ((Button)sender == btPen[b])
				{
					if (Root.CurrentPen == b)
					{
						TogglePenOrHighlighter(b);
					}
					else
					{
						SelectPen(b);
					}
					break;
				}
			}
		}

		public void btEraser_Click(object sender, EventArgs e)
		{
			if (ToolbarMoved)
			{
				ToolbarMoved = false;
				return;
			}

			SelectPen(-1);
		}


		private void btPan_Click(object sender, EventArgs e)
		{
			if (ToolbarMoved)
			{
				ToolbarMoved = false;
				return;
			}

			SelectPen(-3);
		}

		short LastF4Status = 0;
		private void FormCollection_FormClosing(object sender, FormClosingEventArgs e)
		{
			// check if F4 key is pressed and we assume it's Alt+F4
			short retVal = GetKeyState(0x73);
			if ((retVal & 0x8000) == 0x8000 && (LastF4Status & 0x8000) == 0x0000)
			{
				e.Cancel = true;

				// the following block is copyed from tiSlide_Tick() where we check whether ESC is pressed
				if (Root.Snapping > 0)
				{
					ExitSnapping();
				}
				else if (Root.gpPenWidthVisible)
				{
					Root.gpPenWidthVisible = false;
					Root.UponSubPanelUpdate = true;
				}
				else if (Root.Snapping == 0)
					RetreatAndExit();
			}

			LastF4Status = retVal;
		}

		private bool _isCleanedUp = false;
		public void CleanupResources()
		{
			if (_isCleanedUp)
				return;
			_isCleanedUp = true;

			UninstallMouseHook();

			if (dynamicCursor != null)
			{
				try
				{
					IntPtr hOld = dynamicCursor.Handle;
					dynamicCursor.Dispose();
					if (hOld != IntPtr.Zero)
						ModernIcons.DestroyIcon(hOld);
				}
				catch { }
				dynamicCursor = null;
			}

			if (IC != null)
			{
				try
				{
					IC.Enabled = false;
					IC.Dispose();
				}
				catch { }
				IC = null;
			}

			DisposeBitmap(ref image_eraser);
			DisposeBitmap(ref image_eraser_act);
			DisposeBitmap(ref image_pan);
			DisposeBitmap(ref image_pan_act);
			DisposeBitmap(ref image_visible);
			DisposeBitmap(ref image_visible_not);
			DisposeBitmap(ref image_snap);
			DisposeBitmap(ref image_penwidth);
			DisposeBitmap(ref image_dock);
			DisposeBitmap(ref image_dockback);
			DisposeBitmap(ref image_pointer);
			DisposeBitmap(ref image_pointer_act);
			DisposeBitmap(ref image_pencil);
			DisposeBitmap(ref image_pencil_act);
			DisposeBitmap(ref image_highlighter);
			DisposeBitmap(ref image_highlighter_act);
			DisposeBitmap(ref image_clear);
			DisposeBitmap(ref image_undo);
			DisposeBitmap(ref image_exit);

			if (image_pen != null)
			{
				for (int i = 0; i < image_pen.Length; i++)
					DisposeBitmap(ref image_pen[i]);
				image_pen = null;
			}
			if (image_pen_act != null)
			{
				for (int i = 0; i < image_pen_act.Length; i++)
					DisposeBitmap(ref image_pen_act[i]);
				image_pen_act = null;
			}
		}

		private static void DisposeBitmap(ref Bitmap bmp)
		{
			if (bmp != null)
			{
				try { bmp.Dispose(); } catch { }
				bmp = null;
			}
		}

		[DllImport("user32.dll")]
		static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);
		[DllImport("user32.dll", SetLastError = true)]
		static extern UInt32 GetWindowLong(IntPtr hWnd, int nIndex);
		[DllImport("user32.dll")]
		static extern int SetWindowLong(IntPtr hWnd, int nIndex, UInt32 dwNewLong);
		[DllImport("user32.dll")]
		public extern static bool SetLayeredWindowAttributes(IntPtr hwnd, uint crKey, byte bAlpha, uint dwFlags);
		[DllImport("user32.dll", SetLastError = false)]
		static extern IntPtr GetDesktopWindow();
		[DllImport("user32.dll", CharSet = CharSet.Auto, ExactSpelling = true)]
		private static extern short GetKeyState(int keyCode);
		[DllImport("user32.dll")]
		private static extern bool GetKeyboardState(byte[] lpKeyState);
		[DllImport("user32.dll")]
		private static extern short GetAsyncKeyState(int vKey);

		[DllImport("gdi32.dll")]
		static extern int GetDeviceCaps(IntPtr hdc, int nIndex);
		[DllImport("user32.dll")]
		static extern IntPtr GetDC(IntPtr hWnd);
		[DllImport("user32.dll")]
		static extern bool ReleaseDC(IntPtr hWnd, IntPtr hDC);
	}
}
