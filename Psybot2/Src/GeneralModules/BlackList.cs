using System.Linq;
using System.Collections.Generic;
using System.IO;

namespace Psybot2.Src.GeneralModules
{
    internal sealed class BlackList
    {
        private const string DATA_FILE = "black_list.txt";

        private List<ulong> blackIdList = new List<ulong>();

        public BlackList()
        {
            if (File.Exists(DATA_FILE))
            {
                string[] bl = File.ReadAllLines(DATA_FILE);
                for (int i = 0; i < bl.Length; i++)
                {
                    if (ulong.TryParse(bl[i], out ulong id))
                        blackIdList.Add(id);
                }
            }
        }

        public void SaveData()
        {
            if (File.Exists(DATA_FILE))
                File.Delete(DATA_FILE);

            if (blackIdList.Count != 0)
            {
                // convert ulong to string
                string[] contents = blackIdList.Select((a) => a.ToString()).ToArray();
                File.WriteAllLines(DATA_FILE, contents);
            }
        }

        public void Add(ulong id) => blackIdList.Add(id);

        public void Remove(ulong id) => blackIdList.Remove(id);

        public bool Contains(ulong id) => blackIdList.Contains(id);

    }
}
