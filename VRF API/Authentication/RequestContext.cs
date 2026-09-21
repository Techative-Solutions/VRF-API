namespace VRF_API.Authentication
{
    public interface IRequestContext
    {
        long UserID { get; }
        bool IsAdmin { get; }
    }
    public class RequestContext : IRequestContext
    {
        // private readonly IHttpContextAccessor _contextAccessor;
        public RequestContext(IHttpContextAccessor httpContextAccessor)
        {
            var userIdClaim = httpContextAccessor.HttpContext.User.Claims.FirstOrDefault(x => x.Type == "UserID");
            var adminClaim = httpContextAccessor.HttpContext.User.Claims.FirstOrDefault(x => x.Type == "IsAdmin");
            this.UserID = userIdClaim != null ? Convert.ToInt64(userIdClaim.Value) : 0;
            this.IsAdmin = adminClaim != null ? Convert.ToBoolean(adminClaim.Value) : false;
        }

        public long UserID { get; }
        public bool IsAdmin { get; }
    }
}
