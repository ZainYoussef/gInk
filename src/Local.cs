using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace gInk
{
	public class Local
	{
		Dictionary<string, string> Languages = new Dictionary<string, string>();

		public string CurrentLanguageFile;

		public string[] ButtonNamePen = new string[10];

		public string ButtonNamePenwidth = "Pen width";
		public string ButtonNameErasor = "Eraser";
		public string ButtonNamePan = "Pan";
		public string ButtonNameMousePointer = "Mouse pointer";
		public string ButtonNameInkVisible = "Ink visible";
		public string ButtonNameSnapshot = "Snapshot";
		public string ButtonNameUndo = "Undo";
		public string ButtonNameRedo = "Redo";
		public string ButtonNameClear = "Clear";
		public string ButtonNameExit = "Exit drawing";
		public string ButtonNameDock = "Dock";

		public string MenuEntryExit = "Exit";
		public string MenuEntryOptions = "Options";
		public string MenuEntryAbout = "About";

		public string OptionsTabGeneral = "General";
		public string OptionsTabPens = "Pens";
		public string OptionsTabHotkeys = "Hotkeys";

		public string OptionsGeneralLanguage = "Language";
		public string OptionsGeneralCanvascursor = "Canvas cursor";
		public string OptionsGeneralCanvascursorCross = "Coloring dot";
		public string OptionsGeneralCanvascursorArrow = "Normal Windows pointer";
		public string OptionsGeneralCanvascursorPentip = "Coloring dot";
		public string OptionsGeneralSnapshotsavepath = "Snapshot save path";
		public string OptionsGeneralWhitetrayicon = "Use white tray icon";
		public string OptionsGeneralAllowdragging = "Allow dragging toolbar";
		public string OptionsGeneralShowBottomToolbar = "Show bottom toolbar";
		public string OptionsGeneralStyle = "Visual style";
		public string OptionsGeneralNotePenwidth = "Note: pen width panel overides each individual pen width settings";

		public string OptionsPensShow = "Show";
		public string OptionsPensColor = "Color";
		public string OptionsPensAlpha = "Alpha";
		public string OptionsPensWidth = "Width";
		public string OptionsPensPencil = "Pencil";
		public string OptionsPensHighlighter = "Highlighter";
		public string OptionsPensThin = "Thin";
		public string OptionsPensNormal = "Normal";
		public string OptionsPensThick = "Thick";

		public string OptionsHotkeysglobal = "Global hotkey (start drawing, switch between mouse pointer and drawing)";
		public string OptionsHotkeysRadial = "Circular quick action menu";
		public string OptionsHotkeysEnableinpointer = "Enable all following hotkeys in mouse pointer mode (may cause a mess)";

		public string NotificationSnapshot = "Snapshot saved. Click here to browse snapshots.";

		public Local()
		{
			ButtonNamePen[0] = "Pen 0";
			ButtonNamePen[1] = "Pen 1";
			ButtonNamePen[2] = "Pen 2";
			ButtonNamePen[3] = "Pen 3";
			ButtonNamePen[4] = "Pen 4";
			ButtonNamePen[5] = "Pen 5";
			ButtonNamePen[6] = "Pen 6";
			ButtonNamePen[7] = "Pen 7";
			ButtonNamePen[8] = "Pen 8";
			ButtonNamePen[9] = "Pen 9";

			LoadLocalList();
		}

		public void LoadLocalList()
		{
			DirectoryInfo d = new DirectoryInfo("./lang/");
			if (!d.Exists)
				d = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory + "lang");
			if (!d.Exists)
				return;

			FileInfo[] Files = d.GetFiles("*.txt");
			foreach (FileInfo file in Files)
			{
				using (FileStream fini = new FileStream(file.FullName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
				using (StreamReader srini = new StreamReader(fini))
				{
					string sLine;
					do
					{
						sLine = srini.ReadLine();
					}
					while (sLine != null && !sLine.StartsWith("LanguageName"));
					if (sLine == null)
						continue;
					string sPara = sLine.Substring(sLine.IndexOf("=") + 1);
					sPara = sPara.Trim();
					sPara = sPara.Trim('\"');

					Languages.Add(file.Name.Substring(0, file.Name.Length - 4), sPara);
				}
			}
		}

		public List<string> GetLanguagenames()
		{
			List<string> names = new List<string>();
			foreach (KeyValuePair<string, string> pair in Languages)
				names.Add(pair.Value);

			return names;
		}

		public string GetFilenameByLanguagename(string languagename)
		{
			foreach (KeyValuePair<string, string> pair in Languages)
				if (pair.Value == languagename)
					return pair.Key;

			return "";
		}

		public string GetLanguagenameByFilename(string filename)
		{
			foreach (KeyValuePair<string, string> pair in Languages)
				if (pair.Key == filename)
					return pair.Value;

			return "";
		}

		public void LoadLocalFile(string loname)
		{
			string filename = "./lang/" + loname + ".txt";

			if (!File.Exists(filename))
				filename = AppDomain.CurrentDomain.BaseDirectory + "lang/" + loname + ".txt";
			if (!File.Exists(filename))
				return;

			using (FileStream fini = new FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
			using (StreamReader srini = new StreamReader(fini))
			{
				string sLine = "";
				string sName = "", sPara = "";
				while ((sLine = srini.ReadLine()) != null)
				{
					if (sLine.Length > 0 &&
						sLine[0] != '-' &&
						sLine[0] != '%' &&
						sLine[0] != '\'' &&
						sLine[0] != '/' &&
						sLine[0] != '!' &&
						sLine[0] != '[' &&
						sLine[0] != '#' &&
						sLine.Contains("="))
					{
						int eqIdx = sLine.IndexOf("=");
						if (!sLine.Substring(eqIdx + 1).Contains("="))
						{
							sName = sLine.Substring(0, eqIdx).Trim();
							sPara = sLine.Substring(eqIdx + 1).Trim().Trim('\"');

							if (sName.StartsWith("ButtonNamePen") && sName.Length >= 14)
							{
								int penid = 0;
								if (int.TryParse(sName.Substring(13, 1), out penid))
								{
									ButtonNamePen[penid] = sPara;
								}
							}

							System.Reflection.FieldInfo fi = typeof(Local).GetField(sName);
							if (fi != null)
								fi.SetValue(this, sPara);
						}
					}
				}
			}

			CurrentLanguageFile = loname;
		}
	}
}
