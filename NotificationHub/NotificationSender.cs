namespace NotificationHub
{

    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Net;
    using System.Runtime.Serialization.Json;
    using System.Text;
    using System.Web;

    public class NotificationSender
    {

        private OAuthToken oAuthToken = null;

        private string secret = String.Empty;
        private string urlEncodedSecret = String.Empty;

        private string sid = String.Empty;
        private string urlEncodedSid = String.Empty;


        /// <summary>
        /// <c>grant_type=client_credentials&client_id={0}&client_secret={1}&scope=notify.windows.com</c>
        /// </summary>
        private const string BODY = "grant_type=client_credentials&client_id={0}&client_secret={1}&scope=notify.windows.com";

        /// <summary>
        /// <c>application/x-www-form-urlencoded</c>
        /// </summary>
        private const string CONTENT_TYPE_APPLICATION_FORM_URLENCODED = "application/x-www-form-urlencoded";

        /// <summary>
        /// <c>https://login.live.com/accesstoken.srf</c>
        /// </summary>
        private const string LIVE_ADDRESS_AUTHENTICATION = "https://login.live.com/accesstoken.srf";


        public NotificationSender(string secret, string sid)
        {
            if (String.IsNullOrEmpty(secret) || String.IsNullOrEmpty(sid))
            {
                throw new ArgumentNullException("The Secret and SID should be informed");
            }

            this.secret = secret;
            this.urlEncodedSecret = HttpUtility.UrlEncode(this.secret);

            this.sid = sid;
            this.urlEncodedSid = HttpUtility.UrlEncode(this.sid);

            GetAuthorizationToken();
        }

        private void GetAuthorizationToken()
        {
            string response = String.Empty;
            string requestParameters = String.Format(BODY, urlEncodedSid, urlEncodedSecret);

            using (var client = new WebClient())
            {
                client.Headers.Add("Content-Type", CONTENT_TYPE_APPLICATION_FORM_URLENCODED);
                response = client.UploadString(LIVE_ADDRESS_AUTHENTICATION, requestParameters);
            }

            using (var memoryStream = new MemoryStream(Encoding.Unicode.GetBytes(response)))
            {
                var serializer = new DataContractJsonSerializer(typeof(OAuthToken));
                this.oAuthToken = (OAuthToken)serializer.ReadObject(memoryStream);
            }
        }

        private HttpWebRequest PreparePostRequest(string channelUri)
        {
            var request = HttpWebRequest.Create(channelUri) as HttpWebRequest;
            request.Method = "POST";

            request.Headers.Add("X-WNS-Type", "wns/toast");
            request.ContentType = "text/xml";

            request.Headers.Add("Authorization", String.Format("Bearer {0}", this.oAuthToken.AccessToken));

            return request;
        }

        public NotificationResponseModel PostToastToWns(NotificationModel notificationModel)
        {
            var notificationResponseModel = new NotificationResponseModel() { ChannelUri = notificationModel.ChannelUri };

            try
            {
                byte[] contentInBytes = Encoding.UTF8.GetBytes(notificationModel.XmlMessage);

                var request = PreparePostRequest(notificationModel.ChannelUri);

                using (var requestStream = request.GetRequestStream())
                {
                    requestStream.Write(contentInBytes, 0, contentInBytes.Length);
                }

                using (var webResponse = (HttpWebResponse)request.GetResponse())
                {
                    notificationResponseModel.StatusCode = webResponse.StatusCode;
                }
            }
            catch (WebException ex)
            {
                notificationResponseModel.StatusCode = ((HttpWebResponse)ex.Response).StatusCode;

                // StatusCode Unauthorized represents an expiration token => call to GetAuthorizationToken() and try again
                // StatusCode Gone || NotFound represents the ChannelUri is no longer valid
                // StatusCode NotAcceptable represents the ChannelUri is being throttled by WNS

                // In other cases, get the information returned by the call
                // http://msdn.microsoft.com/en-us/library/windows/apps/hh868245.aspx#wnsresponsecodes

                if (ex.Response.Headers["X-WNS-Debug-Trace"] != null)
                {
                    notificationResponseModel.DebugTrace = ex.Response.Headers["X-WNS-Debug-Trace"].ToString();
                }

                if (ex.Response.Headers["X-WNS-Error-Description"] != null)
                {
                    notificationResponseModel.ErrorDescription = ex.Response.Headers["X-WNS-Error-Description"].ToString();
                }

                if (ex.Response.Headers["X-WNS-Msg-ID"] != null)
                {
                    notificationResponseModel.MessageId = ex.Response.Headers["X-WNS-Msg-ID"].ToString();
                }

                if (ex.Response.Headers["X-WNS-NotificationStatus"] != null)
                {
                    notificationResponseModel.NotificationStatus = ex.Response.Headers["X-WNS-NotificationStatus"].ToString();
                }

                if (ex.Response.Headers["X-WNS-Status"] != null)
                {
                    notificationResponseModel.Status = ex.Response.Headers["X-WNS-Status"].ToString();
                }

                if (ex.Response.Headers["Date"] != null)
                {
                    // GMT DateTime (notificationResponseModel.Date.UtcDateTime.ToString())
                    notificationResponseModel.Date = Convert.ToDateTime(ex.Response.Headers["Date"]);
                }
            }
            catch (Exception ex)
            {
                return null;
            }

            return notificationResponseModel;
        }

    }

}