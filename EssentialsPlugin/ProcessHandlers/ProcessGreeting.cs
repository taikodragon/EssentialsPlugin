﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EssentialsPlugin.Utility;
using Sandbox.ModAPI;
using SEModAPIInternal.API.Common;

namespace EssentialsPlugin.ProcessHandler
{
	class GreetingItem
	{
		private DateTime start;
		public DateTime Start
		{
			get { return start; }
			set { start = value; }
		}

		private ulong steamId;
		public ulong SteamId
		{
			get { return steamId; }
			set { steamId = value; }
		}

		private TimeSpan timeout;
		public TimeSpan Timeout
		{
			get { return timeout; }
			set { timeout = value; }
		}

		private bool isNewUser;
		public bool IsNewUser
		{
			get { return isNewUser; }
			set { isNewUser = value; }
		}
	}

	public class ProcessGreeting : ProcessHandlerBase
	{
		private List<GreetingItem> m_greetingList = new List<GreetingItem>();
		private DateTime m_start = DateTime.Now;

		public override int GetUpdateResolution()
		{
			return 1000;
		}

		public override void Handle()
		{
			if (PluginSettings.Instance.GreetingMessage != "")
			{
				if (MyAPIGateway.Players == null)
					return;

				int pos = 0;
				try
				{
					List<IMyPlayer> players = new List<IMyPlayer>();
					pos = 1;
					bool result = false;
					Wrapper.GameAction(() =>
					{
						try
						{
							MyAPIGateway.Players.GetPlayers(players, null);
							result = true;
						}
						catch (Exception ex)
						{
							Logging.WriteLineAndConsole(string.Format("Failed to get player list: {0}", ex.ToString()));
						}
					});

					if(!result)
						return;

					pos = 2;
					lock (m_greetingList)
					{
						for (int r = m_greetingList.Count - 1; r >= 0; r--)
						{
							pos = 3;
							GreetingItem item = m_greetingList[r];
							if(DateTime.Now - item.Start > item.Timeout)
							{
								m_greetingList.RemoveAt(r);
								continue;
							}
							pos = 4;
							IMyPlayer player = players.FirstOrDefault(x => x.SteamUserId == item.SteamId && x.Controller != null && x.Controller.ControlledEntity != null);
							pos = 5;
							if (player != null)
							{
								pos = 6;
								m_greetingList.RemoveAt(r);

								if (item.IsNewUser)
									Communication.SendPrivateInformation(item.SteamId, PluginSettings.Instance.GreetingNewUserMessage.Replace("%name%", player.DisplayName));
								else
									Communication.SendPrivateInformation(item.SteamId, PluginSettings.Instance.GreetingMessage.Replace("%name%", player.DisplayName));
							}
						}

						pos = 7;

					}
				}
				catch (Exception ex)
				{
					Logging.WriteLineAndConsole(string.Format("Handle(): Error at pos - {0}: {1}", pos, ex.ToString()));
				}
			}

			base.Handle();
		}

		public override void OnPlayerJoined(ulong remoteUserId)
		{
			GreetingItem item = new GreetingItem();
			item.SteamId = remoteUserId;
			item.Timeout = TimeSpan.FromMinutes(10);
			item.Start = DateTime.Now;
			item.IsNewUser = PlayerMap.Instance.GetPlayerIdsFromSteamId(remoteUserId).Count() == 0;

			Logging.WriteLineAndConsole(string.Format("New User: {0}", remoteUserId));

			lock (m_greetingList)
			{
				m_greetingList.Add(item);
				Logging.WriteLineAndConsole(string.Format("Greeting Added => {0} (New user: {1})", remoteUserId, item.IsNewUser));
			}

			base.OnPlayerJoined(remoteUserId);
		}

		public override void OnPlayerLeft(ulong remoteUserId)
		{
			lock (m_greetingList)
			{
				if (m_greetingList.Find(x => x.SteamId == remoteUserId) != null)
				{
					Logging.WriteLineAndConsole(string.Format("Greeting Removed => {0}", remoteUserId));
					m_greetingList.RemoveAll(x => x.SteamId == remoteUserId);
				}
			}

			base.OnPlayerLeft(remoteUserId);
		}
	}
}
