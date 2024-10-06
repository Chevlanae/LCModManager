using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LCModManager
{
    namespace Downloads
    {
        public interface IDownload
        {
            string SourceURI { get; set; }
            string SourceType { get; set; }
            string DestinationPath { get; set; }
        }

        public class Download : IDownload
        {
            public string SourceURI { get; set; }
            public string SourceType { get; set; }
            public string DestinationPath { get; set; }

            public void Start()
            {
                
            }
        }
    }
}
