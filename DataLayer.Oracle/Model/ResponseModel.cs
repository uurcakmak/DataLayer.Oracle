using System.Collections.Generic;

namespace DataLayer.Oracle.Model
{
    public class ResponseModel<T> : BasicResponse where T : class
    {
        public ResponseModel(T model, string resultMsg)
        {
            ResultMessage = resultMsg;
            Model = model;
        }

        public ResponseModel()
        {
        }

        public T Model { get; set; }
    }
}
