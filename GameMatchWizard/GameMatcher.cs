using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml.Linq;
using Unbroken.LaunchBox.Plugins;
using Unbroken.LaunchBox.Plugins.Data;

namespace GameMatchWizard
{

	public struct Platform
	{
		public string name;
		public string truePlatformName;

		public Platform(string name, string truePlatformName)
		{
			this.name = name;
			this.truePlatformName = truePlatformName;
		}
	}


	public struct Suspect
	{
		public string name;
		public int databaseID;
		public List<ObjGame> aka;

		public Suspect(string name, int databaseID)
		{
			this.name = name;
			this.databaseID = databaseID;
			this.aka = new List<ObjGame>();	
		}
	}


	internal class GameMatcher
	{
		private static XDocument _docMetadata = null;
		private List<IGame> _missingGames = null;
		private Dictionary<IGame, ObjGame> _GamesToLinkInDatabase = null;


		public List<Platform> plateformList { get; private set; }
		public Platform selectedPlatform { get; private set; }
		public IGame selectedGame { get; private set; }

		public List<Suspect> selectedSuspects { get; private set; }

		public int positionInMissingGames { get; private set; }
		public int numberOfMissingGames { get; private set; }




		public GameMatcher()
		{
			if (_docMetadata == null) Init();

			plateformList = new List<Platform>();
			foreach(var p in PluginHelper.DataManager.GetAllPlatforms())
			{
				plateformList.Add(new Platform( p.Name, String.IsNullOrWhiteSpace(p.ScrapeAs) ? p.Name : p.ScrapeAs ));
			}

			positionInMissingGames = 0;
		}

		public static void Init()
		{
			_docMetadata = XDocument.Load(@"Metadata\Metadata.xml");

		}

		public int SelectPlateform(string namePlatform)
		{
			Unload();
			var platform = PluginHelper.DataManager.GetPlatformByName(namePlatform);
			selectedPlatform = new Platform(platform.Name, String.IsNullOrWhiteSpace(platform.ScrapeAs) ? platform.Name : platform.ScrapeAs);

			_missingGames = platform.GetAllGames(true, true).Where(g => g.LaunchBoxDbId == null || g.LaunchBoxDbId == 0).ToList();

			numberOfMissingGames = _missingGames.Count;

			int nbMissing = _missingGames.Count();
			if (nbMissing > 0)
			{
				positionInMissingGames = 1;
				selectedGame = _missingGames.ElementAt(0);
				_GamesToLinkInDatabase = new Dictionary<IGame, ObjGame>();
				selectedSuspects = GetSuspects();
			}
			else
			{
				selectedSuspects = new List<Suspect>();
				selectedGame = null;
				positionInMissingGames = 0;
			}
			return nbMissing;
		}

		public List<Suspect> GetSuspects()
		{
			var suspectsResult = new List<Suspect>();
			if (positionInMissingGames == 0)
			{
				return suspectsResult;
			}

			// Requête LINQ pour extraire les données
			List<ObjGame> resultsGame = (from game in _docMetadata.Descendants("Game")
										 where (string)game.Element("Platform") == (string)selectedPlatform.truePlatformName
										 select new ObjGame
										 {
											 Name = (string)game.Element("Name"),
											 DatabaseID = (int)int.Parse((string)game.Element("DatabaseID"))
										 }).ToList<ObjGame>();

			List<ObjGame> resultsAlt = (from game in _docMetadata.Descendants("GameAlternateName")
										select new ObjGame
										{
											Name = (string)game.Element("AlternateName"),
											DatabaseID = (int)int.Parse((string)game.Element("DatabaseID"))
										}).ToList<ObjGame>();

			Dictionary<string, int> listGameName = new Dictionary<string, int>();
			Dictionary<int, string> ReverseListGameName = new Dictionary<int, string>();

			foreach (ObjGame game in resultsGame)
			{
				if (!listGameName.ContainsKey(game.Name))
				{
					listGameName.Add(game.Name, game.DatabaseID);
					ReverseListGameName.Add(game.DatabaseID, game.Name);
				}
			}


			foreach (ObjGame game in resultsAlt)
			{
				if (listGameName.ContainsValue(game.DatabaseID) && !listGameName.ContainsKey(game.Name))
				{
					listGameName.Add(game.Name, game.DatabaseID);
				}
			}


			SortedList<int, string> potentialSuspectShortList = new SortedList<int, string>(new DuplicateKeyComparer<int>());
			int potentialSuspectListSize = 5;

			foreach (var game in listGameName)
			{
				int distance = Utils.GetDamerauLevenshteinDistance(selectedGame.Title, game.Key);

				if (potentialSuspectShortList.Count < potentialSuspectListSize)
				{
					string originalName = ReverseListGameName[game.Value];
					if (!potentialSuspectShortList.ContainsValue(originalName)) potentialSuspectShortList.Add(distance, originalName);
				}
				else
				{

					if (distance < potentialSuspectShortList.Last().Key)
					{
						string originalName = ReverseListGameName[game.Value];
						if (!potentialSuspectShortList.ContainsValue(originalName))
						{
							potentialSuspectShortList.Add(distance, game.Key);
							potentialSuspectShortList.RemoveAt(potentialSuspectListSize - 1);
						}
					}
				}
			}


			foreach (var suspects in potentialSuspectShortList)
			{
				int suspectDBID = listGameName[suspects.Value];
				var suspectElem = new Suspect(suspects.Value, suspectDBID);
				suspectElem.aka = resultsAlt.Where(r => r.DatabaseID == suspectDBID).ToList();
				suspectsResult.Add(suspectElem);
			}
			return suspectsResult;
		}
		public string SelectSuspect(int num)
		{
			if(selectedSuspects.Count() >= num)
			{
				var selected = selectedSuspects.ElementAt(num - 1);
				_GamesToLinkInDatabase.Add(selectedGame, new ObjGame{ Name = selected.name, DatabaseID = selected.databaseID });
				return selectedGame.Title + " => " + selected.name;
			}
			return "";
		}

		public bool NextGame()
		{
			if(positionInMissingGames == _missingGames.Count()) return false;
			else
			{
				positionInMissingGames++;
				selectedGame = _missingGames.ElementAt(positionInMissingGames-1);
				selectedSuspects = GetSuspects();
				return true;
			}

		}

		public void Unload()
		{
			_missingGames = null;
			_GamesToLinkInDatabase = null;
			selectedGame = null;
			selectedSuspects = null;
			selectedGame = null;
			positionInMissingGames = 0;
			numberOfMissingGames= 0;
		}

		public void Save()
		{
			IPlaylist playlistGameMatcher = null;
			foreach (var p in PluginHelper.DataManager.GetAllPlaylists())
			{
				if (p.Name == "GameMatcher")
				{
					playlistGameMatcher = p; break;
				}
			}

			if(playlistGameMatcher == null)
			{
				playlistGameMatcher = PluginHelper.DataManager.AddNewPlaylist("GameMatcher");
				playlistGameMatcher.NestedName = "GameMatcher";
				playlistGameMatcher.SortTitle = "###GameMatcher";

			}

			foreach (var game in _GamesToLinkInDatabase)
			{
				IPlaylistGame nnn = playlistGameMatcher.AddNewPlaylistGame();
				game.Key.LaunchBoxDbId = game.Value.DatabaseID;
				nnn.GameId = game.Key.Id;
				nnn.LaunchBoxDbId = game.Key.LaunchBoxDbId;
				nnn.GameFileName = game.Key.ApplicationPath;
				nnn.GameTitle = game.Key.Title;
			}
			

			PluginHelper.DataManager.Save();
			Unload();

		}

		



	}



}
