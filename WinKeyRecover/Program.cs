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


	class Cli
	{
		public static void Intro(char[] validChars)
		{
			string validCharsString = string.Join(", ", validChars);
			string intro =
				  "\n    ###########################"
				+ "\n    # Récupérateur de license #"
				+ "\n    ###########################\n"
				+ "\nInstructions:"
				+ "\n- Renseigner la clé en remplacant les characters manquants par des '*'."
				+ "\n- La clé doit être renseignée comme ceci: 'XXXXX-XXXXX-XXXXX-XXXXX-XXXXX'."
				+ "\n- Les characters autorisés sont: " + validCharsString
				+ "\n- Un fichier nommé 'clés_trouvées.txt' contenant les clés sera créé sur votre bureau.\n";
			Console.WriteLine(intro);
		}

		public static string Get_key(char[] validChars)
		{
			string key;
			Console.Write("Entrez votre clé: ");
			key = Console.ReadLine().ToUpper();
			return key;
		}

		public static void Found_keys(List<List<string>> keysFound)
		{
			string found =
			  "\n    ###########################"
			+ "\n    # Récupérateur de license #"
			+ "\n    ###########################\n"
			+ "\nClés trouvées:\n";
			Console.Clear();
			Console.WriteLine(found);
			for (int j = 0; j < keysFound.Count; j++)
			{
				Console.WriteLine("[>] " + keysFound[j][0] + " Version: " + keysFound[j][1]);
			}
			Console.WriteLine("[+] Travail en cour sur les clés...");
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
		// Partie PKChekcer
		private static readonly IntPtr hModule = NativeMethods.LoadLibrary(".\\pidgenx2.dll");
		private static readonly string fileXml = ".\\Win10pkeyconfig.xrm-ms";
		private static readonly string mspid = "00000";
		private static readonly byte[] array = new byte[50];
		private static readonly byte[] array2 = new byte[164];
		private static readonly byte[] array3 = new byte[1272];
		private static readonly IntPtr intPtr = Marshal.AllocHGlobal(50);
		private static readonly IntPtr intPtr2 = Marshal.AllocHGlobal(164);
		private static readonly IntPtr intPtr3 = Marshal.AllocHGlobal(1272);
		private static string result;
		// Partie Program
		private static int missing = 0;
		private static readonly List<int> missingPosition = new List<int>();
		private static readonly List<string> allPatterns = new List<string>();
		private static readonly List<List<string>> keysFound = new List<List<string>>();
		private static readonly char[] validChars = new char[]
			{
			'B', 'C', 'D', 'F', 'G',
			'H', 'J', 'K', 'M', 'N',
			'P', 'Q', 'R', 'T', 'V',
			'W', 'X', 'Y', '2', '3',
			'4', '6', '7', '8', '9'
			};

		private static string Check_key(string keyTest)
		{
			int num = ((NativeMethods.PidGenX)Marshal.GetDelegateForFunctionPointer(NativeMethods.GetProcAddress(hModule, "PidGenX"), typeof(NativeMethods.PidGenX)))(keyTest, fileXml, mspid, 0, intPtr, intPtr2, intPtr3);
			if (num == 0)
			{
				Marshal.Copy(intPtr3, array3, 0, array3.Length);
				string @string = GetString(array3, 136);
				GetProductDescription("{" + @string + "}");
			}
			else
			{
				result = "Invalid";
			}
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

		private static void GetProductDescription(string aid)
		{
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
		}

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
				Console.WriteLine("Clé invalide.");
			}
			else
            {
				Console.WriteLine();
            }
			return validKey;
		}

		private static void Missing_count(string key)
		{
			for (int i = 0; i < key.Length; i++)
			{
				if (key[i] == '*')
				{
					missing++;
				}
			}
		}

        private static void Missing_positions(string key)
		{
			for (int i = 0; i < key.Length; i++)
			{
				if (key[i] == '*')
				{
					missingPosition.Add(i);
				}
			}
		}

		private static string Replace_missing(char[] key, string patternFinal)
		{
			char[] keyTest = new List<char>(key).ToArray();
			for (int j = 0; j < patternFinal.Length; j++)
			{
				keyTest[missingPosition[j]] = patternFinal[j];
			}
			return new string(keyTest);
		}

		private static void Save_to_file(char[] key, List<List<string>> keysFound)
		{
			string fileName = Environment.GetEnvironmentVariable("userprofile") + "\\Desktop\\clés_trouvées.txt";
			StreamWriter writer = new StreamWriter(fileName);
			writer.WriteLine("Clé initiale: " + new string(key) + "\n");
			writer.WriteLine("Clés trouvées: ");
			for (int i = 0; i < keysFound.Count; i++)
			{
				writer.WriteLine(keysFound[i][0] + " Version: " + keysFound[i][1]);
			}
			writer.Close();
			Console.WriteLine(new string(key));
		}

		private static void Start(char[] key)
		{
			Console.WriteLine("[+] Travail en cour sur les clés...");
			_ = Parallel.For(0, allPatterns.Count, i =>
			{
				string resultat = Check_key(allPatterns[i]);
				if (resultat != "Invalid")
				{
					keysFound.Add(new List<string>() { allPatterns[i], resultat });
					Save_to_file(key, keysFound);
					Cli.Found_keys(keysFound);
				}
			});
		}

		private static void Generate_patterns(char[] key, string pattern, int missing)
		{
			if (missing == 1)
			{
				for (int i = 0; i < validChars.Length; i++)
				{
					allPatterns.Add(Replace_missing(key, pattern + validChars[i]));
				}
			}
			else
			{
				for (int i = 0; i < validChars.Length; i++)
				{
					string newPattern = pattern + validChars[i];
					Generate_patterns(key, newPattern, missing - 1);
				}
			}
		}

		public static void Main()
		{
			bool validKey = false;
			string key = string.Empty;
			array[0] = 50;
			array2[0] = 164;
			array3[0] = 248;
			array3[1] = 4;
			Marshal.Copy(array, 0, intPtr, 50);
			Marshal.Copy(array2, 0, intPtr2, 164);
			Marshal.Copy(array3, 0, intPtr3, 1272);
			Cli.Intro(validChars);
            while (!validKey)
            {
                key = Cli.Get_key(validChars);
                validKey = Key_isvalid(validChars, key);
            }
			Missing_count(key);
			Missing_positions(key);
			Console.WriteLine("[+] Génération des clés...");
			var watch = System.Diagnostics.Stopwatch.StartNew();
			Generate_patterns(key.ToCharArray(), String.Empty, missing);
			Console.WriteLine("[+] Nombre de clés possible: " + allPatterns.Count);
			Start(key.ToCharArray());
			watch.Stop();
			Console.WriteLine("Temps: " + watch.Elapsed + " ");
			Marshal.FreeHGlobal(intPtr);
			Marshal.FreeHGlobal(intPtr2);
			Marshal.FreeHGlobal(intPtr3);
			NativeMethods.FreeLibrary(hModule);
			Cli.End();
		}
	}
}
