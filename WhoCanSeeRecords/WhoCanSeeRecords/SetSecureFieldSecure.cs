using Microsoft.Xrm.Sdk;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WhoCanSeeRecords
{
    class SetSecureFieldSecure : IPlugin
    {
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
            if (messageName.Equals("Create", StringComparison.InvariantCultureIgnoreCase))
            {
                // must register as grant user caller!!
                if (context.Depth <= 1 && stage == (int)eMessageStage.PreEvent)
                {
                    TableRelationation tableRelation = TableRelationation.GetSinglton();
                    Entity target = (Entity)context.InputParameters["Target"];

                    if (tableRelation.Entities.Contains(target.LogicalName.ToLower()))
                    {
                        if (target.Attributes.Contains(General.SecureField))

                            target.Attributes.Remove(General.SecureField);
                        target.Attributes.Add(General.SecureField, true);
                    }
                }
            }
        }
    }
}

