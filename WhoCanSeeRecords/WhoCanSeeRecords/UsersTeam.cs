using Microsoft.Xrm.Sdk;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WhoCanSeeRecords
{
    public class UsersTeam
    {
        static UsersTeam _usersTeam = null;
        static object _lock = new object();
        List<Guid> _usersPremission = null;

        public List<Guid> UsersPremission
        {
            get
            {
                return _usersPremission;
            }
        }

        protected UsersTeam()
        {
            Init();
        }

        void Init()
        {
            if (_usersPremission == null)
            {
                _usersPremission = new List<Guid>();     //users that's on specific team!!!!
                _usersPremission.Add(new Guid("{2CDE69CD-DA28-5555-7777-4022507DD859}"));    //SOME USER    can see
                _usersPremission.Add(new Guid("{2CDE69CD-DA28-4444-8888-4022507DD859}"));    //   SOME USER
                //{2CDE69CD-DA28-2222-6666-4022507DD859} user who can not see
            }
        }

        public static UsersTeam GetSinglton(IOrganizationService service, ConfigCaching config)
        {
            if (_usersTeam == null)
            {
                lock (_lock)
                {
                    if (_usersTeam == null)
                        _usersTeam = new UsersTeam();
                }
            }
            return _usersTeam;
        }
    }
}
