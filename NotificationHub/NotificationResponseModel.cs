namespace NotificationHub
{

    using System;
    using System.Net;

    public class NotificationResponseModel
    {

        public HttpStatusCode StatusCode { get; set; }
        public string ChannelUri { get; set; }

        public string DebugTrace { get; set; }
        public string ErrorDescription { get; set; }
        public string MessageId { get; set; }
        public string NotificationStatus { get; set; }
        public string Status { get; set; }
        public DateTimeOffset Date { get; set; }
    }

}