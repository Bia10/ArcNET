using ArcNET.DataTypes.Common;
using ArcNET.DataTypes.GameObjects;
using ArcNET.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ArcNET.DataTypes
{
    public class SectorReader
    {
        private readonly BinaryReader _reader;

		public SectorReader(BinaryReader reader)
		{
            _reader = reader;
        }

        public Sector ReadSector()
		{
			var sector = new Sector();
            ReadLights(_reader, sector.Lights);
            ReadTiles(_reader, sector.Tiles);
            SkipRoofList(_reader);

			var placeholder = _reader.ReadInt32();
			AnsiConsoleExtensions.Log(placeholder.ToString("X4"),"info");
            AnsiConsoleExtensions.Log(_reader.BaseStream.Position.ToString("X4"), "info");

            if (placeholder < 0xAA0000 || placeholder > 0xAA0004)
			{
                throw new InvalidDataException("Invalid placeholder value read from sector.");
			}
            // All of these seems to be old Arcanum leftovers
			if (placeholder >= 0xAA0001)
			{
				ReadTileScripts(_reader, sector);
			}
			if (placeholder >= 0xAA0002)
			{
				ReadSectorScripts(_reader, sector);
			}
			if (placeholder >= 0xAA0003)
			{
                _reader.ReadInt32(); // Townmap Info
                _reader.ReadInt32(); // Aptitude Adjustment
                _reader.ReadInt32(); // Light Scheme
                _reader.ReadBytes(12);// Sound List
			}
			if (placeholder >= 0xAA0004)
			{
                _reader.ReadBytes(512);
			}
			ReadObjects(_reader, sector);

			if (_reader.BaseStream.Position + 4 != _reader.BaseStream.Length) 
                throw new Exception();

			return sector;
		}

		private static void ReadLights(BinaryReader reader, ICollection<SectorLight> sectorLights)
		{
			var lightCount = reader.ReadInt32();

			for (var i = 0; i < lightCount; ++i)
			{
				var light = ReadLight(reader);
				sectorLights.Add(light);
			}
        }

		private static SectorLight ReadLight(BinaryReader reader)
		{
			var handle = reader.ReadUInt64();

            //var type = reader.ReadInt32();

            var result = new SectorLight
            {
                Handle = handle,
                Position = reader.ReadLocation(true),
                OffsetX = reader.ReadInt32(),
                OffsetY = reader.ReadInt32(),
                Flags0 = reader.ReadInt32(),
                Art = reader.ReadInt32(),
                Color0 = reader.ReadInt32(),
                Color1 = reader.ReadInt32(),
                Unk0 = reader.ReadInt32(),
                Unk1 = reader.ReadInt32()
            };

            // Read the basic light information first
            /*
            if ((type & 0x10) == 0x10 || (type & 0x40) == 0x40)
            {
                var partSys = new SectorLightParticles();
                partSys.ParticleSystemHash = reader.ReadInt32();
                partSys.ParticleSystemHandle = reader.ReadInt32();
                result.Particles = partSys;
            }
            if ((type & 0x40) == 0x40)
            {
                var atNight = new SectorLightAtNight();
                atNight.field0 = reader.ReadInt32();
                atNight.field4 = reader.ReadInt32();
                atNight.field8 = reader.ReadInt32();
                atNight.fieldc = reader.ReadInt32();
                atNight.field10 = reader.ReadInt32();
                atNight.field14 = reader.ReadInt32();
                atNight.field18 = reader.ReadInt32();
                result.AtNight = atNight;

                var partSys = new SectorLightParticles();
                partSys.ParticleSystemHash = reader.ReadInt32();
                partSys.ParticleSystemHandle = reader.ReadInt32();
                atNight.Particles = partSys;
            }
            */

			return result;
		}

		private static void ReadTiles(BinaryReader reader, Sector.SectorTile[] tiles)
		{
            for (var i = 0; i < tiles.Length; ++i)
			{
				tiles[i].Data = reader.ReadUInt32();
				ArtId.ArtIds.Add(tiles[i].Data.ToString("X2"));
			}
		}

		private static void SkipRoofList(BinaryReader reader)
		{
            var isPresent = reader.ReadInt32();
			if (isPresent == 0)
			{
				reader.BaseStream.Seek(256 * 4, SeekOrigin.Current);
			}
        }

		private static void ReadTileScripts(BinaryReader reader, Sector sector)
		{
			var count = reader.ReadInt32();
			//TODO: count > 0
			for (var i = 0; i < count; ++i)
			{
				var script = new TileScript()
				{
					F1 = reader.ReadInt32(),
					F2 = reader.ReadInt32(),
					F3 = reader.ReadInt32(),
					F4 = reader.ReadInt32(),
					F5 = reader.ReadInt32(),
					F6 = reader.ReadInt32()
				};
                sector.TileScripts.Add(script);
			}
		}

		private static void ReadSectorScripts(BinaryReader reader, Sector sector)
		{
			var script = new GameObjectScript()
			{
				Counters = reader.ReadBytes(4),
				Flags = reader.ReadInt32(),
				ScriptId = reader.ReadInt32()
			};

            if (script.ScriptId != 0 || !script.Counters.SequenceEqual(new byte[] { 0x00, 0x00, 0x00, 0x00 }) || script.Flags != 0)
			{
				sector.SectorScript = script;
			}
        }

		private static void ReadObjects(BinaryReader reader, Sector sector)
		{
            var stream = reader.BaseStream;
			var startOfObjects = stream.Position;

			// Move to end of file, last 4 bytes are count of objects
			stream.Seek(-4, SeekOrigin.End);
			var count = reader.ReadInt32();

			// Now move back to start and parse for count found
			stream.Seek(startOfObjects, SeekOrigin.Begin);
            for (var i = 0; i < count; ++i)
			{
				sector.Objects.Add(reader.GetGameObject());
			}
		}
    }
}