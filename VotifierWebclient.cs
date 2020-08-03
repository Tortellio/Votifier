using System;
using System.Net;

namespace fr34kyn01535.Votifier
{
    public class VotifierWebclient : WebClient
    {
        public int Timeout { get; set; }
        public VotifierWebclient(int timeout = 5000)
        {
            Timeout = timeout;
        }
        protected override WebRequest GetWebRequest(Uri address)
        {
            var result = base.GetWebRequest(address);
            result.Timeout = Timeout;
            return result;
        }
    }

}
