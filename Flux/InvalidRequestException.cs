namespace Flux
{
    using System;
    using System.Runtime.Serialization;

    [Serializable]
    public class InvalidRequestException : Exception
    {
        private readonly string _requestText;

        public InvalidRequestException(string requestText)
        {
            _requestText = requestText;
        }

        public InvalidRequestException(string requestText, string message)
            : base(message)
        {
            _requestText = requestText;
        }

        protected InvalidRequestException(
            SerializationInfo info,
            StreamingContext context)
            : base(info, context)
        {
            _requestText = info.GetString("RequestText");
        }

        public string RequestText
        {
            get { return _requestText; }
        }

        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue("RequestText", _requestText);
            base.GetObjectData(info, context);
        }
    }
}