using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

// http://leagueoflegends.wikia.com/wiki/RAF:_Riot_Archive_File
namespace RAFManager
{
    class Program
    {
        public class RafFileHeader
        {
            public byte[] MagicNumber;
            public byte[] Version;
            public byte[] ManagerIndex;
            public byte[] FileListOffset;
            public byte[] PathListOffset;
            public RafFileList FileList;
            public RafPathList PathList;
        }

        public class RafFileList
        {
            public byte[] NumberOfEntries;
            public List<RafFileListEntry> Entries;
        }

        public class RafFileListEntry
        {
            public byte[] PathHash;
            public byte[] DataOffset;
            public byte[] DataSize;
            public byte[] PathListIndex;
        }

        public class RafPathList
        {
            public byte[] PathListSize;
            public byte[] PathListCount;
            public List<RafPathListEntry> PathListEntries;
            public List<string> PathStrings;
        }

        public class RafPathListEntry
        {
            public byte[] PathOffset;
            public byte[] PathLength;
        }

        static void Main(string[] args)
        {
            const string path = @"C:\Riot Games\League of Legends\RADS\projects\lol_game_client\filearchives\0.0.0.25\Archive_2.raf";

            using (var binaryReader = new BinaryReader(File.OpenRead(path)))
            {
                var rafFileHeader = new RafFileHeader
                {
                    MagicNumber = binaryReader.ReadBytes(4),
                    Version = binaryReader.ReadBytes(4),
                    ManagerIndex = binaryReader.ReadBytes(4),
                    FileListOffset = binaryReader.ReadBytes(4),
                    PathListOffset = binaryReader.ReadBytes(4)
                };


                binaryReader.BaseStream.Position = BitConverter.ToInt32(rafFileHeader.FileListOffset, 0);
                var rafFileList = new RafFileList
                {
                    NumberOfEntries = binaryReader.ReadBytes(4)
                };
                rafFileHeader.FileList = rafFileList;
                rafFileList.Entries = new List<RafFileListEntry>();
                for (int i = 0; i < BitConverter.ToInt32(rafFileList.NumberOfEntries, 0); i++)
                {
                    var fileListEntry = new RafFileListEntry
                    {
                        PathHash = binaryReader.ReadBytes(4),
                        DataOffset = binaryReader.ReadBytes(4),
                        DataSize = binaryReader.ReadBytes(4),
                        PathListIndex = binaryReader.ReadBytes(4)
                    };
                    rafFileHeader.FileList.Entries.Add(fileListEntry);
                }

                binaryReader.BaseStream.Position = BitConverter.ToInt32(rafFileHeader.PathListOffset, 0);
                var positionPathList = binaryReader.BaseStream.Position;
                var rafPathList = new RafPathList()
                {
                    PathListSize = binaryReader.ReadBytes(4),
                    PathListCount = binaryReader.ReadBytes(4)
                };
                rafFileHeader.PathList = rafPathList;
                rafPathList.PathListEntries = new List<RafPathListEntry>();
                rafPathList.PathStrings = new List<string>();
         

                for (int i = 0; i < BitConverter.ToInt32(rafPathList.PathListCount, 0); i++)
                {
                    var pathListEntry = new RafPathListEntry
                    {
                        PathOffset = binaryReader.ReadBytes(4),
                        PathLength = binaryReader.ReadBytes(4)
                    };
                    rafPathList.PathListEntries.Add(pathListEntry);
                }

                foreach (var pathListEntry in rafPathList.PathListEntries)
                {
                    var pathOffset = BitConverter.ToInt32(pathListEntry.PathOffset, 0);
                    var pathLength = BitConverter.ToInt32(pathListEntry.PathLength, 0);

                    binaryReader.BaseStream.Position = positionPathList + pathOffset;
                    rafPathList.PathStrings.Add(Encoding.ASCII.GetString(binaryReader.ReadBytes(pathLength - 1)));
                }

                Console.WriteLine("Index 0: " + rafFileHeader.PathList.PathStrings[0]);
            }
            
            Console.ReadKey();
        }
    }
}
