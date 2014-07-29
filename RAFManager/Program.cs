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
            public byte[] Buffer; // Placeholder for content from .raf.dat file
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
            const string filearchiveDir = @"C:\Riot Games\League of Legends\RADS\projects\lol_game_client\filearchives";
            string[] rafFiles = Directory.GetFiles(filearchiveDir, "*.raf", SearchOption.AllDirectories);

            foreach (string rafFilePath in rafFiles)
            {
                using (var binaryReaderRaf = new BinaryReader(File.OpenRead(rafFilePath)))
                {
                    using (var binaryReadRafDat = new BinaryReader(File.OpenRead(Path.ChangeExtension(rafFilePath, ".raf.dat"))))
                    {
                        var rafFileHeader = new RafFileHeader
                        {
                            MagicNumber = binaryReaderRaf.ReadBytes(4),
                            Version = binaryReaderRaf.ReadBytes(4),
                            ManagerIndex = binaryReaderRaf.ReadBytes(4),
                            FileListOffset = binaryReaderRaf.ReadBytes(4),
                            PathListOffset = binaryReaderRaf.ReadBytes(4)
                        };

                        binaryReaderRaf.BaseStream.Position = BitConverter.ToInt32(rafFileHeader.PathListOffset, 0);
                        var positionPathList = binaryReaderRaf.BaseStream.Position;
                        var rafPathList = new RafPathList()
                        {
                            PathListSize = binaryReaderRaf.ReadBytes(4),
                            PathListCount = binaryReaderRaf.ReadBytes(4)
                        };
                        rafFileHeader.PathList = rafPathList;
                        rafPathList.PathListEntries = new List<RafPathListEntry>();
                        rafPathList.PathStrings = new List<string>();


                        for (int i = 0; i < BitConverter.ToInt32(rafPathList.PathListCount, 0); i++)
                        {
                            var pathListEntry = new RafPathListEntry
                            {
                                PathOffset = binaryReaderRaf.ReadBytes(4),
                                PathLength = binaryReaderRaf.ReadBytes(4)
                            };
                            rafPathList.PathListEntries.Add(pathListEntry);

                            long position = binaryReaderRaf.BaseStream.Position; // remember position

                            var pathOffset = BitConverter.ToInt32(pathListEntry.PathOffset, 0);
                            var pathLength = BitConverter.ToInt32(pathListEntry.PathLength, 0);
                            binaryReaderRaf.BaseStream.Position = positionPathList + pathOffset;
                            rafPathList.PathStrings.Add(
                                Encoding.ASCII.GetString(binaryReaderRaf.ReadBytes(pathLength - 1)));

                            binaryReaderRaf.BaseStream.Position = position; // jump back
                        }

                        binaryReaderRaf.BaseStream.Position = BitConverter.ToInt32(rafFileHeader.FileListOffset, 0);
                        var rafFileList = new RafFileList
                        {
                            NumberOfEntries = binaryReaderRaf.ReadBytes(4)
                        };
                        rafFileHeader.FileList = rafFileList;
                        rafFileList.Entries = new List<RafFileListEntry>();
                        for (int i = 0; i < BitConverter.ToInt32(rafFileList.NumberOfEntries, 0); i++)
                        {
                            Console.Write("\x000D{0}%", (i/(double) BitConverter.ToInt32(rafFileList.NumberOfEntries, 0)*100).ToString("0.00"));
                            var fileListEntry = new RafFileListEntry
                            {
                                PathHash = binaryReaderRaf.ReadBytes(4),
                                DataOffset = binaryReaderRaf.ReadBytes(4),
                                DataSize = binaryReaderRaf.ReadBytes(4),
                                PathListIndex = binaryReaderRaf.ReadBytes(4)
                            };
                            rafFileHeader.FileList.Entries.Add(fileListEntry);

                            int fileListEntryOffset = BitConverter.ToInt32(fileListEntry.DataOffset, 0);
                            int fileListEntryDataSize = BitConverter.ToInt32(fileListEntry.DataSize, 0);
                            binaryReadRafDat.BaseStream.Position = fileListEntryOffset;
                            fileListEntry.Buffer = RafDat.Deflate(binaryReadRafDat.ReadBytes(fileListEntryDataSize));

                            string version = new DirectoryInfo(rafFilePath).Parent.Name;
                            RafDat.Save(fileListEntry.Buffer, Path.Combine(version, rafPathList.PathStrings[BitConverter.ToInt32(fileListEntry.PathListIndex, 0)]));
                        }
                    }
                }
            }

            Console.WriteLine("Done.");
            Console.ReadKey();
        }
    }
}
