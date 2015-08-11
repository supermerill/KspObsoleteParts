/*
Copyright 2015 Merill (merill@free.fr)

This file is part of ObsoleteParts.
ObsoleteParts is free software: you can redistribute it and/or modify it 
under the terms of the GNU General Public License as published by 
the Free Software Foundation, either version 3 of the License, 
or (at your option) any later version.

PartUpgrader is distributed in the hope that it will be useful, 
but WITHOUT ANY WARRANTY; without even the implied warranty 
of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. 
See the GNU General Public License for more details.

You should have received a copy of the GNU General Public License 
along with PartUpgrader. If not, see http://www.gnu.org/licenses/.

*/

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace SpaceRace
{
	[KSPAddon(KSPAddon.Startup.SpaceCentre, true)]
	public class ObsoletePartSetter : MonoBehaviour
	{

		private List<string> allTechResearched;
		public Dictionary<String, String> part2ObsoleteTech;
		public Dictionary<String, PartCategories> part2ResearchTech;

		public void Start()
		{
			allTechResearched = new List<string>();
			part2ObsoleteTech = new Dictionary<String, String>();
			part2ResearchTech = new Dictionary<String, PartCategories>();
			GameEvents.OnTechnologyResearched.Add(researchDone);
			GameEvents.onGameStateLoad.Add(loadSave);
			foreach (AvailablePart ap in PartLoader.LoadedPartsList)
			{
				//not kerbalEVA or kerbalEVAfemale
				if (ap.partConfig != null)
				{
					if (ap.partConfig.HasValue("TechObsolete"))
					{
						part2ObsoleteTech[ap.name] = ap.partConfig.GetValue("TechObsolete");
					}
				}
			}
			getAllTechnologies();
			reloadAndUpgrade();
		}

		private void getAllTechnologies()
		{
			//maj currenttech
			allTechResearched.Clear();
			if (HighLogic.CurrentGame.Mode != Game.Modes.SANDBOX)
			{
				ProtoScenarioModule protoScenario = HighLogic.CurrentGame.scenarios.Find(x => x.moduleName == "ResearchAndDevelopment");
				foreach (ConfigNode tech in protoScenario.GetData().GetNodes("Tech"))
				{
					string node = tech.GetValue("id");
					allTechResearched.Add(node);
				}
			}
		}

		public void loadSave(ConfigNode loadedNode)
		{
			if (needReload())
			{
				getAllTechnologies();
				reloadAndUpgrade();
			}
		}

		public bool needReload()
		{
			if (HighLogic.CurrentGame.Mode == Game.Modes.SANDBOX)
			{
				//don't need to reload a sandbox!
				return false;
			}

			List<string> researchedNow = new List<string>();
			researchedNow.AddRange(allTechResearched);
			ProtoScenarioModule protoScenario = HighLogic.CurrentGame.scenarios.Find(x => x.moduleName == "ResearchAndDevelopment");

			foreach (ConfigNode tech in protoScenario.GetData().GetNodes("Tech"))
			{
				string node = tech.GetValue("id");

				if (allTechResearched.Contains(node))
				{
					researchedNow.Remove(node);
				}
				else
				{
					//a new researched note!
					return true;
				}
			}

			//test if we load a revious save without an actuel tech
			if (researchedNow.Count == 0) return false;
			else return true;
		}

		public void researchDone(GameEvents.HostTargetAction<RDTech, RDTech.OperationResult> research)
		{
			//obligé que l'on a changé de tech
			if (research.target == RDTech.OperationResult.Successful)
			{
				//allTechResearched.Add(research.host.title);
				allTechResearched.Add(research.host.techID);
				reloadAndUpgrade();
			}
		}

		public void reloadAndUpgrade()
		{
			try
			{
				//maj part
				foreach (AvailablePart ap in PartLoader.LoadedPartsList)
				{
					if (ap.partConfig != null)
					{
						if (part2ObsoleteTech.ContainsKey(ap.name))
						{
							if (allTechResearched.Contains(part2ObsoleteTech[ap.name]))
							{
								if (!part2ResearchTech.ContainsKey(ap.name))
								{
									part2ResearchTech[ap.name] = ap.category;
								}
								ap.category = PartCategories.none;
							}
							else
							{
								if (part2ResearchTech.ContainsKey(ap.name))
								{
									ap.category = part2ResearchTech[ap.name];
								}
							}
						}
					}

				}
			}
			catch (Exception e)
			{
				print("[obsolete] Error: " + e);
			}
		}
	}
}
