using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.Diagnostics;

public class AppSettings
{
	// Właściwości odpowiadające sekcji AppSettings w pliku JSON
	public string SrcPath { get; set; }
	public string DstPath { get; set; }
	public string LogPath { get; set; }
	public int SecInterval { get; set; }

	public AppSettings() { }
	public AppSettings(string cfgPath)
	{
		if (!File.Exists(cfgPath));

		string jsonCfg = File.ReadAllText(cfgPath);

		var section = JsonSerializer.Deserialize<Wrapper>(jsonCfg);

		SrcPath = section.AppSettings.SrcPath;
		DstPath = section.AppSettings.DstPath;
		LogPath = section.AppSettings.LogPath;
		SecInterval = section.AppSettings.SecInterval;
	}
	
	//zapisz ustawienia do pliku JSON
	public void Save(string jsonPath)
	{
		var section = new Wrapper { AppSettings = this };
		var options = new JsonSerializerOptions { WriteIndented = true };
		string json = JsonSerializer.Serialize(section, options);
		File.WriteAllText(jsonPath, json);
	}

	/* -----------------  prywatne pomocnicze  ----------------- */

	// obiekt-otoczka odpowiadający strukturze pliku
	private class Wrapper
	{
		public AppSettings? AppSettings { get; set; }
	}
}



public class MyClass
{
	public static void Main()
	{
		///////////////////////////////////////////// ustawienia aplikacji /////////////////////////
		///
		Console.WriteLine($"Config file (ENTER standard input Config.json):");
		string path = ReadLineTimeout(15000).Result;
		if (string.IsNullOrWhiteSpace(path))
		{
			path = "Config.json";
		}

		var settings = new AppSettings(path);

		Console.WriteLine($"Actual settings:\nSrcPath: {settings.SrcPath}");
		Console.WriteLine($"DstPath: {settings.DstPath}");
		Console.WriteLine($"LogPath: {settings.LogPath}");
		Console.WriteLine($"SecInterval:  {settings.SecInterval}");

		Console.Write("\nDo you want to change them? (true/false[Enter]): ");
		if (bool.TryParse(ReadLineTimeout(10000).Result, out bool zmieniamy))// && zmieniamy)
		{
			Console.WriteLine("\nNew settings (ENTER = keep original, 7s to timeout):\n");

			settings.SrcPath = ReadConsole("SrcPath", settings.SrcPath);
			settings.DstPath = ReadConsole("DstPath", settings.DstPath);
			settings.LogPath = ReadConsole("LogPath", settings.LogPath);
			settings.SecInterval = ReadIntConsole("SecInterval", settings.SecInterval);

			settings.Save(path);
			Console.WriteLine("\nSettings overwrite.");
		
		}
		else
		{
			Console.WriteLine("\nProceeding with cfg settings.");
		}

		////////////////////////////////Glowna petla aplikacji////////////////////////////////////
		///
		bool exitN = true;
		bool copyBool = true;
		DateTime lastCopyTime = DateTime.Now;
		int timeout = Math.Min(settings.SecInterval * 1000, 5000);

		do
		{
			if (copyBool)
			{
				File.AppendAllText(settings.LogPath, $"{DateTime.Now}: Checking folder synchronization.\n");
				CopyFiles(settings.SrcPath, settings.DstPath, settings.LogPath);
				Console.WriteLine($"\nFiles copying from {settings.SrcPath} to {settings.DstPath} finished.\n\nWaiting {settings.SecInterval} seconds for next copy.\n\nType \"e\" or \"exit\" to close application.\"s\" or \"settings\" for configuration.");
				copyBool = false; // ustaw flagę kopiowania na false, aby nie kopiować ponownie w tej iteracji
				lastCopyTime = DateTime.Now;
			}
			
			//czekaj na wpis lub timeout
			

			string test = ReadLineTimeout(timeout).Result;
			if (!string.IsNullOrWhiteSpace(test))
			{
				if (test == "")
				{
					if ((DateTime.Now - lastCopyTime).TotalSeconds >= settings.SecInterval)
					{
						copyBool = true; // ustaw flagę kopiowania na true, jeśli minął czas oczekiwania
										 //DateTime.Now - lastCopyTime) >= TimeSpan.FromSeconds(settings.SecInterval))
					}
					else
					{
						continue; // jeśli nie minął czas oczekiwania, kontynuuj pętlę
					}
				}
				else if ("exit".StartsWith(test, StringComparison.OrdinalIgnoreCase))
				{
					exitN = false; // ustaw flagę wyjścia, jeśli użytkownik wpisze "e" lub "exit"
					Console.WriteLine("Exiting application.");
					break; // wyjście z pętli, jeśli użytkownik naciśnie dowolny klawisz
				}
				else if ("settings".StartsWith(test, StringComparison.OrdinalIgnoreCase))
				{
					Console.WriteLine("\nNew settings (ENTER to keep original, 7s to timeout):\n");

					//settings.SrcPath = ReadConsole("SrcPath", settings.SrcPath);
					//settings.DstPath = ReadConsole("DstPath", settings.DstPath);
					//settings.LogPath = ReadConsole("LogPath", settings.LogPath);
					settings.SecInterval = ReadIntConsole("SecInterval", settings.SecInterval);
					timeout = Math.Min(settings.SecInterval * 1000, 5000);

					settings.Save(path);
					Console.WriteLine("\nSettings overwrite.");
				}

			}
			else if ((DateTime.Now - lastCopyTime).TotalSeconds >= settings.SecInterval)
			{
				copyBool = true; // ustaw flagę kopiowania na true, jeśli minął czas oczekiwania
								 //DateTime.Now - lastCopyTime) >= TimeSpan.FromSeconds(settings.SecInterval))
			}

		} while (exitN);


	}





	/// <summary>
	/// /////////////////////////////funkcje pomocnicze////////////////////////////////////
	/// </summary>
	/// <param name="label"></param>
	/// <param name="value"></param>
	/// <returns></returns>
	static string ReadConsole(string label, string value)
	{
		Console.Write($"\n{label} [{value}]: ");
		string input = ReadLineTimeout(7000).Result;
		return string.IsNullOrWhiteSpace(input) || input.IndexOfAny(Path.GetInvalidPathChars()) != -1 ? value : input;
	}
	static int ReadIntConsole(string label, int value)
	{
		Console.Write($"\n{label} [{value}]: ");
		string input = ReadLineTimeout(7000).Result;
		if (string.IsNullOrWhiteSpace(input))
			return value;
		else if (int.TryParse(input, out int wy))
			return wy;
		Console.Write("\nNot number keeping cfg settings");
		return value; // jeśli nie udało się sparsować, zwróć oryginalną wartość
	}
	//metoda do odczytu linii z konsoli z timeoutem
	static async Task<string> ReadLineTimeout(int timeMs)
	{
		var task = Task.Run(() => Console.ReadLine());
		var fin = await Task.WhenAny(task, Task.Delay(timeMs));
		return fin == task ? task.Result : "";
	}
	///////////////funkcje pomocnicze obslugi folderow////////////// 
	
	
	
	
	// Skopiuj wszystkie pliki
	static void CopyFiles(string srcDir, string dstDir, string logPath)
	{
		// Sprawdzanie katalogu docelowego
		if (!Directory.Exists(dstDir))
		{
			Log($"Creating folder: {dstDir}", logPath);
			Directory.CreateDirectory(dstDir);
		}
		//zebranie wszystkich plikow z obu katalogow
		var srcTemp = Directory.GetFiles(srcDir, "*.*", SearchOption.AllDirectories).Select(dt => Path.GetRelativePath(srcDir, dt));
		var dstTemp = Directory.GetFiles(dstDir, "*.*", SearchOption.AllDirectories).Select(dt => Path.GetRelativePath(dstDir, dt));
		
		foreach (var s in srcTemp.Except(dstTemp))
		{
			//Sprawdznie ktore pliki istnieja tylko w katalogu zrodlowym i kopiowanie ich do katalogu docelowego
			Log($"New file: {s.ToString()}", logPath);
			var destFile = Path.Combine(dstDir, Path.GetFileName(s));
			File.Copy(s, destFile, true);
		}

		foreach (var d in dstTemp.Except(srcTemp))
		{
			//Sprawdznie ktore pliki istnieja tylko w katalogu docelowym i usuwanie ich
			Log($"File deleted: {d.ToString()}", logPath);
			string temp = Path.Combine(dstDir, d.ToString());
			File.Delete(temp);
		}
		foreach (var b in srcTemp.Intersect(dstTemp))
		{
			//Sprawdznie ktore pliki istnieja w obu katalogach i porownywanie ich, w przypadku zmian kopiowanie ich do katalogu docelowego
			string full1 = Path.Combine(srcDir, b);
			string full2 = Path.Combine(dstDir, b);
			if (File.GetLastWriteTime(full1) == File.GetLastWriteTime(full2) && File.GetAttributes(full1) == File.GetAttributes(full2))
			{
				Log($"No changes: {b.ToString()}", logPath);
				continue;
			}
			else if (File.GetLastWriteTime(full1) != File.GetLastWriteTime(full2) || File.GetAttributes(full1) != File.GetAttributes(full2))
			{
				File.Copy(full1, full2, true);
				Log($"Edited: {b.ToString()}", logPath);
			}

		}

		var srcDirTemp = Directory.GetDirectories(srcDir, "*", SearchOption.AllDirectories).Select(dt => Path.GetRelativePath(srcDir, dt));
		var dstDirTemp = Directory.GetDirectories(dstDir, "*", SearchOption.AllDirectories).Select(dt => Path.GetRelativePath(dstDir, dt));
		foreach (var f in dstDirTemp.Except(srcDirTemp))
		{
			//Sprawdznie ktore folderu istnieja tylko w katalogu docelowym i usuwanie ich, pozostale przejda przez proces sprawdzania zgodnosci plikow
			Log($"Folder deleted: {f.ToString()}", logPath);
			Directory.Delete(Path.Combine(dstDir, f), true);
		}

		foreach (var dir in Directory.GetDirectories(srcDir))
		{
			var destSubDir = Path.Combine(dstDir, Path.GetFileName(dir));
			CopyFiles(dir, destSubDir, logPath);
		}

	}
	static void Log(string message, string logPath)
	{
		File.AppendAllText(logPath, $"{message}\n");
		Console.WriteLine($"{message}\n");
	}


}


///To DO:///
/*
 * 
 * 
 */
