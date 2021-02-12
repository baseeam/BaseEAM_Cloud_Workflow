/*******************************************************
 * Copyright 2016 (C) BaseEAM Systems, Inc
 * All Rights Reserved
*******************************************************/
using BaseEAM.Core.Domain;
using log4net;
using System;
using System.Activities;
using System.Activities.DurableInstancing;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Runtime.DurableInstancing;
using System.Threading;
using System.Xml.Linq;

namespace BaseEAM.Workflows
{
    public static class WorkflowManager
    {
        static WorkflowManager()
        {
        }
        private static readonly ILog logger = LogManager.GetLogger(typeof(WorkflowManager));
        private static string ConnectionString { get { return ConfigurationManager.ConnectionStrings["BaseEAM.Workflow"].ConnectionString; } }

        private static WorkflowApplication CreateWorkflowApplication(System.Activities.Activity workflowDefinition, IDictionary<string, object> parameters = null)
        {
            WorkflowApplication wfApp = null;
            Dictionary<string, object> arguments = new Dictionary<string, object>();
            if (parameters != null)
            {
                foreach (var item in parameters)
                    arguments.Add(item.Key, item.Value);
                wfApp = new WorkflowApplication(workflowDefinition, arguments);
            }
            else
            {
                wfApp = new WorkflowApplication(workflowDefinition);
            }

            return wfApp;
        }

        private static InstanceHandle InitializeInstanceStore(Activity workflowDefinition, WorkflowApplication wfApp)
        {
            InstanceHandle ownerHandle;
            var store = CreateInstanceStore(workflowDefinition, out ownerHandle);
            Dictionary<XName, object> wfScope = new Dictionary<XName, object> { { GetWorkflowHostTypePropertyName(), GetWorkflowHostTypeName(workflowDefinition) } };

            wfApp.InstanceStore = store;

            wfApp.AddInitialInstanceValues(wfScope);

            return ownerHandle;
        }

        private static WorkflowApplication RegisterWorkflowApplicationEvents(InstanceHandle ownerHandle, WorkflowApplication wfApp, AutoResetEvent ars)
        {
            wfApp.PersistableIdle = a => { Debug.WriteLine("PersistableIdle " + wfApp.Id); DestroyInstanceOwner(wfApp, ownerHandle); return PersistableIdleAction.Unload; };
            wfApp.Unloaded = b => { Debug.WriteLine("Unloaded " + wfApp.Id); DestroyInstanceOwner(wfApp, ownerHandle); ars.Set(); };
            wfApp.Completed = c => { Debug.WriteLine("Completed " + wfApp.Id); OnWorkflowCompleted(c); DestroyInstanceOwner(wfApp, ownerHandle); ars.Set(); };
            wfApp.Aborted = d => { Debug.WriteLine("Aborted " + wfApp.Id); OnWorkflowAborted(d); DestroyInstanceOwner(wfApp, ownerHandle); ars.Set(); };
            wfApp.OnUnhandledException = e => { Debug.WriteLine("OnUnhandledException " + wfApp.Id); ars.Set(); return HandleError(e); };

            return wfApp;
        }

        public static WorkflowApplication StartWorkflow(System.Activities.Activity workflowDefinition, IDictionary<string, object> parameters = null)
        {
            var ars = new AutoResetEvent(false);
            WorkflowApplication wfApp = CreateWorkflowApplication(workflowDefinition, parameters);
            InstanceHandle ownerHandle = InitializeInstanceStore(workflowDefinition, wfApp);
            RegisterWorkflowApplicationEvents(ownerHandle, wfApp, ars);

            wfApp.Run();

            ars.WaitOne();

            return wfApp;
        }

        private static WorkflowApplication LoadWorkflowApplication(System.Activities.Activity workflowDefinition, Guid instanceId)
        {
            var wfApp = CreateWorkflowApplication(workflowDefinition);
            wfApp.Load(instanceId);
            return wfApp;
        }

        public static WorkflowApplication ResumeWorkflow(System.Activities.Activity workflowDefinition, Guid instanceId, string actionName, WorkflowVariable variable)
        {
            // Try to load the workflow and resume the bookmark
            var ars = new AutoResetEvent(false);
            WorkflowApplication wfApp = CreateWorkflowApplication(workflowDefinition);
            InstanceHandle ownerHandle = InitializeInstanceStore(workflowDefinition, wfApp);
            RegisterWorkflowApplicationEvents(ownerHandle, wfApp, ars);

            wfApp.Load(instanceId);

            // Resume the bookmark and Wait for the workflow to complete
            if (wfApp.ResumeBookmark("ActionExecuted", variable) != BookmarkResumptionResult.Success)
            {
                throw new InvalidOperationException("Unable to resume workflow with action name = " + actionName);
            }

            ars.WaitOne();

            return wfApp;
        }

        public static WorkflowApplication CancelWorkflow(System.Activities.Activity workflowDefinition, Guid instanceId)
        {
            // Try to load the workflow and cancel it.
            var ars = new AutoResetEvent(false);
            WorkflowApplication wfApp = CreateWorkflowApplication(workflowDefinition);
            InstanceHandle ownerHandle = InitializeInstanceStore(workflowDefinition, wfApp);
            RegisterWorkflowApplicationEvents(ownerHandle, wfApp, ars);

            wfApp.Load(instanceId);

            wfApp.Cancel();

            ars.WaitOne();

            return wfApp;
        }

        private static void OnWorkflowCompleted(WorkflowApplicationCompletedEventArgs args)
        {
            Debug.WriteLine("OnWorkflowCompleted");
        }

        private static void OnWorkflowAborted(WorkflowApplicationAbortedEventArgs args)
        {
            logger.Error(args.Reason);
            Debug.WriteLine("OnWorkflowAborted");
        }

        private static UnhandledExceptionAction HandleError(WorkflowApplicationUnhandledExceptionEventArgs args)
        {
            logger.Error(args.UnhandledException);
            return UnhandledExceptionAction.Abort;
        }

        private static void DestroyInstanceOwner(WorkflowApplication wfApp, InstanceHandle instanceHandle)
        {
            try
            {
                if (instanceHandle.IsValid)
                {
                    //WriteDebug("DestroyInstanceOwner: " + wfApp.Id);
                    var deleteOwnerCmd = new DeleteWorkflowOwnerCommand();
                    wfApp.InstanceStore.Execute(instanceHandle, deleteOwnerCmd, TimeSpan.FromSeconds(30));
                    wfApp.InstanceStore.DefaultInstanceOwner = null;
                }
                else
                {
                    Debug.WriteLine("DestroyInstanceOwner: HANDLE IS FREED: " + wfApp.Id);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("DestroyInstanceOwner", ex);
                throw ex;
            }
        }

        private static SqlWorkflowInstanceStore CreateInstanceStore(System.Activities.Activity workflow, out InstanceHandle ownerHandle)
        {
            try
            {
                var store = new SqlWorkflowInstanceStore(ConnectionString);
                ownerHandle = store.CreateInstanceHandle();

                var wfHostTypeName = GetWorkflowHostTypeName(workflow);
                var WorkflowHostTypePropertyName = GetWorkflowHostTypePropertyName();

                var ownerCommand = new CreateWorkflowOwnerCommand() { InstanceOwnerMetadata = { { WorkflowHostTypePropertyName, new InstanceValue(wfHostTypeName) } } };
                var owner = store.Execute(ownerHandle, ownerCommand, TimeSpan.FromSeconds(30)).InstanceOwner;
                ownerHandle.Free();
                store.DefaultInstanceOwner = owner;
                return store;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        private static XName GetWorkflowHostTypePropertyName()
        {
            return XNamespace.Get("urn:schemas-microsoft-com:System.Activities/4.0/properties").GetName("WorkflowHostType");
        }

        private static XName GetWorkflowHostTypeName(System.Activities.Activity workflowDefinition)
        {
            return XName.Get(workflowDefinition.GetType().FullName, "http://www.baseeam.com/2016/workflow");
        }
    }
}
