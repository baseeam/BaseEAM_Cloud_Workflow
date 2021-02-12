/*******************************************************
 * Copyright 2016 (C) BaseEAM Systems, Inc
 * All Rights Reserved
*******************************************************/
using BaseEAM.Core;
using BaseEAM.Core.Data;
using BaseEAM.Core.Domain;
using BaseEAM.Services;
using BaseEAM.Workflows.Activities.Design;
using BaseEAM.Workflows.Core.Infrastructure;
using System;
using System.Activities;
using System.ComponentModel;

namespace BaseEAM.Workflows.Activities
{
    [Designer(typeof(ApproveTaskDesigner))]
    public sealed class ApproveTask : CodeActivity
    {
        [RequiredArgument]
        [Category("Task Arguments")]
        [Description("The input data when starting a new workflow instance.")]
        public InArgument<WorkflowInput> WorkflowInput { get; set; }

        [RequiredArgument]
        [Category("Task Arguments")]
        [Description("The variable data when running a workflow instance.")]
        public InArgument<WorkflowVariable> WorkflowVariable { get; set; }

        [RequiredArgument]
        [Category("Task Information")]
        [DefaultValue("")]
        [Description("The name of this task activity.")]
        public string Name { get; set; }

        [Category("Task Timing")]
        [DefaultValue(null)]
        [Description("The datetime that this task is expected to start and will appear in the assigned users' Inbox/Assignments.")]
        public InArgument<DateTime?> ExpectedStartDateTime { get; set; }

        [Category("Task Timing")]
        [DefaultValue(null)]
        [Description("The datetime that this assignment is overdue.")]
        public InArgument<DateTime?> DueDateTime { get; set; }

        [RequiredArgument]
        [Category("Task Users")]
        [DefaultValue("")]
        [Description("The list of users who will be assigned to this task. It can be an AssignmentGroup, a list of emails or a C# method.")]
        public string Users { get; set; }

        [Category("Task Users")]
        [DefaultValue(false)]
        [Description("A flag indicate that whether WF will assign tasks to all users.")]
        public InArgument<bool> AssignAll { get; set; }

        [Category("Communication")]
        [DefaultValue("")]
        [Description("The message template name that is used to generate a message to send to users.")]
        public string MessageTemplate { get; set; }

        protected override void Execute(CodeActivityContext context)
        {
            var workflowInput = this.WorkflowInput.Get(context);
            var workflowVariable = this.WorkflowVariable.Get(context);
            //in case of start workflow, variable workflowVariable will be null
            if (workflowVariable == null)
                workflowVariable = new WorkflowVariable
                {
                    CurrentUserId = workflowInput.CreatedUserId,
                    CurrentAction = "",
                    CurrentComment = "",
                    EntityId = workflowInput.EntityId,
                    EntityType = workflowInput.EntityType,
                    WorkflowDefinitionId = workflowInput.WorkflowDefinitionId
                };

            //set the currentUser to ThreadStatic to be used later
            var userRepository = WorkflowServiceEngine.Instance.WorkflowContainerManager.Resolve<IRepository<User>>();
            var currentUser = userRepository.GetById(workflowVariable.CurrentUserId);
            WorkflowContext.CurrentUser = currentUser;

            //create assignment
            var expectedDateTime = this.ExpectedStartDateTime.Get(context);
            var dueDateTime = this.DueDateTime.Get(context);

            // if Users is a C# method then we handle it here
            var result = ActivityHelper.GetUsers(Users, workflowInput.EntityId);
            if (!string.IsNullOrEmpty(result))
                Users = result;

            // create assignment
            var wfService = WorkflowServiceEngine.Instance.WorkflowContainerManager.Resolve<IWorkflowBaseService>();
            wfService.CreateApproveAssignment(workflowVariable.EntityId,
                workflowVariable.EntityType,
                currentUser,
                context.WorkflowInstanceId.ToString(),
                workflowVariable.WorkflowDefinitionId,
                Name,
                expectedDateTime,
                dueDateTime,
                Users,
                workflowVariable.CurrentAction,
                workflowVariable.CurrentComment,
                this.MessageTemplate
            );
        }
    }
}
