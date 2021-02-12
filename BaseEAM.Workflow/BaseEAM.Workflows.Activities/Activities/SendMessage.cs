/*******************************************************
 * Copyright 2016 (C) BaseEAM Systems, Inc
 * All Rights Reserved
*******************************************************/
using System.Activities;
using System.ComponentModel;
using BaseEAM.Core.Domain;
using BaseEAM.Workflows.Core.Infrastructure;
using BaseEAM.Core.Data;
using BaseEAM.Core;
using BaseEAM.Services;
using BaseEAM.Workflows.Activities.Design;

namespace BaseEAM.Workflows.Activities
{
    [Designer(typeof(SendMessageDesigner))]
    public sealed class SendMessage : CodeActivity
    {
        [RequiredArgument]
        [Category("Arguments")]
        [Description("The input data when starting a new workflow instance.")]
        public InArgument<WorkflowInput> WorkflowInput { get; set; }

        [RequiredArgument]
        [Category("Arguments")]
        [Description("The variable data when running a workflow instance.")]
        public InArgument<WorkflowVariable> WorkflowVariable { get; set; }

        [RequiredArgument]
        [Category("Users")]
        [DefaultValue("")]
        [Description("The list of users who will be received the message. It can be an AssignmentGroup, a list of emails or a C# method.")]
        public string Users { get; set; }

        [Category("Communication")]
        [DefaultValue("")]
        [Description("The message template name that is used to generate a message to send to users.")]
        public string MessageTemplate { get; set; }

        protected override void Execute(CodeActivityContext context)
        {
            var workflowInput = this.WorkflowInput.Get(context);
            var workflowVariable = this.WorkflowVariable.Get(context);
            //set the currentUser to ThreadStatic to be used later
            var userRepository = WorkflowServiceEngine.Instance.WorkflowContainerManager.Resolve<IRepository<User>>();
            var currentUser = userRepository.GetById(workflowVariable.CurrentUserId);
            WorkflowContext.CurrentUser = currentUser;

            // if Users is a C# method then we handle it here
            var result = ActivityHelper.GetUsers(Users, workflowInput.EntityId);
            if (!string.IsNullOrEmpty(result))
                Users = result;

            // send message
            var wfService = WorkflowServiceEngine.Instance.WorkflowContainerManager.Resolve<IWorkflowBaseService>();
            wfService.SendMessage(workflowVariable.EntityId,
                workflowVariable.EntityType,
                Users,
                this.MessageTemplate);
        }
    }
}
