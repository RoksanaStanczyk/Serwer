using DarkRift;
using DarkRift.Server;
using Scripts.Helpers;
using Scripts.Networking;
using System;
using System.Collections.Generic;
using System.Text;

namespace Scripts.Models {
	class MatchModel {

		public enum SlateStatus {
			NONE,
			PLAYER1,
			PLAYER2
		}

		private static ushort globalID = 0;

		public readonly ushort id;
		private PlayerModel player1;
		private PlayerModel player2;

		private ushort currentPlayerClientID;

		public SlateStatus[] slates;

		public bool MatchOver = false;

		public ushort CurrentPlayerClientID {
			get { return currentPlayerClientID; }
		}

		public MatchModel(PlayerModel player1, PlayerModel player2) {

			id = ++globalID;

			currentPlayerClientID = player1.Client.ID;
			this.player1 = player1;
			this.player2 = player2;

			slates = new SlateStatus[9];
		}

	
		public bool PlayerTakesSlate(ushort slateIndex, IClient client) {

			if (currentPlayerClientID != client.ID) {
				Console.WriteLine("Nie Twoja tura");
				return false;
			}

			
			if (slateIndex >= slates.Length) {
				Console.WriteLine("Złe pole");
				return false;
			}

			if (slates[slateIndex] != SlateStatus.NONE) {
				Console.WriteLine("pole zajęte");
				return false;
			}

			if (player1.Client == client) {
				Console.WriteLine($"gracz 1 (klient-{player1.Client.ID}) zaznaczył pozycję: {slateIndex}");
			} else if (player2.Client == client) {
				Console.WriteLine($"gracz 2 ( klient-{player2.Client.ID}) zaznaczył pozycję: {slateIndex}");
			}

			
			slates[slateIndex] = player1.Client == client ? SlateStatus.PLAYER1 : SlateStatus.PLAYER2;

			using (DarkRiftWriter writer = DarkRiftWriter.Create()) {

				ushort winnerClientID = 0;
				bool win = MatchHelper.GetWinner(slates, player1.Client.ID, player2.Client.ID, out winnerClientID);
				bool draw = true;

				if (win == false) {
					// spr czy pełne
					for(int i = 0; i < slates.Length; i++) {
						if (slates[i] == SlateStatus.NONE) {
							draw = false;
							break;
						}
					}
				}

				writer.Write(slateIndex);
				writer.Write(client.ID);
				Console.WriteLine($"wykonanie ruchu przez klienta o id: {client.ID}.");

				if (win) {
					writer.Write((byte)1);
				} else if (draw) {
					writer.Write((byte)2);
				} else {
					writer.Write((byte)0);
				}

				if (win) {
					MatchOver = true;
					writer.Write(winnerClientID);
					Console.BackgroundColor = ConsoleColor.Yellow;
					Console.ForegroundColor = ConsoleColor.DarkGreen;
					Console.WriteLine("mamy ZWYCIĘZCĘ");
					Console.BackgroundColor = ConsoleColor.Black;
					Console.ForegroundColor = ConsoleColor.White;
				} else if (draw) {
					MatchOver = true;
					Console.BackgroundColor = ConsoleColor.Yellow;
					Console.ForegroundColor = ConsoleColor.DarkRed;
					Console.WriteLine("REMIS");
					Console.BackgroundColor = ConsoleColor.Black;
					Console.ForegroundColor = ConsoleColor.White;
				}
				using (Message msg = Message.Create((ushort)Tags.Tag.SERVER_CONFIRM_SLATE_TAKEN, writer)) {
					player1.Client.SendMessage(msg, SendMode.Reliable);
					player2.Client.SendMessage(msg, SendMode.Reliable);
				}

				currentPlayerClientID = currentPlayerClientID == player1.Client.ID ? player2.Client.ID : player1.Client.ID;
				Console.WriteLine($"kolej klienta o id: {currentPlayerClientID}");
			}

			return true;
		}

	}
}
