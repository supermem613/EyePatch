namespace EyePatch
{
    public class EyePatchException : Exception
    {
        public EyePatchException(string message) : base(message)
        {
        }

        public EyePatchException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}