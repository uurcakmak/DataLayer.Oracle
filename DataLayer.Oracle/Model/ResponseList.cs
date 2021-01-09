using System.Collections.Generic;

namespace DataLayer.Oracle.Model
{
    public class ResponseList<T> : BasicResponse where T : class
    {
        public ResponseList(List<T> list, string resultMsg)
        {
            ResultMessage = resultMsg;
            List = list;
        }

        public ResponseList()
        {
        }

        public List<T> List { get; set; }
    }
}
