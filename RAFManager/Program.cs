using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using zlib = ComponentAce.Compression.Libs.zlib;
using System.Linq;

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
            //const string pathRaf = @"C:\Riot Games\League of Legends\RADS\projects\lol_game_client\filearchives\0.0.0.25\Archive_2.raf";
            const string pathRaf = @"D:\League of Legends\RADS\projects\lol_game_client\filearchives\0.0.0.25\Archive_2.raf";
            const string pathRafDat = @"D:\League of Legends\RADS\projects\lol_game_client\filearchives\0.0.0.25\Archive_2.raf.dat";

            using (var binaryReaderRaf = new BinaryReader(File.OpenRead(pathRaf)))
            {
                using (var binaryReadRafDat = new BinaryReader(File.OpenRead(pathRafDat)))
                {
                    RafFileHeader rafFileHeader = new RafFileHeader
                    {
                        MagicNumber = binaryReaderRaf.ReadBytes(4),
                        Version = binaryReaderRaf.ReadBytes(4),
                        ManagerIndex = binaryReaderRaf.ReadBytes(4),
                        FileListOffset = binaryReaderRaf.ReadBytes(4),
                        PathListOffset = binaryReaderRaf.ReadBytes(4)
                    };

                    binaryReaderRaf.BaseStream.Position = BitConverter.ToInt32(rafFileHeader.FileListOffset, 0);
                    var rafFileList = new RafFileList
                    {
                        NumberOfEntries = binaryReaderRaf.ReadBytes(4)
                    };
                    rafFileHeader.FileList = rafFileList;
                    rafFileList.Entries = new List<RafFileListEntry>();
                    for (int i = 0; i < BitConverter.ToInt32(rafFileList.NumberOfEntries, 0); i++)
                    {
                        var fileListEntry = new RafFileListEntry
                        {
                            PathHash = binaryReaderRaf.ReadBytes(4),
                            DataOffset = binaryReaderRaf.ReadBytes(4),
                            DataSize = binaryReaderRaf.ReadBytes(4),
                            PathListIndex = binaryReaderRaf.ReadBytes(4)
                        };
                        rafFileHeader.FileList.Entries.Add(fileListEntry);
                    }

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
                    }

                    foreach (var pathListEntry in rafPathList.PathListEntries)
                    {
                        var pathOffset = BitConverter.ToInt32(pathListEntry.PathOffset, 0);
                        var pathLength = BitConverter.ToInt32(pathListEntry.PathLength, 0);

                        binaryReaderRaf.BaseStream.Position = positionPathList + pathOffset;
                        rafPathList.PathStrings.Add(Encoding.ASCII.GetString(binaryReaderRaf.ReadBytes(pathLength - 1)));
                    }


                    // 120 = 100% 60 = x   100
                    for (int i = 0; i < rafFileHeader.FileList.Entries.Count; i++)
                    {
                        Console.Write("\x000D{0}%   ", ((double)i / (double)rafFileHeader.FileList.Entries.Count * 100).ToString("0.00"));
                        var fileListEntry = rafFileHeader.FileList.Entries.FirstOrDefault(item => BitConverter.ToInt32(item.PathListIndex, 0) == i);

                        int fileListEntryOffset = BitConverter.ToInt32(fileListEntry.DataOffset, 0);
                        int fileListEntryDataSize = BitConverter.ToInt32(fileListEntry.DataSize, 0);
                        binaryReadRafDat.BaseStream.Position = fileListEntryOffset;
                        byte[] buffer = binaryReadRafDat.ReadBytes(fileListEntryDataSize);

                        string path = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location), "export", rafFileHeader.PathList.PathStrings[i]));
                        try
                        {
                            using (MemoryStream mStream = new MemoryStream(buffer))
                            {
                                using (zlib.ZInputStream zinput = new zlib.ZInputStream(mStream))
                                {
                                    List<byte> dBuffer = new List<byte>();
                                    int data;
                                    while ((data = zinput.Read()) != -1)
                                    {
                                        dBuffer.Add((byte)data);
                                    }
                                    buffer = dBuffer.ToArray();
                                }
                            }
                        }
                        catch (Exception x)
                        {
                        }
                        if (!Directory.Exists(Path.GetDirectoryName(path)))
                        {
                            Directory.CreateDirectory(Path.GetDirectoryName(path));
                        }
                        File.WriteAllBytes(path, buffer);                                     
                    }
                }
            }

            Console.ReadKey();
        }
    }
}
