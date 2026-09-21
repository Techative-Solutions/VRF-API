using System.Data.Odbc;
using VRF_API.Repository;

namespace VRF_API.Services
{

    public interface IUserService
    { 
    
    }
        public class UserService : IUserService
    {

        private readonly IConfiguration _configuration;
        private readonly OdbcConnection _connection;
        private readonly string _webDbName;
        private readonly string _anotherDbName;


        public UserService(IConfiguration configuration, OdbcConnection connection )
        {
            _configuration = configuration;
            _connection = connection;
            _webDbName = _configuration.GetValue<string>("DBName:WebDbName") ?? "WebDbName";
      
        }
    }
}
