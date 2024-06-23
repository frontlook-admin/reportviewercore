using System.Collections.Generic;

namespace ReportViewerCore.Sample.WinForms
{
    public class RdlcData
    {
        public RdlcData()
        {
            
        }
        public RdlcData(string _dtName, object _dtValue)
        {
            DataName = _dtName;
            DataValue = _dtValue;
        }
        public string DataName { get; set; }
        public object DataValue { get; set; }
    }
    public class RdlcDatal
    {
        public RdlcDatal()
        {
            
        }
        public RdlcDatal(string _dtName, List<object> _dtValue)
        {
            DataName = _dtName;
            DataValue = _dtValue;
        }
        public string DataName { get; set; }
        public List<object> DataValue { get; set; }
    }
}
