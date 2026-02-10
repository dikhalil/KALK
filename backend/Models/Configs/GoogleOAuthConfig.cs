using System;

namespace Backend.Models.Configs
{
    public class GoogleOAuthWeb
    {
        public string client_id { get; set; }
        public string client_secret { get; set; }
        public string[] redirect_uris { get; set; }
        public string[] javascript_origins { get; set; }
        public string auth_uri { get; set; }
        public string token_uri { get; set; }
    }

    public class GoogleOAuthConfig
    {
        public GoogleOAuthWeb web { get; set; }
    }
}
