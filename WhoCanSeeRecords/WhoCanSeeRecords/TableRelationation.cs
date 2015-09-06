using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WhoCanSeeRecords
{
    public class TableRelationation
    {
        public const string INCIDENT = "incident";
        public const string ACTIVITYPOINTER = "activitypointer";
        public const string ACTIVITYID = "activityid";
        static TableRelationation _tableRelationation = null;
        public const string PERFIX_ALIAS = "gu";
        public const string DOT_ALIAS = ".";
        static object _lock = new object();

        List<string> _entitiesActivities = null;
        List<string> _entities = null;

        protected TableRelationation()
        {
            Init();
        }

        public List<string> Entities
        {
            get
            {
                return _entities;
            }
        }

        public List<string> Activities
        {
            get
            {
                return _entitiesActivities;
            }
        }

        void Init()
        {
            _entities = new List<string>();
            _entities.Add("new_testsecure");
            _entities.Add("appointment");
            _entities.Add("fax");
            _entities.Add("letter");
            _entities.Add("task");
            _entities.Add("email");
            _entities.Add("phonecall");
            _entities.Add("serviceappointment");
            _entities.Add("new_action_document");
            _entities.Add("new_doc_approve_activity");

            _entitiesActivities = new List<string>();
            _entitiesActivities.Add("appointment");
            _entitiesActivities.Add("fax");
            _entitiesActivities.Add("letter");
            _entitiesActivities.Add("task");
            _entitiesActivities.Add("email");
            _entitiesActivities.Add("phonecall");
            _entitiesActivities.Add("serviceappointment");
            _entitiesActivities.Add("new_action_document");
            _entitiesActivities.Add("new_doc_approve_activity");
        }

        public static TableRelationation GetSinglton()
        {
            if (_tableRelationation == null)
            {
                lock (_lock)
                {
                    if (_tableRelationation == null)
                    {
                        _tableRelationation = new TableRelationation();
                    }
                }
            }
            return _tableRelationation;
        }


    }
}

