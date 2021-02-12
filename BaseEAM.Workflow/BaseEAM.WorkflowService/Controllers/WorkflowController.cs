/*******************************************************
 * Copyright 2016 (C) BaseEAM Systems, Inc
 * All Rights Reserved
*******************************************************/
using BaseEAM.Core.Data;
using BaseEAM.Core.Domain;
using BaseEAM.Data;
using BaseEAM.Workflows;
using BaseEAM.Workflows.Core.Infrastructure;
using BaseEAM.Workflows.Inventory;
using log4net;
using System;
using System.Activities;
using System.Activities.XamlIntegration;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using System.Web.Http;
using System.Xaml;

namespace BaseEAM.WorkflowService.Controllers
{
    public class WorkflowController : ApiController
    {
        private static readonly ILog logger = LogManager.GetLogger(typeof(WorkflowController));
        private readonly DapperContext _dapperContext;
        public WorkflowController()
        {
            _dapperContext = new DapperContext(ConfigurationManager.ConnectionStrings["BaseEAM"].ConnectionString);
        }

        [Route("api/workflow/startworkflow")]
        [HttpPost]
        public string StartWorkflow([FromBody]WorkflowServiceParameter param)
        {
            try
            {
                logger.InfoFormat("Starting Workflow: EntityId = {0}, EntityType = {1}, CurrentUserId = {2}, WorkflowDefinitionId = {3}, WorkflowVersion = {4}", 
                    param.EntityId, param.EntityType, param.CurrentUserId, param.WorkflowDefinitionId, param.WorkflowVersion);

                //new workflow definition from workflow repository
                var workflowDefinition = GetWorkflowDefinition(param);

                WorkflowApplication wfApp = null;
                Dictionary<string, object> inputs = new Dictionary<string, object>();
                var workflowInput = new WorkflowInput
                {
                    CreatedUserId = param.CurrentUserId,
                    EntityId = param.EntityId,
                    EntityType = param.EntityType,
                    WorkflowDefinitionId = param.WorkflowDefinitionId
                };
                inputs.Add("WorkflowInput", workflowInput);
                wfApp = CustomWorkflowManager.StartWorkflow(workflowDefinition, inputs);

                logger.InfoFormat("Started workflow: EntityId = {0}, EntityType = {1}, WorkflowInstanceId = {2}, CurrentUserId = {3}, WorkflowDefinitionId = {4}",
                    param.EntityId, param.EntityType, wfApp.Id.ToString(), param.CurrentUserId, param.WorkflowDefinitionId);

                return wfApp.Id.ToString();
            }
            catch (Exception ex)
            {
                //log exception
                logger.ErrorFormat("StartWorkflow: {0}", ex.Message);
                throw this.ExceptionInternalServerError(ex);
            }
        }

        [Route("api/workflow/triggerworkflowaction")]
        [HttpPost]
        public string TriggerWorkflowAction([FromBody]WorkflowServiceParameter param)
        {
            try
            {
                logger.InfoFormat("Triggering workflow action: EntityId = {0}, EntityType = {1}, WorkflowInstanceId = {2}, ActionName = {3}, CurrentUserId = {4}, Comment = {5}, WorkflowVersion = {6}, WorkflowDefinitionId = {7}",
                    param.EntityId, param.EntityType, param.WorkflowInstanceId, param.ActionName, param.CurrentUserId, param.Comment, param.WorkflowVersion, param.WorkflowDefinitionId);

                //new workflow definition from workflow repository
                var workflowDefinition = GetWorkflowDefinition(param);

                WorkflowApplication wfApp = null;
                var variable = new WorkflowVariable
                {
                    EntityId = param.EntityId,
                    EntityType = param.EntityType,
                    WorkflowDefinitionId = param.WorkflowDefinitionId,
                    CurrentUserId = param.CurrentUserId,
                    CurrentAction = param.ActionName,
                    CurrentComment = param.Comment
                };
                wfApp = CustomWorkflowManager.ResumeWorkflow(workflowDefinition, new Guid(param.WorkflowInstanceId), param.ActionName, variable);

                logger.InfoFormat("Triggered workflow action: EntityId = {0}, EntityType = {1}, WorkflowInstanceId = {2}, ActionName = {3}, CurrentUserId = {4}, Comment = {5}, WorkflowVersion = {6}, WorkflowDefinitionId = {7}",
                    param.EntityId, param.EntityType, param.WorkflowInstanceId, param.ActionName, param.CurrentUserId, param.Comment, param.WorkflowVersion, param.WorkflowDefinitionId);

                return wfApp.Id.ToString();
            }
            catch (Exception ex)
            {
                //log exception
                logger.ErrorFormat("TriggerWorkflowAction: {0}", ex.Message);
                throw this.ExceptionInternalServerError(ex);
            }
        }

        [Route("api/workflow/cancelworkflow")]
        [HttpPost]
        public string CancelWorkflow([FromBody]WorkflowServiceParameter param)
        {
            try
            {
                logger.InfoFormat("Cancel workflow: EntityId = {0}, EntityType = {1}, WorkflowInstanceId = {2}, CurrentUserId = {3}, WorkflowVersion = {4}, WorkflowDefinitionId = {5}",
                    param.EntityId, param.EntityType, param.WorkflowInstanceId, param.CurrentUserId, param.WorkflowVersion, param.WorkflowDefinitionId);

                //new workflow definition from workflow repository
                var workflowDefinition = GetWorkflowDefinition(param);

                WorkflowApplication wfApp = null;
                wfApp = CustomWorkflowManager.CancelWorkflow(workflowDefinition, new Guid(param.WorkflowInstanceId));

                logger.InfoFormat("Cancelled workflow: EntityId = {0}, EntityType = {1}, WorkflowInstanceId = {2}, CurrentUserId = {3}, WorkflowVersion = {4}, WorkflowDefinitionId = {5}",
                    param.EntityId, param.EntityType, param.WorkflowInstanceId, param.CurrentUserId, param.WorkflowVersion, param.WorkflowDefinitionId);

                return wfApp.Id.ToString();
            }
            catch (Exception ex)
            {
                //log exception
                logger.ErrorFormat("CancelWorkflow: {0}", ex.Message);
                throw this.ExceptionInternalServerError(ex);
            }
        }

        private Activity GetWorkflowDefinition(WorkflowServiceParameter param)
        {
            var wfDefVersionRepository = WorkflowServiceEngine.Instance.WorkflowContainerManager.Resolve<IRepository<WorkflowDefinitionVersion>>();
            var wfDefVersion = new WorkflowDefinitionVersion();
            string xaml = "";
            //if starting workflow
            if(param.WorkflowDefinitionId == 0)
            {
                //get the latest version of the default wf def
                wfDefVersion = wfDefVersionRepository.GetAll()
                    .Where(w => w.WorkflowDefinition.EntityType == param.EntityType && w.WorkflowDefinition.IsDefault == true)
                    .OrderByDescending(w => w.WorkflowVersion)
                    .First();

                param.WorkflowDefinitionId = wfDefVersion.WorkflowDefinitionId.Value;
            }
            else if (param.WorkflowVersion == 0)
            {
                //get the latest version of the specified wf def
                wfDefVersion = wfDefVersionRepository.GetAll()
                    .Where(w => w.WorkflowDefinition.EntityType == param.EntityType && w.WorkflowDefinition.Id == param.WorkflowDefinitionId)
                    .OrderByDescending(w => w.WorkflowVersion)
                    .First();
            }
            else
            {
                //get the specified version of the specified wf def
                wfDefVersion = wfDefVersionRepository.GetAll()
                    .Where(w => w.WorkflowDefinition.EntityType == param.EntityType && w.WorkflowDefinition.Id == param.WorkflowDefinitionId
                                    && w.WorkflowVersion == param.WorkflowVersion)
                    .OrderByDescending(w => w.WorkflowVersion)
                    .First();
            }

            xaml = wfDefVersion.WorkflowXamlFileContent;

            string _byteOrderMarkUtf8 = Encoding.UTF8.GetString(Encoding.UTF8.GetPreamble());
            if (xaml.StartsWith(_byteOrderMarkUtf8))
            {
                var lastIndexOfUtf8 = _byteOrderMarkUtf8.Length - 1;
                xaml = xaml.Remove(0, lastIndexOfUtf8);
            }

            //Load workflow definition from xaml
            ActivityXamlServicesSettings settings = new ActivityXamlServicesSettings
            {
                CompileExpressions = true
            };
            XamlXmlReaderSettings xamlReaderSettings = new XamlXmlReaderSettings { LocalAssembly = typeof(ReceiptWorkflow).Assembly };
            var stream = new MemoryStream(Encoding.UTF8.GetBytes(xaml));
            XamlReader xamlReader = new XamlXmlReader(stream, xamlReaderSettings);
            var activity = ActivityXamlServices.Load(xamlReader, settings);
            return activity;
        }

        [Route("api/workflow/test")]
        [HttpGet]
        public string Test()
        {
            return "Hello world!";
        }
    }
}
