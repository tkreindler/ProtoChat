namespace Service
{
    public interface IClientServicer
    {
        /// <summary>
        /// Kicks off listening asynchronously on a background thread
        /// </summary>
        void Start();
    }
}
