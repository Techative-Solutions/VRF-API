namespace VRF_API.Services
{
        public class SessionManager
        {
            private readonly IHttpContextAccessor _httpContextAccessor;

            public SessionManager(IHttpContextAccessor httpContextAccessor)
            {
                _httpContextAccessor = httpContextAccessor;
            }


            private ISession Session => _httpContextAccessor.HttpContext.Session;

            public void Set(string key, string value)
            {
                
                Session.SetString($"{key}", value);
            }

            public string Get(string key)
            {
               
                return Session.GetString($"{key}");
            }

            public void Remove(string key)
            {
                Session.Remove($"{key}");
            }
        }
}
