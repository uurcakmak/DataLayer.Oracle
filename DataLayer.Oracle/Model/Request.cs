namespace DataLayer.Oracle.Model
{
    public class Request<T>
    {
        /// <summary>
        /// Id of the executing user.
        /// </summary>
        public int? UserId { get; set; }

        public T Data { get; set; }

        public Request() { }

        public Request(T data)
        {
            Data = data;
        }

        public Request(int userId, T data)
        {
            UserId = userId;
            Data = data;
        }
    }
}
