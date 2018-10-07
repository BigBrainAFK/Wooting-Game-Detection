using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Wooting_Game_Detection
{
	class Program
	{
		[DllImport("wootingrgb.dll")]
		public static extern bool wooting_rgb_kbd_connected();

		[DllImport("wootingrgb.dll")]
		public static extern bool wooting_rgb_send_feature(int commandId, int parameter0, int parameter1,
			int parameter2, int parameter3);

		[DllImport("wootingrgb.dll")]
		public static extern bool wooting_rgb_array_set_single(byte row, byte column, byte red, byte green, byte blue);

		[DllImport("wootingrgb.dll")]
		public static extern bool wooting_rgb_array_update_keyboard();

		[DllImport("wootingrgb.dll")]
		public static extern bool wooting_rgb_reset();

		[DllImport("user32.dll")]
		private static extern IntPtr GetForegroundWindow();

		[DllImport("user32.dll")]
		private static extern int GetWindowText(IntPtr hWnd, StringBuilder text, int count);

		[DllImport("kernel32.dll")]
		public static extern bool SetConsoleCtrlHandler(HandlerRoutine Handler, bool State);

		public delegate bool HandlerRoutine(CtrlTypes CtrlType);

		public enum CtrlTypes
		{
			CTRL_C_EVENT = 0,
			CTRL_BREAK_EVENT,
			CTRL_CLOSE_EVENT,
			CTRL_LOGOFF_EVENT = 5,
			CTRL_SHUTDOWN_EVENT
		}

		private const int Feature_SwitchProfile = 23;

		private static bool ConsoleCtrlHandler(CtrlTypes Type)
		{
			if (Type == CtrlTypes.CTRL_CLOSE_EVENT)
				wooting_rgb_reset();

			return true;
		}

		static void Main(string[] args)
		{
			if (!wooting_rgb_kbd_connected())
				return; // no keyboard

			Tuple<string, int>[] games = // string - window title, int - desired profile
			{
				new Tuple<string, int>("Forza Horizon 4", 1),
				new Tuple<string, int>("Forza Horizon 3", 1),
				new Tuple<string, int>("TheCrew2", 2),
				new Tuple<string, int>("Spotify", 3),
			};

			int PreviousProfile = 0;

			const int LayoutColumns = 21;
			const int LayoutRows = 6;

			SetConsoleCtrlHandler(ConsoleCtrlHandler, true); // reset on exit

			var TitleBuffer = new StringBuilder(256);
			while (true)
			{
				Thread.Sleep(200);

				if (GetWindowText(GetForegroundWindow(), TitleBuffer, 256) == 0) // get foreground window's title
					continue; // continue if the return value is 0, error

				var Result = games.FirstOrDefault(x => x.Item1 == TitleBuffer.ToString()); // first object where the first item in a tuple equals to the window title
				var DesiredProfile = Result?.Item2 ?? 0; // use the second item in a tuple as the desired profile, if result isn't null

				if (DesiredProfile == PreviousProfile) continue; // do nothing, the profile shouldn't be changed

				Console.WriteLine($"Switch profile {DesiredProfile}");
				wooting_rgb_send_feature(Feature_SwitchProfile, 0, 0, 0, DesiredProfile); // send the switch profile command to the keyboard

				for (var Row = 0; Row < LayoutRows; Row++)
				{
					for (var Column = 0; Column < LayoutColumns; Column++)
					{
						// figure out the colors
						// is there a better way to do this? brain died
						var red = DesiredProfile == 1 || DesiredProfile == 0 ? (byte) 255 : (byte) 0;
						var green = DesiredProfile == 2 || DesiredProfile == 0 ? (byte) 255 : (byte) 0;
						var blue = DesiredProfile == 3 || DesiredProfile == 0 ? (byte) 255 : (byte) 0;

						wooting_rgb_array_set_single((byte) Row, (byte) Column, red, green, blue); // set the color in a buffer
					}
				}

				wooting_rgb_array_update_keyboard(); // update colors on keyboard 

				PreviousProfile = DesiredProfile;
			}
		}
	}
}