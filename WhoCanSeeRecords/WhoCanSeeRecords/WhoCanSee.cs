﻿using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace WhoCanSeeRecords
{
    public class WhoCanSee : IPlugin
    {

        const string Query = "Query";

        public void Execute(IServiceProvider serviceProvider)
        {
            if (serviceProvider == null)
                throw new ArgumentNullException("serviceProvider");
            ITracingService tracingService = (ITracingService)serviceProvider.GetService(typeof(ITracingService));
            IPluginExecutionContext context = (IPluginExecutionContext)serviceProvider.GetService(typeof(IPluginExecutionContext));
            IOrganizationServiceFactory serviceFactory = (IOrganizationServiceFactory)serviceProvider.GetService(typeof(IOrganizationServiceFactory));
            IOrganizationService service = serviceFactory.CreateOrganizationService(context.UserId);
            string messageName = context.MessageName;
            int stage = context.Stage;

            if (messageName.Equals("RetrieveMultiple", StringComparison.InvariantCultureIgnoreCase))
            {
                if (context.Depth <= 1 && stage == (int)eMessageStage.PreEvent)
                    GenerateQuery(context, service); // must register as grant user caller!!

                else if (context.Depth <= 1 && stage == (int)eMessageStage.PostEvent)
                    SecretSecureActivityPointer(context, service); //default caller user
            }

            if (messageName.Equals("Retrieve", StringComparison.InvariantCultureIgnoreCase))
            {
                if (context.Depth <= 1 && stage == (int)eMessageStage.PostEvent)
                    CanSeeCurrentRecord(context, service);//default caller user 
                //it's must be because in a Create event we set a caller to be grant user 
                //and the caller on create event must get parent entiity that's has a secure field
                //and after in a pipeline on retrive event he can get the record

                else if (context.Depth <= 1 && stage == (int)eMessageStage.PreEvent)
                    AddSecureFieldIfNotExists(context, service);// prefer register as grant user caller!!
                //(it's preffer to be a grant user becuase he get cache users from team or other resource data)

            }
        }

        public void SecretSecureActivityPointer(IPluginExecutionContext context, IOrganizationService service)
        {

            if (!context.OutputParameters.Contains("BusinessEntityCollection"))
                return;

            EntityCollection results = (EntityCollection)context.OutputParameters["BusinessEntityCollection"];
            if (results != null && results.Entities != null && results.Entities.Count > 0)
            {
                if (results.EntityName != TableRelationation.ACTIVITYPOINTER) return;

                ConfigCaching configCaching = GetCacheConfig(service);
                UsersTeam userteam = UsersTeam.GetSinglton(service, configCaching);

                if (userteam.UsersPremission.Contains(context.InitiatingUserId))
                    return;

                TableRelationation tableRelation = TableRelationation.GetSinglton();
                foreach (Entity entity in results.Entities)
                {
                    bool isSecure = false;
                    AliasedValue aliasValue = null;
                    foreach (var activityName in tableRelation.Activities)
                    {
                        var aliasName = TableRelationation.PERFIX_ALIAS + activityName + TableRelationation.DOT_ALIAS + General.SecureField;
                        if (entity.Attributes.Contains(aliasName))
                        {
                            aliasValue = entity.GetAttributeValue<AliasedValue>(aliasName);
                            isSecure = aliasValue != null && aliasValue.Value is bool ? (bool)aliasValue.Value : false;
                            break;
                        }
                    }

                    if (isSecure)
                    {
                        if (entity.Attributes.Contains("subject"))
                            entity.Attributes["subject"] = General.SecretField;

                        if (entity.Attributes.Contains("description"))
                            entity.Attributes["description"] = General.SecretField;

                        if (entity.GetAttributeValue<EntityReference>("regardingobjectid") != null)
                            ((EntityReference)entity["regardingobjectid"]).Name = General.SecretField;

                        if (entity.GetAttributeValue<DateTime?>("actualend") != null)
                            entity.Attributes["actualend"] = null;

                        if (entity.GetAttributeValue<DateTime?>("actualstart") != null)
                            entity.Attributes["actualstart"] = null;

                        if (entity.GetAttributeValue<EntityReference>("ownerid") != null)
                            entity["ownerid"] = null;
                    }
                    //   secureTemp.Add(e);
                    //results.Entities.Remove(e);
                }
                //foreach(var secure in secureTemp)
                //{
                //     results.Entities.Remove(secure);
                //}
            }
        }

        /// <summary>
        /// Pre RetrieveMultiple
        /// </summary>
        /// <param name="context"></param>
        /// <param name="service"></param>
        public void GenerateQuery(IPluginExecutionContext context, IOrganizationService service)
        {
            if (context.InputParameters.Contains(Query))
            {
                TableRelationation tableRelation = TableRelationation.GetSinglton();
                // Get the query
                if (context.InputParameters[Query] is QueryExpression)
                {
                    QueryExpression query = (QueryExpression)context.InputParameters[Query];
                    AppendQueryExpression(query, context, service, tableRelation);
                }

                else if (context.InputParameters[Query] is FetchExpression)
                {
                    try
                    {
                        FetchExpression fetchQuery = (FetchExpression)context.InputParameters[Query];
                        var query = fetchQuery.Query;
                        if (!FetchXmlForEntity(query, tableRelation))
                            return;

                        FetchXmlToQueryExpressionRequest request = new FetchXmlToQueryExpressionRequest { FetchXml = query };

                        var response = service.Execute(request) as FetchXmlToQueryExpressionResponse;
                        if (response != null && response.Query != null)
                            AppendQueryExpression(response.Query, context, service, tableRelation);
                    }
                    catch
                    {
                        // for aggrate reason can be exception or other things
                    }
                }
            }
        }

        /// <summary>
        /// parse xml to see if it's entity secure      for prevent excute        FetchXmlToQueryExpressionResponse
        /// </summary>
        /// <param name="query"></param>
        /// <param name="tableRelation"></param>
        /// <returns></returns>
        bool FetchXmlForEntity(string query, TableRelationation tableRelation)
        {
            // parse xml to see if it's entity secure 
            // for prevent excute    FetchXmlToQueryExpressionResponse
            XElement fetch = XElement.Parse(query);

            if (fetch != null && fetch.Element("entity") != null && fetch.Element("entity").Attribute("name") != null)
            {
                var entityname = fetch.Element("entity").Attribute("name").Value;
                if (entityname == TableRelationation.ACTIVITYPOINTER)
                    return true;

                if (tableRelation.Entities.Contains(entityname.ToLower()))
                    return true;
            }
            return false;
        }

        /// <summary>
        ///    AppendQueryExpression
        /// </summary>
        /// <param name="queryExpression"></param>
        /// <param name="context"></param>
        /// <param name="service"></param>
        /// <param name="tableRelation"></param>
        void AppendQueryExpression(QueryExpression queryExpression, IPluginExecutionContext context, IOrganizationService service, TableRelationation tableRelation)
        {
            ConfigCaching configCaching = GetCacheConfig(service);
            UsersTeam userteam = UsersTeam.GetSinglton(service, configCaching);

            //if (userteam.UsersPremission.Contains(context.InitiatingUserId))
            //    return;
            if (userteam.UsersPremission.Contains(context.InitiatingUserId))
            {
                if (queryExpression.EntityName.ToLower() == TableRelationation.INCIDENT)
                {
                    var filter = queryExpression.Criteria;
                    RemoveSecureFilter(filter);
                    return;
                }
                else
                    return;
            }
            if (queryExpression.EntityName == TableRelationation.ACTIVITYPOINTER)
            {
                AppendAllActivities(queryExpression, tableRelation);
                return;
            }

            if (tableRelation.Entities.Contains(queryExpression.EntityName.ToLower()))
            {
                var filter = queryExpression.Criteria;
                AppendFilter(filter);
            }
        }

        void AppendAllActivities(QueryExpression query, TableRelationation tableRelation)
        {

            if (query.LinkEntities == null)
                return;

            foreach (var activityName in tableRelation.Activities)
            {

                query.LinkEntities.Add(new LinkEntity
                     {
                         EntityAlias = TableRelationation.PERFIX_ALIAS + activityName,
                         JoinOperator = JoinOperator.LeftOuter,
                         LinkFromEntityName = TableRelationation.ACTIVITYPOINTER,
                         LinkFromAttributeName = TableRelationation.ACTIVITYID,
                         LinkToEntityName = activityName,
                         LinkToAttributeName = TableRelationation.ACTIVITYID,
                         Columns = new ColumnSet(General.SecureField)
                     });

            }
        }

        /// <summary>
        /// on Retrieve Multiple event
        /// </summary>
        /// <param name="filter"></param>
        void AppendFilter(FilterExpression filter)
        {
            if (filter == null)
                filter = new FilterExpression(LogicalOperator.And);

            bool hasConditionAlready = false;
            foreach (var condition in filter.Conditions)
            {
                if (condition.AttributeName == General.SecureField)
                {
                    if (condition.Values == null)
                        continue;

                    condition.Values.Clear();
                    condition.Operator = ConditionOperator.Equal;
                    condition.Values.Add(false);
                    hasConditionAlready = true;
                    break;
                }
            }

            if (!hasConditionAlready) filter.AddCondition(new ConditionExpression(General.SecureField, ConditionOperator.Equal, false));

        }

        /// <summary>
        /// on Pre Retrive
        /// </summary>
        /// <param name="context"></param>
        void AddSecureFieldIfNotExists(IPluginExecutionContext context, IOrganizationService service)
        {
            var primaryEntityName = context.PrimaryEntityName;
            if (!String.IsNullOrEmpty(primaryEntityName))
            {
                TableRelationation tableRelation = TableRelationation.GetSinglton();
                ConfigCaching configCaching = GetCacheConfig(service);
                // load userTeam if not loaded yet because is in grant user
                UsersTeam userteam = UsersTeam.GetSinglton(service, configCaching);
                if (tableRelation.Entities.Contains(primaryEntityName.ToLower()))
                {
                    if (context.InputParameters.Contains("ColumnSet"))
                    {
                        ColumnSet columns = context.InputParameters["ColumnSet"] as ColumnSet;
                        if (columns != null && !columns.AllColumns)
                        { // Validate if exists
                            if (!columns.Columns.Contains(General.SecureField))
                                columns.AddColumn(General.SecureField);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// on Post Retrive
        /// </summary>
        /// <param name="context"></param>
        void CanSeeCurrentRecord(IPluginExecutionContext context, IOrganizationService service)
        {
            Entity entity = context.OutputParameters["BusinessEntity"] as Entity;

            //must be a caller id (UserId) and not InitiatingUserId 
            // 1. the retrieve must be on caller id and not strong user (not register specific user on plugin
            // 2. and when we register  a Create Step on entity that's has secure field 
            //we must grant a strong user to be on a plugin  for get from parent entity the field secure and set him on current entity
            if (entity != null)
            {
                TableRelationation tableRelation = TableRelationation.GetSinglton();
                if (tableRelation.Entities.Contains(entity.LogicalName.ToLower()))
                {
                    ConfigCaching configCaching = GetCacheConfig(service);
                    UsersTeam userteam = UsersTeam.GetSinglton(service, configCaching);
                    if (userteam.UsersPremission.Contains(context.UserId))
                        return;

                    if (entity.Attributes.Contains(General.SecureField))
                    {
                        bool canSee = entity.GetAttributeValue<bool>(General.SecureField);
                        if (canSee == true)
                            throw new InvalidPluginExecutionException("אינך מורשה לראות את הרשומה");
                    }
                }
            }
        }
        
        /// <summary>
        /// retrivemultiple event not fire on choose =>"include: related "regarding" records" view  in account
        /// but in view when choosen "include:this record only" the retrivemultiple event is fired
        /// so we set is_secure = false on filter  (incident associated view ) and it's customize both senario.
        /// so what it's will be is when choosen "include: related "regarding" records" it's not going here (this event) and retrive only new_secure=false  from the customization
        /// but when  choosen "include:this record only" it's goes here (retrivemultiple event) and test
        /// if it's admin users it's remove this condition new_secure and the end user can see everything
        /// otherwise don't do nothing and the filter will be from the customization mean new_secure=false
        /// </summary>
        /// <param name="filter"></param>
        void RemoveSecureFilter(FilterExpression filter)
        {
            if (filter == null)
                filter = new FilterExpression(LogicalOperator.And);
       
            List<ConditionExpression> condtionsRemove = new List<ConditionExpression>();
            foreach (var condition in filter.Conditions)
            {
                if (condition.AttributeName == General.SecureField)
                {
                    condtionsRemove.Add(condition);
                    break;
                }
            }

            foreach (var condtionRemove in condtionsRemove)
              filter.Conditions.Remove(condtionRemove);
        }

        ConfigCaching GetCacheConfig(IOrganizationService service)
        {
            return new ConfigCaching();
        }

    }

}

