#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using XansTools.Utilities.RW.DataPersistence;

namespace DreamsOfInfiniteGlass.Character.PlayerCharacter.DataStorage { 

	public class MechSaveData : ISaveable {
		public void SaveToStream(SaveScope scope, BinaryWriter writer) {
			throw new NotImplementedException();
		}

		public void ReadFromStream(SaveScope scope, BinaryReader reader) {
			throw new NotImplementedException();
		}
	}
}
