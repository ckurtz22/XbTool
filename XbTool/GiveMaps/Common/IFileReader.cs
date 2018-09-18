using System.Collections.Generic;

namespace GiveMaps.Common
{
    public interface IFileReader
    {
        byte[] ReadFile(string filename);
        IEnumerable<string> FindFiles(string pattern);
        bool Exists(string filename);
    }
}
