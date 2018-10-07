using System;
using System.Collections.Generic;
using System.IO;
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

		private const int Feature_LoadRgbProfile = 7;
		private const int Feature_SwitchProfile = 23;
		private const int Feature_Reset = 32;

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

			wooting_rgb_send_feature(Feature_Reset, 0, 0, 0, 0); // use profile colors

            var lines = File.ReadAllLines("config.ini");

            List<Tuple<string, int>> games = new List<Tuple<string, int>>();
            foreach (var line in lines)
            {
                var splitted = line.Split(',');
                games.Add(new Tuple<string, int>(splitted[0], Convert.ToInt32(splitted[1])));
            }

            int PreviousProfile = 0;

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
				wooting_rgb_send_feature(Feature_LoadRgbProfile, 0, 0, 0, DesiredProfile); // update colors on keyboard 

				PreviousProfile = DesiredProfile;
			}
		}
	}
}