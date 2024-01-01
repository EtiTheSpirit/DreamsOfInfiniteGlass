using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XansTools.Utilities;
using XansTools.Utilities.RW.DataPersistence;

namespace DreamsOfInfiniteGlass.Data.Persistent {
	
	public class GlassSaveData : ISaveable {

		public static GlassSaveData Instance { get; } = new GlassSaveData();

		public bool HasMechPlayerExploded { get; set; }

		public int ImportantThingsDone { get; set; }


		public void SaveToStream(SaveScope scope, BinaryWriter writer) {
			writer.Write(HasMechPlayerExploded);
			writer.Write(ImportantThingsDone);
		}

		public void ReadFromStream(SaveScope scope, BinaryReader reader) {
			HasMechPlayerExploded = reader.ReadBoolean();
			ImportantThingsDone = reader.ReadInt32();
		}
	}

}
