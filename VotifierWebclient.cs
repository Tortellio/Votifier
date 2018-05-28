using System;
using System.Net;

namespace fr34kyn01535.Votifier
{
    public class VotifierWebclient : WebClient
    {
        //time in milliseconds
        public int Timeout { get; private set; }

        public VotifierWebclient(int timeout = 5000)
        {
            Timeout = timeout;
        }

        protected override WebRequest GetWebRequest(Uri address)
        {
            var result = base.GetWebRequest(address);
            if (result != null)
                result.Timeout = Timeout;
            return result;
        }
    }
}
