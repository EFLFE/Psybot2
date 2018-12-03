using System;
using System.IO;

namespace Psybot2.Src.EF
{
    internal class BinaryStream
    {
        public readonly FileStream FStream;
        public readonly BinaryReader Reader;
        public readonly BinaryWriter Writer;

        public BinaryStream(string path, FileMode mode, FileAccess access, FileShare share)
        {
            FStream = File.Open(path, mode, access, share);
            Reader = new BinaryReader(FStream);
            Writer = new BinaryWriter(FStream);
        }

        public void Unload()
        {
            FStream.Close();
            FStream.Dispose();
        }

    }
}
