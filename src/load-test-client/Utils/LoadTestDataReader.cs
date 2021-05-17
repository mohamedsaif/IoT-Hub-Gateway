using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LoadTestClient.Utils
{
    public class LoadTestDataReader
    {
        public static void LoadData(string filePath)
        {
            var reader = new StreamReader(filePath);
        }
    }
}
