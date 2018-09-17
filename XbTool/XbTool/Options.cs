using XbTool.Common;
using XbTool.Types;

namespace XbTool
{
    public class Options
    {
        public Game Game { get; set; }
        public string ArdFilename { get; set; }
        public string ArhFilename { get; set; }
        public string DataDir { get; set; }
        public string Input { get; set; }
        public string Output { get; set; }
        public string Filter { get; set; }
        public string Xb2Dir { get; set; }
		public BdatCollection Tables { get; set; }
        public IProgressReport Progress { get; set; }
    }

}
