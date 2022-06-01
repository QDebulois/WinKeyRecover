using System;
using System.IO;
using System.Xml;
using System.Text;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Runtime.InteropServices;


namespace WinKeyRecover
{
	class NativeMethods
	{
		[DllImport("kernel32.dll")]
		public static extern IntPtr LoadLibrary(string dllToLoad);

		[DllImport("kernel32.dll")]
        public static extern bool FreeLibrary(IntPtr hModule);

		[DllImport("kernel32.dll")]
		public static extern IntPtr GetProcAddress(IntPtr hModule, string procedureName);

		[UnmanagedFunctionPointer(CallingConvention.StdCall)]
		public delegate int PidGenX([MarshalAs(UnmanagedType.LPWStr)] string ProductKey, [MarshalAs(UnmanagedType.LPWStr)] string PkeyPath, [MarshalAs(UnmanagedType.LPWStr)] string MSPID, int UnknownUsage, IntPtr ProductID, IntPtr DigitalProductID, IntPtr DigitalProductID4);
	}


	class PKChecker
	{
		public string Check_productKey(string productKey, string fileXml, string fileDll, string mspid)
		{
			IntPtr hModule = NativeMethods.LoadLibrary(fileDll);
			string description;
			string result;
			byte[] array = new byte[50];
			byte[] array2 = new byte[164];
			byte[] array3 = new byte[1272];
			IntPtr intPtr = Marshal.AllocHGlobal(50);
			IntPtr intPtr2 = Marshal.AllocHGlobal(164);
			IntPtr intPtr3 = Marshal.AllocHGlobal(1272);
			array[0] = 50;
			array2[0] = 164;
			array3[0] = 248;
			array3[1] = 4;
			Marshal.Copy(array, 0, intPtr, 50);
			Marshal.Copy(array2, 0, intPtr2, 164);
			Marshal.Copy(array3, 0, intPtr3, 1272);
            int num = ((NativeMethods.PidGenX)Marshal.GetDelegateForFunctionPointer(NativeMethods.GetProcAddress(hModule, "PidGenX"), typeof(NativeMethods.PidGenX)))(productKey, fileXml, mspid, 0, intPtr, intPtr2, intPtr3);
			if (num == 0)
			{
				Marshal.Copy(intPtr3, array3, 0, array3.Length);
				string @string = GetString(array3, 136);
				string productDescription = GetProductDescription(fileXml, "{" + @string + "}");
				description = productDescription;
			}
			else
			{
				description = "Invalid";
			}
			Marshal.FreeHGlobal(intPtr2);
			Marshal.FreeHGlobal(intPtr3);
			NativeMethods.FreeLibrary(hModule);
			result = description;
			return result;
		}

		private static string GetString(byte[] bytes, int index)
		{
			int num = index;
			while (bytes[num] != 0 || bytes[num + 1] != 0)
			{
				num++;
			}
			return Encoding.ASCII.GetString(bytes, index, num - index).Replace("\0", "");
		}

		private static string GetProductDescription(string fileXml, string aid)
		{
			string result;
			XmlDocument xmlDocument = new XmlDocument();
			xmlDocument.Load(fileXml);
			Stream inStream = new MemoryStream(Convert.FromBase64String(xmlDocument.GetElementsByTagName("tm:infoBin")[0].InnerText));
			xmlDocument.Load(inStream);
			XmlNamespaceManager xmlNamespaceManager = new XmlNamespaceManager(xmlDocument.NameTable);
			xmlNamespaceManager.AddNamespace("pkc", "http://www.microsoft.com/DRM/PKEY/Configuration/2.0");
			XmlNode xmlNode = xmlDocument.SelectSingleNode("/pkc:ProductKeyConfiguration/pkc:Configurations/pkc:Configuration[pkc:ActConfigId='" + aid + "']", xmlNamespaceManager);
			if (xmlNode == null)
			{
				xmlNode = xmlDocument.SelectSingleNode("/pkc:ProductKeyConfiguration/pkc:Configurations/pkc:Configuration[pkc:ActConfigId='" + aid.ToUpper() + "']", xmlNamespaceManager);
			}
			result = xmlNode.ChildNodes.Item(3).InnerText;
			return result;
		}
	}


	class Cli
	{
		public static void Intro(char[] validChars)
		{
			string validCharsString = string.Join(", ", validChars);
			string intro =
				  "\n    ###########################"
				+ "\n    # R�cup�rateur de license #"
				+ "\n    ###########################\n"
				+ "\nInstructions:"
				+ "\n- Renseigner la cl� en remplacant les characters manquants par des '*'."
				+ "\n- La cl� doit �tre renseign�e comme ceci: 'XXXXX-XXXXX-XXXXX-XXXXX-XXXXX'."
				+ "\n- Les characters autoris�s sont: " + validCharsString
				+ "\n- Un fichier nomm� 'cl�s_trouv�es.txt' contenant les cl�s sera cr�� sur votre bureau.\n";
			Console.WriteLine(intro);
		}

		public static string Get_key(char[] validChars)
		{
			string key;
			Console.Write("Entrez votre cl�: ");
			key = Console.ReadLine().ToUpper();
			return key;
		}

		public static void Found_keys(List<List<string>> keysFound)
		{
			string found =
			  "\n    ###########################"
			+ "\n    # R�cup�rateur de license #"
			+ "\n    ###########################\n"
			+ "\nCl�s trouv�es:\n";
			Console.Clear();
			Console.WriteLine(found);
			for (int j = 0; j < keysFound.Count; j++)
			{
				Console.WriteLine("[>] " + keysFound[j][0] + " Version: " + keysFound[j][1]);
			}
			Console.WriteLine();
		}

		public static void End()
		{
			Console.WriteLine("\n    ###########################");
			Console.WriteLine("Appuyer sur 'Entrer' pour quitter.");
			Console.ReadLine();
			System.Environment.Exit(0);
		}
	}


	class Program
	{
		private static bool Key_isvalid(char[] validChars, string key)
		{
			bool validKey = true;
			if (key != string.Empty && key.Length == 29 && key.Contains('*'))
			{
				for (int i = 0; i < key.Length; i++)
				{
					if (key[i] != '-')
					{
						if (!validChars.Contains(key[i]) && key[i] != '*')
						{
							validKey = false;
						}
					}
				}
			}
			else
			{
				validKey = false;
			}
			if (!validKey)
			{
				Console.WriteLine("Cl� invalide.");
			}
			else
            {
				Console.WriteLine();
            }
			return validKey;
		}

		private static int Missing_count(string key)
		{
			int missing = 0;
			for (int i = 0; i < key.Length; i++)
			{
				if (key[i] == '*')
				{
					missing++;
				}
			}
			return missing;
		}

		private static List<int> Missing_positions(string key)
		{
			List<int> missingPosition = new List<int>();
			for (int i = 0; i < key.Length; i++)
			{
				if (key[i] == '*')
				{
					missingPosition.Add(i);
				}
			}
			return missingPosition;
		}

		private static string Replace_missing(char[] key, List<int> missingPos, string patternFinal)
		{
			char[] keyTest = new List<char>(key).ToArray();
			for (int j = 0; j < patternFinal.Length; j++)
			{
				keyTest[missingPos[j]] = patternFinal[j];
			}
			return new string(keyTest);
		}

		private static void Save_to_file(char[] key, List<List<string>> keysFound)
		{
			string fileName = Environment.GetEnvironmentVariable("userprofile") + "\\Desktop\\cl�s_trouv�es.txt";
			StreamWriter writer = new StreamWriter(fileName);
			writer.WriteLine("Cl� initiale: " + new string(key) + "\n");
			writer.WriteLine("Cl�s trouv�es: ");
			for (int i = 0; i < keysFound.Count; i++)
			{
				writer.WriteLine(keysFound[i][0] + " Version: " + keysFound[i][1]);
			}
			writer.Close();
			Console.WriteLine(new string(key));
		}

		private static void Start(char[] validChars, char[] key, List<int> missingPos,
			string pattern, int missing, List<List<string>> keysFound, string fileXml, string fileDll, string mspid)
		{
			if (missing == 1)
			{
				PKChecker pkc = new PKChecker();
				_ = Parallel.For(0, validChars.Length, i =>
				{
					string patternFinal = pattern + validChars[i];
					string keyTest = Replace_missing(key, missingPos, patternFinal);
					Console.Write("[+] Travail sur: " + keyTest + "\r");
					string resultat = pkc.Check_productKey(keyTest, fileXml, fileDll, mspid);
					if (resultat != "Invalid")
					{
						keysFound.Add(new List<string>() { keyTest, resultat });
						Save_to_file(key, keysFound);
						Cli.Found_keys(keysFound);
					}
				});
			}
			else
			{
				for (int i = 0; i < validChars.Length; i++)
				{
					string patternNew = pattern + validChars[i];
					Start(validChars, key, missingPos, patternNew, missing - 1, keysFound, fileXml, fileDll, mspid);
				}
			}
		}

		public static void Main()
		{
			bool validKey = false;
			string key = string.Empty;
			string mspid = "00000";
			string fileDll = ".\\pidgenx2.dll";
			string fileXml = ".\\Win10pkeyconfig.xrm-ms";
			List<List<string>> keysFound = new List<List<string>>();
			char[] validChars = new char[]
			{
			'B', 'C', 'D', 'F', 'G',
			'H', 'J', 'K', 'M', 'N',
			'P', 'Q', 'R', 'T', 'V',
			'W', 'X', 'Y', '2', '3',
			'4', '6', '7', '8', '9'
			};
			Cli.Intro(validChars);
            while (!validKey)
            {
                key = Cli.Get_key(validChars);
                validKey = Key_isvalid(validChars, key);
            }
			var watch = System.Diagnostics.Stopwatch.StartNew();
			int missing = Missing_count(key);
			List<int> missingPosition = Missing_positions(key);
            Start(validChars, key.ToCharArray(), missingPosition, string.Empty, missing, keysFound, fileXml, fileDll, mspid);
			watch.Stop();
			Console.WriteLine("Temps: " + watch.Elapsed + " ");
			Cli.End();
		}
	}
}

/*
 * Run 1: 12.98
 * Run 2: 13.53
 * Run 3: 12.72
 * Run 4: 13.04
 */
