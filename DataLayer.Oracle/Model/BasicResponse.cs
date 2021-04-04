namespace DataLayer.Oracle.Model
{
    public class BasicResponse
    {
        public BasicResponse()
        {
        }

        public BasicResponse(string resultMsg)
        {
            ResultMessage = resultMsg;
        }

        public string ResultCode { get; set; } = string.Empty;

        public string ResultMessage { get; set; } = string.Empty;

        public bool Result => string.IsNullOrEmpty(ResultMessage);
    }
}
